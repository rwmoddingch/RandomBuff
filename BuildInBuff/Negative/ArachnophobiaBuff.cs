using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;

using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{


    internal class ArachnophobiaIBuffEntry : IBuffEntry
    {
        public static BuffID arachnophobiaID = new BuffID("Arachnophobia", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ArachnophobiaIBuffEntry>(arachnophobiaID);
        }

        public static void HookOn()
        {
            On.Creature.Die += Creature_Die;
        }   

        private static void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            if(self.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.Spider &&
               self.abstractCreature.creatureTemplate.type != MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.MotherSpider &&
               self.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.Leech &&
               self.abstractCreature.creatureTemplate.type != CreatureTemplate.Type.SeaLeech && !self.dead &&
               !self.Template.smallCreature)
            {
                BuffUtils.Log(arachnophobiaID,"Arachnophobia Creature_Die");
                int max = Mathf.RoundToInt(Random.Range(5, 12) * Custom.LerpMap(self.TotalMass,1,10,0.35f,2f));
                for (int i = 0; i < max; i++)
                {
                    AbstractCreature creature = new AbstractCreature(self.abstractCreature.world,
                        StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null,
                        self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());

                    creature.RealizeInRoom();
                    var targetPos = self.firstChunk.pos;
                    var targetRad = self.firstChunk.rad;
                    if ((self.bodyChunkConnections?.Length ?? 0) != 0)
                    {
                        var connect = RXRandom.AnyItem(self.bodyChunkConnections);
                        var r = Random.value;
                        targetPos = Vector2.Lerp(connect.chunk1.pos, connect.chunk2.pos,r);
                        targetRad = Mathf.Lerp(connect.chunk1.rad, connect.chunk2.rad, r);
                    }
                    foreach (var chunk in creature.realizedCreature.bodyChunks)
                        chunk.pos = chunk.lastPos = targetPos + Custom.RNV() * Random.Range(0,1.5f)*targetRad;
                }
            }

            orig(self);
        }
    }
}
