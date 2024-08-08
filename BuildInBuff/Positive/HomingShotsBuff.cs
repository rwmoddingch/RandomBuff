using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using BuiltinBuffs.Duality;

namespace BuiltinBuffs.Positive
{
    internal class HomingShotsBuff : Buff<HomingShotsBuff, HomingShotsBuffData>
    {
        public override BuffID ID => HomingShotsBuffEntry.HomingShots;
        
        public HomingShotsBuff()
        {
        }
    }

    internal class HomingShotsBuffData : BuffData
    {
        public override BuffID ID => HomingShotsBuffEntry.HomingShots;
    }

    internal class HomingShotsBuffEntry : IBuffEntry
    {
        public static BuffID HomingShots = new BuffID("HomingShots", true);

        public static int StackLayer
        {
            get
            {
                return HomingShots.GetBuffData().StackLayer;
            }
        }

        public static float HomingSpeed
        {
            get
            {
                return 4f * Custom.LerpMap(StackLayer, 0, 3, 1, 3, 1);
            }
        }

        public static bool shouldFire = true;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<HomingShotsBuff, HomingShotsBuffData, HomingShotsBuffEntry>(HomingShots);
        }
        
        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if(self.room != null && self.abstractCreature == self.room.game.Players[0])
            {
                List<PhysicalObject>[] physicalObjects = self.room.physicalObjects;
                List<AbstractCreature> abstractCreature = self.room.abstractRoom.creatures;
                for (int i = 0; i < physicalObjects.Length; i++)
                {
                    for (int j = 0; j < physicalObjects[i].Count; j++)
                    {
                        PhysicalObject physicalObject = physicalObjects[i][j];
                        if (physicalObject is Weapon && (physicalObject as Weapon).thrownBy == self)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            Creature target = null;
                            float minDist = 100000f;

                            if (weapon.mode == Weapon.Mode.Thrown)
                            {
                                for (int k = 0; k < self.room.abstractRoom.creatures.Count; k++)
                                {
                                    if (self.room.abstractRoom.creatures[k].realizedCreature != null &&
                                        !(self.room.abstractRoom.creatures[k].realizedCreature is Player))
                                    {
                                        Creature creature = self.room.abstractRoom.creatures[k].realizedCreature;
                                        shouldFire = true;
                                        if (creature is Overseer && (creature as Overseer).AI.LikeOfPlayer(self.abstractCreature) > 0.5f)
                                        {
                                            shouldFire = false;
                                        }
                                        if (creature is Lizard)
                                        {
                                            foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Lizard).AI.relationshipTracker.relationships.
                                                Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == self.abstractCreature))
                                            {
                                                if ((creature as Lizard).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                                    shouldFire = false;
                                            }
                                        }
                                        if (creature is Scavenger && 
                                            (double)(creature as Scavenger).abstractCreature.world.game.session.creatureCommunities.
                                            LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, 
                                                        (creature as Scavenger).abstractCreature.world.game.world.RegionNumber, 
                                                        self.playerState.playerNumber) > 0.5)
                                        {
                                            shouldFire = false;
                                        }
                                        if (creature is Cicada)
                                        {
                                            foreach (RelationshipTracker.DynamicRelationship relationship in (creature as Cicada).AI.relationshipTracker.relationships.
                                                Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == self.abstractCreature))
                                            {
                                                if ((creature as Cicada).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                                    shouldFire = false;
                                            }
                                        }
                                        if (shouldFire)
                                        {
                                            if (Custom.DistLess(weapon.firstChunk.pos, self.room.abstractRoom.creatures[k].realizedCreature.mainBodyChunk.pos, minDist))
                                            {
                                                target = self.room.abstractRoom.creatures[k].realizedCreature;
                                                minDist = Custom.Dist(weapon.firstChunk.pos, self.room.abstractRoom.creatures[k].realizedCreature.mainBodyChunk.pos);
                                            }
                                        }
                                    }
                                }
                                if (target != null)
                                {
                                    float dist = Custom.Dist(weapon.firstChunk.pos, target.firstChunk.pos);
                                    float distPower = Custom.LerpMap(dist, 10f, 500f + 100f * StackLayer, 0f, 1f);/*
                                    float distPower = 200f / (dist + 10f);
                                    weapon.firstChunk.vel *= Custom.LerpMap(weapon.firstChunk.vel.magnitude, 1f, 10f, 0.99f, 0.9f);
                                    weapon.firstChunk.vel += distPower * Vector2.ClampMagnitude(target.firstChunk.pos - weapon.firstChunk.pos, HomingSpeed) / HomingSpeed * 3f;*/
                                    weapon.firstChunk.vel = Vector3.Slerp(weapon.firstChunk.vel, 
                                                                          weapon.firstChunk.vel.magnitude * Custom.DirVec(weapon.firstChunk.pos, target.firstChunk.pos), 
                                                                          0.2f * Mathf.Pow(distPower, 1f / (float)StackLayer));
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
