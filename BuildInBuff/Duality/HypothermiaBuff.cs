using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class Hypothermia : Buff<Hypothermia, HypothermiaData>
    {
        public override BuffID ID => HypothermiaIBuffEntry.HypothermiaID;
    }

    internal class HypothermiaData : BuffData
    {
        public override BuffID ID => HypothermiaIBuffEntry.HypothermiaID;
    }

    internal class HypothermiaIBuffEntry : IBuffEntry
    {
        public static BuffID HypothermiaID = new BuffID("Hypothermia", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<Hypothermia, HypothermiaData, HypothermiaIBuffEntry>(HypothermiaID);
        }

        public static void HookOn()
        {

            On.AbstractCreature.Update += AbstractCreature_Update;
            On.Creature.HypothermiaUpdate += Creature_HypothermiaUpdate;

        }

        private static void Creature_HypothermiaUpdate(On.Creature.orig_HypothermiaUpdate orig, Creature self)
        {
            orig.Invoke(self);

            self.Hypothermia = 0f;
            if (self.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Overseer)
            {
                self.HypothermiaExposure = 0f;
                self.Hypothermia = 0f;
                return;
            }
            if (ModManager.MSC && self.Submersion > 0f)
            {
                foreach (IProvideWarmth blizzardHeatSource in self.room.blizzardHeatSources)
                {
                    float num = Vector2.Distance(self.firstChunk.pos, blizzardHeatSource.Position());
                    if (self.abstractCreature.Hypothermia > 0.001f && blizzardHeatSource.loadedRoom == self.room && num < blizzardHeatSource.range)
                    {
                        float num2 = Mathf.InverseLerp(blizzardHeatSource.range, blizzardHeatSource.range * 0.2f, num);
                        self.abstractCreature.Hypothermia -= Mathf.Lerp(blizzardHeatSource.warmth * num2, 0f, self.HypothermiaExposure);
                        if (self.abstractCreature.Hypothermia < 0f)
                        {
                            self.abstractCreature.Hypothermia = 0f;
                        }
                    }
                }
                if (!self.dead)
                {
                    self.Hypothermia = Mathf.Lerp(0f, RainWorldGame.DefaultHeatSourceWarmth * 0.1f, Mathf.InverseLerp(0.1f, 0.95f, self.room.world.rainCycle.CycleProgression));
                    if (!self.abstractCreature.HypothermiaImmune)
                    {
                        float num3 = (float)self.room.world.rainCycle.cycleLength + (float)RainWorldGame.BlizzardHardEndTimer(self.room.game.IsStorySession);
                        self.Hypothermia += Mathf.Lerp(0f, RainWorldGame.BlizzardMaxColdness, Mathf.InverseLerp(0f, num3, (float)self.room.world.rainCycle.timer));
                        self.Hypothermia += Mathf.Lerp(0f, 50f, Mathf.InverseLerp(num3, num3 * 5f, (float)self.room.world.rainCycle.timer));
                    }
                    self.Hypothermia += Mathf.Lerp(9100f, 5350f, Mathf.InverseLerp(0f, (float)self.room.world.rainCycle.cycleLength + 4300f, (float)self.room.world.rainCycle.timer)) * self.Submersion;
                    self.HypothermiaExposure = self.Submersion;
                    self.Hypothermia += self.Submersion / 7000f;
                    self.Hypothermia *= Mathf.InverseLerp(50f, -10f, self.TotalMass);
                }
                else
                {
                    self.HypothermiaExposure = 1f;
                    self.Hypothermia = Mathf.Lerp(0f, 4E-05f, Mathf.InverseLerp(0.8f, 1f, self.room.world.rainCycle.CycleProgression));
                    self.Hypothermia += self.Submersion / 6000f;
                    self.Hypothermia += Mathf.InverseLerp(50f, -10f, self.TotalMass) / 1000f;
                }
                if (self.Hypothermia > 1.5f)
                {
                    self.Hypothermia *= 2.3f;
                }
                else if (self.Hypothermia > 0.8f)
                {
                    self.Hypothermia *= 0.5f;
                }
                if (self.abstractCreature.HypothermiaImmune)
                {
                    self.Hypothermia /= 80f;
                }
                self.Hypothermia = Mathf.Clamp(self.Hypothermia, -1f, 0.0055f);
                self.Hypothermia += self.Hypothermia;
                if (self.Hypothermia >= 0.8f && self.Consious && self.room != null && !self.room.abstractRoom.shelter)
                {
                    if (self.Hypothermia > 0.0003f)
                    {
                        if (self.HypothermiaStunDelayCounter < 0)
                        {
                            int st = (int)Mathf.Lerp(5f, 60f, Mathf.Pow(self.Hypothermia / 2f, 8f));
                            self.HypothermiaStunDelayCounter = (int)Random.Range(300f - self.Hypothermia * 120f, 500f - self.Hypothermia * 100f);
                            self.Stun(st);
                        }
                    }
                    else
                    {
                        self.HypothermiaStunDelayCounter = Random.Range(200, 500);
                    }
                }
                if (self.Hypothermia >= 1f && (float)self.stun > 50f && !self.dead)
                {
                    self.Die();
                    return;
                }
            }
            else
            {
                if (self.Hypothermia > 2f)
                {
                    self.Hypothermia = 2f;
                }
                self.Hypothermia = Mathf.Lerp(self.Hypothermia, 0f, 0.001f);
                self.HypothermiaExposure = 0f;
            }
            if (self.room != null && !self.room.abstractRoom.shelter)
            {
                self.HypothermiaStunDelayCounter--;
            }
        }

        private static void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig.Invoke(self, time);
            if (self.realizedCreature != null)
            {
                if (self.InDen || self.HypothermiaImmune)
                {
                    self.Hypothermia = Mathf.Lerp(self.Hypothermia, 0f, 0.04f);
                    return;
                }
            }
        }
    }
}
