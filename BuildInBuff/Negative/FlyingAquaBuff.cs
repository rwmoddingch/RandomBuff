
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.RuntimeDetour;
using System.Reflection;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
 

    internal class FlyingAquaIBuffEntry : IBuffEntry
    {
        public static readonly BuffID FlyingAquaBuffID = new BuffID("FlyingAqua", true);
        private static bool lockWing;
        private static bool lockAqua;


        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FlyingAquaBuff,FlyingAquaBuffData,FlyingAquaIBuffEntry>(FlyingAquaBuffID);
        }

        public static void HookOn()
        {
            _ = new Hook(typeof(Centipede).GetProperty(nameof(Centipede.Centiwing), 
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(), 
                typeof(FlyingAquaIBuffEntry).GetMethod("Hook_Centiwing", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));
            _ = new Hook(typeof(Centipede).GetProperty(nameof(Centipede.AquaCenti),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).GetGetMethod(),
                typeof(FlyingAquaIBuffEntry).GetMethod("Hook_AquaCenti", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static));

            On.Centipede.Update += Centipede_Update;
            On.Centipede.AccessibleTile_IntVector2 += Centipede_AccessibleTile_IntVector2;
            On.CentipedeAI.Update += CentipedeAI_Update;
            On.Centipede.Fly += Centipede_Fly;

            lockAqua = false;
            lockWing = false;
        }

        private static void Centipede_Fly(On.Centipede.orig_Fly orig, Centipede self)
        {
            lockAqua = self.AquacentiSwim;
            orig(self);
            lockAqua = false;
        }


        private static void CentipedeAI_Update(On.CentipedeAI.orig_Update orig, CentipedeAI self)
        {
            lockWing = self.centipede.AquaCenti;
            orig.Invoke(self);
            lockWing = false;
        }

        private static bool Centipede_AccessibleTile_IntVector2(On.Centipede.orig_AccessibleTile_IntVector2 orig, Centipede self, RWCustom.IntVector2 testPos)
        {
            bool result = orig.Invoke(self, testPos);
            if (self.AquaCenti)
            {
                if (self.Centiwing && !self.flying)
                {
                    return result;
                }

                if (testPos.y != self.room.defaultWaterLevel)
                {
                    var template = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Centiwing);
                    result = result || self.room.aimap.TileAccessibleToCreature(testPos, template);
                }

                return result;
            }

            return result;
        }

        private static void Centipede_Update(On.Centipede.orig_Update orig, Centipede self, bool eu)
        {
            lockWing = self.AquaCenti;
            orig.Invoke(self, eu);
            lockWing = false;

        }

        public static bool Hook_Centiwing(Func<Centipede, bool>orig, Centipede self)
        {
            return orig(self) || lockWing;
        }

        public static bool Hook_AquaCenti(Func<Centipede, bool> orig, Centipede self)
        {
            return orig(self) && !lockAqua;
        }
    }


    public class FlyingAquaBuffData : BuffData
    {
        public override BuffID ID => FlyingAquaIBuffEntry.FlyingAquaBuffID;
    }


    public class FlyingAquaBuff : Buff<FlyingAquaBuff,FlyingAquaBuffData>
    {
        public override BuffID ID => FlyingAquaIBuffEntry.FlyingAquaBuffID;

        public FlyingAquaBuff()
        {
            var aqua = StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.AquaCenti);
            aqua.canFly = true;
            aqua.throughSurfaceVision = 0.8f;
            aqua.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
            aqua.pathingPreferencesTiles[(int)AItile.Accessibility.Air] =
                new PathCost(1, PathCost.Legality.Allowed);

        }

        public override void Destroy()
        {
            base.Destroy();
            var aqua = StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.AquaCenti);
            aqua.canFly = false;
            aqua.throughSurfaceVision = 0.2f;
            aqua.waterRelationship = CreatureTemplate.WaterRelationship.WaterOnly;
            aqua.pathingPreferencesTiles[(int)AItile.Accessibility.Air] =
                new PathCost(1000, PathCost.Legality.IllegalTile);
        }
    }
}
