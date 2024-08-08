using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
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
                var player = game.AlivePlayers.FirstOrDefault().realizedCreature as Player;
                for (int i = 0; i < Random.value*20; i++)
                {
                    try
                    {
                        var type = RXRandom.AnyItem(
                            ExtEnum<DataPearl.AbstractDataPearl.DataPearlType>.values.entries.Where(s =>
                                s != MoreSlugcatsEnums.DataPearlType.Spearmasterpearl.value &&
                                s != DataPearl.AbstractDataPearl.DataPearlType.PebblesPearl.value).ToArray());


                        var pearl = new DataPearl.AbstractDataPearl(
                            game.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                            player.room.GetWorldCoordinate(player.DangerPos), game.GetNewID(), -1, -1, null,
                            new DataPearl.AbstractDataPearl.DataPearlType(type, false));
                        game.AlivePlayers[0].Room.AddEntity(pearl);
                        pearl.RealizeInRoom();
                        pearl.realizedObject.firstChunk.vel += Custom.RNV() * Random.value * 20;
                    }
                    catch (Exception e)
                    {
                        BuffUtils.LogException(RedEnvelopeBuffEntry.RedEnvelopeID, e);
                    }
            
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