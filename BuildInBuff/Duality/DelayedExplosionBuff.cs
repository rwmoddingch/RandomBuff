using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Duality
{
    internal class DelayedExplosionBuff : Buff<DelayedExplosionBuff, DelayedExplosionBuffData>
    {
        public override BuffID ID => DelayedExplosionBuffEntry.DelayedExplosion;
    }

    class DelayedExplosionBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 5;
        public override BuffID ID => DelayedExplosionBuffEntry.DelayedExplosion;
    }

    class DelayedExplosionBuffEntry : IBuffEntry
    {
        public static BuffID DelayedExplosion = new BuffID("DelayedExplosion", true);
        public static ConditionalWeakTable<ScavengerBomb, DelayedExplosionModule> module = new ConditionalWeakTable<ScavengerBomb, DelayedExplosionModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DelayedExplosionBuff, DelayedExplosionBuffData, DelayedExplosionBuffEntry>(DelayedExplosion);
        }

        public static void HookOn()
        {
            On.Player.ThrowObject += Player_ThrowObject;
            On.Scavenger.Throw += Scavenger_Throw;
            On.ScavengerBomb.HitSomething += ScavengerBomb_HitSomething;
            On.ScavengerBomb.TerrainImpact += ScavengerBomb_TerrainImpact;
            On.ScavengerBomb.Update += ScavengerBomb_Update;
        }


        private static void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (self.grasps[grasp] != null && self.grasps[grasp].grabbed is ScavengerBomb bomb)
            {
                if (module.TryGetValue(bomb, out var bombmodule))
                {
                    bombmodule.startBurn = true;
                }
            }
            orig(self, grasp, eu);
        }

        private static void Scavenger_Throw(On.Scavenger.orig_Throw orig, Scavenger self, Vector2 throwDir)
        {
            if (self.grasps[0] != null && self.grasps[0].grabbed is ScavengerBomb bomb)
            {
                if (module.TryGetValue(bomb, out var bombmodule))
                {
                    bombmodule.startBurn = true;
                }
            }
            orig(self, throwDir);
        }

        private static void ScavengerBomb_Update(On.ScavengerBomb.orig_Update orig, ScavengerBomb self, bool eu)
        {
            orig(self, eu);
            if (module.TryGetValue(self, out var bombModule))
            {
                bombModule.Update();
                if (bombModule.startBurn)
                {
                    self.burn = 999;
                }
            }
            else
            {
                module.Add(self, new DelayedExplosionModule(self));
            }
        }

        private static bool ScavengerBomb_HitSomething(On.ScavengerBomb.orig_HitSomething orig, ScavengerBomb self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (module.TryGetValue(self, out var bombModule))
            {
                return bombModule.HitSomething(result, eu);
            }
            return orig(self, result, eu);
        }

        private static void ScavengerBomb_TerrainImpact(On.ScavengerBomb.orig_TerrainImpact orig, ScavengerBomb self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (module.TryGetValue(self, out var bombModule))
            {
                bombModule.TerrainImpact(chunk, direction, speed, firstContact);
                return;
            }
            orig(self, chunk, direction, speed, firstContact);
        }

    }

    internal class DelayedExplosionModule
    {
        public WeakReference<ScavengerBomb> bombRef;
        public ScavengerBomb self;
        public bool startBurn;
        float delayBurn = 90;

        public DelayedExplosionModule(ScavengerBomb scavengerBomb)
        {
            bombRef = new WeakReference<ScavengerBomb>(scavengerBomb);
            self = scavengerBomb;
            delayBurn = 90;
            startBurn = false;
        }

        public bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj == null)
            {
                return false;
            }
            self.vibrate = 20;
            self.ChangeMode(Weapon.Mode.Free);
            if (result.obj is Creature)
            {
                (result.obj as Creature).Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass), result.chunk, result.onAppendagePos, Creature.DamageType.Explosion, 0.01f, 85f);
            }
            else if (result.chunk != null)
            {
                result.chunk.vel += self.firstChunk.vel * self.firstChunk.mass / result.chunk.mass;
            }
            else if (result.onAppendagePos != null)
            {
                (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, self.firstChunk.vel * self.firstChunk.mass);
            }
            if (delayBurn <= 0)
            {
                self.Explode(result.chunk);
            }
            return true;
        }

        public void TerrainImpact(int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (firstContact)
            {
                if (speed * self.bodyChunks[chunk].mass > 7f)
                {
                    self.room.ScreenMovement(new Vector2?(self.bodyChunks[chunk].pos), Custom.IntVector2ToVector2(direction) * speed * self.bodyChunks[chunk].mass * 0.1f, Mathf.Max((speed * self.bodyChunks[chunk].mass - 30f) / 50f, 0f));
                }
                if (speed > 4f && speed * self.bodyChunks[chunk].loudness * Mathf.Lerp(self.bodyChunks[chunk].mass, 1f, 0.5f) > 0.5f)
                {
                    self.room.InGameNoise(new Noise.InGameNoise(self.bodyChunks[chunk].pos + IntVector2.ToVector2(direction) * self.bodyChunks[chunk].rad * 0.9f, Mathf.Lerp(350f, Mathf.Lerp(100f, 1500f, Mathf.InverseLerp(0.5f, 20f, speed * self.bodyChunks[chunk].loudness * Mathf.Lerp(self.bodyChunks[chunk].mass, 1f, 0.5f))), 0.5f), self, 1f));
                }
            }
            if (self.floorBounceFrames > 0 && (direction.x == 0 || self.room.GetTile(self.firstChunk.pos).Terrain == Room.Tile.TerrainType.Slope))
            {
                return;
            }
            if (self.ignited)
            {
                //startBurn = true;
                if (delayBurn <= 0)
                {
                    self.Explode(null);
                }
            }
        }

        public void Update()
        {
            if (!startBurn) return;
            if (delayBurn > 0)
            {
                delayBurn--;
                if (self.room != null && !self.slatedForDeletetion)
                {
                    if (delayBurn % 40 == 0)
                    {
                        self.room.PlaySound(SoundID.Gate_Clamp_Lock, self.firstChunk, false, 0.5f, 3f + Random.value);
                    }
                }
            }
            if (delayBurn <= 0 && self.room != null && !self.slatedForDeletetion)
            {
                self.Explode(null);
            }
        }
    }
}
