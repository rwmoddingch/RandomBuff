using RWCustom;
using Smoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive.DronesFromAFarBuff
{
    internal class BuffLaserDroneGraphics : GraphicsModule
    {
        public static Color defaultLaserColor = new Color(1f, 0.26f, 0.45f);

        public Color droneFlameColor = new Color(0f, 0.62f, 1f);
        public Color laserColor = new Color(1f, 0.26f, 0.45f);

        public readonly int gunGraphicsStartIndex = 12;
        public readonly int droneWingStartIndex = 8;
        public readonly int droneFlameIndex = 5;

        public readonly int laserPannelAIndex = 0;
        public readonly int laserPannelBIndex = 1;
        public readonly int bodySegmentIndex = 2;
        public readonly int jetPannelIndex = 3;
        public readonly int gunPointerIndex = 4;

        public readonly float droneLength = 20f;
        public readonly float circleRad = 11f;
        public readonly float wingWidth = 6f;
        public readonly float gunLength = 20f;
        public readonly float gunWidth = 4f;

        public readonly float maxUnfoldAngle = 60f;
        public readonly float maxMovementRollVel = 10f;
        public readonly float chargeRollVel = 20f;

        public readonly float scaleYFactor = 0.55f;
        public readonly float scaleXFactor = 0.65f;

        public BuffLaserDrone drone;
        public LaserGunGraphics gunGraphics;
        public FlameTrail flameTrailWhite;
        public FlameTrail flameTrailCyan;
        public FlameTrail flameTrailBlue;

        public DroneWing[] wings;

        public int rollDir = 1;
        public float rollAngle = 0f;
        public float currentRollVel = 0f;

        public bool flameSpit = false;
        public bool getModuleColor = false;

        public Vector2 jetPos;

        public float TiltSin => Mathf.Sin(45f * Mathf.Deg2Rad * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, drone.MaxVelocity, 1f, 0.2f));
        public float TiltCos => Mathf.Cos(45f * Mathf.Deg2Rad * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, drone.MaxVelocity, 1f, 0.2f));
        public float wingUnfoldAngle => maxUnfoldAngle * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, drone.MaxVelocity, 1f, 0.2f);
        public float wingUnfoldRadAngle => wingUnfoldAngle * Mathf.Deg2Rad;

        public BuffLaserDroneGraphics(PhysicalObject ow) : base(ow, false)
        {
            drone = ow as BuffLaserDrone;

            cullRange = 1400f;

            flameTrailWhite = new FlameTrail(this, droneFlameIndex, Color.white, 4, 2.5f);
            flameTrailCyan = new FlameTrail(this, droneFlameIndex + 1, Color.cyan, 8, 2f);
            flameTrailBlue = new FlameTrail(this, droneFlameIndex + 2, Color.blue, 12, 1.8f);

            wings = new DroneWing[3];
            for (int i = 0; i < wings.Length; i++)
            {
                wings[i] = new DroneWing(this, droneWingStartIndex + i, 360f * i / (float)wings.Length);
            }
            gunGraphicsStartIndex = droneWingStartIndex + wings.Length;
            gunGraphics = new LaserGunGraphics(this, gunGraphicsStartIndex);
            gunGraphics.SetLaserColor(laserColor);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            //Plugin.Log(laserColor.ToString());

            sLeaser.sprites = new FSprite[8 + wings.Length + gunGraphics.TotalSprites];

            sLeaser.sprites[jetPannelIndex] = new FSprite("Circle20", true);
            sLeaser.sprites[bodySegmentIndex] = new FSprite("pixel", true);
            sLeaser.sprites[laserPannelBIndex] = new FSprite("Circle20", true);
            sLeaser.sprites[laserPannelAIndex] = new FSprite("Circle20", true);
            sLeaser.sprites[gunPointerIndex] = new FSprite("pixel", true);

            flameTrailWhite.InitiateSprites(sLeaser, rCam);
            flameTrailCyan.InitiateSprites(sLeaser, rCam);
            flameTrailBlue.InitiateSprites(sLeaser, rCam);

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].InitSprites(sLeaser, rCam);
            }

            gunGraphics.InitSprites(sLeaser, rCam);


            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Midground"));
            base.InitiateSprites(sLeaser, rCam);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null) newContatiner = rCam.ReturnFContainer("Midground");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                newContatiner.AddChild(sLeaser.sprites[i]);
            }

            sLeaser.sprites[jetPannelIndex].MoveBehindOtherNode(sLeaser.sprites[bodySegmentIndex]);

            sLeaser.sprites[flameTrailWhite.sprite].MoveBehindOtherNode(sLeaser.sprites[jetPannelIndex]);
            sLeaser.sprites[flameTrailCyan.sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrailWhite.sprite]);
            sLeaser.sprites[flameTrailBlue.sprite].MoveBehindOtherNode(sLeaser.sprites[flameTrailCyan.sprite]);

            sLeaser.sprites[laserPannelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[bodySegmentIndex]);
            for (int i = 0; i < wings.Length; i++)
            {
                sLeaser.sprites[laserPannelBIndex].MoveInFrontOfOtherNode(sLeaser.sprites[wings[i].startIndex]);
            }

            sLeaser.sprites[laserPannelAIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPannelBIndex]);
            sLeaser.sprites[gunPointerIndex].MoveInFrontOfOtherNode(sLeaser.sprites[laserPannelAIndex]);

        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[laserPannelAIndex].color = Color.white;
            sLeaser.sprites[laserPannelBIndex].color = laserColor;
            sLeaser.sprites[gunPointerIndex].color = laserColor;


            sLeaser.sprites[jetPannelIndex].color = palette.blackColor;
            sLeaser.sprites[bodySegmentIndex].color = palette.blackColor;

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].ApplyPalette(sLeaser, rCam, palette);
            }

            base.ApplyPalette(sLeaser, rCam, palette);
        }

        public void ResetColor(RoomCamera.SpriteLeaser sLeaser)
        {
            sLeaser.sprites[laserPannelAIndex].color = Color.white;
            sLeaser.sprites[laserPannelBIndex].color = laserColor;
            sLeaser.sprites[gunPointerIndex].color = laserColor;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            if (culled) return;

            //if (!getModuleColor && drone.owner != null && PlayerPatchs.modules.TryGetValue(drone.owner, out var module))
            //{
            //    laserColor = module.laserColor;
            //    getModuleColor = true;
            //    gunGraphics.SetLaserColor(laserColor);
            //    ResetColor(sLeaser);
            //}

            drone.infRotation.CaculateSmoothedRealRotion(timeStacker);

            Vector2 bodyPos = Vector2.Lerp(owner.bodyChunks[0].lastPos, owner.bodyChunks[0].pos, timeStacker);
            float rotation = drone.infRotation.SmoothedRealRotation;
            Vector2 dir = Custom.DegToVec(rotation);

            sLeaser.sprites[gunPointerIndex].SetPosition(bodyPos + dir * (gunLength * scaleXFactor * TiltCos / 2f - gunWidth * scaleYFactor / 2f) - camPos);
            sLeaser.sprites[gunPointerIndex].scaleX = TiltCos * scaleXFactor * gunLength;
            sLeaser.sprites[gunPointerIndex].scaleY = gunWidth * scaleYFactor;
            sLeaser.sprites[gunPointerIndex].rotation = rotation + 90f;

            sLeaser.sprites[laserPannelAIndex].SetPosition(bodyPos - camPos);
            sLeaser.sprites[laserPannelAIndex].rotation = rotation + 90f;
            sLeaser.sprites[laserPannelAIndex].scaleY = 0.8f * scaleYFactor;
            sLeaser.sprites[laserPannelAIndex].scaleX = TiltSin * 0.8f * scaleXFactor;
            sLeaser.sprites[laserPannelBIndex].SetPosition(bodyPos - camPos);
            sLeaser.sprites[laserPannelBIndex].rotation = rotation + 90f;
            sLeaser.sprites[laserPannelBIndex].scaleY = scaleYFactor;
            sLeaser.sprites[laserPannelBIndex].scaleX = TiltSin * scaleXFactor;

            sLeaser.sprites[bodySegmentIndex].SetPosition(bodyPos - dir * droneLength * 0.5f * TiltCos * scaleXFactor - camPos);
            sLeaser.sprites[bodySegmentIndex].rotation = rotation + 90f;
            sLeaser.sprites[bodySegmentIndex].scaleY = circleRad * scaleYFactor * 2f;
            sLeaser.sprites[bodySegmentIndex].scaleX = droneLength * scaleXFactor * TiltCos;

            sLeaser.sprites[jetPannelIndex].SetPosition(bodyPos - dir * droneLength * TiltCos * scaleXFactor - camPos);
            sLeaser.sprites[jetPannelIndex].rotation = rotation + 90f;
            sLeaser.sprites[jetPannelIndex].scaleY = scaleYFactor;
            sLeaser.sprites[jetPannelIndex].scaleX = TiltSin * scaleXFactor;

            for (int i = 0; i < wings.Length; i++)
            {
                wings[i].DrawSprites(sLeaser, rCam, timeStacker, camPos, bodyPos, i);
            }

            jetPos = bodyPos - dir * droneLength * TiltCos * scaleXFactor;

            flameTrailWhite.DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPos);
            flameTrailCyan.DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPos);
            flameTrailBlue.DrawSprite(sLeaser, rCam, timeStacker, camPos, jetPos);


            gunGraphics.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public override void Update()
        {
            base.Update();
            if (culled) return;

            flameSpit = !flameSpit;
            float targetRollVel = 0f;
            if (!gunGraphics.Gun.shouldCharge)
            {
                if (Random.value < 0.01f)
                {
                    rollDir = -rollDir;
                }
                targetRollVel = rollDir * maxMovementRollVel * Custom.LerpMap(drone.mainBodyChunk.vel.magnitude, 0, drone.MaxVelocity, 0f, 1f);
            }
            else targetRollVel = rollDir * chargeRollVel;

            currentRollVel = Mathf.Lerp(currentRollVel, targetRollVel, 0.1f);
            rollAngle += currentRollVel;

            flameTrailWhite.Update();
            flameTrailCyan.Update();
            flameTrailBlue.Update();
        }

        public override void Reset()
        {
            base.Reset();
            Vector2 dir = Custom.DegToVec(drone.CourseAngle);
            jetPos = drone.DangerPos - dir * droneLength * TiltCos * scaleXFactor;

            flameTrailWhite.Reset();
            flameTrailCyan.Reset();
            flameTrailBlue.Reset();
        }

        public class DroneWing
        {
            public BuffLaserDroneGraphics droneGraphics;
            public int startIndex;
            public float rotationBias;

            public float lastRollAngle;

            //0 = A, 1 = B, 2 = C, 3 = D
            public Vector2[] lastPostions = new Vector2[4];
            public Vector2[] positions = new Vector2[4];

            public float thisRollAngle => droneGraphics.rollAngle + rotationBias;
            public float wingWidth => droneGraphics.wingWidth;
            public float wingLength => droneGraphics.droneLength * droneGraphics.scaleXFactor;
            public float rad => droneGraphics.circleRad * droneGraphics.scaleYFactor * 0.8f;
            public float unfoldAngle => droneGraphics.wingUnfoldRadAngle;

            public DroneWing(BuffLaserDroneGraphics droneGraphics, int startIndex, float rotationBias = 0f)
            {
                this.droneGraphics = droneGraphics;
                this.rotationBias = rotationBias;
                this.startIndex = startIndex;

                lastRollAngle = thisRollAngle;

                UpdatePos(1f);
                for (int i = 0; i < 4; i++)
                {
                    lastPostions[i] = positions[i];
                }
            }

            public void InitSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites[startIndex] = new CustomFSprite("pixel");
            }

            public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                for (int i = 0; i < 4; i++)
                {
                    (sLeaser.sprites[startIndex] as CustomFSprite).verticeColors[i] = palette.blackColor;
                }
            }

            public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 bodyPos, int index)
            {
                UpdatePos(timeStacker);
                for (int i = 0; i < 4; i++)
                {
                    Vector2 pos = Vector2.Lerp(lastPostions[i], positions[i], timeStacker);
                    (sLeaser.sprites[startIndex] as CustomFSprite).MoveVertice(i, pos + bodyPos - camPos);
                }
            }

            public void UpdatePos(float timeStacker)
            {
                for (int i = 0; i < 4; i++)
                {
                    lastPostions[i] = positions[i];
                }

                float rollAngle = Mathf.Lerp(lastRollAngle, thisRollAngle, timeStacker);
                lastRollAngle = rollAngle;


                Vector2 dir = Custom.DegToVec(rollAngle);
                Vector2 perpendicularDir = Custom.PerpendicularVector(dir);

                Vector2 posM = dir * rad;
                Vector2 posN = dir * (rad + wingLength * Mathf.Sin(unfoldAngle));

                Vector2 foldWingVec = new Vector2(-wingLength * droneGraphics.TiltCos * Mathf.Cos(unfoldAngle), 0f);
                foldWingVec = Custom.rotateVectorDeg(foldWingVec, droneGraphics.drone.infRotation.SmoothedRealRotation - 90f);

                positions[0] = posM + perpendicularDir * wingWidth / 2f;
                positions[1] = posM - perpendicularDir * wingWidth / 2f;
                positions[2] = posN - perpendicularDir * wingWidth / 2f;
                positions[3] = posN + perpendicularDir * wingWidth / 2f;

                for (int i = 0; i < 4; i++)
                {
                    positions[i].x *= droneGraphics.TiltSin;

                    positions[i] = Custom.rotateVectorDeg(positions[i], droneGraphics.drone.infRotation.SmoothedRealRotation - 90f);
                    if (i == 2 || i == 3) positions[i] += foldWingVec;
                }
            }
        }

        public class FlameTrail
        {
            public int maxTrailLength;

            public BuffLaserDroneGraphics owner;

            public List<Vector2> positionsList;
            public List<Color> colorsList;

            public int sprite;
            public int savPoss;
            public float powFactor;
            public Color color;
            public FlameTrail(BuffLaserDroneGraphics owner, int sprite, Color color, int maxTrailLength = 20, float powFactor = 1.5f)
            {
                this.sprite = sprite;
                this.owner = owner;
                this.savPoss = maxTrailLength;
                this.color = color;
                this.maxTrailLength = maxTrailLength;
                this.powFactor = powFactor;
                this.Reset();
            }

            public void Reset()
            {
                this.positionsList = new List<Vector2>
                {
                    owner.jetPos
                };
                this.colorsList = new List<Color>
                {
                    color
                };
            }

            public Vector2 GetSmoothPos(int i, float timeStacker)
            {
                return Vector2.Lerp(this.GetPos(i + 1), this.GetPos(i), timeStacker);
            }

            public Vector2 GetPos(int i)
            {
                return this.positionsList[Custom.IntClamp(i, 0, this.positionsList.Count - 1)];
            }

            public Color GetCol(int i)
            {
                var col = colorsList[Custom.IntClamp(i, 0, this.colorsList.Count - 1)];
                return col;
            }

            public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                int segments = savPoss - 1;
                bool pointyTip = false;
                bool customColor = true;
                TriangleMesh.Triangle[] array = new TriangleMesh.Triangle[(segments - 1) * 4 + (pointyTip ? 1 : 2)];
                for (int i = 0; i < segments - 1; i++)
                {
                    int num = i * 4;
                    for (int j = 0; j < 4; j++)
                    {
                        array[num + j] = new TriangleMesh.Triangle(num + j, num + j + 1, num + j + 2);
                    }
                }
                array[(segments - 1) * 4] = new TriangleMesh.Triangle((segments - 1) * 4, (segments - 1) * 4 + 1, (segments - 1) * 4 + 2);
                if (!pointyTip)
                {
                    array[(segments - 1) * 4 + 1] = new TriangleMesh.Triangle((segments - 1) * 4 + 1, (segments - 1) * 4 + 2, (segments - 1) * 4 + 3);
                }
                TriangleMesh triangleMesh = new TriangleMesh("pixel", array, customColor, true);
                triangleMesh.alpha = 0.8f;

                sLeaser.sprites[this.sprite] = triangleMesh;
            }

            public void DrawSprite(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos, Vector2 jetPos)
            {
                Vector2 vector = jetPos;
                float num = owner.circleRad * owner.scaleYFactor * 0.5f;
                for (int i = 0; i < this.savPoss - 1; i++)
                {
                    Vector2 smoothPos = this.GetSmoothPos(i, timeStacker);
                    Vector2 smoothPos2 = this.GetSmoothPos(i + 1, timeStacker);
                    Vector2 dir = (vector - smoothPos).normalized;
                    Vector2 perDir = Custom.PerpendicularVector(dir);
                    dir *= Vector2.Distance(vector, smoothPos2) / 5f;
                    float width = Mathf.Pow(Custom.LerpMap(i, 0, savPoss - 1, 1f, 0f), powFactor);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4, vector - perDir * num * width - dir - camPos);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 1, vector + perDir * num * width - dir - camPos);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 2, smoothPos - perDir * num * width + dir - camPos);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).MoveVertice(i * 4 + 3, smoothPos + perDir * num * width + dir - camPos);
                    vector = smoothPos;
                }
                for (int j = 0; j < (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length; j++)
                {
                    float num2 = (float)j / (float)((sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors.Length - 1);
                    (sLeaser.sprites[this.sprite] as TriangleMesh).verticeColors[j] = GetCol(j);
                }
            }

            public void SetVisible(RoomCamera.SpriteLeaser sLeaser, bool visible)
            {
                sLeaser.sprites[sprite].isVisible = visible;
            }

            public void Update()
            {
                float vel = owner.drone.mainBodyChunk.vel.magnitude;
                Vector2 dir = Custom.DegToVec(owner.drone.CourseAngle);

                this.positionsList.Insert(0, owner.jetPos);
                if (this.positionsList.Count > maxTrailLength)
                {
                    this.positionsList.RemoveAt(maxTrailLength);
                }
                this.colorsList.Insert(0, color);
                if (this.colorsList.Count > maxTrailLength)
                {
                    this.colorsList.RemoveAt(maxTrailLength);
                }
                for (int i = 0; i < positionsList.Count; i++)
                {
                    positionsList[i] -= dir * 1.5f * 2f * Custom.LerpMap(vel, 0f, 1.5f, 1f, 0f);
                }

            }
        }
    }

    internal class DamageEffect
    {
        public BuffLaserDrone drone;

        public Smolder droneSmoke;
        public Color RandomSparkColor
        {
            get
            {
                Color laserCol = BuffLaserDroneGraphics.defaultLaserColor;
                //if (PlayerPatchs.modules.TryGetValue(drone.owner, out var module) && module.ownDrones)
                //{
                //    laserCol = module.laserColor;
                //}
                return Color.Lerp(Color.Lerp(laserCol, Color.blue, Random.value * 0.5f), Color.white, Random.value);
            }
        }
        public DamageEffect(BuffLaserDrone drone)
        {
            this.drone = drone;
        }

        public void InitEffect()
        {
            droneSmoke = new Smolder(drone.room, drone.DangerPos, drone.firstChunk, null);
            drone.room.AddObject(droneSmoke);
        }

        public void Update(Vector2 jetPos, bool eu)
        {
            try
            {
                if (drone == null) return;
                if (drone.droneState == null) return;
                switch (drone.droneState.life)
                {
                    case 0:
                    case 1:
                        if (Random.value < 0.1f)
                        {
                            drone.room.AddObject(new Spark(drone.DangerPos, Custom.RNV() * Mathf.Lerp(2f, 14f, Random.value), RandomSparkColor, null, 10, 40));
                        }
                        if (droneSmoke != null && droneSmoke.room != drone.room)
                        {
                            droneSmoke.Destroy();
                            droneSmoke = null;
                        }
                        if (droneSmoke == null)
                        {
                            if (drone.room != null) InitEffect();
                        }
                        else
                        {
                            droneSmoke.life = 500;
                        }
                        break;
                    case 2:
                        if (Random.value < 0.05f)
                        {
                            drone.room.AddObject(new Spark(drone.DangerPos, Custom.RNV() * Mathf.Lerp(2f, 14f, Random.value), RandomSparkColor, null, 10, 40));
                        }
                        break;
                    case 3:
                    default:
                        if (droneSmoke != null)
                        {
                            droneSmoke.Destroy();
                            droneSmoke = null;
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }
    }
}
