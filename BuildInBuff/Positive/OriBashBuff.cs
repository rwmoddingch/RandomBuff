using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

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
                    bash.InitiateSprites(game.cameras[0].spriteLeasers.
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

    internal class OriBashBuffData : BuffData
    {
        public override BuffID ID => OriBashBuffEntry.OriBash;
    }

    internal class OriBashBuffEntry : IBuffEntry
    {
        public static BuffID OriBash = new BuffID("OriBash", true);
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
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
        }
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!BashFeatures.TryGetValue(self, out _))
                BashFeatures.Add(self, new Bash(self));
        }
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (BashFeatures.TryGetValue(self.player, out var bash))
                bash.ApplyPalette(sLeaser, rCam, palette);
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
                bash.InitiateSprites(sLeaser, rCam);
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
        Creature bashTarget;
        int outroTimer = 0;
        bool isBash = false;
        int startSprite = 0;

        int bashTimer = 0;
        bool isPressUse;

        private Vector2 dir;


        public static FAtlas arrow;

        WeakReference<Player> ownerRef;

        public Bash(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
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


            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[startSprite]);
            rCam.ReturnFContainer("HUD").AddChild(sLeaser.sprites[startSprite + 1]);
        }

        public Vector2 GetInputDirection(Player player,Vector2 sourcePos)
        {
            if (BuffPlayerData.Instance.GetKeyBind(OriBashBuffEntry.OriBash) == KeyCode.None.ToString())
                return Custom.DirVec(sourcePos,
                    new Vector2(Input.mousePosition.x, Input.mousePosition.y));
            return RWInput.PlayerInput(player.playerState.playerNumber).analogueDir;
        }


        public bool GetInput()
        {
            return Input.GetMouseButton(1) ||
                   BuffInput.GetKey(BuffPlayerData.Instance.GetKeyBind(OriBashBuffEntry.OriBash));
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            Player self;
            if (!ownerRef.TryGetTarget(out self))
                return;

            if (isBash)
            {
                
                dir = Vector3.Slerp(dir, GetInputDirection(self,sLeaser.sprites[startSprite].GetPosition()),0.02f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite].x = Mathf.Lerp(self.mainBodyChunk.lastPos.x, self.mainBodyChunk.pos.x, timeStacker) - camPos.x;
                sLeaser.sprites[startSprite].y = Mathf.Lerp(self.mainBodyChunk.lastPos.y, self.mainBodyChunk.pos.y, timeStacker) - camPos.y;
                sLeaser.sprites[startSprite].scaleX = Mathf.Lerp(sLeaser.sprites[startSprite].scale,0.75f, 0.02f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite].rotation = Custom.VecToDeg(Vector2.Perpendicular(dir));
                sLeaser.sprites[startSprite + 1].scale = Mathf.Lerp(sLeaser.sprites[startSprite + 1].scale, 15, 0.03f / (rCam.game.paused ? 1 : BuffCustom.TimeSpeed));
                sLeaser.sprites[startSprite + 1].SetPosition(sLeaser.sprites[startSprite].GetPosition());
                sLeaser.sprites[startSprite + 1].color = Color.white;
                sLeaser.sprites[startSprite + 1].alpha = 1;

                if (!GetInput() || bashTarget == null || !self.Consious)
                {
                    isBash = false;
                    OriBashBuffEntry.UpdateSpeed = 1000;
                    foreach (var chunk in self.bodyChunks)
                        chunk.vel = dir * 20;

                    if (bashTarget != null)
                    {
                        foreach (var chunk in bashTarget.bodyChunks)
                            chunk.vel = dir * 15 * -1.25f;
                        bashTarget = null;
                        outroTimer = 10;
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

            
            sLeaser.sprites[startSprite].isVisible = sLeaser.sprites[startSprite].scaleX > 0.01f;
            
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[startSprite].color = Color.white;
        }


        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var self))
                return;
            if (!self.Consious) return;
            if (outroTimer > 0)
            {
                self.mushroomEffect = 0.1f;
                outroTimer--;
            }

            if (isBash)
            {
                bashTimer++;
                self.mushroomEffect = 0.1f;
                if (bashTimer == 4)
                {
                    isBash = false;
                    OriBashBuffEntry.UpdateSpeed = 1000;
                }
            }

            if (GetInput() && !isPressUse)
            { 
                dir = self.input[0].analogueDir;
                bashTimer = 0;
                isPressUse = true;
                if (self.room.abstractRoom != null)
                {
                    foreach (var creature in self.room.abstractRoom.creatures)
                    {
                        if (creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                            continue;
                        if (creature.realizedCreature != null)
                        {
                            bool can = false;
                            foreach (var chunk in creature.realizedCreature.bodyChunks)
                                if (Custom.DistLess(self.mainBodyChunk.pos, chunk.pos, 75))
                                {
                                    can = true;
                                    break;
                                }
                            if (can)
                            {
                                bashTarget = creature.realizedCreature;
                                OriBashBuffEntry.UpdateSpeed = 2;
                                isBash = true;
                                break;
                            }
                        }
                    }
                }

            }
            else if (!Input.GetMouseButton(1))
            {
                isPressUse = false;
            }
        }
    }
}
