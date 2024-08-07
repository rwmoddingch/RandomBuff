using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;

namespace BuiltinBuffs.Positive
{
    internal class LavaImmuneBuffData : BuffData
    {
        public override BuffID ID => LavaImmuneBuffEntry.LavaImmune;
    }

    internal class LavaImmuneBuff : Buff<LavaImmuneBuff,LavaImmuneBuffData>
    {
        public override BuffID ID => LavaImmuneBuffEntry.LavaImmune;

        public LavaImmuneBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var ply in game.Players)
                    ply.lavaImmune = true;
            }

        }

        public override void Destroy()
        {
            base.Destroy();
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var ply in game.Players)
                    ply.lavaImmune = false;
            }
        }
    }

    internal class LavaImmuneBuffEntry : IBuffEntry
    {

        public static readonly BuffID LavaImmune = new BuffID(nameof(LavaImmune), true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<LavaImmuneBuff, LavaImmuneBuffData, LavaImmuneBuffEntry>(LavaImmune);
        }

        public static void HookOn()
        {
            On.AbstractCreature.ctor += AbstractCreature_ctor;
        }

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self,world, creatureTemplate, realizedCreature, pos, ID);
            if (creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat)
                self.lavaImmune = true;
        }
    }
}
