using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class OriBashBuff : Buff<OriBashBuff,OriBashBuffData>
    {
        public override BuffID ID => OriBashBuffEntry.OriBash;

        public override bool Triggerable => false;

        public OriBashBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var bash = new Bash(player);
                    bash.InitiateSprites(player.graphicsModule as PlayerGraphics, game.cameras[0].spriteLeasers.
                        First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                    OriBashBuffEntry.BashFeatures.Add(player,bash);
                }
            }
        }

        public override void Destroy()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.Players.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    OriBashBuffEntry.BashFeatures.Remove(player);
                }
            }
        }

    }
        
    internal class OriBashBuffData : KeyBindBuffData
    {
        public override BuffID ID => OriBashBuffEntry.OriBash;

    }

    internal class OriBashBuffEntry : IBuffEntry
    {
        public static readonly BuffID OriBash = new BuffID("OriBash", true);

        public static readonly SoundID BashEnd = new SoundID(nameof(BashEnd), true);
        public void OnEnable()
        {
           
            BuffRegister.RegisterBuff<OriBashBuff,OriBashBuffData,OriBashBuffEntry>(OriBash);
        }

        public static void HookOn()
        {
            IL.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;

            On.Player.Update += Player_Update;
            On.Player.ctor += Player_ctor;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
        }


        public static void LoadAssets()
        {
            BuffSounds.LoadSound(BashEnd,OriBash.GetStaticData().AssetPath,new BuffSoundGroupData(),new BuffSoundData("bash"));
        }
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!BashFeatures.TryGetValue(self, out _) && !self.isNPC)
                BashFeatures.Add(self, new Bash(self));
        }
  

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (BashFeatures.TryGetValue(self.player, out var bash))
                bash.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (BashFeatures.TryGetValue(self.player, out var bash))
            {
                bash.InitiateSprites(self,sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self,sLeaser,rCam, newContatiner);
            if (BashFeatures.TryGetValue(self.player, out var bash))
                bash.AddToContainer(sLeaser, rCam);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (BashFeatures.TryGetValue(self, out var bash))
                bash.Update();
        }


        private static void RainWorldGame_RawUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After, i => i.MatchLdfld<MainLoopProcess>("framesPerSecond"),
                                              i => i.MatchStfld<MainLoopProcess>("framesPerSecond")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<RainWorldGame>>(game =>
                {
                    if (UpdateSpeed < game.framesPerSecond)
                        game.framesPerSecond = UpdateSpeed;
                });
            }
            else
                BuffUtils.LogError(OriBash,"IL HOOK FAILED");
        }

        public static int UpdateSpeed = 1000;


        public static ConditionalWeakTable<Player, Bash> BashFeatures = new ConditionalWeakTable<Player, Bash>();


    }

    public class Bash
    {
        PhysicalObject bashTarget;
        int outroTimer = 0;
        bool isBash = false;
        int startSprite = int.MaxValue;

        int bashTimer = 0;
        bool isPressUse;

        private Vector2 dir;


        public static FAtlas arrow;

        WeakReference<Player> ownerRef;

        public Bash(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void InitiateSprites(PlayerGraphics self,RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            startSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, sLeaser.sprites.Length + 2);
            if(arrow == null)
                arrow = Futile.atlasManager.LoadImage("buffassets/cardinfos/positive/oribash/arrow");
            sLeaser.sprites[startSprite] = new FSprite(arrow.name);
            sLeaser.sprites[startSprite].scale = 0.75f;
            sLeaser.sprites[startSprite].scaleY *= 0.7f;
            sLeaser.sprites[startSprite + 1] = new FSprite("Futile_White", true);
            sLeaser.sprites[startSprite + 1].shader = rCam.room.game.rainWorld.Shaders["GravityDisruptor"];
            sLeaser.sprites[startSprite + 1].scale = 15;
            sLeaser.sprites[startSprite + 1].alpha = 0f;
            self.AddToContainer(sLeaser,rCam,null);
            
           
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (startSprite < sLeaser.sprites.Length)
            {
                rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[startSprite]);
                rCam.ReturnFContainer("Bloom").AddChild(sLeaser.sprites[startSprite + 1]);
            }
        }


        public Vector2 GetInputDirection(Player player,Vector2 sourcePos)
        {
            if (BuffPlayerData.Instance.GetKeyBind(OriBashBuffEntry.OriBash) == KeyCode.None.ToString())
                return Custom.DirVec(sourcePos,
                    new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            return RWInput.PlayerInput(player.playerState.playerNumber).analogueDir;
        }


        public bool GetInput(Player player)
        {
            if (OriBashBuff.Instance.Data[player.playerState.playerNumber] != KeyCode.None)
                return Input.GetKey(OriBashBuff.Instance.Data[player.playerState.playerNumber]);
            if(BuffPlayerData.Instance.GetKeyBind(OriBashBuffEntry.OriBash) == KeyCode.None.ToString())
                return Input.GetMouseButton(1);
            return BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(OriBashBuffEntry.OriBash));
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;

            if (isBash)
            {
                var center = bashTarget ?? self;
                dir = Vector3.Slerp(dir, GetInputDirection(self,sLeaser.sprites[startSprite].GetPosition()),0.02f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite].x = Mathf.Lerp(center.firstChunk.lastPos.x, center.firstChunk.pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[startSprite].y = Mathf.Lerp(center.firstChunk.lastPos.y, center.firstChunk.pos.y, timeStacker) - camPos.y;
                sLeaser.sprites[startSprite].scaleX = Mathf.Lerp(sLeaser.sprites[startSprite].scale,0.75f, 0.02f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite].rotation = Custom.VecToDeg(Vector2.Perpendicular(dir));
                sLeaser.sprites[startSprite + 1].scale = Mathf.Lerp(sLeaser.sprites[startSprite + 1].scale, 15, 0.02f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite + 1].SetPosition(sLeaser.sprites[startSprite].GetPosition());
                sLeaser.sprites[startSprite + 1].color = Color.white;
                sLeaser.sprites[startSprite + 1].alpha = 1;
                
                if (!GetInput(self) || bashTarget == null || !self.Consious)
                {
                    isBash = false;
                    OriBashBuffEntry.UpdateSpeed = 1000;
                    foreach (var chunk in self.bodyChunks)
                        chunk.vel = dir * 20;

                    if (bashTarget != null)
                    {
                        foreach (var chunk in bashTarget.bodyChunks)
                            chunk.vel = dir * 15 * -1.25f;
                        var minChunk = bashTarget.bodyChunks.OrderBy(i => Custom.Dist(i.pos, self.mainBodyChunk.pos)).First();
                        var centerPos = Vector2.Lerp(self.mainBodyChunk.pos, minChunk.pos,0.5f);
                        ParticleEmitter emitter = new ParticleEmitter(self.room)
                            { pos = centerPos, lastPos = centerPos };

                        if (bashTarget is Weapon weapon)
                        {
                            weapon.ChangeMode(Weapon.Mode.Free);
                            weapon.SetRandomSpin();
                        }

                        emitter.ApplyEmitterModule(new SetEmitterLife(emitter,5,false));

                        emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, Random.Range(3, 6)));

                        emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                        emitter.ApplyParticleModule(new SetRandomPos(emitter,20));
                        emitter.ApplyParticleModule(new SetRandomLife(emitter, 15, 45));
                        emitter.ApplyParticleModule(new SetRandomVelocity(emitter,Custom.DegToVec(Custom.VecToDeg(-dir) + 20)*10, Custom.DegToVec(Custom.VecToDeg(-dir) - 20)*25));
                        emitter.ApplyParticleModule(new SetConstColor(emitter,   (self.ShortCutColor()*2).CloneWithNewAlpha(1)));
                        emitter.ApplyParticleModule(new SetRandomScale(emitter,1,4));
                        emitter.ApplyParticleModule(new AddElement(emitter,
                            new Particle.SpriteInitParam("Circle20", "", 8, 1, 0.05f)));
                        emitter.ApplyParticleModule(new AddElement(emitter,
                            new Particle.SpriteInitParam("Futile_White", "LightSource", 8, 0.2f, 3)));
                        emitter.ApplyParticleModule(new AddElement(emitter,
                            new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.2f, 1f)));
                        emitter.ApplyParticleModule(new SetOriginalAlpha(emitter,0));
                        emitter.ApplyParticleModule(new AlphaOverLife(emitter, (particle, f) =>
                        {
                            particle.vel *= 0.89f;
                            return Mathf.InverseLerp(0, 0.05f, f) * Mathf.Pow(Mathf.InverseLerp(1, 0.35f, f), 1.5f);
                        }));
                        self.room.AddObject(new Explosion.ExplosionLight(centerPos,450,0.5f,15,self.ShortCutColor()));
                        ParticleSystem.ApplyEmitterAndInit(emitter);
                        bashTarget = null;
                        outroTimer = 10;
                        self.room.PlaySound(OriBashBuffEntry.BashEnd,self.mainBodyChunk.pos,0.15f,1);
                    }

                }
            }
            else if (outroTimer >= 0)
            {
                var num = Mathf.InverseLerp(0, 10, outroTimer);
                sLeaser.sprites[startSprite + 1].scale = Mathf.Pow(num, 0.5f) * 15;
                sLeaser.sprites[startSprite + 1].alpha = Mathf.Pow(num, 0.5f);
                sLeaser.sprites[startSprite + 1].color = Color.Lerp(Color.black, Color.white, Mathf.Pow(num, 0.5f));
                sLeaser.sprites[startSprite].scaleX = Mathf.Pow(Mathf.InverseLerp(3, 10, outroTimer), 0.5f) * 0.75f;
            }
            else
            {
                sLeaser.sprites[startSprite + 1].alpha = 0;
                sLeaser.sprites[startSprite].scaleX = 0;
                sLeaser.sprites[startSprite + 1].scale = 0;
            }

            
            sLeaser.sprites[startSprite].isVisible = sLeaser.sprites[startSprite].scaleX > 0.01f;
            
        }

 


        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (!self.Consious || self.grabbedBy.Any())
            {   
                if (isBash)
                {
                    isBash = false;
                    OriBashBuffEntry.UpdateSpeed = 1000;
                    outroTimer = 10;
                }

                return;
            }
            if (outroTimer > 0)
            {
                outroTimer--;
            }

            if (isBash)
            {
                bashTimer++;
                if (bashTimer == 4)
                {
                    isBash = false;
                    OriBashBuffEntry.UpdateSpeed = 1000;
                    bashTarget = null;
                }
            }

            if (GetInput(self) && !isPressUse)
            { 
                dir = self.input[0].analogueDir;
                bashTimer = 0;
                isPressUse = true;
                if (self.room.abstractRoom != null)
                {

                    if (OriBashBuff.Instance.Data.StackLayer >= 2)
                    {
                        foreach (var obj in self.room.updateList.OfType<PhysicalObject>())
                        {
                            if(obj.grabbedBy.Any(i => i.grabber == self) || self.slugOnBack?.slugcat == obj || self.spearOnBack?.spear == obj)
                                continue;
                            if (obj is Creature crit && crit.Template.type == CreatureTemplate.Type.Slugcat)
                                continue;

                            bool can = false;
                            foreach (var chunk in obj.bodyChunks)
                                if (IsDistLess(self.mainBodyChunk.pos, chunk, 75))
                                {
                                    can = true;
                                    break;
                                }
                            if (can)
                            {
                                bashTarget = obj;
                                OriBashBuffEntry.UpdateSpeed = 2;
                                isBash = true;
                                self.pyroParryCooldown = 0;
                                self.canJump = 15;
                                self.pyroJumpped = false;
                                break;
                            }

                        }
                    }
                    else
                    {
                        foreach (var creature in self.room.abstractRoom.creatures)
                        {
                            if (creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                                continue;
                            if (creature.realizedCreature != null && !creature.realizedCreature.inShortcut)
                            {
                                if (creature.realizedCreature.grabbedBy.Any(i => i.grabber == self) || self.slugOnBack?.slugcat == creature.realizedCreature)
                                    continue;
                                bool can = false;
                                foreach (var chunk in creature.realizedCreature.bodyChunks)
                                    if (IsDistLess(self.mainBodyChunk.pos,chunk, 75))
                                    {
                                        can = true;
                                        break;
                                    }
                                if (can)
                                {
                                    bashTarget = creature.realizedCreature;
                                    OriBashBuffEntry.UpdateSpeed = 2;
                                    isBash = true;
                                    self.pyroParryCooldown = 0;
                                    self.canJump = 15;
                                    self.pyroJumpped = false;
                                    break;
                                }
                            }
                        }
                    }
                    
                }

            }
            else if (!GetInput(self))
            {
                isPressUse = false;
            }

            bool IsDistLess(Vector2 centerPos ,BodyChunk checkChunk,float dest, int delay = 5)
            {
                for (int i = 0; i < delay; i++)
                    if (Custom.DistLess(centerPos, checkChunk.vel * i + checkChunk.pos, dest))
                        return true;
                return false;
            }
        }
    }
}
