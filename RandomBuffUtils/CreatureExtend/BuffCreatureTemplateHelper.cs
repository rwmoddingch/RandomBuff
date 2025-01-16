using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static AItile;
using static PathCost;

namespace RandomBuffUtils.CreatureExtend
{
    public static class BuffCreatureTemplateHelper
    {
        public static List<TileTypeResistance> BuildTileTypeResistance(List<TileTypeResistance> orig, AItile.Accessibility accessibility, float resistance, PathCost.Legality legality = Legality.Allowed)
        {
            for(int i = 0;i < orig.Count; i++)
            {
                if (orig[i].accessibility == accessibility)
                {
                    orig[i] = new TileTypeResistance(accessibility, resistance, legality);
                    return orig;
                }
            }

            orig.Add(new TileTypeResistance(accessibility, resistance, legality));
            return orig;
        }

        public static List<TileTypeResistance> CopyFrom(List<TileTypeResistance> orig, CreatureTemplate.Type type)
        {
            var target = StaticWorld.GetCreatureTemplate(type);
            for (int l = 0; l < target.pathingPreferencesTiles.Length; l++)
            {
                AItile.Accessibility acc = (AItile.Accessibility)l;
                float resistance = target.pathingPreferencesTiles[l].resistance;
                PathCost.Legality legality = target.pathingPreferencesTiles[l].legality;

                BuildTileTypeResistance(orig, acc, resistance, legality);
            }
            return orig;
        }


        public static List<TileConnectionResistance> BuildTileConnectionResistance(List<TileConnectionResistance> orig, MovementConnection.MovementType movementType, float resistance, PathCost.Legality legality = Legality.Allowed)
        {
            for (int i = 0; i < orig.Count; i++)
            {
                if (orig[i].movementType == movementType)
                {
                    orig[i] = new TileConnectionResistance(movementType, resistance, legality);
                    return orig;
                }
            }

            orig.Add(new TileConnectionResistance(movementType, resistance, legality));
            return orig;
        }
    }
}
