
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

namespace BuiltinBuffs.Duality
{


    internal class InfoOverloadIBuffEntry : IBuffEntry
    {
        public static BuffID InfoOverloadBuffID = new BuffID("InfoOverload", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<InfoOverloadIBuffEntry>(InfoOverloadBuffID);
        }

        public static void HookOn()
        {
            On.DataPearl.AbstractDataPearl.ctor += AbstractDataPearl_ctor;
        }

        private static void AbstractDataPearl_ctor(On.DataPearl.AbstractDataPearl.orig_ctor orig, DataPearl.AbstractDataPearl self, World world, AbstractPhysicalObject.AbstractObjectType objType, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID, int originRoom, int placedObjectIndex, PlacedObject.ConsumableObjectData consumableData, DataPearl.AbstractDataPearl.DataPearlType dataPearlType)
        {
            if ((dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc || dataPearlType == DataPearl.AbstractDataPearl.DataPearlType.Misc2))
            {
                
                self.type = MoreSlugcatsEnums.AbstractObjectType.HalcyonPearl;
                objType = MoreSlugcatsEnums.AbstractObjectType.HalcyonPearl;
                dataPearlType = MoreSlugcatsEnums.DataPearlType.RM;

            }
            orig.Invoke(self, world, objType, realizedObject, pos, ID, originRoom, placedObjectIndex, consumableData, dataPearlType);
        }
    }
}
