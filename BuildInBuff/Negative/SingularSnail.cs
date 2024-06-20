using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using RWCustom;
using System.Collections.Generic;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using BuiltinBuffs.Negative;

namespace HotDogBuff
{
    class SingularSnailBuff : Buff<SingularSnailBuff, SingularSnailBuffData> { public override BuffID ID => SingularSnailBuffEntry.SingularSnailID; }
    class SingularSnailBuffData : BuffData { public override BuffID ID => SingularSnailBuffEntry.SingularSnailID; }
    class SingularSnailBuffEntry : IBuffEntry
    {
        public static BuffID SingularSnailID = new BuffID("SingularSnailID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SingularSnailBuff, SingularSnailBuffData, SingularSnailBuffEntry>(SingularSnailID);
        }
        public static void HookOn()
        {
            On.Snail.Click += Snail_Click;
            On.Snail.MiniClick += Snail_MiniClick;
        }

        private static void Snail_MiniClick(On.Snail.orig_MiniClick orig, Snail self)
        {
            orig.Invoke(self);
            Suck(self);
        }

        private static void Snail_Click(On.Snail.orig_Click orig, Snail self)
        {
            orig.Invoke(self);
            Explode(self);

        }
        public static void Explode(Snail self)
        {
            //SingularSnailBuff.Instance.TriggerSelf(true);
            Debug.Log("SINGULARITYSNAIL EXPLODE");
            Vector2 vector = Vector2.Lerp(self.firstChunk.pos, self.firstChunk.lastPos, 0.35f);
            self.room.AddObject(new MoreSlugcats.SingularityBomb.SparkFlash(self.firstChunk.pos, 300f, new Color(0f, 0f, 1f)));
            self.room.AddObject(new RandomBuffUtils.DamageOnlyExplosion(self.room, self, vector, 7, 450f, 6.2f, 10f, 280f, 0.25f, self, 0.3f, 160f, 1f, CreatureTemplate.Type.Snail));
            self.room.AddObject(new RandomBuffUtils.DamageOnlyExplosion(self.room, self, vector, 7, 2000f, 4f, 0f, 400f, 0.25f, self, 0.3f, 200f, 1f, CreatureTemplate.Type.Snail));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, self.ShortCutColor()));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            self.room.AddObject(new Explosion.ExplosionLight(vector, 2000f, 2f, 60, self.ShortCutColor()));
            self.room.AddObject(new ShockWave(vector, 350f, 0.485f, 80, true));
            self.room.AddObject(new ShockWave(vector, 2000f, 0.185f, 40, false));
            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (self.room.GetTile(vector + a * 20f).Solid)
                {
                    if (!self.room.GetTile(vector - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    self.room.AddObject(new Spark(vector + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(self.ShortCutColor(), new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                }
                self.room.AddObject(new Explosion.FlashingSmoke(vector + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), self.ShortCutColor(), Random.Range(3, 11)));
            }
            for (int k = 0; k < 6; k++)
            {
                self.room.AddObject(new MoreSlugcats.SingularityBomb.BombFragment(vector, Custom.DegToVec(((float)k + Random.value) / 6f * 360f) * Mathf.Lerp(18f, 38f, Random.value)));
            }
            self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 0.9f);
            for (int l = 0; l < self.abstractPhysicalObject.stuckObjects.Count; l++)
            {
                self.abstractPhysicalObject.stuckObjects[l].Deactivate();
            }
            self.room.PlaySound(SoundID.Bomb_Explode, vector);
            self.room.InGameNoise(new Noise.InGameNoise(vector, 9000f, self, 1f));
            for (int m = 0; m < self.room.physicalObjects.Length; m++)
            {
                for (int n = 0; n < self.room.physicalObjects[m].Count; n++)
                {
                    if (self.room.physicalObjects[m][n] is Creature && Custom.Dist(self.room.physicalObjects[m][n].firstChunk.pos, self.firstChunk.pos) < 350f)
                    {
                        if ((self.room.physicalObjects[m][n] is Snail)) continue;
                        if (self != null)
                        {
                            (self.room.physicalObjects[m][n] as Creature).killTag = self.abstractCreature;
                        }
                        (self.room.physicalObjects[m][n] as Creature).Die();
                    }
                    if (self.room.physicalObjects[m][n] is MoreSlugcats.ElectricSpear)
                    {
                        if ((self.room.physicalObjects[m][n] as MoreSlugcats.ElectricSpear).abstractSpear.electricCharge == 0)
                        {
                            (self.room.physicalObjects[m][n] as MoreSlugcats.ElectricSpear).Recharge();
                        }
                        else
                        {
                            (self.room.physicalObjects[m][n] as MoreSlugcats.ElectricSpear).ExplosiveShortCircuit();
                        }
                    }
                }
            }
            self.room.InGameNoise(new Noise.InGameNoise(self.firstChunk.pos, 1200f, self, 1f));
        }

        public static void Suck(Snail self)
        {
            self.firstChunk.vel = new Vector2(0f, 0f);
            for (int n = 0; n < self.room.physicalObjects.Length; n++)
            {
                for (int num = 0; num < self.room.physicalObjects[n].Count; num++)
                {
                    for (int num2 = 0; num2 < self.room.physicalObjects[n][num].bodyChunks.Length; num2++)
                    {
                        BodyChunk bodyChunk2 = self.room.physicalObjects[n][num].bodyChunks[num2];
                        if (Vector2.Distance(self.firstChunk.pos, bodyChunk2.pos) < 350f && bodyChunk2 != self.firstChunk)
                        {
                            if (bodyChunk2.owner is Snail) continue;
                            bodyChunk2.vel += (self.firstChunk.pos - bodyChunk2.pos) * bodyChunk2.mass * self.clickCounter;
                            if (bodyChunk2.vel.magnitude > 50f)
                            {
                                bodyChunk2.vel = bodyChunk2.vel.normalized * 50f;
                            }
                        }
                    }
                }
            }
            self.room.AddObject(new Explosion.ExplosionLight(self.firstChunk.pos, 2 * 5f, 1f, 1, self.ShortCutColor()));
            self.room.AddObject(new ShockWave(self.firstChunk.pos, 3f * self.clickCounter * 10f, 0.45f, 1, false));
        }
    }

}
