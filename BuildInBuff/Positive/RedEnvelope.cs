using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HotDogGains.Positive
{
    //红包
    class RedEnvelopeBuff : Buff<RedEnvelopeBuff, RedEnvelopeBuffData>
    {
        public override BuffID ID => RedEnvelopeBuffEntry.RedEnvelopeID;
        public override bool Trigger(RainWorldGame game)
        {
            if (game.AlivePlayers.FirstOrDefault() != null && !game.AlivePlayers[0].realizedCreature.inShortcut &&
                game.AlivePlayers[0].realizedCreature.room != null)
            {
                var player = game.AlivePlayers[0].realizedCreature as Player;
                for (int i = 0; i < Random.value*20; i++)
                {
                    var pearl = new DataPearl.AbstractDataPearl(
                     game.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                     player.room.GetWorldCoordinate(player.DangerPos), game.GetNewID(), -1, -1, null,
                     new DataPearl.AbstractDataPearl.DataPearlType(ExtEnum<DataPearl.AbstractDataPearl.DataPearlType>.values.entries[Random.Range(0, ExtEnum<DataPearl.AbstractDataPearl.DataPearlType>.values.entries.Count)], false));

                    pearl.RealizeInRoom();
                    pearl.realizedObject.firstChunk.vel += Custom.RNV() * Random.value * 20;
                }
                player.room.PlaySound(SoundID.SANDBOX_Add_Item,player.mainBodyChunk);
                return true;
            }

            return false;
        }

    }
    class RedEnvelopeBuffData : BuffData { public override BuffID ID => RedEnvelopeBuffEntry.RedEnvelopeID; }
    class RedEnvelopeBuffEntry : IBuffEntry
    {
        public static BuffID RedEnvelopeID = new BuffID("RedEnvelopeID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RedEnvelopeBuff, RedEnvelopeBuffData, RedEnvelopeBuffEntry>(RedEnvelopeID);
        }
        public static void HookOn()
        {

        }
    }
}