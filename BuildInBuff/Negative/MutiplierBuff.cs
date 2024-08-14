using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuiltinBuffs.Negative
{
    internal class MultiplierBuff : Buff<MultiplierBuff, MultiplierBuffData>
    {
        static int creatureLimit = 200;
        public override BuffID ID => MultiplierBuffEntry.multiplierBuffID;

        public MultiplierBuff()
        {
            MyTimer = new DownCountBuffTimer(MultiplyCreature, 60);
        }

        public bool IgnoreThisCreature(AbstractCreature abstractCreature)
        {
            return abstractCreature.creatureTemplate.smallCreature || abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat;
        }


        public void MultiplyCreature(BuffTimer timer, RainWorldGame game)
        {
            var world = game.world;

            int totalCreatureInRegin = 0;
            List<AbstractCreature> abstractCreaturesToAdd = new List<AbstractCreature>();
            Dictionary<AbstractCreature, AbstractRoom> cretToRoom = new Dictionary<AbstractCreature, AbstractRoom>();

            foreach(var room in world.abstractRooms.Where((room) => !room.shelter && !room.gate))
            {
                totalCreatureInRegin += room.entities.Count((entity) => entity is AbstractCreature);
                totalCreatureInRegin += room.entitiesInDens.Count((entity) => entity is AbstractCreature);
            }

            foreach (var abRoom in world.abstractRooms)
            {
                if (!abRoom.shelter && !abRoom.gate)
                {
                    if (abRoom.entities.Count > 0)
                    {
                        foreach (var entity in abRoom.entities.ToArray())
                        {
                            if (totalCreatureInRegin > creatureLimit) break;
                            if (entity is AbstractCreature)
                            {
                                if (IgnoreThisCreature(entity as AbstractCreature)) continue;
                                totalCreatureInRegin++;

                                var newCreature = SpawnUperCreature(entity as AbstractCreature);

                                if (newCreature != null)
                                {
                                    newCreature.saveCreature = false;
                                    abstractCreaturesToAdd.Add(newCreature);
                                    cretToRoom.Add(newCreature, abRoom);
                                    totalCreatureInRegin++;
                                }
                            }
                        }
                    }
                    if (abRoom.entitiesInDens.Count > 0)
                    {
                        AbstractWorldEntity[] entityCopy = new AbstractWorldEntity[abRoom.entitiesInDens.Count];
                        abRoom.entitiesInDens.CopyTo(entityCopy);
                        foreach (var entity in entityCopy)
                        {
                            if (totalCreatureInRegin > creatureLimit) break;
                            if (entity is AbstractCreature)
                            {
                                if (IgnoreThisCreature(entity as AbstractCreature)) continue;
                                totalCreatureInRegin++;

                                var newCreature = SpawnUperCreature(entity as AbstractCreature);

                                if (newCreature != null)
                                {
                                    abstractCreaturesToAdd.Add(newCreature);
                                    cretToRoom.Add(newCreature, abRoom);
                                    totalCreatureInRegin++;
                                }
                            }
                        }
                    }
                }
            }
            if (abstractCreaturesToAdd.Count > 0)
            {
                foreach (var creature in abstractCreaturesToAdd)
                {
                    AbstractRoom abRoom = cretToRoom[creature];
                    abRoom.AddEntity(creature);
                    if (abRoom.realizedRoom != null)
                    {
                        creature.RealizeInRoom();
                    }
                }
            }
        }

        public AbstractCreature SpawnUperCreature(AbstractCreature origCreature)
        {
            AbstractRoom abRoom = origCreature.Room;
            World world = abRoom.world;
            CreatureTemplate.Type type = origCreature.creatureTemplate.type;
            if (type == null) return null;
            else if (type == CreatureTemplate.Type.GarbageWorm)
                return null;
            else if (type == CreatureTemplate.Type.Slugcat)
                return null;

            WorldCoordinate pos = origCreature.pos;
            AbstractCreature abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, pos, world.game.GetNewID());
            return abstractCreature;
        }
    }

    internal class MultiplierBuffData : BuffData
    {
        public override BuffID ID => MultiplierBuffEntry.multiplierBuffID;
    }

    internal class MultiplierBuffEntry : IBuffEntry
    {
        public static BuffID multiplierBuffID = new BuffID("Multiplier", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MultiplierBuff, MultiplierBuffData, MultiplierBuffEntry>(multiplierBuffID);
        }
    }
}
