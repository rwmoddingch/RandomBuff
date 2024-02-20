using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class ThundThrowBuff : Buff<ThundThrowBuff, ThundThrowBuffData>
    {
        public override BuffID ID => DozerBuffEntry.dozerBuffID;
    }

    internal class ThundThrowBuffData : BuffData
    {
        public override BuffID ID => DozerBuffEntry.dozerBuffID;
    }

    internal class ThundThrowBuffEntry : IBuffEntry
    {
        public static BuffID thundTHrowBuffID = new BuffID("ThundThrow", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ThundThrowBuff, ThundThrowBuffData, ThundThrowBuffEntry>(thundTHrowBuffID);
        }

        public static void HookOn()
        {
            On.Weapon.ChangeMode += Weapon_ChangeMode;
        }

        private static void Weapon_ChangeMode(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            if (!(self is Rock rock))
            {
                orig.Invoke(self, newMode);
                return;
            }

            if(self.mode == Weapon.Mode.Thrown && newMode == Weapon.Mode.Free)
            {
                self.room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, self.firstChunk.pos, 2f, 1.4f - Random.value * 0.4f);
                self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.4f - Random.value * 0.4f);

                LightningBolt lightningBolt;
                
            }
            orig.Invoke(self, newMode);
        }
    }

    internal class LightningGenenrator : UpdatableAndDeletable
    {
        LightningBolt lightning;
        public LightningGenenrator(Vector2 pos, Room room)
        {
            this.room = room;
            room.AddObject(lightning = new LightningBolt(pos + Vector2.up * 900f, pos, 1, 4f, 0.25f));
        }
    }
}
