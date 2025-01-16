using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive.DronesFromAFarBuff
{
    internal class DroneLaserGun
    {
        public readonly int chargeRequier;
        public readonly int gunCoolDownRequier;
        public readonly float damagePerShot;
        public readonly bool killTagFromPlayer;

        public BuffLaserDrone drone;

        public int charge = 0;
        public int coolDown = 0;

        public bool shouldCharge = false;


        public BuffLaserDroneAI AI => drone.AI;
        public float GunDir => drone.CourseAngle;
        public float chargeProgress => Custom.LerpMap(charge, 0, chargeRequier * 0.7f, 0f, 1f);
        public float flashProgress => Custom.LerpMap(coolDown, gunCoolDownRequier, gunCoolDownRequier / 2, 1f, 0f);
        public DroneLaserGun(BuffLaserDrone drone)
        {
            this.drone = drone;

            chargeRequier = 120;
            gunCoolDownRequier = chargeRequier / 2;
            damagePerShot = 0.5f;
            killTagFromPlayer = true;

        }

        public void Update()
        {
            if (coolDown > 0) coolDown--;
            else if (shouldCharge)
            {
                if (charge < chargeRequier) charge++;
                else
                {
                    Shoot();
                }
            }
            else
            {
                if (charge > 0) charge--;
            }
        }

        public void Shoot()
        {
            coolDown = gunCoolDownRequier;
            charge = 0;
            if (drone.owner == null) return;

            AI.realizedTarget.Violence(killTagFromPlayer ? drone.owner.mainBodyChunk : null, null, AI.realizedTarget.mainBodyChunk, null, Creature.DamageType.Explosion, damagePerShot, 20f);

            Centipede centipede = AI.realizedTarget as Centipede;
            if (centipede != null && !centipede.Small)
            {
                int shootOffShellIndex = centipede.mainBodyChunkIndex;
                if (!centipede.CentiState.shells[shootOffShellIndex])
                {
                    shootOffShellIndex = Random.Range(0, centipede.bodyChunks.Length);
                }
                if (centipede.CentiState.shells[shootOffShellIndex])
                {
                    centipede.shellJustFellOff = shootOffShellIndex;
                    centipede.CentiState.shells[shootOffShellIndex] = false;
                    if (centipede.graphicsModule != null)
                    {
                        for (int j = 0; j < (centipede.Red ? 3 : 1); j++)
                        {
                            CentipedeShell centipedeShell = new CentipedeShell(centipede.bodyChunks[shootOffShellIndex].pos, Custom.RNV() * Random.value * ((j == 0) ? 3f : 6f), (centipede.graphicsModule as CentipedeGraphics).hue, (centipede.graphicsModule as CentipedeGraphics).saturation, centipede.bodyChunks[shootOffShellIndex].rad * 1.8f * 0.071428575f * 1.2f, centipede.bodyChunks[shootOffShellIndex].rad * 1.3f * 0.09090909f * 1.2f);
                            if (centipede.abstractCreature.IsVoided())
                            {
                                centipedeShell.lavaImmune = true;
                            }
                            centipede.room.AddObject(centipedeShell);
                        }
                    }
                    if (centipede.Red)
                    {
                        centipede.room.PlaySound(SoundID.Red_Centipede_Shield_Falloff, centipede.mainBodyChunk);
                    }
                }
            }


            drone.room.AddObject(new ShockWave(AI.realizedTarget.DangerPos, 330f, 0.045f, 5, false));
            drone.room.PlaySound(SoundID.Bomb_Explode, AI.realizedTarget.DangerPos);
            drone.room.InGameNoise(new Noise.InGameNoise(AI.realizedTarget.DangerPos, 9000f, drone, 1f));
            drone.room.AddObject(new Explosion.ExplosionLight(AI.realizedTarget.DangerPos, 280f, 1f, 7, drone.ShortCutColor()));
            drone.room.AddObject(new Explosion.ExplosionLight(AI.realizedTarget.DangerPos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            drone.room.AddObject(new ExplosionSpikes(drone.room, AI.realizedTarget.DangerPos, 14, 30f, 9f, 7f, 170f, drone.ShortCutColor()));
        }
    }

    internal class LaserGunGraphics
    {
        public readonly int minLineCount = 5;
        public readonly int maxLineCount = 10;

        BuffLaserDroneGraphics droneGraphics;
        LaserLine[] laserLines;

        public int startIndex;
        Vector2 bodyPos;
        Color laserCol;

        public int TotalSprites => laserLines.Length;
        public DroneLaserGun Gun => droneGraphics.drone.gun;

        public LaserGunGraphics(BuffLaserDroneGraphics graphics, int startIndex)
        {
            droneGraphics = graphics;
            System.Random random = new System.Random(droneGraphics.drone.abstractCreature.ID.RandomSeed);
            laserLines = new LaserLine[random.Next(minLineCount, maxLineCount)];
            this.startIndex = startIndex;
        }

        public void SetLaserColor(Color color)
        {
            laserCol = color;
        }

        public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            for (int i = 0; i < laserLines.Length; i++)
            {
                laserLines[i] = new LaserLine(this, i + startIndex);
                laserLines[i].InitSprites(sLeaser, rCam);
                //Debug.Log("[BlastLaserCat]TotalLaserSprites : " + TotalSprites.ToString());
            }
        }

        public void Update()
        {

        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            bodyPos = Vector2.Lerp(droneGraphics.owner.bodyChunks[0].lastPos, droneGraphics.owner.bodyChunks[0].pos, timeStacker);
            for (int i = 0; i < laserLines.Length; i++)
            {
                laserLines[i].DrawSprites(sLeaser, rCam, timeStacker, camPos);
            }
        }

        public class LaserLine : SharedPhysics.IProjectileTracer
        {
            public readonly float maxBiasAngle = 90f;
            public readonly float laserWidth = 1f;
            public readonly float maxLaserWidth = 10f;

            public LaserGunGraphics gunGraphics;
            public float deltaDegDir = 0f;
            public int spriteIndex;

            public float currentDegDir => deltaDegDir * (1f - gunGraphics.Gun.chargeProgress) * (gunGraphics.Gun.coolDown > 0 ? 0f : 1f) + gunGraphics.Gun.GunDir;
            public float dynamicLaserWidth => laserWidth * Mathf.Clamp(maxLaserWidth * (gunGraphics.Gun.flashProgress), 1f, maxLaserWidth) / 2f;

            public LaserLine(LaserGunGraphics gunGraphics, int spriteIndex)
            {
                this.gunGraphics = gunGraphics;
                this.spriteIndex = spriteIndex;
            }

            public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[spriteIndex] = new CustomFSprite("pixel") { shader = rCam.game.rainWorld.Shaders["Hologram"], isVisible = false };
                for (int i = 0; i < 4; i++)
                {
                    (sLeaser.sprites[spriteIndex] as CustomFSprite).verticeColors[i] = gunGraphics.laserCol;
                }
                deltaDegDir = Mathf.Lerp(-maxBiasAngle, maxBiasAngle, Random.value);
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (gunGraphics.Gun.chargeProgress > 0 || gunGraphics.Gun.flashProgress > 0)
                {
                    (sLeaser.sprites[spriteIndex] as CustomFSprite).isVisible = true;
                }
                else
                {
                    (sLeaser.sprites[spriteIndex] as CustomFSprite).isVisible = false;
                    return;
                }

                Vector2 dir = Custom.DegToVec(currentDegDir);
                Vector2 perpDir = Custom.PerpendicularVector(dir);

                Vector2 output = gunGraphics.bodyPos + dir * 10000f;
                Vector2 corner = Vector2.zero;
                bool getCollisionResult = true;
                SharedPhysics.CollisionResult collisionResult = new SharedPhysics.CollisionResult();

                try { collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(this, gunGraphics.Gun.drone.room, gunGraphics.bodyPos, ref output, 5f, 1, null, false); }
                catch { getCollisionResult = false; }

                if (getCollisionResult)
                {
                    corner = collisionResult.collisionPoint;
                }
                else
                {
                    corner = Custom.RectCollision(gunGraphics.bodyPos, gunGraphics.bodyPos + dir * 100000f, rCam.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
                    IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(rCam.room, gunGraphics.bodyPos, corner);
                    if (intVector != null)
                    {
                        corner = Custom.RectCollision(corner, gunGraphics.bodyPos, rCam.room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
                    }
                }

                (sLeaser.sprites[spriteIndex] as CustomFSprite).MoveVertice(0, gunGraphics.bodyPos + perpDir * dynamicLaserWidth - camPos);
                (sLeaser.sprites[spriteIndex] as CustomFSprite).MoveVertice(1, gunGraphics.bodyPos - perpDir * dynamicLaserWidth - camPos);
                (sLeaser.sprites[spriteIndex] as CustomFSprite).MoveVertice(2, corner - perpDir * dynamicLaserWidth - camPos);
                (sLeaser.sprites[spriteIndex] as CustomFSprite).MoveVertice(3, corner + perpDir * dynamicLaserWidth - camPos);

                Color color = Color.Lerp(Color.white, gunGraphics.laserCol, gunGraphics.Gun.coolDown > 0 ? (1f - gunGraphics.Gun.flashProgress) : 1f);
                for (int i = 0; i < 4; i++)
                {
                    (sLeaser.sprites[spriteIndex] as CustomFSprite).verticeColors[i] = Custom.RGB2RGBA(color, gunGraphics.Gun.flashProgress + gunGraphics.Gun.chargeProgress);
                }
            }

            public bool HitThisObject(PhysicalObject obj)
            {
                try
                {
                    return obj is Creature && obj == gunGraphics.Gun.AI.realizedTarget;
                }
                catch
                {
                    return false;
                }
            }

            public bool HitThisChunk(BodyChunk chunk)
            {
                return true;
            }
        }
    }
}
