using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RandomBuffUtils.CreatureExtend;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Positive.DronesFromAFarBuff
{
    internal class DronesFromAfarBuffEntry
    {
        public static BuffID dronesFromAFarBuffID = new BuffID("DronesFromAfar", true);
        public static LaserDroneRegister laserDroneRegister = new LaserDroneRegister();
    }

    internal class DronesFromAfarBuffData : BuffData
    {
        public override BuffID ID => DronesFromAfarBuffEntry.dronesFromAFarBuffID;
    }

    internal class DronesFromAfarBuff : Buff<DronesFromAfarBuff, DronesFromAfarBuffData>, PlayerUtils.IOWnPlayerUtilsPart
    {
        public override BuffID ID => DronesFromAfarBuffEntry.dronesFromAFarBuffID;

        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }

        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            return new DronesModule();
        }

        public class DronesModule : PlayerUtils.PlayerModulePart
        {
            public PlayerExtraMovement playerExtraMovement;
            public Color laserColor;

            public override void Update(Player player, bool eu)
            {
                base.Update(player, eu);
                playerExtraMovement.Update(player);
            }

            public class PlayerExtraMovement
            {
                public Vector2 extraVelocity = Vector2.zero;

                public void Update(Player player)
                {
                    float factor = Mathf.InverseLerp(0, player.slugcatStats.runspeedFac * 4f, player.mainBodyChunk.vel.magnitude);
                    factor = Mathf.Lerp(0.001f, 1f, 1f - factor);

                    player.mainBodyChunk.vel += factor * extraVelocity;
                    extraVelocity *= 0.6f;
                }
                public void PlusSpeed(Vector2 acc)
                {
                    extraVelocity += acc;
                }
            }
        }
    }

    internal class LaserDroneRegister : BuffCreatureRegister
    {
        public LaserDroneRegister() : base("BuffLaserDrone", new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f))
        {
            MyRealizeCreatureInfo = new RealizeCreatureInfo(100, false, Player.ObjectGrabability.TwoHands);
            MyDevMapInfo = new DevMapInfo("bld", new Color(1f, 0.26f, 0.45f));
        }

        public override IEnumerable<RelationshipEstablishInfo> AddRelationshipEstablishInfos()
        {
            yield return new RelationshipEstablishInfo("Scavenger", new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f), new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Attacks, 0.2f));
        }

        public override CreatureTemplate CreateCreatureTemplate()
        {
            var tileTypeResistances = new List<TileTypeResistance>();
            var connectionResistances = new List<TileConnectionResistance>();

            BuffCreatureTemplateHelper.BuildTileTypeResistance(tileTypeResistances, AItile.Accessibility.Air, 1f);
            BuffCreatureTemplateHelper.BuildTileTypeResistance(tileTypeResistances, AItile.Accessibility.OffScreen, 1f, PathCost.Legality.Unallowed);
            BuffCreatureTemplateHelper.BuildTileTypeResistance(tileTypeResistances, AItile.Accessibility.Solid, 1f, PathCost.Legality.IllegalTile);


            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.Standard, 1f);
            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.OpenDiagonal, 0.5f);
            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.ShortCut, 1f);
            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.NPCTransportation, 1f, PathCost.Legality.Unallowed);
            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.OffScreenMovement, 1f, PathCost.Legality.IllegalTile);
            BuffCreatureTemplateHelper.BuildTileConnectionResistance(connectionResistances, MovementConnection.MovementType.BetweenRooms, 0.5f, PathCost.Legality.Allowed);

            CreatureTemplate t = new CreatureTemplate(Type, null, tileTypeResistances, connectionResistances, new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f));
            t.AI = true;
            t.offScreenSpeed = 1f;
            t.abstractedLaziness = 10;
            t.roamBetweenRoomsChance = 1f;
            t.visualRadius = 1600f;
            t.movementBasedVision = 1f;
            t.communityInfluence = 0f;
            t.meatPoints = 0;
            t.dangerousToPlayer = 0f;
            t.grasps = 1;

            t.bodySize = 0.5f;
            t.shortcutSegments = 1;

            t.canFly = true;
            t.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;

            t.doPreBakedPathing = true;
            t.requireAImap = true;
            t.preBakedPathingAncestor = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly);
            t.waterPathingResistance = 2f;
            t.canSwim = false;

            t.instantDeathDamageLimit = 3f;
            t.baseDamageResistance = 10f;
            t.baseStunResistance = 10f;
            return t;
        }
    }
}
