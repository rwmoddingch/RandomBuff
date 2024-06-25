using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using BuiltinBuffs.Duality;
using MoreSlugcats;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System.Diagnostics;

namespace BuiltinBuffs.Positive
{
    internal class WallNutBowlingBuff : Buff<WallNutBowlingBuff, WallNutBowlingBuffData>
    {
        public override BuffID ID => WallNutBowlingBuffEntry.WallNutBowling;
        public WallNutBowlingBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var nut = new WallNutBowling(player);
                    WallNutBowlingBuffEntry.WallNutBowlingFeatures.Add(player, nut);
                }
            }
        }
    }

    internal class WallNutBowlingBuffData : BuffData
    {
        public override BuffID ID => WallNutBowlingBuffEntry.WallNutBowling;
    }

    internal class WallNutBowlingBuffEntry : IBuffEntry
    {
        public static BuffID WallNutBowling = new BuffID("WallNutBowling", true);

        public static ConditionalWeakTable<Player, WallNutBowling> WallNutBowlingFeatures = new ConditionalWeakTable<Player, WallNutBowling>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<WallNutBowlingBuff, WallNutBowlingBuffData, WallNutBowlingBuffEntry>(WallNutBowling);
        }

        public static void HookOn()
        {
            IL.Player.Collide += Player_CollideIL;

            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
        }
        private static void Player_CollideIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                ILCursor find = new ILCursor(il);
                ILLabel pos = null;
                //找到胖猫翻滚结束的地方
                if (find.TryGotoNext(MoveType.After,
                    (i) => i.Match(OpCodes.Ldarg_0),
                    (i) => i.Match(OpCodes.Ldc_I4_S),
                    (i) => i.MatchStfld<Player>("gourmandAttackNegateTime"),
                    (i) => i.Match(OpCodes.Br)))
                {
                    pos = find.MarkLabel();
                    //BuffPlugin.Log("Player_CollideIL Find Pos to MarkLabel!");
                }

                if (c.TryGotoNext(MoveType.After,
                    (i) => i.Match(OpCodes.Br),
                    (i) => i.MatchLdsfld<ModManager>("MSC"),
                    (i) => i.Match(OpCodes.Brfalse)))
                    //插入到胖猫的翻滚前
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldarg_1);
                    c.EmitDelegate<Func<Player, PhysicalObject, bool>> ((self, otherObject) =>
                    {
                        //BuffPlugin.Log("Player_CollideIL MatchFind!");
                        if (otherObject is Creature && pos != null)
                        {
                            if (self.animation == Player.AnimationIndex.Roll)
                            {
                                bool flag4 = otherObject is Player && !Custom.rainWorld.options.friendlyFire;
                                if (!(otherObject as Creature).dead &&
                                    (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC &&
                                    (!ModManager.CoopAvailable || !flag4))
                                {
                                    Custom.Log(new string[]
                                    {
                                        "SLUGROLLED! stun: 120 damage: 1"
                                    });
                                    self.room.ScreenMovement(new Vector2?(self.bodyChunks[0].pos), self.mainBodyChunk.vel * self.bodyChunks[0].mass * 5f * 0.1f, Mathf.Max((self.bodyChunks[0].mass - 30f) / 50f, 0f));
                                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                                    (otherObject as Creature).SetKillTag(self.abstractCreature);
                                    (otherObject as Creature).Violence(self.mainBodyChunk, new Vector2?(new Vector2(self.mainBodyChunk.vel.x * 5f, self.mainBodyChunk.vel.y)), 
                                                                       otherObject.firstChunk, null, Creature.DamageType.Blunt, 1f, 120f);

                                    if (self.input[0].x == 0)
                                    {
                                        self.animation = Player.AnimationIndex.None;
                                        self.mainBodyChunk.vel.Scale(new Vector2(-0.5f, -0.5f));
                                        self.rollDirection = 0;
                                    }

                                    if (((otherObject as Creature).State is HealthState && ((otherObject as Creature).State as HealthState).ClampedHealth == 0f) || (otherObject as Creature).State.dead)
                                    {
                                        self.room.PlaySound(SoundID.Spear_Stick_In_Creature, self.mainBodyChunk, false, 1.7f, 1f);
                                    }
                                    else
                                    {
                                        self.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Terrain, self.mainBodyChunk, false, 1.2f, 1f);
                                    }
                                }
                                return true;
                            }
                        }
                        return false;
                    });

                    c.Emit(OpCodes.Brtrue, pos);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!WallNutBowlingFeatures.TryGetValue(self, out _))
                WallNutBowlingFeatures.Add(self, new WallNutBowling(self));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (WallNutBowlingFeatures.TryGetValue(self, out var wallNutBowling) &&
                self.animation == Player.AnimationIndex.Roll && self.input[0].x != 0)
            {
                self.rollCounter = 0;
                self.stopRollingCounter = 0;
                wallNutBowling.EmitterUpdate();
            }
        }
    }

    internal class WallNutBowling
    {
        WeakReference<Player> ownerRef;

        private Color color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
        private HSLColor hsvColor = new HSLColor(37f / 359f, 60f / 100f, 89f / 100f);
        private int counter;

        public WallNutBowling(Player player)
        {
            ownerRef = new WeakReference<Player>(player);
        }

        public void EmitterUpdate()
        {
            if (!ownerRef.TryGetTarget(out var self) || self.room == null)
                return;
            if (counter > 0)
                counter--;
            if (counter == 0)
            {
                counter = 40;
                var emitter = new ParticleEmitter(self.room);
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 60, false));
                emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, self));

                emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 100, 20));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "")));
                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 0.15f)));
                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 5, 10));
                emitter.ApplyParticleModule(new SetRandomVelocity(emitter, 15 * (-self.input[0].x) * Vector2.right, (15 * (-self.input[0].x) + 2f) * Vector2.right));
                emitter.ApplyParticleModule(new SetRandomColor(emitter, hsvColor.hue - 0.05f, hsvColor.hue + 0.05f, hsvColor.saturation, hsvColor.lightness));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(8f, 2f), new Vector2(26f, 2f)));
                emitter.ApplyParticleModule(new SetRandomRotation(emitter, Custom.VecToDeg((-self.input[0].x) * Vector2.right) - 90f, Custom.VecToDeg((-self.input[0].x) * Vector2.right) - 90f));
                emitter.ApplyParticleModule(new SetRandomPos(emitter, 20f));


                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, a) =>
                {
                    return p.setScaleXY * (1f - a);
                }));

                emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, a) =>
                {
                    Color color = Color.Lerp(Color.white, p.setColor, a);
                    color.a = 1f - a;
                    return color;
                }));

                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }
    }
}
