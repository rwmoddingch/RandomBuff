using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static RandomBuffUtils.PlayerUtils.PlayerModuleGraphicPart;
using static RandomBuffUtils.PlayerUtils;
using RandomBuffUtils;
using MoreSlugcats;
using System.Runtime.CompilerServices;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class MeteorSpearCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.MeteorSpear;

        public override string IconElement => "BuffCosmetic_MeteorSpear";

        public override SlugcatStats.Name BindCat => MoreSlugcatsEnums.SlugcatStatsName.Spear;

        private void Spear_Thrown(On.Spear.orig_Thrown orig, Spear self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            self.room?.AddObject(TailPool.GetMeteorTail(self,self.room));
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new MeteorSpearUtils());
            On.Spear.Thrown += Spear_Thrown;
        }

        public override void Destroy()
        {
            base.Destroy();
            On.Spear.Thrown -= Spear_Thrown;
        }
    }

    public class MeteorSpearUtils : IOWnPlayerUtilsPart
    {
        public PlayerModulePart InitPart(PlayerModule module)
        {
            return null;
        }

        public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
        {
            return null;
        }
    }

    public static class TailPool
    {
        public static Queue<MeteorTail> meteorTails = new Queue<MeteorTail>();

        public static MeteorTail GetMeteorTail(Spear spear, Room room)
        {
            if (meteorTails.Count == 0)
                return new MeteorTail(spear,room);
            else
            {
                var tail = meteorTails.Dequeue();
                tail.bindSpear = spear;
                tail.room = room;
                tail.shouldFade = false;
                tail.slatedForDeletetion = false;
                tail.CreateEmitter();
                return tail;
            }
        }

        public static void RecycleMeteorTail(MeteorTail meteorTail)
        {
            meteorTail.bindSpear = null;
            meteorTail.shouldFade = true;
            meteorTails.Enqueue(meteorTail);
        }
    }

    public class MeteorTail : CosmeticSprite
    {
        public List<Vector2> partPos = new List<Vector2>();
        public List<Vector2> partLastPos = new List<Vector2>();
        public Spear bindSpear;
        public bool shouldFade;
        public float hue;
        public float lastHue;
        public static int seg = 10;
        public static float width = 3f;

        public ParticleEmitter dust;
        public ParticleEmitter fallingStar;

        public MeteorTail(Spear spear, Room room)
        {
            bindSpear = spear;
            this.room = room;

            CreateEmitter();
        }

        public void CreateEmitter()
        {
            dust = new ParticleEmitter(room);
            dust.ApplyEmitterModule(new RateSpawnerModule(dust, 40, 40));
            dust.ApplyEmitterModule(new SetEmitterLife(dust, 100, true, false));
            dust.ApplyEmitterModule(new BindEmitterToPhysicalObject(dust, bindSpear, 0));
            dust.ApplyParticleModule(new AddElement(dust, new Particle.SpriteInitParam("Futile_White", "FlatLight", scale: 0.5f, constCol: Color.cyan)));
            dust.ApplyParticleModule(new AddElement(dust, new Particle.SpriteInitParam("Futile_White", "FlatLight", scale: 0.375f, constCol: Color.white)));
            dust.ApplyParticleModule(new SetMoveType(dust, Particle.MoveType.Global));
            dust.ApplyParticleModule(new SetRandomPos(dust, 10f));
            dust.ApplyParticleModule(new SetRandomLife(dust, 30, 40));
            dust.ApplyParticleModule(new SetRandomRotation(dust, 0, 360f));
            dust.ApplyParticleModule(new SetVelociyFromEmitter(dust, 0f));
            dust.ApplyParticleModule(new SetRandomScale(dust, 0f, 0f));
            dust.ApplyParticleModule(new ConstantAcc(dust, new Vector2(0, -0.15f)));
            dust.ApplyParticleModule(new ScaleOverLife(dust, (particle, lifeParam) =>
            {
                float t = Mathf.Max(0f, Mathf.Min(lifeParam * 5f, 1f - lifeParam));
                return Mathf.Lerp(1f, 1.5f, particle.randomParam1) * t;
            }));
            dust.ApplyParticleModule(new VelocityOverLife(dust, (particle, lifeParam) =>
            {
                return particle.setVel * (1f - lifeParam) + new Vector2(0f, -0.1f);
            }));
            ParticleSystem.ApplyEmitterAndInit(dust);

            fallingStar = new ParticleEmitter(room);
            fallingStar.ApplyEmitterModule(new RateSpawnerModule(fallingStar, 40, 10));
            fallingStar.ApplyEmitterModule(new SetEmitterLife(fallingStar, 100, true, false));
            fallingStar.ApplyEmitterModule(new BindEmitterToPhysicalObject(fallingStar, bindSpear, 0));
            fallingStar.ApplyParticleModule(new AddElement(fallingStar, new Particle.SpriteInitParam("Futile_White", "FlatLight", scale: 3f, constCol: new Color(202f/255f, 165f/255f, 55f/255f, 1f))));
            fallingStar.ApplyParticleModule(new AddElement(fallingStar, new Particle.SpriteInitParam("buffassets/illustrations/MStar", "Basic", scale: 1f)));
            fallingStar.ApplyParticleModule(new SetMoveType(fallingStar, Particle.MoveType.Global));
            fallingStar.ApplyParticleModule(new SetRandomPos(fallingStar, 10f));
            fallingStar.ApplyParticleModule(new SetRandomLife(fallingStar, 30, 40));
            fallingStar.ApplyParticleModule(new SetRandomRotation(fallingStar, 0, 360f));
            fallingStar.ApplyParticleModule(new SetVelociyFromEmitter(fallingStar, 0f));
            fallingStar.ApplyParticleModule(new SetRandomScale(fallingStar, 0f, 0f));
            fallingStar.ApplyParticleModule(new ConstantAcc(fallingStar, new Vector2(0, -0.15f)));
            fallingStar.ApplyParticleModule(new ScaleOverLife(fallingStar, (particle, lifeParam) =>
            {
                float t = Mathf.Max(0f, Mathf.Min(lifeParam * 4f, 1f - lifeParam));
                return Mathf.Lerp(0.5f, 0.75f, particle.randomParam1) * t;
            }));
            fallingStar.ApplyParticleModule(new VelocityOverLife(fallingStar, (particle, lifeParam) =>
            {
                return particle.setVel * (lifeParam) + new Vector2(0f, -1f);
            }));
            fallingStar.ApplyParticleModule(new RotationOverLife(fallingStar, (particle, lifeParam) =>
            {
                return Mathf.Lerp(0f, 240f, lifeParam);
            }));
            fallingStar.ApplyParticleModule(new ColorOverLife(fallingStar, (particle, lifeParam) =>
            {
                return Custom.HSL2RGB((particle.randomParam1 + 0.25f * lifeParam)%1, 1f, 0.8f);
            }));
            ParticleSystem.ApplyEmitterAndInit(fallingStar);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(seg, false, true);
            //sLeaser.sprites[1] = TriangleMesh.MakeLongMesh(seg, false, true);
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);            
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            //rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[1]);
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[0]);
        }

        public void DynamicColor(RoomCamera.SpriteLeaser sLeaser, float timeStacker)
        {
            for (int i = 0; i < (sLeaser.sprites[0] as TriangleMesh).vertices.Length / 2; i++)
            {
                float num = Mathf.Lerp(lastHue, hue, timeStacker);
                float num2 = Mathf.Lerp(num, num + 0.25f, Mathf.InverseLerp(2 * seg - 1, 0, i));
                Color col = Custom.HSL2RGB(num2 % 1f, 1f, 0.75f);
                Color col2 = Custom.HSL2RGB(num2 % 1f, 1f, 0.5f);
                col2.a = Mathf.Lerp(1f, 0.6f, Mathf.InverseLerp(2 * seg - 1, 0, i));

                (sLeaser.sprites[0] as TriangleMesh).verticeColors[2 * i] = col;
                (sLeaser.sprites[0] as TriangleMesh).verticeColors[2 * i + 1] = col;
                //(sLeaser.sprites[1] as TriangleMesh).verticeColors[2 * i] = col2;
                //(sLeaser.sprites[1] as TriangleMesh).verticeColors[2 * i + 1] = col2;
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (bindSpear == null || slatedForDeletetion) return;
            Vector2 spearPos = Vector2.Lerp(bindSpear.firstChunk.lastPos, bindSpear.firstChunk.pos, timeStacker);
            for (int i = 0; i < (sLeaser.sprites[0] as TriangleMesh).vertices.Length / 2; i++)
            {
                if (partLastPos.Count == 0)
                {
                    (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i, spearPos - camPos);
                    (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i + 1, spearPos - camPos);
                    //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i, spearPos - camPos);
                    //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i + 1, spearPos - camPos);
                }
                else
                {
                    if (partLastPos.Count > i)
                    {
                        float trueWidth = width * Mathf.Cos(0.375f * Mathf.PI * Mathf.InverseLerp((sLeaser.sprites[0] as TriangleMesh).vertices.Length / 2 - 1, 0, i));
                        Vector2 vector = Custom.PerpendicularVector((partPos[i] - partLastPos[i]));
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i, Vector2.Lerp(partLastPos[i], partPos[i], timeStacker) + trueWidth * vector - camPos);
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i + 1, Vector2.Lerp(partLastPos[i], partPos[i], timeStacker) - trueWidth * vector - camPos);
                        //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i, Vector2.Lerp(partLastPos[i], partPos[i], timeStacker) + 1.5f * trueWidth * vector - camPos);
                        //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i + 1, Vector2.Lerp(partLastPos[i], partPos[i], timeStacker) - 1.5f * trueWidth * vector - camPos);
                    }
                    else
                    {

                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i, spearPos - camPos);
                        (sLeaser.sprites[0] as TriangleMesh).MoveVertice(2 * i + 1, spearPos - camPos);
                        //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i, spearPos - camPos);
                        //(sLeaser.sprites[1] as TriangleMesh).MoveVertice(2 * i + 1, spearPos - camPos);
                    }

                }

            }

            DynamicColor(sLeaser, timeStacker);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (bindSpear == null) return;
            if (bindSpear.slatedForDeletetion || bindSpear.room == null)
            {
                this.Destroy();
                return;
            }
            if (slatedForDeletetion) return;

            lastHue = hue;
            hue += 0.016f;
            hue = hue % 1f;

            shouldFade = bindSpear.mode != Weapon.Mode.Thrown;

            Vector2 spearpos = bindSpear.firstChunk.pos;
            Vector2 spearlastpos = bindSpear.firstChunk.lastPos;

            if (!shouldFade)
            {
                partPos.Add(spearpos);
                if (partPos.Count > 1)
                {
                    partLastPos.Add(partPos[partPos.Count - 2]);
                }
                else
                {
                    partLastPos.Add(spearlastpos);
                }
            }            

            if (partPos.Count > 2 * seg || (shouldFade && partPos.Count > 0))
            {
                partPos.RemoveAt(0);
            }

            if (partLastPos.Count > 2 * seg || (shouldFade && partLastPos.Count > 0))
            {
                partLastPos.RemoveAt(0);
            }

            if (partPos.Count == 0 && shouldFade)
            {
                this.Destroy();
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            dust.Die();
            fallingStar.Die();
            TailPool.RecycleMeteorTail(this);
        }
    }

}
