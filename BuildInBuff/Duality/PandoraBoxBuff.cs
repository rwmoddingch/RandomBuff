using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class PandoraBoxBuff : Buff<PandoraBoxBuff, PandoraBoxBuffData>
    {
        public override BuffID ID => PandoraBoxIBuffEntry.PandoraBoxBuffID;
    }

    internal class PandoraBoxBuffData : BuffData
    {
        public override BuffID ID => PandoraBoxIBuffEntry.PandoraBoxBuffID;
    }

    internal class PandoraBoxIBuffEntry : IBuffEntry
    {
        public static BuffID PandoraBoxBuffID = new BuffID("PandoraBox", true);
        public static SandboxGameSession session;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PandoraBoxBuff, PandoraBoxBuffData, PandoraBoxIBuffEntry>(PandoraBoxBuffID);
        }

        public static void HookOn()
        {
            On.Player.Regurgitate += Player_Regurgitate;
        }

        private static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
        {
            if (self.objectInStomach != null)
            {
                AbstractPhysicalObject origObject = self.objectInStomach;
                bool replaceSuccessfully = false;
                for (int i = 0; i < 5 && !replaceSuccessfully; i++)
                {
                    WorldCoordinate coordinate = self.coord;
                    AbstractPhysicalObject.AbstractObjectType type = new AbstractPhysicalObject.AbstractObjectType(ExtEnum<AbstractPhysicalObject.AbstractObjectType>.values.entries[Random.Range(0, ExtEnum<AbstractPhysicalObject.AbstractObjectType>.values.entries.Count)]);
                    int intdata = Random.Range(0, 5);
                    AbstractPhysicalObject nextObject = null;

                    if (SkipThisItem(type))
                        continue;
                    
                    try
                    {
                        session.SpawnItems(new IconSymbol.IconSymbolData(null, type, intdata), coordinate, self.room.game.GetNewID());
                        nextObject = self.objectInStomach = session.game.world.GetAbstractRoom(0).entities.Pop() as AbstractPhysicalObject;
                        self.objectInStomach.world = self.room.world;
                        self.room.abstractRoom.AddEntity(self.objectInStomach);
                        orig.Invoke(self);
                        replaceSuccessfully = true;
                    }
                    catch (Exception e)
                    {
                        BuffPlugin.Log($"Try to spawn {type}, but meet exception.you can just ignore this.\n{e}");
                        if(nextObject != null)
                        {
                            if(nextObject.realizedObject != null)
                            {
                                nextObject.realizedObject.Destroy();
                                if(nextObject.realizedObject is IDrawable)
                                {
                                    foreach(var sleaser in self.room.game.cameras[0].spriteLeasers)
                                    {
                                        if (sleaser.drawableObject == nextObject.realizedObject)
                                            sleaser.CleanSpritesAndRemove();
                                    }
                                }
                                nextObject.Destroy();
                            }
                        }
                    }
                }
                if(!replaceSuccessfully)
                {
                    self.objectInStomach = origObject;
                    orig.Invoke(self);
                }
            }
            else
                orig.Invoke(self);
        }

        public static bool SkipThisItem(AbstractPhysicalObject.AbstractObjectType type)
        {
            if (type == AbstractPhysicalObject.AbstractObjectType.EggBugEgg)
                return true;
            return false;
        }
    }
}
