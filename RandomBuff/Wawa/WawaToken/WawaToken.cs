using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RandomBuff.Wawa.ChatConv;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace RandomBuff.Wawa.WawaToken
{
    internal class WawaToken : CosmeticSprite
    {
        static int StalkStartAt = 3;
        ParticleEmitter particleEmitter;
        PlainWhiteCardRenderer cardRenderer;

        float rotY, lastRotY;
        Vector2 floatBias;
        Vector2 playerBias;
        Vector2 hoverPos;

        int timer, convID;
        float emitParticleTimer;
        bool tokenEnabled = true;
        float tokenAlive, lastTokenAlive;
        float tokenFlash, lastTokenFlash;
        float disableExp, lastDisableExp;

        Vector2 tokenPos, tokenLastPos;

        public int CardSprite => 0;

        //stalkPart
        private float headDist = 15f;
        private float coordSeg = 3f;

        public float alive => tokenAlive;
        public int BaseSprite => StalkStartAt;
        public int Arm1Sprite => 1 + StalkStartAt;
        public int Arm2Sprite => 2 + StalkStartAt;
        public int Arm3Sprite => 3 + StalkStartAt;
        public int Arm4Sprite => 4 + StalkStartAt;
        public int Arm5Sprite => 5 + StalkStartAt;
        public int ArmJointSprite => 6 + StalkStartAt;
        public int SocketSprite => 7 + StalkStartAt;
        public int HeadSprite => 8 + StalkStartAt;
        public int LampSprite => 9 + StalkStartAt;
        public int SataFlasher => 10 + StalkStartAt;
        public int TotalSprites => (ModManager.MSC ? 11 : 10) + coord.GetLength(0) + StalkStartAt;


        public CollectToken token;

        public Vector2[,] stalk;

        public Vector2 stalkBasePos;

        public Vector2 mainDir;

        public float flip;

        public Vector2 armPos;

        public Vector2 lastArmPos;

        public Vector2 armVel;

        public Vector2 armGetToPos;

        public Vector2 head;

        public Vector2 lastHead;
        public Vector2 headVel;
        public Vector2 headDir;
        public Vector2 lastHeadDir;
        public float armLength;
        private Vector2[,] coord;
        private float coordLength;

        private float[,] curveLerps;
        private float keepDistance;
        private float sinCounter;
        private float lastSinCounter;
        private float lampPower;
        private float lastLampPower;
        private SharedPhysics.TerrainCollisionData scratchTerrainCollisionData;
        public Color lampColor;
        public bool forceSatellite;
        public int sataFlasherLight;
        private Color lampOffCol;

        public WawaToken(Room room, Vector2 hoverPos, Vector2 basePos, int convID, bool tokenAlreadyRead = false)
        {
            this.room = room;
            this.convID = convID;
            this.tokenEnabled = !tokenAlreadyRead;
            cardRenderer = CardRendererManager.GetPlainWhiteRenderer();
            cardRenderer.Texture.scale = 0.035f;
            //lastPos = pos = hoverPos;
            timer = Random.Range(0, 40);

            
            this.hoverPos = hoverPos;
            this.stalkBasePos = basePos;
            lampPower = 1f;
            lastLampPower = 1f;
            lampColor = Color.white;

            Random.State state = Random.state;
            Random.InitState((int)(hoverPos.x * 10f) + (int)(hoverPos.y * 10f));
            curveLerps = new float[2, 5];
            for (int i = 0; i < curveLerps.GetLength(0); i++)
            {
                curveLerps[i, 0] = 1f;
                curveLerps[i, 1] = 1f;
            }
            curveLerps[0, 3] = Random.value * 360f;
            curveLerps[1, 3] = Mathf.Lerp(10f, 20f, Random.value);
            flip = ((Random.value < 0.5f) ? (-1f) : 1f);
            mainDir = Custom.DirVec(basePos, hoverPos);
            coordLength = Vector2.Distance(basePos, hoverPos) * 0.6f;
            coord = new Vector2[(int)(coordLength / coordSeg), 3];
            armLength = Vector2.Distance(basePos, hoverPos) / 2f;
            armPos = basePos + mainDir * armLength;
            lastArmPos = armPos;
            armGetToPos = armPos;
            for (int j = 0; j < coord.GetLength(0); j++)
            {
                coord[j, 0] = armPos;
                coord[j, 1] = armPos;
            }
            head = hoverPos - mainDir * headDist;
            lastHead = head;
            Random.state = state;

            //EmitterInit();
        }

        void EmitterInit()
        {
            particleEmitter = new ParticleEmitter(room);
            particleEmitter.ApplyParticleSpawn(new CustomRateSpawnerModule(particleEmitter, 50, ParticleSpawnRate));

            particleEmitter.ApplyParticleModule(new AddElement(particleEmitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", alpha: 0.1f)));
            particleEmitter.ApplyParticleModule(new AddElement(particleEmitter, new Particle.SpriteInitParam("pixel", "StormIsApproaching.AdditiveDefault", alpha: 0.3f, constCol: Color.white)));
            particleEmitter.ApplyParticleModule(new SetMoveType(particleEmitter, Particle.MoveType.Global));
            particleEmitter.ApplyParticleModule(new SetRandomLife(particleEmitter, 40 * 4, 40 * 6));
            particleEmitter.ApplyParticleModule(new SetConstColor(particleEmitter, Color.white));
            particleEmitter.ApplyParticleModule(new SetRandomPos(particleEmitter, 10f));
            particleEmitter.ApplyParticleModule(new SetRandomScale(particleEmitter, 2f, 2.5f));
            particleEmitter.ApplyParticleModule(new VelocityOverLife(particleEmitter, (p, l) =>
            {
                return Custom.RNV() * 0.1f + Vector2.down * 0.1f + Custom.DegToVec(360f * p.randomParam1) * Mathf.InverseLerp(0.1f, 0f, l) * 0.5f;
            }));
            particleEmitter.ApplyParticleModule(new SimpleParticlePhysic(particleEmitter, false, true));
            particleEmitter.ApplyParticleModule(new AlphaOverLife(particleEmitter,
               (p, l) =>
               {
                   if (l < 0.2f)
                   {
                       return l * 5f;
                   }
                   else if (l > 0.8f)
                   {
                       float f = 1f - (l - 0.8f) * 5f;
                       return f - Random.value * 0.15f * f;
                   }
                   return 1f - Random.value * 0.15f;
               }));
            ParticleSystem.ApplyEmitterAndInit(particleEmitter);
        }

        float ParticleSpawnRate()
        {
            return alive * 2f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            TokenUpdate();            
            StalkUpdate();
        }

        void TokenUpdate()
        {
            lastRotY = rotY;
            tokenLastPos = tokenPos;
            lastTokenAlive = tokenAlive;
            lastTokenFlash = tokenFlash;

            if (!tokenEnabled)
            {
                lastDisableExp = disableExp;
                if(disableExp < 1f) 
                {
                    disableExp += 1 / 20f;
                }
                if (tokenAlive > 0)
                    tokenAlive -= 1 / 20f;
                return;
            }

            float dist = float.MaxValue;
            Vector2 playerPos = Vector2.zero;

            rotY += 360f / 160f;
            timer++;
            for (int n = 0; n < room.game.session.Players.Count; n++)
            {
                if (room.game.session.Players[n].realizedCreature != null && room.game.session.Players[n].realizedCreature.Consious && (room.game.session.Players[n].realizedCreature as Player).dangerGrasp == null && room.game.session.Players[n].realizedCreature.room == room)
                {
                    float d = Vector2.Distance(room.game.session.Players[n].realizedCreature.mainBodyChunk.pos, hoverPos);
                    if (d < dist)
                    {
                        dist = d;
                        playerPos = room.game.session.Players[n].realizedCreature.mainBodyChunk.pos;
                    }
                }
            }
            tokenFlash = Mathf.Max(0f, tokenFlash - 1 / 20f);
            if (rotY % 180 == 0)
            {
                tokenFlash = 1f;
            }


            bool PlayerInclose = false;
            if (playerPos != Vector2.zero)
            {
                if (Custom.DistLess(playerPos, hoverPos, 60f))
                {
                    Pop();
                }
                if (Custom.DistLess(playerPos, hoverPos, 160f))
                {
                    playerBias = Vector2.Lerp(playerBias, Custom.DirVec(hoverPos, playerPos) * Custom.LerpMap(Vector2.Distance(hoverPos, playerPos), 40f, 160f, 80f, 0f, 0.5f), 0.15f * Random.value);
                    PlayerInclose = true;
                }
            }
            else
            {
                playerBias = Vector2.Lerp(playerBias, Vector2.zero, 0.15f * Random.value);
            }

            
            if (PlayerInclose)
            {
                tokenAlive = Mathf.Min(1f, tokenAlive + 1 / 20f);
                emitParticleTimer += 2 / 40f;
                if(emitParticleTimer > 1f)
                {
                    emitParticleTimer--;
                    room.AddObject(new TokenSpark(room, tokenPos, Custom.RNV() * 0.5f, 160));
                }
            }
            else
            {
                tokenAlive = Mathf.Max(0f, tokenAlive - 1 / 20f);
                tokenFlash = 0f;
            }

            floatBias = new Vector2(0f, 10f * Mathf.Sin(Mathf.PI * 2 * timer / 320f));
            tokenPos = hoverPos + playerBias;
        }

        void StalkUpdate()
        {
            lastArmPos = armPos;
            armPos += armVel;
            armPos = Custom.MoveTowards(armPos, armGetToPos, (0.8f + armLength / 150f) / 2f);
            armVel *= 0.8f;
            armVel += Vector2.ClampMagnitude(armGetToPos - armPos, 4f) / 11f;
            lastHead = head;
            //if (ModManager.MSC && token != null && token.placedObj != null && (token.placedObj.data as CollectToken.CollectTokenData).isWhite && (token.placedObj.data as CollectToken.CollectTokenData).ChatlogCollect != null && (token.placedObj.data as CollectToken.CollectTokenData).ChatlogCollect.Index >= ChatlogData.ChatlogID.Chatlog_Broadcast0.Index && (token.placedObj.data as CollectToken.CollectTokenData).ChatlogCollect.Index <= ChatlogData.ChatlogID.Chatlog_Broadcast19.Index)
            //{
            //    sataFlasherLight += 2;
            //}
            head += headVel;
            headVel *= 0.8f;
            if (token != null && token.slatedForDeletetion)
            {
                token = null;
            }
            lastLampPower = lampPower;
            lastSinCounter = sinCounter;
            sinCounter += UnityEngine.Random.value * lampPower;
            //if (token != null)
            //{
            //    lampPower = Custom.LerpAndTick(lampPower, 1f, 0.02f, 0.016666668f);
            //}
            //else
            //{
            //    lampPower = Mathf.Max(0f, lampPower - 0.008333334f);
            //}

            if (timer % 80 == 0 && tokenEnabled)
                lampPower = 1f;
            lampPower = Mathf.Max(0f, lampPower - 0.008333334f);
            if (!Custom.DistLess(head, armPos, coordLength))
            {
                headVel -= Custom.DirVec(armPos, head) * (Vector2.Distance(armPos, head) - coordLength) * 0.8f;
                head -= Custom.DirVec(armPos, head) * (Vector2.Distance(armPos, head) - coordLength) * 0.8f;
            }
            headVel += Helper.Vec3ToVec2(Vector3.Slerp(Custom.DegToVec(StalkGetCurveLerp(0, 0.5f, 1f)), new Vector2(0f, 1f), 0.4f)) * 0.4f;
            lastHeadDir = headDir;
            Vector2 vector = hoverPos;
            if (true)
            {
                vector = Vector2.Lerp(hoverPos, tokenPos, alive);
            }
            headVel -= Custom.DirVec(vector, head) * (Vector2.Distance(vector, head) - headDist) * 0.8f;
            head -= Custom.DirVec(vector, head) * (Vector2.Distance(vector, head) - headDist) * 0.8f;
            headDir = Custom.DirVec(head, vector);
            if (UnityEngine.Random.value < 1f / Mathf.Lerp(300f, 60f, alive))
            {
                Vector2 b = stalkBasePos + mainDir * armLength * 0.7f + Custom.RNV() * UnityEngine.Random.value * armLength * Mathf.Lerp(0.1f, 0.3f, alive);
                if (SharedPhysics.RayTraceTilesForTerrain(room, armGetToPos, b))
                {
                    armGetToPos = b;
                }
                StalkNewCurveLerp(0, curveLerps[0, 3] + Mathf.Lerp(-180f, 180f, UnityEngine.Random.value), Mathf.Lerp(1f, 2f, alive));
                StalkNewCurveLerp(1, Mathf.Lerp(10f, 20f, Mathf.Pow(UnityEngine.Random.value, 0.75f)), Mathf.Lerp(0.4f, 0.8f, alive));
            }
            headDist = StalkGetCurveLerp(1, 0.5f, 1f);
            if (token != null)
            {
                keepDistance = Custom.LerpAndTick(keepDistance, Mathf.Sin(Mathf.Clamp01(token.glitch) * 3.1415927f) * alive, 0.006f, alive / ((keepDistance < token.glitch) ? 40f : 80f));
            }
            headDist = Mathf.Lerp(headDist, 50f, Mathf.Pow(keepDistance, 0.5f));
            Vector2 vector2 = Custom.DirVec(Custom.InverseKinematic(stalkBasePos, armPos, armLength * 0.65f, armLength * 0.35f, flip), armPos);
            for (int i = 0; i < coord.GetLength(0); i++)
            {
                float num = Mathf.InverseLerp(-1f, (float)coord.GetLength(0), (float)i);
                Vector2 vector3 = Custom.Bezier(armPos, armPos + vector2 * coordLength * 0.5f, head, head - headDir * coordLength * 0.5f, num);
                coord[i, 1] = coord[i, 0];
                coord[i, 0] += coord[i, 2];
                coord[i, 2] *= 0.8f;
                coord[i, 2] += (vector3 - coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                coord[i, 0] += (vector3 - coord[i, 0]) * Mathf.Lerp(0f, 0.25f, Mathf.Sin(num * 3.1415927f));
                if (i > 2)
                {
                    coord[i, 2] += Custom.DirVec(coord[i - 2, 0], coord[i, 0]);
                    coord[i - 2, 2] -= Custom.DirVec(coord[i - 2, 0], coord[i, 0]);
                }
                if (i > 3)
                {
                    coord[i, 2] += Custom.DirVec(coord[i - 3, 0], coord[i, 0]) * 0.5f;
                    coord[i - 3, 2] -= Custom.DirVec(coord[i - 3, 0], coord[i, 0]) * 0.5f;
                }
                if (num < 0.5f)
                {
                    coord[i, 2] += vector2 * Mathf.InverseLerp(0.5f, 0f, num) * Mathf.InverseLerp(5f, 0f, (float)i);
                }
                else
                {
                    coord[i, 2] -= headDir * Mathf.InverseLerp(0.5f, 1f, num);
                }
            }
            StalkConnectCoord();
            StalkConnectCoord();
            for (int j = 0; j < coord.GetLength(0); j++)
            {
                SharedPhysics.TerrainCollisionData cd = scratchTerrainCollisionData.Set(coord[j, 0], coord[j, 1], coord[j, 2], 2f, new IntVector2(0, 0), true);
                cd = SharedPhysics.HorizontalCollision(room, cd);
                cd = SharedPhysics.VerticalCollision(room, cd);
                coord[j, 0] = cd.pos;
                coord[j, 2] = cd.vel;
            }
            for (int k = 0; k < curveLerps.GetLength(0); k++)
            {
                curveLerps[k, 1] = curveLerps[k, 0];
                curveLerps[k, 0] = Mathf.Min(1f, curveLerps[k, 0] + curveLerps[k, 4]);
            }
        }

        public void Pop()
        {
            tokenEnabled = false;
            rotY = 0f;
            //lastTokenAlive = tokenAlive = 0f;

            room.AddObject(new WawaChatHUD(room, WawaChatLoader.convs[convID]));
            room.PlaySound(SoundID.Token_Collect, hoverPos, 1f, 1f);

            for (int i = 0;i < 20; i++)
            {
                room.AddObject(new TokenSpark(room, tokenPos, Custom.RNV() * (2f + 4f * Random.value), 40));
            }
            room.AddObject(new ShockWave(pos, 1300f, 0.1f, 40));
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            for (int i = StalkStartAt; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].color = palette.blackColor;
            }
            lampOffCol = Color.Lerp(palette.blackColor, new Color(1f, 1f, 1f), 0.15f);
        }


        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[TotalSprites];

            sLeaser.sprites[0] = cardRenderer.Texture;
            //sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["Hologram"];

            sLeaser.sprites[1] = new FSprite("buffinfos\\BuiltinBuffs\\cardinfos\\positive\\lancethrower\\lancethrowerspark", true);
            sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];

            sLeaser.sprites[2] = new FSprite(BuffUIAssets.CircleGradient20, true);
            sLeaser.sprites[2].shader = rCam.room.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];

            sLeaser.sprites[this.BaseSprite] = new FSprite("Circle20", true);
            sLeaser.sprites[BaseSprite].scaleX = 0.5f;
            sLeaser.sprites[BaseSprite].scaleY = 0.7f;
            sLeaser.sprites[BaseSprite].rotation = Custom.VecToDeg(mainDir);
            sLeaser.sprites[Arm1Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm1Sprite].scaleX = 4f;
            sLeaser.sprites[Arm1Sprite].anchorY = 0f;
            sLeaser.sprites[Arm2Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm2Sprite].scaleX = 3f;
            sLeaser.sprites[Arm2Sprite].anchorY = 0f;
            sLeaser.sprites[Arm3Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm3Sprite].scaleX = 1.5f;
            sLeaser.sprites[Arm3Sprite].scaleY = armLength * 0.6f;
            sLeaser.sprites[Arm3Sprite].anchorY = 0f;
            sLeaser.sprites[Arm4Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm4Sprite].scaleX = 3f;
            sLeaser.sprites[Arm4Sprite].scaleY = 8f;
            sLeaser.sprites[Arm5Sprite] = new FSprite("pixel", true);
            sLeaser.sprites[Arm5Sprite].scaleX = 6f;
            sLeaser.sprites[Arm5Sprite].scaleY = 8f;
            sLeaser.sprites[ArmJointSprite] = new FSprite("JetFishEyeA", true);
            sLeaser.sprites[LampSprite] = new FSprite("tinyStar", true);
            sLeaser.sprites[SocketSprite] = new FSprite("pixel", true);
            sLeaser.sprites[SocketSprite].scaleX = 5f;
            sLeaser.sprites[SocketSprite].scaleY = 9f;
            if (true)
            {
                sLeaser.sprites[HeadSprite] = new FSprite("MiniSatellite", true);
                sLeaser.sprites[SataFlasher] = new FSprite("Futile_White", true);
                sLeaser.sprites[SataFlasher].shader = rCam.room.game.rainWorld.Shaders["FlatLight"];
                sLeaser.sprites[SataFlasher].scale = 0.8f;
                sLeaser.sprites[SataFlasher].isVisible = false;
            }
            else
            {
                sLeaser.sprites[HeadSprite] = new FSprite("pixel", true);
                sLeaser.sprites[HeadSprite].scaleX = 4f;
                sLeaser.sprites[HeadSprite].scaleY = 6f;
                if (ModManager.MSC)
                {
                    sLeaser.sprites[SataFlasher] = new FSprite("pixel", true);
                    sLeaser.sprites[SataFlasher].isVisible = false;
                }
            }
            for (int i = 0; i < coord.GetLength(0); i++)
            {
                sLeaser.sprites[StalkCoordSprite(i)] = new FSprite("pixel", true);
                sLeaser.sprites[StalkCoordSprite(i)].scaleX = ((i % 2 == 0) ? 2f : 3f);
                sLeaser.sprites[StalkCoordSprite(i)].scaleY = 5f;
            }
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            FContainer tokenContainer = rCam.ReturnFContainer("Midground");
            FContainer effectContainer = rCam.ReturnFContainer("Water");

            for(int i  = 0; i < sLeaser.sprites.Length; i++)
            {
                sLeaser.sprites[i].RemoveFromContainer();
                if(i < StalkStartAt)
                {
                    effectContainer.AddChild(sLeaser.sprites[i]);
                }
                else
                    tokenContainer.AddChild(sLeaser.sprites[i]);
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
                CleanRenderer();
            }
            if (slatedForDeletetion)
                return;
            TokenDrawSprites(sLeaser, rCam, timeStacker, camPos);
            StalkDrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        void TokenDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            float rY = Mathf.Lerp(lastRotY, rotY, timeStacker);
            float smoothAlive = Helper.LerpEase(Mathf.Lerp(lastTokenAlive, tokenAlive, timeStacker));
            Vector2 pos = Vector2.Lerp(tokenLastPos, tokenPos, timeStacker) - camPos;

            sLeaser.sprites[0].SetPosition(pos);
            cardRenderer.Rotation = new Vector2(0f, rY);
            sLeaser.sprites[0].scale = smoothAlive * 0.035f;
            sLeaser.sprites[0].alpha = smoothAlive;
            sLeaser.sprites[0].isVisible = smoothAlive > 0f;

            float smoothFlash = Helper.EaseOutElastic(Mathf.Lerp(lastTokenFlash, tokenFlash, timeStacker)) * smoothAlive;
            smoothFlash -= Random.value * smoothFlash * 0.15f;
            sLeaser.sprites[1].alpha = smoothFlash *  0.5f;
            sLeaser.sprites[1].scale = 1f * smoothFlash;
            sLeaser.sprites[1].isVisible = smoothAlive > 0f;
            sLeaser.sprites[1].SetPosition(pos + Vector2.right * 3f);

            sLeaser.sprites[2].alpha = smoothFlash * 0.5f;
            sLeaser.sprites[2].scale = 2f * smoothFlash;
            sLeaser.sprites[2].isVisible = smoothAlive > 0f;
            sLeaser.sprites[2].SetPosition(pos);
        }

        void StalkDrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            sLeaser.sprites[this.BaseSprite].x = stalkBasePos.x - camPos.x;
            sLeaser.sprites[BaseSprite].y = stalkBasePos.y - camPos.y;
            Vector2 vector = Vector2.Lerp(lastHead, head, timeStacker);
            Vector2 v = Vector3.Slerp(lastHeadDir, headDir, timeStacker);
            Vector2 vector2 = Vector2.Lerp(lastArmPos, armPos, timeStacker);
            Vector2 vector3 = Custom.InverseKinematic(stalkBasePos, vector2, armLength * 0.65f, armLength * 0.35f, flip);
            sLeaser.sprites[Arm1Sprite].x = stalkBasePos.x - camPos.x;
            sLeaser.sprites[Arm1Sprite].y = stalkBasePos.y - camPos.y;
            sLeaser.sprites[Arm1Sprite].scaleY = Vector2.Distance(stalkBasePos, vector3);
            sLeaser.sprites[Arm1Sprite].rotation = Custom.AimFromOneVectorToAnother(stalkBasePos, vector3);
            sLeaser.sprites[Arm2Sprite].x = vector3.x - camPos.x;
            sLeaser.sprites[Arm2Sprite].y = vector3.y - camPos.y;
            sLeaser.sprites[Arm2Sprite].scaleY = Vector2.Distance(vector3, vector2);
            sLeaser.sprites[Arm2Sprite].rotation = Custom.AimFromOneVectorToAnother(vector3, vector2);
            sLeaser.sprites[SocketSprite].x = vector2.x - camPos.x;
            sLeaser.sprites[SocketSprite].y = vector2.y - camPos.y;
            sLeaser.sprites[SocketSprite].rotation = Custom.VecToDeg(Vector3.Slerp(Custom.DirVec(vector3, vector2), Custom.DirVec(vector2, Vector2.Lerp(coord[0, 1], coord[0, 0], timeStacker)), 0.4f));
            Vector2 p = Vector2.Lerp(stalkBasePos, vector3, 0.3f);
            Vector2 p2 = Vector2.Lerp(vector3, vector2, 0.4f);
            sLeaser.sprites[Arm3Sprite].x = p.x - camPos.x;
            sLeaser.sprites[Arm3Sprite].y = p.y - camPos.y;
            sLeaser.sprites[Arm3Sprite].rotation = Custom.AimFromOneVectorToAnother(p, p2);
            sLeaser.sprites[Arm4Sprite].x = p2.x - camPos.x;
            sLeaser.sprites[Arm4Sprite].y = p2.y - camPos.y;
            sLeaser.sprites[Arm4Sprite].rotation = Custom.AimFromOneVectorToAnother(p, p2);
            p += Custom.DirVec(stalkBasePos, vector3) * (armLength * 0.1f + 2f);
            sLeaser.sprites[Arm5Sprite].x = p.x - camPos.x;
            sLeaser.sprites[Arm5Sprite].y = p.y - camPos.y;
            sLeaser.sprites[Arm5Sprite].rotation = Custom.AimFromOneVectorToAnother(stalkBasePos, vector3);
            sLeaser.sprites[LampSprite].x = p.x - camPos.x;
            sLeaser.sprites[LampSprite].y = p.y - camPos.y;
            sLeaser.sprites[LampSprite].color = Color.Lerp(lampOffCol, lampColor, Mathf.Lerp(lastLampPower, lampPower, timeStacker) * Mathf.Pow(UnityEngine.Random.value, 0.5f) * (0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(lastSinCounter, sinCounter, timeStacker) / 6f)));
            sLeaser.sprites[ArmJointSprite].x = vector3.x - camPos.x;
            sLeaser.sprites[ArmJointSprite].y = vector3.y - camPos.y;
            sLeaser.sprites[HeadSprite].x = vector.x - camPos.x;
            sLeaser.sprites[HeadSprite].y = vector.y - camPos.y;
            if (ModManager.MSC && forceSatellite && sLeaser.sprites[HeadSprite].element.name != "MiniSatellite")
            {
                sLeaser.sprites[HeadSprite].SetElementByName("MiniSatellite");
                sLeaser.sprites[HeadSprite].scaleX = 1f;
                sLeaser.sprites[HeadSprite].scaleY = 1f;
            }
            if (ModManager.MSC && ((true) || forceSatellite))
            {
                sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(v) - 90f;
                if (sataFlasherLight >= 99)
                {
                    sLeaser.sprites[SataFlasher].isVisible = !sLeaser.sprites[SataFlasher].isVisible;
                    sataFlasherLight = 0;
                }
                sLeaser.sprites[SataFlasher].color = Color.Lerp(Color.white, lampOffCol, UnityEngine.Random.value * 0.1f);
                sLeaser.sprites[SataFlasher].alpha = 0.9f + UnityEngine.Random.value * 0.09f;
                sLeaser.sprites[SataFlasher].x = vector.x + v.x * 5f - camPos.x;
                sLeaser.sprites[SataFlasher].y = vector.y + v.y * 5f - camPos.y;
            }
            else
            {
                sLeaser.sprites[HeadSprite].rotation = Custom.VecToDeg(v);
                if (ModManager.MSC)
                {
                    sLeaser.sprites[SataFlasher].isVisible = false;
                }
            }
            Vector2 p3 = vector2;
            for (int i = 0; i < coord.GetLength(0); i++)
            {
                Vector2 vector4 = Vector2.Lerp(coord[i, 1], coord[i, 0], timeStacker);
                sLeaser.sprites[StalkCoordSprite(i)].x = vector4.x - camPos.x;
                sLeaser.sprites[StalkCoordSprite(i)].y = vector4.y - camPos.y;
                sLeaser.sprites[StalkCoordSprite(i)].rotation = Custom.AimFromOneVectorToAnother(p3, vector4);
                p3 = vector4;
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            CleanRenderer();
        }

        private float StalkGetCurveLerp(int curveLerp, float sCurveK, float timeStacker)
        {
            return Mathf.Lerp(curveLerps[curveLerp, 2], curveLerps[curveLerp, 3], Custom.SCurve(Mathf.Lerp(curveLerps[curveLerp, 1], curveLerps[curveLerp, 0], timeStacker), sCurveK));
        }

        public int StalkCoordSprite(int s)
        {
            return (ModManager.MSC ? 11 : 10) + s + StalkStartAt;
        }


        private void StalkConnectCoord()
        {
            coord[0, 2] -= Custom.DirVec(armPos, coord[0, 0]) * (Vector2.Distance(armPos, coord[0, 0]) - coordSeg);
            coord[0, 0] -= Custom.DirVec(armPos, coord[0, 0]) * (Vector2.Distance(armPos, coord[0, 0]) - coordSeg);
            for (int i = 1; i < coord.GetLength(0); i++)
            {
                if (!Custom.DistLess(coord[i - 1, 0], coord[i, 0], coordSeg))
                {
                    Vector2 vector = Custom.DirVec(coord[i, 0], coord[i - 1, 0]) * (Vector2.Distance(coord[i - 1, 0], coord[i, 0]) - coordSeg);
                    coord[i, 2] += vector * 0.5f;
                    coord[i, 0] += vector * 0.5f;
                    coord[i - 1, 2] -= vector * 0.5f;
                    coord[i - 1, 0] -= vector * 0.5f;
                }
            }
            coord[coord.GetLength(0) - 1, 2] -= Custom.DirVec(head, coord[coord.GetLength(0) - 1, 0]) * (Vector2.Distance(head, coord[coord.GetLength(0) - 1, 0]) - coordSeg);
            coord[coord.GetLength(0) - 1, 0] -= Custom.DirVec(head, coord[coord.GetLength(0) - 1, 0]) * (Vector2.Distance(head, coord[coord.GetLength(0) - 1, 0]) - coordSeg);
        }
        private void StalkNewCurveLerp(int curveLerp, float to, float speed)
        {
            if (curveLerps[curveLerp, 0] < 1f || curveLerps[curveLerp, 1] < 1f)
            {
                return;
            }
            curveLerps[curveLerp, 2] = curveLerps[curveLerp, 3];
            curveLerps[curveLerp, 3] = to;
            curveLerps[curveLerp, 4] = speed / Mathf.Abs(curveLerps[curveLerp, 2] - curveLerps[curveLerp, 3]);
            curveLerps[curveLerp, 0] = 0f;
            curveLerps[curveLerp, 1] = 0f;
        }

        void CleanRenderer()
        {
            if (cardRenderer == null)
                return;
            CardRendererManager.RecycleCardRenderer(cardRenderer);
            cardRenderer = null;
        }
    }

    public class TokenSpark : CosmeticSprite
    {
        int life, lastLife, initLife;
        
        public TokenSpark(Room room, Vector2 pos, Vector2 vel, int life)
        {
            this.lastPos = this.pos = pos;
            this.vel = vel;
            this.lastLife = this.life = this.initLife = life;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            if (lastLife == life && life == 0)
                Destroy();

            lastLife = life;
            if (life > 0)
                life--;

            vel *= 0.95f;
            vel += Custom.RNV() * 0.02f;
            vel += Vector2.down * 0.01f;
            if (room.GetTile(pos).Solid)
                Destroy();
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("pixel", true) { scale = 2f};
            sLeaser.sprites[1] = new FSprite(BuffUIAssets.CircleGradient20, true) { scale = 0.5f };

            sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];
            sLeaser.sprites[1].shader = rCam.room.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Bloom"));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;

            float l = Mathf.Lerp(lastLife, life, timeStacker) / initLife;
            if (l < 0.2f)
            {
                l *= 5f;
            }
            else if (l < 0.8f)
            {
                l = 1f;
            }
            else
                l = 1f - (l - 0.8f) * 5f;
            l = Helper.EaseInOutCubic(l);
            l = Mathf.Max(0f, l - Random.value * 0.25f);

            for(int i = 0;i < 2;i++)
            {
                sLeaser.sprites[i].alpha = 0.2f * l;
                sLeaser.sprites[i].SetPosition(smoothPos);
            }
        }
    }
}
