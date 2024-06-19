using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace BuiltinBuffs.Duality
{
    internal class CentiCrackerBuff : Buff<CentiCrackerBuff, CentiCrackerBuffData>
    {
        public override BuffID ID => CentiCrackerBuffEntry.CentiCracker;
    }

    class CentiCrackerBuffData : BuffData
    {
        public override BuffID ID => CentiCrackerBuffEntry.CentiCracker;
    }

    class CentiCrackerBuffEntry : IBuffEntry
    {
        public static BuffID CentiCracker = new BuffID("CentiCracker", true);
        public static ConditionalWeakTable<AbstractCreature, CrackerModule> crackers = new ConditionalWeakTable<AbstractCreature, CrackerModule> { };
        public static Color explodeColor = new Color(1f, 0.4f, 0.3f);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CentiCrackerBuff, CentiCrackerBuffData, CentiCrackerBuffEntry>(CentiCracker);
        }

        public static void HookOn()
        {
            On.Centipede.Violence += Centipede_Violence;
            On.Centipede.Update += Centipede_Update;
            On.Player.ctor += Player_ctor;
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            self.surfaceFriction = 0f;
            self.airFriction = 0f;
        }

        private static void Centipede_Violence(On.Centipede.orig_Violence orig, Centipede self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (!crackers.TryGetValue(self.abstractCreature, out var value))
            {
                crackers.Add(self.abstractCreature, new CrackerModule());
            }

            try
            {
                if (self.CentiState.shells[hitChunk.index])
                {
                    if (crackers.TryGetValue(self.abstractCreature, out var crackerData))
                    {
                        if (self.room != null)
                        {
                            self.room.AddObject(new RandomBuffUtils.DamageOnlyExplosion(self.room, self, hitChunk.pos, 8, 40f * self.size, 20f * self.size, 5f * self.size, 
                                100f * self.size, 0, self, 1f, 0f, 0f, CreatureTemplate.Type.Centipede));
                            self.room.AddObject(new ShockWave(hitChunk.pos, 40f * self.size + 20f, 0.5f, 8, true));
                            self.room.AddObject(new Explosion.ExplosionLight(hitChunk.pos, 40f * self.size + 20f, 0.8f, 8, Color.white));
                            self.room.AddObject(new Explosion.FlashingSmoke(hitChunk.pos, Custom.RNV() * 3f * UnityEngine.Random.value, 1f, Color.white, explodeColor, 11));
                            self.room.PlaySound(SoundID.Bomb_Explode, hitChunk, false, 0.8f * self.size + 0.2f, 1.2f);
                        }
                        crackerData.crackChunk.Add(new IntVector2(hitChunk.index - 1, hitChunk.index + 1));
                    }
                }
            }
            catch(Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }           
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
                      
        }

        private static void Centipede_Update(On.Centipede.orig_Update orig, Centipede self, bool eu)
        {
            orig(self, eu);
            try
            {
                if (crackers.TryGetValue(self.abstractCreature, out var crackerData))
                {
                    crackerData.crackingCooldown--;
                    for (int i = 0; i < crackerData.crackChunk.Count; i++)
                    {
                        if (crackerData.crackingCooldown <= 0)
                        {
                            if (crackerData.crackChunk[i].x >= 0)
                            {
                                if (self.CentiState.shells[crackerData.crackChunk[i].x])
                                {
                                    if (self.room != null)
                                    {
                                        self.room.AddObject(new RandomBuffUtils.DamageOnlyExplosion(self.room, self, self.bodyChunks[crackerData.crackChunk[i].x].pos, 8, 40f * self.size, 20f * self.size, 5f * self.size,
                                            100f * self.size, 0, self, 1f, 0f, 0f, CreatureTemplate.Type.Centipede));
                                        self.shellJustFellOff = crackerData.crackChunk[i].x;
                                        self.CentiState.shells[crackerData.crackChunk[i].x] = false;
                                        if (self.graphicsModule != null)
                                        {
                                            for (int j = 0; j < (self.Red ? 3 : 1); j++)
                                            {
                                                CentipedeShell centipedeShell = new CentipedeShell(self.bodyChunks[crackerData.crackChunk[i].x].pos, Custom.RNV() * UnityEngine.Random.value * ((j == 0) ? 3f : 6f),
                                                    (self.graphicsModule as CentipedeGraphics).hue, (self.graphicsModule as CentipedeGraphics).saturation, self.bodyChunks[crackerData.crackChunk[i].x].rad * 1.8f * 0.0714285746f * 1.2f,
                                                    self.bodyChunks[crackerData.crackChunk[i].x].rad * 1.3f * 0.09090909f * 1.2f);
                                                if (self.abstractCreature.IsVoided())
                                                {
                                                    centipedeShell.lavaImmune = true;
                                                }
                                                self.room.AddObject(centipedeShell);
                                                self.room.AddObject(new ShockWave(self.bodyChunks[crackerData.crackChunk[i].x].pos, 40f * self.size + 20f, 0.5f, 8, true));
                                                self.room.AddObject(new Explosion.ExplosionLight(self.bodyChunks[crackerData.crackChunk[i].x].pos, 40f * self.size + 20f, 0.8f, 8, Color.white));
                                                self.room.AddObject(new Explosion.FlashingSmoke(self.bodyChunks[crackerData.crackChunk[i].x].pos, Custom.RNV() * 3f * UnityEngine.Random.value, 1f, Color.white, explodeColor, 11));
                                                self.room.PlaySound(SoundID.Bomb_Explode, self.bodyChunks[crackerData.crackChunk[i].x], false, 0.8f * self.size + 0.2f, 1.2f);
                                            }
                                        }

                                    }
                                }
                                
                            }
                            if (crackerData.crackChunk[i].y < self.bodyChunks.Length)
                            {
                                if (self.CentiState.shells[crackerData.crackChunk[i].y])
                                {
                                    if (self.room != null)
                                    {
                                        self.room.AddObject(new RandomBuffUtils.DamageOnlyExplosion(self.room, self, self.bodyChunks[crackerData.crackChunk[i].y].pos, 8, 40f * self.size, 20f * self.size, 5f * self.size,
                                            100f * self.size, 0, self, 1f, 0f, 0f, CreatureTemplate.Type.Centipede));
                                        self.shellJustFellOff = crackerData.crackChunk[i].y;
                                        self.CentiState.shells[crackerData.crackChunk[i].y] = false;
                                        if (self.graphicsModule != null)
                                        {
                                            for (int j = 0; j < (self.Red ? 3 : 1); j++)
                                            {
                                                CentipedeShell centipedeShell = new CentipedeShell(self.bodyChunks[crackerData.crackChunk[i].y].pos, Custom.RNV() * UnityEngine.Random.value * ((j == 0) ? 3f : 6f),
                                                    (self.graphicsModule as CentipedeGraphics).hue, (self.graphicsModule as CentipedeGraphics).saturation, self.bodyChunks[crackerData.crackChunk[i].y].rad * 1.8f * 0.0714285746f * 1.2f,
                                                    self.bodyChunks[crackerData.crackChunk[i].y].rad * 1.3f * 0.09090909f * 1.2f);
                                                if (self.abstractCreature.IsVoided())
                                                {
                                                    centipedeShell.lavaImmune = true;
                                                }
                                                self.room.AddObject(centipedeShell);
                                                self.room.AddObject(new ShockWave(self.bodyChunks[crackerData.crackChunk[i].y].pos, 40f * self.size + 20f, 0.5f, 8, true));
                                                self.room.AddObject(new Explosion.ExplosionLight(self.bodyChunks[crackerData.crackChunk[i].y].pos, 40f * self.size + 20f, 0.8f, 8, Color.white));
                                                self.room.AddObject(new Explosion.FlashingSmoke(self.bodyChunks[crackerData.crackChunk[i].y].pos, Custom.RNV() * 3f * UnityEngine.Random.value, 1f, Color.white, explodeColor, 11));
                                                self.room.PlaySound(SoundID.Bomb_Explode, self.bodyChunks[crackerData.crackChunk[i].y], false, 0.8f * self.size + 0.2f, 1.2f);
                                            }
                                        }

                                    }
                                }

                            }
                            crackerData.crackChunk[i] += new IntVector2(-1, 1);
                            crackerData.crackingCooldown = 10;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }
    }

    public class CrackerModule
    {
        public bool cracked;
        public int crackingCooldown = 10;
        public List<IntVector2> crackChunk;
        
        public CrackerModule() 
        { 
            crackChunk = new List<IntVector2>();
        }
    }
}
