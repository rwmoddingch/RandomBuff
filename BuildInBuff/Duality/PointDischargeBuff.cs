using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class PointDischargeBuffEntry : IBuffEntry
    {
        public static BuffID pointDischargeBuffID = new BuffID("PointDischarge", true);

        static ConditionalWeakTable<Centipede, ElectricGenerator> generatorMapper = new ConditionalWeakTable<Centipede, ElectricGenerator>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PointDischargeBuffEntry>(pointDischargeBuffID);
        }

        public static void HookOn()
        {
            On.Centipede.ctor += Centipede_ctor;
            On.Centipede.Update += Centipede_Update;
            On.CentipedeAI.Update += CentipedeAI_Update;

            On.CentipedeGraphics.AddToContainer += CentipedeGraphics_AddToContainer;
            On.CentipedeGraphics.ApplyPalette += CentipedeGraphics_ApplyPalette;
            On.CentipedeGraphics.DrawSprites += CentipedeGraphics_DrawSprites;
            On.RoomCamera.SpriteLeaser.RemoveAllSpritesFromContainer += SpriteLeaser_RemoveAllSpritesFromContainer;
        }

        private static void SpriteLeaser_RemoveAllSpritesFromContainer(On.RoomCamera.SpriteLeaser.orig_RemoveAllSpritesFromContainer orig, RoomCamera.SpriteLeaser self)
        {
            orig.Invoke(self);
            if(self.drawableObject is CentipedeGraphics centipedeGraphics)
            {
                if (generatorMapper.TryGetValue(centipedeGraphics.centipede, out var generator))
                {
                    generator.testLabel.RemoveFromContainer();
                }
            }
        }

        private static void CentipedeGraphics_AddToContainer(On.CentipedeGraphics.orig_AddToContainer orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if(generatorMapper.TryGetValue(self.centipede, out var generator))
            {
                rCam.ReturnFContainer("HUD").AddChild(generator.testLabel);
            }
        }

        private static void CentipedeGraphics_ApplyPalette(On.CentipedeGraphics.orig_ApplyPalette orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig.Invoke(self, sLeaser, rCam, palette);
            if (generatorMapper.TryGetValue(self.centipede, out var generator))
            {
                generator.ApplyPalette(self, sLeaser, rCam, palette);
            }
        }

        private static void CentipedeGraphics_DrawSprites(On.CentipedeGraphics.orig_DrawSprites orig, CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (generatorMapper.TryGetValue(self.centipede, out var generator))
                generator.DrawSprites(self, sLeaser, rCam, timeStacker, camPos);
        }

        private static void CentipedeAI_Update(On.CentipedeAI.orig_Update orig, CentipedeAI self)
        {
            orig.Invoke(self);
            if(generatorMapper.TryGetValue(self.centipede, out var generator))
                generator.AIUpdate(self);
        }

        private static void Centipede_Update(On.Centipede.orig_Update orig, Centipede self, bool eu)
        {
            if (generatorMapper.TryGetValue(self, out var generator))
            {
                generator.Update(self);
            }
            orig.Invoke(self, eu);
        }

        private static void Centipede_ctor(On.Centipede.orig_ctor orig, Centipede self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            if(self.size > 0.5f && !generatorMapper.TryGetValue(self, out var _))
            {
                generatorMapper.Add(self, new ElectricGenerator(self));
            }
        }
    }

    internal class ElectricGenerator
    {
        static Color lightCol = Helper.GetRGBColor(84, 186, 255);
        static float range = 200f;

        WeakReference<Centipede> centipedeRef;
        internal FLabel testLabel;

        Color paletteBlack;

        internal float interest;

        internal int charge;
        internal int chargeRequirement;
        internal int chargeCoolDown;

        internal int dischargeCounter;
        internal static int dischargeRequirement = 80;

        int flash;

        float flashStrength;
        LightSource headLight;
        LightSource tailLight;

        public ElectricGenerator(Centipede centipede)
        {
            centipedeRef = new WeakReference<Centipede>(centipede);
            chargeRequirement = (int)Mathf.Lerp(40, 10, centipede.size * (0.5f*Random.value + 0.5f));
            testLabel = new FLabel(Custom.GetFont(), "");

            headLight = new LightSource(centipede.bodyChunks.First().pos, true, lightCol, null) {  alpha = 0f, rad = 0f };

            tailLight = new LightSource(centipede.bodyChunks.Last().pos, true, lightCol, null) { alpha = 0f, rad = 0f };
        }

        public void Update(Centipede self)
        {
            if (self.slatedForDeletetion)
            {
                if (headLight.slatedForDeletetion)
                {
                    headLight.Destroy();
                    tailLight.Destroy();
                }
                return;
            }

            if(dischargeCounter > 0)
            {
                dischargeCounter--;
                if (dischargeCounter == 0)
                    Discharge(self);
            }

            if (chargeCoolDown > 0)
                chargeCoolDown--;

            if(charge > 0 && flash == 0)
            {
                flashStrength = 1f;
                flash += (int)Mathf.Lerp(80, 10, (float)charge / chargeRequirement);
            }
            if (flash > 0)
                flash--;

            if(flashStrength > 0)
            {
                UpdateLight();

                flashStrength = Mathf.Lerp(flashStrength, 0f, 0.25f);
                if (Mathf.Approximately(flashStrength, 0f))
                {
                    flashStrength = 0f;
                    UpdateLight();
                }

                void UpdateLight()
                {
                    headLight.alpha = flashStrength;
                    headLight.rad = self.size * range * flashStrength;

                    tailLight.alpha = flashStrength;
                    tailLight.rad = self.size * range * flashStrength;
                }
            }

            if (headLight.room != self.room)
            {
                headLight.RemoveFromRoom();
                tailLight.RemoveFromRoom();

                if(self.room != null)
                {
                    self.room.AddObject(headLight);
                    self.room.AddObject(tailLight);
                }
            }
            headLight.pos = self.firstChunk.pos;
            tailLight.pos = self.bodyChunks.Last().pos;

            //testLabel.text = $"interest:{interest}\ncharge:{charge}\nchargeCoolDown:{chargeCoolDown}\ndischargeCounter:{dischargeCounter}";
        }

        public void ApplyPalette(CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            paletteBlack = palette.blackColor;
        }

        public void DrawSprites(CentipedeGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            testLabel.SetPosition(Vector2.Lerp(self.centipede.firstChunk.lastPos, self.centipede.firstChunk.pos, timeStacker) - camPos);

            Color flashColor = Color.Lerp(lightCol, Color.white, Mathf.Pow(flashStrength, 2f));
            Color col = Color.Lerp(paletteBlack, flashColor, flashStrength);
            for (int m = 0;m < 2; m++)
            {
                for(int n = 0;n < 2; n++)
                {
                    for(int i = 0;i < 2; i++)
                    {
                        sLeaser.sprites[self.WhiskerSprite(m, n, i)].color = col;
                    }
                }
            }
        }

        public void AIUpdate(CentipedeAI centipedeAI)
        {
            if (chargeCoolDown > 0 || dischargeCounter > 0)
                return;

            if(centipedeAI.behavior == CentipedeAI.Behavior.Hunt)
            {
                interest = Mathf.Lerp(interest, 0.06f, 0.7f * Random.value);
            }
            else if(centipedeAI.behavior == CentipedeAI.Behavior.InvestigateSound)
            {
                interest = Mathf.Lerp(interest, 0.03f, 0.5f * Random.value);
            }
            else if(centipedeAI.behavior == CentipedeAI.Behavior.Flee)
            {
                interest = Mathf.Lerp(interest, 0.04f, 0.5f * Random.value);
            }
            else
            {
                if(interest > 0)
                {
                    interest = Mathf.Lerp(interest, 0f, 0.15f);
                    if (Mathf.Approximately(interest, 0f))
                        interest = 0f;
                }
            }

            if (interest > 0f)
            {
                interest = Mathf.Lerp(interest, interest * 2f, centipedeAI.centipede.size);//biggersize, much more interest
                interest *= 0.5f;
            }

            if(Random.value < interest && charge < chargeRequirement)
                Charge(centipedeAI.centipede, 1);
            if (charge > 0 && Random.value < 0.02f)
                Charge(centipedeAI.centipede, -1);
        }

        public void Charge(Centipede self, int c)
        {
            charge += c;
            if (charge < 0)
                charge = 0;

            if(charge >= chargeRequirement)
            {
                charge = chargeRequirement;
                dischargeCounter = dischargeRequirement;
                if(self.room != null)
                {
                    self.room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, self.firstChunk, false, self.size, 1f);
                    self.room.PlaySound(SoundID.Centipede_Electric_Charge_LOOP, self.bodyChunks.Last(), false, self.size, 1f);
                }
            }    
        }

        public void Discharge(Centipede self)
        {
            chargeCoolDown = (int)Mathf.Lerp(200, 120, self.size);
            charge = 0;
            interest = 0f;

            flashStrength = 2f;
            self.room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, self.firstChunk.pos, self.size, 1.4f - Random.value * 0.4f);

            self.room.AddObject(new ShockWave(self.firstChunk.pos, range * self.size, 0.05f * self.size, 10));
            self.room.AddObject(new ShockWave(self.bodyChunks.Last().pos, range * self.size, 0.05f * self.size, 10));

            foreach (var obj in self.room.updateList)
            {
                if(obj is Player player && player.grasps != null && player.grasps.Length == 2)
                {
                    if (Vector2.Distance(player.DangerPos, self.firstChunk.pos) > range && Vector2.Distance(player.DangerPos, self.bodyChunks.Last().pos) > range)
                        continue;

                    bool playerNeedStun = false;
                    for(int i = 0;i < 2; i++)
                    {
                        var grasp = player.grasps[i];
                        if (grasp.grabbed is Spear spear)
                        {
                            playerNeedStun = true;

                            grasp.Release();
                            spear.Destroy();

                            AbstractSpear abSpear = new AbstractSpear(self.room.world, null, player.abstractCreature.pos, self.room.game.GetNewID(), false, true);
                            abSpear.RealizeInRoom();

                            player.SlugcatGrab(abSpear.realizedObject, i);
                            ESpearRecharge(abSpear.realizedObject as ElectricSpear);
                            continue;
                        }

                        if(grasp.grabbed is ElectricSpear eSpear)
                        {
                            playerNeedStun = true;
                            ESpearRecharge(eSpear);
                            continue;
                        }
                    }

                    if (playerNeedStun)
                        player.Stun((int)(self.size * 120));
                }
            }
            self.Stun(200);

            void ESpearRecharge(ElectricSpear electricSpear)
            {
                electricSpear.abstractSpear.electricCharge = 3;
                electricSpear.room.PlaySound(SoundID.Jelly_Fish_Tentacle_Stun, electricSpear.firstChunk.pos);
                electricSpear.room.AddObject(new Explosion.ExplosionLight(electricSpear.firstChunk.pos, 200f, 1f, 4, new Color(0.7f, 1f, 1f)));
                electricSpear.Spark();
                electricSpear.Zap();
                electricSpear.room.AddObject(new ZapCoil.ZapFlash(electricSpear.sparkPoint, 25f));
            }
        }

    }
}
