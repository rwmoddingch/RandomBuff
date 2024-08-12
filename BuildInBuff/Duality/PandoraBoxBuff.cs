using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using UnityEngine;
using Random = UnityEngine.Random;
using RandomBuff.Core.Game;
using MoreSlugcats;

namespace BuiltinBuffs.Duality
{
    internal class PandoraBoxIBuffEntry : IBuffEntry
    {
        public static BuffID PandoraBoxBuffID = new BuffID("PandoraBox", true);
        public static SandboxGameSession session;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PandoraBoxIBuffEntry>(PandoraBoxBuffID);
            session = Helper.GetUninit<SandboxGameSession>();
            session.game = Helper.GetUninit<RainWorldGame>();
            session.game.overWorld = Helper.GetUninit<OverWorld>();
            session.game.overWorld.activeWorld = Helper.GetUninit<World>();
            session.game.world.abstractRooms = new AbstractRoom[1];
            session.game.world.abstractRooms[0] = Helper.GetUninit<AbstractRoom>();
            session.game.world.abstractRooms[0].entities = new List<AbstractWorldEntity>();
        }

        public static void HookOn()
        {
            On.Player.Regurgitate += Player_Regurgitate;
        }

        private static void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
        {
            BuffUtils.Log(PandoraBoxBuffID,"Player_Regurgitate");
            if (self.objectInStomach != null)
            {
                BuffUtils.Log(PandoraBoxBuffID, "Player_Regurgitate yes");
                AbstractPhysicalObject origObject = self.objectInStomach;
                bool replaceSuccessfully = false;

                if(self.room.updateList.Count(u => u is PhysicalObject physical && (physical.abstractPhysicalObject.type == MoreSlugcatsEnums.AbstractObjectType.HRGuard)) > 0)
                {
                    self.objectInStomach = new AbstractPhysicalObject(self.room.world, MoreSlugcatsEnums.AbstractObjectType.SingularityBomb, null, self.coord, self.room.game.GetNewID());
                    replaceSuccessfully = true;
                    orig.Invoke(self);
                }

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
                        BuffUtils.Log(PandoraBoxBuffID, $"Try to spawn {type}, but meet exception.you can just ignore this.\n{e}");
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
                BuffPoolManager.Instance.TriggerBuff(PandoraBoxBuffID, true);
            }
            else
                orig.Invoke(self);
        }

        public static bool SkipThisItem(AbstractPhysicalObject.AbstractObjectType type)
        {
            if (type == AbstractPhysicalObject.AbstractObjectType.EggBugEgg)
                return true;
            else if (type == AbstractPhysicalObject.AbstractObjectType.Creature)
                return true;
            else if (type == MoreSlugcatsEnums.AbstractObjectType.HRGuard)
            {
                if(Random.value < 0.001f)
                {
                    return false;
                }
                return true;
            }
            return false;
        }
    }
}
