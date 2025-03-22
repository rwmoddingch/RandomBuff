using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa.WawaDevInterface
{
    internal class WawaDevHooks
    {
        public static void HooksOn()
        {
            On.DevInterface.ObjectsPage.CreateObjRep += ObjectsPage_CreateObjRep;
            On.DevInterface.ObjectsPage.Refresh += ObjectsPage_Refresh;
            On.DevInterface.ObjectsPage.DevObjectGetCategoryFromPlacedType += ObjectsPage_DevObjectGetCategoryFromPlacedType;
            On.DevInterface.ObjectsPage.Signal += ObjectsPage_Signal;
            On.DevInterface.ObjectsPage.RemoveObject += ObjectsPage_RemoveObject;
            On.PlacedObject.GenerateEmptyData += PlacedObject_GenerateEmptyData;
        }

        private static void ObjectsPage_RemoveObject(On.DevInterface.ObjectsPage.orig_RemoveObject orig, DevInterface.ObjectsPage self, DevInterface.PlacedObjectRepresentation objRep)
        {
            orig.Invoke(self, objRep);
            if(objRep.pObj.type == Wawa.WawaTokenPlacedType)
            {
                WawaChatRoomSettings.DevRemoveToken(self.owner.room);
            }
        }

        private static void ObjectsPage_Signal(On.DevInterface.ObjectsPage.orig_Signal orig, DevInterface.ObjectsPage self, DevInterface.DevUISignalType type, DevInterface.DevUINode sender, string message)
        {
            orig.Invoke(self, type, sender, message);
            if (type == DevInterface.DevUISignalType.ButtonClick)
            {
                string idstring = sender.IDstring;
                if (idstring != null)
                {
                    if (idstring == "Save_Settings")
                    {
                        WawaChatRoomSettings.Save();
                    }
                }
            }
        }

        private static void PlacedObject_GenerateEmptyData(On.PlacedObject.orig_GenerateEmptyData orig, PlacedObject self)
        {
            orig.Invoke(self);
            if(self.type == Wawa.WawaTokenPlacedType)
            {
                self.data = new WawaTokenData(self);
            }
        }

        private static void ObjectsPage_Refresh(On.DevInterface.ObjectsPage.orig_Refresh orig, DevInterface.ObjectsPage self)
        {
            orig.Invoke(self);
            if (BuffPlugin.DevEnabled)
            {
                var obj = WawaChatRoomSettings.GetToken(self.owner.room);
                if(obj != null)
                {
                    self.CreateObjRep(obj.type, obj);
                }
            }
        }

        private static DevInterface.ObjectsPage.DevObjectCategories ObjectsPage_DevObjectGetCategoryFromPlacedType(On.DevInterface.ObjectsPage.orig_DevObjectGetCategoryFromPlacedType orig, global::DevInterface.ObjectsPage self, PlacedObject.Type type)
        {
            if (type == Wawa.WawaTokenPlacedType)
                return DevInterface.ObjectsPage.DevObjectCategories.Tutorial;
            return orig.Invoke(self, type);
        }

        private static void ObjectsPage_CreateObjRep(On.DevInterface.ObjectsPage.orig_CreateObjRep orig, global::DevInterface.ObjectsPage self, PlacedObject.Type tp, PlacedObject pObj)
        {
            if(tp == Wawa.WawaTokenPlacedType && BuffPlugin.DevEnabled)
            {
                if (pObj == null)
                {
                    pObj = new PlacedObject(tp, null);
                    pObj.pos = self.owner.room.game.cameras[0].pos + Vector2.Lerp(self.owner.mousePos, new Vector2(-683f, 384f), 0.25f) + Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.2f;
                    WawaChatRoomSettings.DevSetTokenPlaced(self.owner.room, pObj);
                }

                //BuffPlugin.Log($"Dev - {self.owner == null} {}")
                var rep = new WawaTokenRep(self.owner, tp.ToString() + "_Rep", self, pObj);
                self.tempNodes.Add(rep);
                self.subNodes.Add(rep);
            }
            else
                orig.Invoke(self, tp, pObj);
        }
    }
}
