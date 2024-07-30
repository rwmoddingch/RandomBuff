using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class SensorBombEntry : IBuffEntry
    {
        public static BuffID sensorBombBuffID = new BuffID("SensorBomb", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SensorBombEntry>(sensorBombBuffID);
        }

        public static void HookOn()
        {
            On.ScavengerBomb.HitSomething += ScavengerBomb_HitSomething;
            On.ScavengerBomb.Update += ScavengerBomb_Update;
            On.ScavengerBomb.WeaponDeflect += ScavengerBomb_WeaponDeflect;
            On.ScavengerBomb.TerrainImpact += ScavengerBomb_TerrainImpact;
            On.ScavengerBomb.Thrown += ScavengerBomb_Thrown;
            On.Weapon.HitWall += Weapon_HitWall;
            //On.BodyChunk.CheckHorizontalCollision += BodyChunk_CheckHorizontalCollision;
            //On.BodyChunk.CheckVerticalCollision += BodyChunk_CheckVerticalCollision;
        }

        private static void Weapon_HitWall(On.Weapon.orig_HitWall orig, Weapon self)
        {
            if (self is ScavengerBomb)
            {
                if (self.room.BeingViewed)
                {
                    for (int i = 0; i < 7; i++)
                    {
                        self.room.AddObject(new Spark(self.firstChunk.pos + self.throwDir.ToVector2() * (self.firstChunk.rad - 1f), Custom.DegToVec(UnityEngine.Random.value * 360f) * 10f * UnityEngine.Random.value + -self.throwDir.ToVector2() * 10f, new Color(1f, 1f, 1f), null, 2, 4));
                    }
                }
                self.room.ScreenMovement(new Vector2?(self.firstChunk.pos), self.throwDir.ToVector2() * 1.5f, 0f);
                self.room.PlaySound((self is Spear) ? SoundID.Spear_Bounce_Off_Wall : SoundID.Rock_Hit_Wall, self.firstChunk);

                BounceBomb(self as ScavengerBomb);
            }
            else
                orig.Invoke(self);
        }

        private static void BodyChunk_CheckVerticalCollision(On.BodyChunk.orig_CheckVerticalCollision orig, BodyChunk self)
        {
            Vector2 vel = self.vel;
            orig.Invoke(self);
            if (self.vel != vel && self.owner is ScavengerBomb)
                self.vel += -(vel.normalized + Custom.RNV() * 0.4f).normalized * 0.1f * (self.vel.magnitude);
        }

        private static void BodyChunk_CheckHorizontalCollision(On.BodyChunk.orig_CheckHorizontalCollision orig, BodyChunk self)
        {
            Vector2 vel = self.vel;
            orig.Invoke(self);
            if (self.vel != vel && self.owner is ScavengerBomb)
                self.vel += -(vel.normalized + Custom.RNV() * 0.4f).normalized * 0.1f * (self.vel.magnitude);
        }

        private static void ScavengerBomb_Thrown(On.ScavengerBomb.orig_Thrown orig, ScavengerBomb self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, RWCustom.IntVector2 throwDir, float frc, bool eu)
        {
            orig.Invoke(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            self.bounce = 0.95f;
            self.surfaceFriction = 0.95f;
            self.firstChunk.terrainSqueeze = 2.5f;
            self.room.AddObject(new SensorBombScanLine(self));
        }

        private static void ScavengerBomb_TerrainImpact(On.ScavengerBomb.orig_TerrainImpact orig, ScavengerBomb self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            bool origIgnite = self.ignited;
            self.ignited = false;   
            orig.Invoke(self, chunk, direction, speed, firstContact);
            self.ignited = origIgnite;
        }

        private static void ScavengerBomb_WeaponDeflect(On.ScavengerBomb.orig_WeaponDeflect orig, ScavengerBomb self, Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            self.firstChunk.pos = Vector2.Lerp(self.firstChunk.pos, inbetweenPos, 0.5f);
            self.vibrate = 20;
            self.firstChunk.vel = deflectDir * bounceSpeed * 0.5f;
        }

        private static void ScavengerBomb_Update(On.ScavengerBomb.orig_Update orig, ScavengerBomb self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.burn > 0f)
            {
                self.burn += 0.033333335f * 0.95f;
                if ((self.firstChunk.pos - self.firstChunk.lastPos).magnitude * 40f <= 15f)
                    self.Explode(null);
            }

            if (self.mode == Weapon.Mode.Thrown)
            {
                IntVector2 pos = self.room.GetTilePosition(self.firstChunk.pos);

                if (self.firstChunk.ContactPoint.x != 0 || self.firstChunk.contactPoint.y != 0)
                {
                    BounceBomb(self);
                }

                foreach (var creature in self.room.updateList.Where((u) => u is Creature).Select((u) => u as Creature))
                {
                    if (creature is Player)
                        continue;

                    if (Vector2.Distance(creature.DangerPos, self.firstChunk.pos) < 100f)
                    {
                        self.Explode(null);
                        return;
                    }
                }
            }

        }

        private static bool ScavengerBomb_HitSomething(On.ScavengerBomb.orig_HitSomething orig, ScavengerBomb self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj == null)
            {
                return false;
            }
            self.firstChunk.vel = -self.firstChunk.vel;
            return true;
        }

        static void BounceBomb(ScavengerBomb bomb, Vector2? overrideNorm = null)
        {
            Vector2 norm = overrideNorm ?? -(bomb.firstChunk.contactPoint.ToVector2().normalized);
            Vector2 dir = (norm + Custom.RNV() * 0.5f).normalized;
            bomb.firstChunk.vel += bomb.firstChunk.vel.magnitude * 0.1f * dir;
        }
    }

    internal class SensorBombScanLine : CosmeticSprite
    {
        static float rad = 150f;
        static float rotationVel = 720f;
        static Color scanlineCol = Color.red;

        ScavengerBomb bindBomb;
        float rotation;

        public SensorBombScanLine(ScavengerBomb bomb)
        {
            room = bomb.room;
            bindBomb = bomb;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2) }, true) { shader = rCam.game.rainWorld.Shaders["Hologram"] };
            sLeaser.sprites[1] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[1] { new TriangleMesh.Triangle(0, 1, 2) }, true) { shader = rCam.game.rainWorld.Shaders["Hologram"] };

            AddToContainer(sLeaser, rCam, null);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            Color fadeCol = scanlineCol;
            fadeCol.a = 0f;

            TriangleMesh mesh1 = sLeaser.sprites[0] as TriangleMesh;
            mesh1.verticeColors[0] = scanlineCol;
            mesh1.verticeColors[1] = fadeCol;
            mesh1.verticeColors[2] = fadeCol;

            TriangleMesh mesh2 = sLeaser.sprites[1] as TriangleMesh;
            mesh2.verticeColors[0] = scanlineCol;
            mesh2.verticeColors[1] = fadeCol;
            mesh2.verticeColors[2] = fadeCol;
        }

        //public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        //{
        //    base.AddToContainer(sLeaser, rCam, newContatiner);
        //    if (newContatiner == null)
        //        newContatiner = rCam.ReturnFContainer("Items");
        //    foreach(var sprite in sLeaser.sprites)
        //        newContatiner.AddChild(sprite);
        //}

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            if (bindBomb.slatedForDeletetion)
                Destroy();

            rotation += rotationVel / 40f;

            while (rotation > 360f)
                rotation -= 360f;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
                return;

            Vector2 dir1 = Custom.DegToVec(rotation);
            Vector2 perpDir1 = Custom.PerpendicularVector(dir1);
            Vector2 dir2 = -dir1;
            Vector2 perpDir2 = -perpDir1;

            Vector2 smoothPos = Vector2.Lerp(bindBomb.firstChunk.lastPos, bindBomb.firstChunk.pos, timeStacker) - camPos;

            TriangleMesh mesh1 = sLeaser.sprites[0] as TriangleMesh;
            mesh1.MoveVertice(0, smoothPos);
            mesh1.MoveVertice(1, dir1 * rad + perpDir1 * rad * 0.8f + smoothPos);
            mesh1.MoveVertice(2, dir1 * rad + perpDir2 * rad * 0.8f + smoothPos);

            TriangleMesh mesh2 = sLeaser.sprites[1] as TriangleMesh;
            mesh2.MoveVertice(0, smoothPos);
            mesh2.MoveVertice(1, dir2 * rad + perpDir2 * rad * 0.8f + smoothPos);
            mesh2.MoveVertice(2, dir2 * rad + perpDir1 * rad * 0.8f + smoothPos);
        }
    }
}
