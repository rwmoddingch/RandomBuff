using RandomBuff.Render.UI.Component;
using RandomBuffUtils;
using RandomBuffUtils.Simple3D;
using System.Collections.Generic;
using UnityEngine;
using static RandomBuffUtils.Simple3D.AnimationTimeLine;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class HoloScoreCosmetic : CosmeticUnlock, PlayerUtils.IOWnPlayerUtilsPart
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.HoloScore;

        public override string IconElement => "BuffCosmetic_HoloScore";

        public override SlugcatStats.Name BindCat => SlugcatStats.Name.Red;

        static HoloScoreCosmetic()
        {
            On.SocialEventRecognizer.Killing += SocialEventRecognizer_Killing;
        }

        private static void SocialEventRecognizer_Killing(On.SocialEventRecognizer.orig_Killing orig, SocialEventRecognizer self, Creature killer, Creature victim)
        {
            orig.Invoke(self, killer, victim);
            if (killer is Player player && player.slugcatStats.name == SlugcatStats.Name.Red)
            {
                Room room = player.room;
                if(room == null)
                    room = victim.room;
                if (room == null)
                    return;

                if(PlayerUtils.TryGetModulePart<HoloScoreModule ,HoloScoreCosmetic>(player, out var module))
                {
                    room.AddObject(new HoloScore(room, victim, module.kills));
                    module.kills++;
                }
            }
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(this);
        }


        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            return new HoloScoreModule();
        }

        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }

        public class HoloScoreModule : PlayerUtils.PlayerModulePart
        {
            public int kills;

            public override void Update(Player player, bool eu)
            {
                base.Update(player, eu);
                if (Input.GetKeyDown(KeyCode.W))
                {
                    player.room.AddObject(new HoloScore(player.room, player));
                }
            }
        }
    
        public class HoloScore : CosmeticSprite, BuffCardTimer.IOwnBuffTimer
        {
            public static Color FrontCol = Color.green;
            public static Color BackCol = new Color(0, 0.8f, 0f, 0.8f);

            RandomBuffUtils.Simple3D.AnimationTimeLine timeLine;

            //固定部分
            Mesh3DFrameRenderer pyramidUp;
            Mesh3DFrameRenderer pyramidDown;

            Mesh3DDotMatrixRenderer insideDotMatrix;

            BuffCountDisplay countDisplay;
            FContainer countDisplayCountainer;

            string killIcon;
            int killIconSpriteIndex;

            int lightSpriteIndex;

            int killCount;
            bool addKillCount;
            public int Second => killCount;

            //可变部分
            int smallDiamondsCount;
            List<Mesh3DFrameRenderer> smallDiamonds = new List<Mesh3DFrameRenderer>();
            List<Mesh3DFrameRenderer> smallTriangles = new List<Mesh3DFrameRenderer>();
            int totSpriteCount = 0;

            float killIconScaleX = 0f;
            float lastKillIconScaleX = 0f;

            float lightAlpha;

            public HoloScore(Room room, Creature creature, int currentCount = 1)
            {
                this.room = room;
                pos = lastPos = creature.DangerPos;
                this.killCount = currentCount;
                killIcon = CreatureSymbol.SpriteNameOfCreature(new IconSymbol.IconSymbolData(creature.abstractCreature.creatureTemplate.type, AbstractPhysicalObject.AbstractObjectType.Creature, 0));

                timeLine = new AnimationTimeLine(400, false);

                smallDiamondsCount = Random.Range(4, 8);

                CreateFixedElements();
                InitFixedElementAnims();

                CreateRandomElements();
                InitRandomElementAnimations();

                timeLine.enable = true;
                BuffPlugin.Log($"HoloScore init");
            }

            void CreateFixedElements()
            {
                killIconSpriteIndex = totSpriteCount;
                totSpriteCount++;
                lightSpriteIndex = totSpriteCount;
                totSpriteCount++;

                pyramidUp = new Mesh3DFrameRenderer(MeshHelper.CreatePyramid(new Vector3(40f, 40f, 40f)), totSpriteCount) { shader = "Hologram" };
                pyramidUp.SetVerticeColor(FrontCol, true);
                pyramidUp.SetVerticeColor(BackCol, false);
                pyramidUp.maxZ = 20f;
                pyramidUp.minZ = -20f;
                totSpriteCount += pyramidUp.totalSprites;
               
                pyramidDown = new Mesh3DFrameRenderer(MeshHelper.CreatePyramid(new Vector3(40f, -40f, 40f)), totSpriteCount) { shader = "Hologram" };
                pyramidDown.SetVerticeColor(FrontCol, true);
                pyramidDown.SetVerticeColor(BackCol, false);
                totSpriteCount += pyramidDown.totalSprites;

                countDisplayCountainer = new FContainer();
                countDisplay = new BuffCountDisplay(countDisplayCountainer, this, FrontCol, "Hologram") { scale = 1.5f };
                countDisplay.HardSet();

                insideDotMatrix = new Mesh3DDotMatrixRenderer(MeshHelper.Create6x6DotMatrixMesh(3f), totSpriteCount, 1f) { shader = "Hologram"};
                insideDotMatrix.SetVerticeColor(FrontCol, true);
                insideDotMatrix.SetVerticeColor(BackCol, false);
                totSpriteCount += insideDotMatrix.totalSprites;
            }

            void InitFixedElementAnims()
            {
                var pyramidLocalRotationYTrack = new AnimationTimeLine.Track<float>(timeLine, (f) =>
                {
                    pyramidUp.mesh.localRotation = new Vector3(pyramidUp.mesh.localRotation.x, f, pyramidUp.mesh.localRotation.z);
                    pyramidDown.mesh.localRotation = new Vector3(pyramidDown.mesh.localRotation.x, -f, pyramidDown.mesh.localRotation.z);
                },
               (current, next, t) => Mathf.Lerp(current, next, t), 0f);
                pyramidLocalRotationYTrack.AddKeyFrame(400, 360 * 10);
                timeLine.tracks.Add(pyramidLocalRotationYTrack);

                var pyramidScaleTrack = new AnimationTimeLine.Track<float>(timeLine,
                    (f) =>
                    {
                        pyramidUp.mesh.scale = f;
                        pyramidDown.mesh.scale = f;
                    },
                    (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                pyramidScaleTrack.AddKeyFrame(40, 0.4f);
                pyramidScaleTrack.AddKeyFrame(360, 0.4f);
                pyramidScaleTrack.AddKeyFrame(400, 0f);
                timeLine.tracks.Add(pyramidScaleTrack);

                var pyramidPosSpreadTrack = new AnimationTimeLine.Track<float>(timeLine,
                    (f) =>
                    {
                        for(int i = 0;i < pyramidUp.mesh.origVertices.Count; i++)
                        {
                            pyramidUp.mesh.position = Vector3.up * f;
                            pyramidDown.mesh.position = Vector3.down * f;
                        }
                    },
                    (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                pyramidPosSpreadTrack.AddKeyFrame(40, 0f);
                pyramidPosSpreadTrack.AddKeyFrame(60, 10f);
                pyramidPosSpreadTrack.AddKeyFrame(80, 20f);
                pyramidPosSpreadTrack.AddKeyFrame(120, 20f);
                pyramidPosSpreadTrack.AddKeyFrame(140, 5f);
                pyramidPosSpreadTrack.AddKeyFrame(180, 20f);
                pyramidPosSpreadTrack.AddKeyFrame(400, 20f);
                timeLine.tracks.Add(pyramidPosSpreadTrack);

                var killIconScaleXTrack = new AnimationTimeLine.Track<float>(timeLine,
                    (f) =>
                    {
                        lastKillIconScaleX = killIconScaleX;
                        killIconScaleX = f;
                    },
                    (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                killIconScaleXTrack.AddKeyFrame(60, 0f);
                killIconScaleXTrack.AddKeyFrame(80, 1f);
                killIconScaleXTrack.AddKeyFrame(120, 1f);
                killIconScaleXTrack.AddKeyFrame(130, 0f);
                killIconScaleXTrack.AddKeyFrame(400, 0f);
                timeLine.tracks.Add(killIconScaleXTrack);

                var killCountAlphaTrack = new AnimationTimeLine.Track<float>(timeLine,
                   (f) =>
                   {
                       countDisplay.alpha = f;
                       if(f == 1f && !addKillCount)
                       {
                           killCount++;
                           addKillCount = true;
                       }
                   },
                   (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                killCountAlphaTrack.AddKeyFrame(140, 0f);
                killCountAlphaTrack.AddKeyFrame(180, 0.99f);
                killCountAlphaTrack.AddKeyFrame(240, 1f);
                killCountAlphaTrack.AddKeyFrame(360, 1f);
                killCountAlphaTrack.AddKeyFrame(400, 0f);
                timeLine.tracks.Add(killCountAlphaTrack);


                var lightAlphaTrack = new AnimationTimeLine.Track<float>(timeLine,
                   (f) =>
                   {
                       lightAlpha = f;
                   },
                   (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                lightAlphaTrack.AddKeyFrame(40, 0.5f);
                lightAlphaTrack.AddKeyFrame(360, 0.5f);
                lightAlphaTrack.AddKeyFrame(400, 0f);
                timeLine.tracks.Add(lightAlphaTrack);


                var matrixScaleTrack = new AnimationTimeLine.Track<float>(timeLine,
                  (f) =>
                  {
                      insideDotMatrix.mesh.scale = f;
                  },
                  (current, next, t) => Mathf.Lerp(current, next, t), 0f)
                {
                    easeFunction = Helper.EaseInOutCubic
                };
                matrixScaleTrack.AddKeyFrame(20, 1f);
                matrixScaleTrack.AddKeyFrame(40, 1f);
                matrixScaleTrack.AddKeyFrame(60, 0f);
                timeLine.tracks.Add(matrixScaleTrack);

                var matrixLocalRotationYTrack = new AnimationTimeLine.Track<float>(timeLine, (f) =>
                {
                    insideDotMatrix.mesh.localRotation = new Vector3(f*2f, f, pyramidUp.mesh.localRotation.z);
                },
              (current, next, t) => Mathf.Lerp(current, next, t), 0f);
                matrixLocalRotationYTrack.AddKeyFrame(60, 180);
                timeLine.tracks.Add(matrixLocalRotationYTrack);
            }

            void CreateRandomElements()
            {
                for (int i = 0; i < smallDiamondsCount; i++)
                {
                    var smallDiamond = MeshHelper.CreateDiamond(new Vector2(3f, 6f));

                    var smallDiamondMeshRenderer = new Mesh3DFrameRenderer(smallDiamond, totSpriteCount, 1f) { autoCaculateZ = false };
                    totSpriteCount += smallDiamondMeshRenderer.totalSprites;
                    smallDiamondMeshRenderer.shader = "Hologram";

                    smallDiamondMeshRenderer.SetVerticeColor(FrontCol, true);
                    smallDiamondMeshRenderer.SetVerticeColor(BackCol, false);

                    smallDiamond.scale = 0f;
                    smallDiamond.globalRotation = new Vector3(0f, Random.value * 120f, Random.value * 120f - 60f);
                    smallDiamondMeshRenderer.maxZ = 40f;
                    smallDiamondMeshRenderer.minZ = -40f;

                    smallDiamonds.Add(smallDiamondMeshRenderer);
                }
            }

            void InitRandomElementAnimations()
            {
                for (int i = 0; i < smallDiamondsCount; i++)
                {
                    int localIndex = i;
                    var scaleTrack = new Track<float>(
                        timeLine,
                        (t) => smallDiamonds[localIndex].mesh.scale = t,
                        (current, next, t) => Mathf.Lerp(current, next, t),
                        0f
                    )
                    {
                        easeFunction = Helper.EaseInOutCubic
                    };

                    scaleTrack.AddKeyFrame(40 + i, 0f);
                    scaleTrack.AddKeyFrame(40 + i + 20, 1f);
                    scaleTrack.AddKeyFrame(370 - i * 5, 1f);
                    scaleTrack.AddKeyFrame(370 - i * 5 + 30, 0f);
                    scaleTrack.AddKeyFrame(400, 0f);
                    timeLine.tracks.Add(scaleTrack);

                    var positionTrack = new Track<float>(
                        timeLine,
                        (t) => smallDiamonds[localIndex].mesh.position = new Vector3(t, 0f, 0f),
                        (current, next, t) => Mathf.Lerp(current, next, t),
                        0f
                    )
                    {
                        easeFunction = Helper.EaseInOutCubic
                    };

                    positionTrack.AddKeyFrame(40 + i, 0f);
                    positionTrack.AddKeyFrame(40 + i + 20, 25f + i * 2);
                    positionTrack.AddKeyFrame(580 - i * 5, 25f + i * 2);
                    positionTrack.AddKeyFrame(580 - i * 5 + 30, 0f);
                    scaleTrack.AddKeyFrame(820, 0f);
                    timeLine.tracks.Add(positionTrack);
                }
            }

            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                sLeaser.sprites = new FSprite[totSpriteCount];
                sLeaser.containers = new FContainer[1];
                sLeaser.containers[0] = countDisplayCountainer;


                sLeaser.sprites[killIconSpriteIndex] = new FSprite(killIcon) { color = FrontCol , scaleX = 0f, shader = rCam.game.rainWorld.Shaders["Hologram"] };
                sLeaser.sprites[lightSpriteIndex] = new FSprite("Futile_White") { color = FrontCol, scale = 5f, shader = rCam.game.rainWorld.Shaders["FlatLight"] };

                pyramidUp.InitSprites(sLeaser, rCam);
                pyramidDown.InitSprites(sLeaser, rCam);
                insideDotMatrix.InitSprites(sLeaser, rCam);

                for (int i = 0;i < smallDiamondsCount;i++)
                    smallDiamonds[i].InitSprites(sLeaser, rCam);

                //insideDotMatrix.InitSprites(sLeaser, rCam);
                BuffPlugin.Log($"HoloScore InitiateSprites");
                AddToContainer(sLeaser, rCam, null);
            }

            public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
                newContatiner = rCam.ReturnFContainer("HUD");

                foreach (var fSprite in sLeaser.sprites)
                {
                    fSprite.RemoveFromContainer();
                    newContatiner.AddChild(fSprite);
                }

                pyramidUp.AddToContainer(sLeaser, newContatiner);
                pyramidDown.AddToContainer(sLeaser, newContatiner);
                insideDotMatrix.AddToContainer(sLeaser, newContatiner);
                for (int i = 0; i < smallDiamondsCount; i++)
                    smallDiamonds[i].AddToContainer(sLeaser, newContatiner);
                newContatiner.AddChild(countDisplayCountainer);
                //insideDotMatrix.AddToContainer(sLeaser, newContatiner);

                BuffPlugin.Log($"HoloScore AddToContainer");
            }

            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                if (slatedForDeletetion)
                    return;
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

                Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
                float smoothScaleX = Mathf.Lerp(lastKillIconScaleX, killIconScaleX, timeStacker);

                sLeaser.sprites[killIconSpriteIndex].SetPosition(smoothPos - camPos);
                sLeaser.sprites[killIconSpriteIndex].scaleX = smoothScaleX;

                sLeaser.sprites[lightSpriteIndex].SetPosition(smoothPos - camPos);
                sLeaser.sprites[lightSpriteIndex].alpha = lightAlpha;

                pyramidUp.DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothPos);
                pyramidDown.DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothPos);
                insideDotMatrix.DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothPos);

                countDisplay.GrafUpdate(timeStacker);
                countDisplay.pos = pos - camPos;

                for (int i = 0; i < smallDiamondsCount; i++)
                    smallDiamonds[i].DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothPos);

                //insideDotMatrix.DrawSprites(sLeaser, rCam, timeStacker, camPos, smoothPos);
            }

            public override void Update(bool eu)
            {
                base.Update(eu);
                timeLine.Update();

                pyramidUp.Update();
                pyramidDown.Update();
                countDisplay.Update();
                insideDotMatrix.Update();

                for (int i = 0; i < smallDiamondsCount; i++)
                {
                    smallDiamonds[i].mesh.localRotation = new Vector3(0f, (smallDiamonds[i].mesh.localRotation.y + i + 1f) % 360f, 0f);
                    smallDiamonds[i].mesh.globalRotation = new Vector3(smallDiamonds[i].mesh.globalRotation.x, (smallDiamonds[i].mesh.globalRotation.y - 2f - i) % 360f, Mathf.Sin(Time.time * 0.2f * (i / 3f + 1f)) * 30f);
                    smallDiamonds[i].Update();
                }

                if (timeLine.Finished)
                {
                    BuffPlugin.Log($"HoloScore destroy");
                    Destroy();
                }
            }
        }
    }
}
