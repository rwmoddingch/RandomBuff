using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;

using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;

using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class ArachnophobiaBuff : Buff<ArachnophobiaBuff, ArachnophobiaBuffData>
    {
        public override BuffID ID => ArachnophobiaIBuffEntry.arachnophobiaID;
    }

    internal class ArachnophobiaBuffData : BuffData
    {
        public override BuffID ID => ArachnophobiaIBuffEntry.arachnophobiaID;
    }

    internal class ArachnophobiaIBuffEntry : IBuffEntry
    {
        public static BuffID arachnophobiaID = new BuffID("Arachnophobia", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ArachnophobiaBuff,ArachnophobiaBuffData,ArachnophobiaIBuffEntry>(arachnophobiaID);
        }

        public static void HookOn()
        {
            On.Creature.Die += Creature_Die;
        }   

        private static void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            orig(self);
            int max = Random.Range(5, 12);
            for (int i = 0; i < max; i++)
            {
                AbstractCreature creature = new AbstractCreature(self.abstractCreature.world,
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Spider), null,
                    self.abstractCreature.pos, self.abstractCreature.world.game.GetNewID());

                creature.Realize();
                foreach (var chunk in creature.realizedCreature.bodyChunks)
                    chunk.pos = chunk.lastPos = self.firstChunk.pos + Custom.RNV() * Random.Range(0, 10);

            }
        }
    }
}
