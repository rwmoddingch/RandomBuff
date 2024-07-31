using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class SwarmMissileBuff : Buff<SwarmMissileBuff, SwarmMissileBuffData>, PlayerUtils.IOWnPlayerUtilsPart
    {
        public override BuffID ID => SwarmMissileBuffEntry.swarmMissileBuffID;

        public SwarmMissileBuff()
        {
            PlayerUtils.AddPart(this);
        }

        public override void Destroy()
        {
            base.Destroy();
            PlayerUtils.RemovePart(this);
        }

        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }

        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            return new SwarmSpawner();
        }

        internal class SwarmSpawner : PlayerUtils.PlayerModulePart
        {
            int counter;
            public override void Update(Player player, bool eu)
            {
                if(player.objectInStomach != null && player.objectInStomach.type == AbstractPhysicalObject.AbstractObjectType.SporePlant)
                {
                    counter++;
                }

                while(counter >= 80 && player.room != null)
                {
                    counter -= 80;
                    for (int i = 0; i < Random.Range(3, 6); i++)
                        player.room.AddObject(new SwarmMissile(player.room, player.DangerPos, Custom.RNV(), true));
                }
            }
        }
    }

    internal class SwarmMissileBuffData : BuffData
    {
        public override BuffID ID => SwarmMissileBuffEntry.swarmMissileBuffID;
    }


    internal class SwarmMissileBuffEntry : IBuffEntry
    {
        public static BuffID swarmMissileBuffID = new BuffID("SwarmMissile", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SwarmMissileBuff, SwarmMissileBuffData, SwarmMissileBuffEntry>(swarmMissileBuffID);
        }

        static int counter;
        public static void HookOn()
        {
            //On.Player.Update += Player_Update;
            On.SporePlant.AddDestinationBee += SporePlant_AddDestinationBee;
            On.SporePlant.AddBee += SporePlant_AddBee;
        }

        private static SporePlant.Bee SporePlant_AddBee(On.SporePlant.orig_AddBee orig, SporePlant self, SporePlant.Bee.Mode mode)
        {
            self.room.AddObject(new SwarmMissile(self.room, self.firstChunk.pos, Custom.RNV(), mode == SporePlant.Bee.Mode.BuzzAroundHive));
            return null;
        }

        private static void SporePlant_AddDestinationBee(On.SporePlant.orig_AddDestinationBee orig, SporePlant self)
        {
            if (self.possibleDestinations == null || self.possibleDestinations.Count == 0)
            {
                return;
            }
            self.room.AddObject(new SwarmMissile(self.room, self.firstChunk.pos, Custom.RNV()));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.room == null)
                return;

            counter++;
            if(counter >= 10)
            {
                counter -= 10;

                self.room.AddObject(new SwarmMissile(self.room, self.firstChunk.pos, Custom.RNV()));
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                self.room.AddObject(new SwarmMissile(self.room, self.firstChunk.pos, Custom.RNV()));
            }
        }
    }

    public class Missile : UpdatableAndDeletable
    {
        protected CreatureTemplate.Type[] ignoreTargets = new CreatureTemplate.Type[0];

        bool preferLeft;
        protected float traceDetectRad;

        public float maxAngularVelPerFrame;
        public float wallDetectAngle;
        public float avoidWallDistance;

        public Vector2 pos, lastPos;
        public Vector2 dir;
        public float vel;

        public int noDetectCounter;

        float likelyToBias;
        Vector2 randomBiasTarget;
        int randomBiasTargetCounter;

        public bool hasTarget;

        public Missile(Room room, Vector2 launchPos, Vector2 launchDir, float launchVel, float maxAngularVel, float wallDetectAngle, int noDetectCounter, float traceDetectRad, float avoidWallDistance)
        {
            this.room = room;
            lastPos = pos = launchPos;
            dir = launchDir;
            vel = launchVel;

            this.maxAngularVelPerFrame = maxAngularVel / 40f;
            this.noDetectCounter = noDetectCounter;
            this.wallDetectAngle = wallDetectAngle;
            this.traceDetectRad = traceDetectRad;
            this.avoidWallDistance = avoidWallDistance;

            preferLeft = Random.value < 0.5f;
            likelyToBias = Mathf.Lerp(0.2f, 0.5f, Random.value);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;
            lastPos = pos;

            if (noDetectCounter > 0)
                noDetectCounter--;

            float dirRotation = Custom.VecToDeg(dir);
            float applyAnglarVelDir = 0f;

            Vector2 wallDetectDir1 = Custom.DegToVec(dirRotation + wallDetectAngle);
            Vector2 wallDetectDir2 = Custom.DegToVec(dirRotation - wallDetectAngle);
            float distance1 = RayDetectReturnDistance(wallDetectDir1);
            float distance2 = RayDetectReturnDistance(wallDetectDir2);


            if (distance1 < avoidWallDistance && distance2 < avoidWallDistance)
            {
                if (distance1 < distance2)
                    distance2 += avoidWallDistance;
                else
                    distance1 += avoidWallDistance;
                //applyAnglarVelDir = (preferLeft ? -1f : 1f) * Mathf.Clamp01(1f - Mathf.Min(distance1, distance2) / avoidWallDistance);
            }

            if(distance1 < avoidWallDistance)
            {
                applyAnglarVelDir = -1 * Mathf.Clamp01(1f - distance1 / avoidWallDistance);
            }
            else if (distance2 < avoidWallDistance)
            {
                applyAnglarVelDir = 1f * Mathf.Clamp01(1f - distance2 / avoidWallDistance);
            }

            if (randomBiasTargetCounter > 0)
                randomBiasTargetCounter--;
            else if (Random.value < likelyToBias * 0.1f)
            {
                randomBiasTarget = (Custom.RNV() - dir * 0.5f) * traceDetectRad * 0.9f + pos;
                randomBiasTargetCounter = Mathf.FloorToInt(Random.value * 40 * likelyToBias);
            }


            Vector2? traceTargetPos = null;
            float traceTargetEffectFactor = float.MaxValue;
            float traceTargetDistance = 0f;

            if(noDetectCounter == 0)
            {
                foreach (var v_r in GetDetectablePosAndRad())
                {
                    Vector2 targetPos = new Vector2(v_r.x, v_r.y);
                    float rad = v_r.z;

                    float distance = Vector2.Distance(pos, targetPos);
                    if (distance > traceDetectRad)
                        continue;
                    if (distance < rad)
                    {
                        ExplodeAt(targetPos);
                        return;
                    }

                    float effecetFactor = distance /** (2f - Vector2.Dot((targetPos - pos).normalized, dir))*/;
                    if (effecetFactor < traceTargetEffectFactor)
                    {
                        traceTargetEffectFactor = effecetFactor;
                        traceTargetPos = targetPos;
                        traceTargetDistance = distance;
                    }
                }
            }
            
            hasTarget = traceTargetPos != null;
            if (randomBiasTargetCounter > 0)
            {
                traceTargetPos = randomBiasTarget;
                traceTargetDistance = Vector2.Distance(randomBiasTarget, pos);
            }

            if (traceTargetPos != null)
            {
                Vector2 traceToDir = (traceTargetPos.Value - pos);
                Vector2 perpDir = Custom.DegToVec(Custom.VecToDeg(dir) + 90f);
                float localY = Vector2.Dot(perpDir, traceToDir.normalized);
                float angleDelta = localY > 0f ? 1f : -1f;
                applyAnglarVelDir += angleDelta * Mathf.Clamp01(1f - traceTargetDistance / traceDetectRad);
                //BuffUtils.Log("Missile", $"{traceToDir.x},{traceToDir.y} | {localY}");
            }

            applyAnglarVelDir = Mathf.Clamp(applyAnglarVelDir, -1f, 1f);

            dir = Custom.DegToVec(dirRotation + applyAnglarVelDir * maxAngularVelPerFrame);
            pos += dir * vel / 40f;

            if (room.GetTile(pos).Solid)
            {
                ExplodeAt(pos);
                return;
            }
        }

        public float RayDetectReturnDistance(Vector2 dir)
        {
            Vector2 corner = Custom.RectCollision(pos, pos + dir * 100000f, room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(room, pos, corner);
            if (intVector != null)
            {
                corner = Custom.RectCollision(corner, pos, room.TileRect(intVector.Value)).GetCorner(FloatRect.CornerLabel.D);
            }
            return Vector2.Distance(corner, pos);
        }

        public virtual IEnumerable<Vector3> GetDetectablePosAndRad()
        {
            foreach(var crit in room.updateList.Where((u) => u is Creature).Select((u) => u as Creature))
            {
                if (!ignoreTargets.Contains(crit.abstractCreature.creatureTemplate.type) && !crit.dead)
                    yield return new Vector3(crit.DangerPos.x, crit.DangerPos.y, crit.mainBodyChunk.rad);
            }
        }

        public virtual void ExplodeAt(Vector2 pos)
        {
            Destroy();
        }

        public void Explosion(Explosion explosion)
        {
            if (Vector2.Distance(explosion.pos, pos) < explosion.rad && noDetectCounter == 0)
                ExplodeAt(pos);
        }
    }

    public class TestMissile : Missile, IDrawable
    {
        public TestMissile(Room room, Vector2 launchPos, Vector2 launchDir, float launchVel, float maxAngularVel = 480f, float wallDetectAngle = 35f, int noDetectCounter = 40,float traceDetectRad = 600f, float avoidWallDistance = 150f) : base(room, launchPos, launchDir, launchVel, maxAngularVel, wallDetectAngle, noDetectCounter, traceDetectRad, avoidWallDistance)
        {
            ignoreTargets = new CreatureTemplate.Type[1] { CreatureTemplate.Type.Slugcat };
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Circle20") { scale = 0.5f};
            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("HUD");

            foreach (var sprite in sLeaser.sprites)
                newContatiner.AddChild(sprite);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) - camPos);

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }
    }

    public class SwarmMissile : Missile, IDrawable
    {
        static float maxVel = 360f;
        static int tailPosCount = 5;
        float blink, lastBlink;
        float blinkFreq;
        float counter;
        float damage;
        int life;

        Color blackColor;
        static Color flashCol = Color.green;
        static Color flashColAlphaZero = flashCol.CloneWithNewAlpha(0f);


        List<Vector2> tailPosList = new List<Vector2>(); 

        public SwarmMissile(Room room, Vector2 launchPos, Vector2 launchDir, bool ignorePlayer = false) : base(room, launchPos, launchDir, 80f, 720f, 35f, 10, 600f, 150f)
        {
            if(ignorePlayer)
                ignoreTargets = new CreatureTemplate.Type[1] { CreatureTemplate.Type.Slugcat };
            damage = Mathf.Lerp(0.1f, 0.3f, Random.value);
            for(int i = 0; i < tailPosCount; i++)
                tailPosList.Add(pos);

            life = Random.Range(400, 600);
            counter = Random.value;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(tailPosCount - 1, false, true, "Futile_White");
            sLeaser.sprites[1] = new FSprite("bee", true);
            sLeaser.sprites[2] = new FSprite("pixel", true);


            AddToContainer(sLeaser, rCam, null);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Water");

            foreach (var sprite in sLeaser.sprites)
                newContatiner.AddChild(sprite);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            blackColor = palette.blackColor;
            sLeaser.sprites[2].color = flashCol;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            if(life > 0)
            {
                life--;
                if (life == 0)
                    ExplodeAt(pos);
            }

            if(vel < maxVel)
            {
                vel += 360f / 40f;
                if (vel > maxVel)
                    vel = maxVel;
            }

            tailPosList.Insert(0, pos);
            if (tailPosList.Count > tailPosCount)
                tailPosList.RemoveAt(tailPosCount);

            blinkFreq = hasTarget ? 10f : 1f;
            counter += 1 / 40f * blinkFreq;

            while (counter > 1)
            {
                counter--;
                blink = 1f;
            }

            lastBlink = blink;
            blink = Mathf.Lerp(blink, 0f, 0.25f);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
            sLeaser.sprites[1].SetPosition(vector - camPos);
            sLeaser.sprites[1].rotation = Custom.VecToDeg(dir);
            sLeaser.sprites[2].SetPosition(vector - camPos);

            float smoothBlink = Mathf.Lerp(lastBlink, blink, timeStacker);
            smoothBlink = Mathf.Pow(smoothBlink, 3f);

            //float a = Mathf.Clamp(Mathf.Sin(Mathf.InverseLerp(0f, 0.5f, smoothBlink) * Mathf.PI), 0f, 1f); ;
            sLeaser.sprites[1].color = Color.Lerp(blackColor, flashCol, smoothBlink);
            sLeaser.sprites[2].scaleX = smoothBlink * 20f;

            sLeaser.sprites[2].color = Color.Lerp(flashCol, Color.white, Mathf.Pow(smoothBlink, 2f));
            sLeaser.sprites[2].alpha = smoothBlink;

            var triangleMesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < tailPosCount - 1; i++)
            {
                float width = 1.3f * (1f - i / (float)tailPosCount);

                Vector2 smoothPos = GetSmoothPos(i, timeStacker);
                Vector2 smoothPos2 = GetSmoothPos(i + 1, timeStacker);
                Vector2 v2 = (vector - smoothPos).normalized;
                Vector2 v3 = Custom.PerpendicularVector(v2);
                v2 *= Vector2.Distance(vector, smoothPos2) / 5f;
                triangleMesh.MoveVertice(i * 4, vector - v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 1, vector + v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 2, smoothPos - v3 * width + v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 3, smoothPos + v3 * width + v2 - camPos);

                for (int j = 0; j < 4; j++)
                    triangleMesh.verticeColors[i * 4 + j] = Color.Lerp(flashCol, flashColAlphaZero, 0.3f * (i / (float)tailPosCount) + 0.7f - smoothBlink * 0.3f);

                vector = smoothPos;
            }

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public override void ExplodeAt(Vector2 pos)
        {
            base.ExplodeAt(pos);

            room.AddObject(new SootMark(room, pos, 20f * damage, true));
            room.AddObject(new Explosion(room, null, pos, 7, 50f, 0.5f, damage, Mathf.Floor(10 * damage), 0f, null, 0f, 0, 0.1f));
            room.AddObject(new Explosion.ExplosionLight(pos, 280f * damage, 1f, 7, Color.black));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f * damage, 1f, 3, flashCol));
            //room.AddObject(new ExplosionSpikes(room, p.pos, 14, 30f * damage, 9f, 7f * damage, 170f * damage, Color.black));
            room.AddObject(new ShockWave(pos, 330f * damage, 0.0035f * damage, 5, false));

            room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f * damage);
            if (hasTarget)
            {
                room.PlaySound(SoundID.Bomb_Explode, pos, 0.25f + Random.value * 0.05f, 1f + Random.value * 0.2f + 5f);
                room.PlaySound(SoundID.Gate_Clamp_Lock, pos, 0.05f + Random.value * 0.05f, 50f);
            }
        }

        Vector2 GetSmoothPos(int i, float timeStacker)
        {
            return Vector2.Lerp(GetPos(i + 1), GetPos(i), timeStacker);
        }

        Vector2 GetPos(int i)
        {
            return tailPosList[Custom.IntClamp(i, 0, tailPosList.Count - 1)];
        }
    }
}
