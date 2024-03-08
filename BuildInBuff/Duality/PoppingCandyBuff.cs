using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{


    internal class PoppingCandyIBuffEntry : IBuffEntry
    {
        public static BuffID PoppingCandyBuffID = new BuffID("PoppingCandy", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PoppingCandyIBuffEntry>(PoppingCandyBuffID);
        }

        public static void HookOn()
        {
            On.Player.ObjectEaten += Player_ObjectEaten;
        }

        private static void Player_ObjectEaten(On.Player.orig_ObjectEaten orig, Player self, IPlayerEdible edible)
        {
            orig.Invoke(self, edible);
            if(edible is OracleSwarmer)
            {
                self.room.AddObject(new CreaturePopping(self, 80));
            }
        }
    }

    public class CreaturePopping : UpdatableAndDeletable
    {
        Creature crit;
        int counter;

        public CreaturePopping(Creature crit, int duration)
        {
            this.crit = crit;
            counter = duration;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter--;
            if (counter < 1)
            {
                Destroy();
                return;
            }
            Vector2 vector = Custom.RNV();
            for (int i = 0; i < crit.bodyChunks.Length; i++)
            {
                vector = Vector3.Slerp(-vector.normalized, Custom.RNV(), Random.value);
                vector *= Mathf.Min(3f, Random.value * 3f / Mathf.Lerp(crit.bodyChunks[i].mass, 1f, 0.5f)) * Mathf.InverseLerp(0f, 80f, (float)counter) * 2f;
                crit.bodyChunks[i].pos += vector;
                crit.bodyChunks[i].vel += vector * 0.5f;
            }
            if (crit.graphicsModule != null && crit.graphicsModule.bodyParts != null)
            {
                for (int j = 0; j < crit.graphicsModule.bodyParts.Length; j++)
                {
                    vector = Vector3.Slerp(-vector.normalized, Custom.RNV(), Random.value);
                    vector *= Random.value * 2f * Mathf.InverseLerp(0f, 120f, (float)counter) * 2f;
                    crit.graphicsModule.bodyParts[j].pos += vector;
                    crit.graphicsModule.bodyParts[j].vel += vector;
                    if (crit.graphicsModule.bodyParts[j] is Limb)
                    {
                        (crit.graphicsModule.bodyParts[j] as Limb).mode = Limb.Mode.Dangle;
                    }
                }
            }
        }
    }
}
