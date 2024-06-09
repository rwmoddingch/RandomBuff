using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RandomBuffUtils.PlayerUtils;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class AscendSparkleCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.AscendSparkle;

        public override string IconElement => "BuffCosmetic_AscendSparkle";

        public override SlugcatStats.Name BindCat => MoreSlugcatsEnums.SlugcatStatsName.Saint;

        static AscendSparkleCosmetic()
        {
            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new AscendSparkleUtils());
        }

        private static void Player_ClassMechanicsSaint(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if(c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdsfld<SoundID>("Firecracker_Bang")))
            {
                BuffPlugin.Log("Player_ClassMechanicsSaint 1");
                if(c1.TryGotoNext(MoveType.After, (i) => i.MatchPop()))
                {
                    BuffPlugin.Log("Player_ClassMechanicsSaint 2");
                    c1.Emit(OpCodes.Ldarg_0);
                    c1.EmitDelegate<Action<Player>>((p) =>
                    {
                        if (PlayerUtils.TryGetGraphicPart<AscendSparkleGraphicModule, AscendSparkleUtils>(p, out var part))
                        {
                            part.Burst(p.graphicsModule as PlayerGraphics);
                        }
                    });
                }
            }
        }
    }

    internal class AscendSparkleUtils : IOWnPlayerUtilsPart
    {
        public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
        {
            if (module.PlayerRef.TryGetTarget(out var player))
            {
                (player.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = 9;
                (player.abstractCreature.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap = 9;
            }

            if (module.Name == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                return new AscendSparkleGraphicModule();
            return null;
        }

        public PlayerModulePart InitPart(PlayerModule module)
        {
            return null;
        }
    }

    internal class AscendSparkleGraphicModule : PlayerUtils.PlayerModuleGraphicPart
    {
        ParticleEmitter emitter;

        public override void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitSprites(sLeaserInstance, self, sLeaser, rCam);
            sLeaserInstance.sprites = new FSprite[0];
        }

        public override void Update(PlayerGraphics playerGraphics)
        {
            if(emitter != null)
            {
                if (playerGraphics.player.room != emitter.room)
                {
                    emitter.Die();
                    emitter = null;
                }

                if (!playerGraphics.player.monkAscension)
                {
                    emitter.Die();
                    emitter = null;
                }
            }
            else
            {
                if(playerGraphics.player.room != null && playerGraphics.player.monkAscension)
                {
                    emitter = new ParticleEmitter(playerGraphics.player.room);
                    emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 200, 120));

                    emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));
                    emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight")));

                    emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                    emitter.ApplyParticleModule(new SetAscendCirclePosAndVel(emitter, playerGraphics));
                    emitter.ApplyParticleModule(new SetRandomLife(emitter, 20, 40));
                    Color col = PlayerGraphics.SlugcatColor(playerGraphics.CharacterForColor);
                    col.a = 0.1f;
                    emitter.ApplyParticleModule(new SetConstColor(emitter, col));
                    emitter.ApplyParticleModule(new SetRandomScale(emitter, 3f, 5f));

                    emitter.ApplyParticleModule(new ScaleOverLife(emitter, (particle, lifeParam) =>
                    {
                        return particle.setScaleXY * (1f - lifeParam);
                    }));
                    emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
                    {
                        float a = (1f - l) * 0.05f;
                        Color col = p.setColor;
                        col.a = a;
                        return col;
                    }));

                    emitter.ApplyParticleModule(new ConstantAcc(emitter, new Vector2(0f, -0.5f)));

                    emitter.ApplyParticleModule(new TrailDrawer(emitter,0, 20)
                    {
                        alpha = (p, i, a) => 1f - i / (float)a,
                        gradient = (p, i, a) => Color.Lerp(new Color(p.setColor.r, p.setColor.g, p.setColor.b), Color.blue, i/(float)a),
                        width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x * 0.3f,
                        alphaModifyOverLife = (p, l) => 1f - l,
                    });;
                    emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 1 }));
                    ParticleSystem.ApplyEmitterAndInit(emitter);
                }
            }
        }

        public void Burst(PlayerGraphics playerGraphics)
        {
            var emitter = new ParticleEmitter(playerGraphics.player.room);
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 200));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "")));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight")));

            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetAscendKarmaBurstPosAndVel(emitter, playerGraphics));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 60));
            Color col = PlayerGraphics.SlugcatColor(playerGraphics.CharacterForColor);
            col.a = 0.1f;
            emitter.ApplyParticleModule(new SetConstColor(emitter, col));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 3f, 5f));

            emitter.ApplyParticleModule(new VelocityOverLife(emitter, (p, l) =>
            {
                return p.setVel * Mathf.Pow(1f - l, 2f);
            }));

            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (particle, lifeParam) =>
            {
                return particle.setScaleXY * (1f - lifeParam);
            }));
            emitter.ApplyParticleModule(new ColorOverLife(emitter, (p, l) =>
            {
                float a = (1f - l) * 0.05f;
                Color col = p.setColor;
                col.a = a;
                return col;
            }));

            emitter.ApplyParticleModule(new TrailDrawer(emitter, 0, 20)
            {
                alpha = (p, i, a) => Mathf.Pow(1f - i / (float)a, 2f),
                gradient = (p, i, a) => Color.Lerp(new Color(p.setColor.r, p.setColor.g, p.setColor.b), Color.blue, i / (float)a),
                width = (p, i, a) => (1f - i / (float)a) * p.setScaleXY.x * 0.3f,
                alphaModifyOverLife = (p, l) => 1f - l,
            }); ;
            emitter.ApplyParticleModule(new DefaultDrawer(emitter, new int[1] { 1 }));
            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }

    public class SetAscendCirclePosAndVel : EmitterModule, IParticleInitModule
    {
        static float rad = 30f;

        PlayerGraphics bindPlayerGraphics;
        float circleFac;
        bool perpVel;

        public SetAscendCirclePosAndVel(ParticleEmitter emitter, PlayerGraphics playerGraphics, bool perpVel = true) : base(emitter)
        {
            bindPlayerGraphics = playerGraphics;
            this.perpVel = perpVel;
        }

        public override void Update()
        {
            base.Update();
            while (circleFac > 1f)
                circleFac--;
            circleFac += 1 / 20f;
        }

        public void ApplyInit(Particle particle)
        {
            Vector2 pos = new Vector2(bindPlayerGraphics.rubberMarkX + bindPlayerGraphics.rubberMouseX, bindPlayerGraphics.rubberMarkY + 60f + bindPlayerGraphics.rubberMouseY) + emitter.room.game.cameras[0].pos;

            float x = Mathf.Cos(circleFac * Mathf.PI * 2f) * rad;
            float y = Mathf.Sin(circleFac * Mathf.PI * 2f) * rad;

            Vector2 radDir = new Vector2(x, y);
            Vector2 tangentDir = Custom.PerpendicularVector(radDir.normalized);

            particle.HardSetPos(pos + radDir + Custom.RNV() * Random.value * 5f);
            particle.SetVel(perpVel ? tangentDir * 1f : radDir.normalized * 1f);
        }
    }

    public class SetAscendKarmaBurstPosAndVel : EmitterModule, IParticleInitModule
    {
        static float rad = 30f;
        static float vel = 5f;
        PlayerGraphics bindPlayerGraphics;

        public SetAscendKarmaBurstPosAndVel(ParticleEmitter emitter, PlayerGraphics playerGraphics) : base(emitter)
        {
            bindPlayerGraphics = playerGraphics;
        }

        public void ApplyInit(Particle particle)
        {
            //Vector2 pos = new Vector2(bindPlayerGraphics.rubberMarkX + bindPlayerGraphics.rubberMouseX, bindPlayerGraphics.rubberMarkY + 60f + bindPlayerGraphics.rubberMouseY) + emitter.room.game.cameras[0].pos;
            Vector2 pos = new Vector2(bindPlayerGraphics.player.mainBodyChunk.pos.x + bindPlayerGraphics.player.burstX, bindPlayerGraphics.player.mainBodyChunk.pos.y + bindPlayerGraphics.player.burstY + 60f);

            if (particle.randomParam1 < 0.5f)
            {
                float p = particle.randomParam1 * 2f;

                float x = Mathf.Cos(p * Mathf.PI * 2f) * rad;
                float y = Mathf.Sin(p * Mathf.PI * 2f) * rad;
                Vector2 radDir = new Vector2(x, y);

                particle.HardSetPos(pos + radDir + Custom.RNV() * Random.value * 5f);
                particle.SetVel(radDir.normalized * vel);
            }
            else if(particle.randomParam1 >= 0.5f && particle.randomParam1 < 0.75f)
            {
                float p = (particle.randomParam1 - 0.5f) * 4f;
                p = p * 2f - 1f;

                float x = 0.707f * p * rad;
                float y = 0.707f * p * rad;
                Vector2 radDir = new Vector2(x, y);

                particle.HardSetPos(pos + radDir + Custom.RNV() * Random.value * 2f);
                particle.SetVel(radDir.normalized * Mathf.Abs(p) * vel);
            }
            else if (particle.randomParam1 >= 0.75f)
            {
                float p = (particle.randomParam1 - 0.75f) * 4f;
                p = p * 2f - 1f;

                float x = 0.707f * p * rad;
                float y = -0.707f * p * rad;
                Vector2 radDir = new Vector2(x, y);

                particle.HardSetPos(pos + radDir + Custom.RNV() * Random.value * vel);
                particle.SetVel(radDir.normalized * Mathf.Abs(p) * vel);
            }
        }
    }
}
