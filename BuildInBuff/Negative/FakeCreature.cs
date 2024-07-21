using System;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuffUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class FakeCreatureBuffData : BuffData
    {
        public static BuffID FakeCreatureID = new BuffID("FakeCreature", true);
        public override BuffID ID => FakeCreatureID;
    }

    internal class FakeCreatureBuff : Buff<FakeCreatureBuff, FakeCreatureBuffData>
    {
        public override BuffID ID => FakeCreatureBuffData.FakeCreatureID;

        private int waitCounter = 0;

        public override void Update(RainWorldGame game)
        {

            base.Update(game);
            waitCounter++;
            foreach (var player in game.Players)
            {
                if (player.realizedCreature?.room != null &&
                    player.realizedCreature?.room == game.cameras[0].room && !game.cameras[0].room.abstractRoom.gate)
                {
                    foreach (var shortCut in player.realizedCreature.room.shortcuts.Where(i =>
                                 i.shortCutType == ShortcutData.Type.RoomExit &&
                                 ((Custom.Dist(i.StartTile.ToVector2() * 20f, player.realizedCreature.DangerPos) > 200 &&
                                   Custom.Dist(i.StartTile.ToVector2() * 20f, player.realizedCreature.DangerPos) < 600) || Random.value > 0.5f)))
                    {

                        if (Random.value < Custom.LerpMap(Custom.Dist(
                                shortCut.StartTile.ToVector2() * 20f - new Vector2(10, 10),
                                player.realizedCreature.DangerPos), 60, 300, 0.06f, 0.02f, 0.4f) / 20f * 1.0f *
                            Mathf.Clamp01(waitCounter - 80) *
                            Custom.LerpMap(waitCounter, 80, 120, 0.1f, 1f) *
                            Custom.LerpMap(waitCounter, 300, 500, 1f, 2f))
                        {

                            AbstractCreature acreature = new AbstractCreature(player.world,
                                FakeCreatureEntry.templates[Random.Range(0, FakeCreatureEntry.templates.Length)],
                                null, player.pos, game.GetNewID());
                            acreature.Realize();
                            var creature = acreature.realizedCreature;
                            creature.inShortcut = true;
                            if (Random.value > 0.025f)
                            {
                                var module = new FakeCreatureModule(creature);
                                FakeCreatureHook.modules.Add(creature, module);
                                BuffUtils.Log(FakeCreatureBuffData.FakeCreatureID,
                                    $"Create creature with fake module! {shortCut.destNode}, {acreature.creatureTemplate.type}, {module.maxCounter}");
                            }
                            else
                            {
                                BuffUtils.Log(FakeCreatureBuffData.FakeCreatureID,
                                    $"Wow! Create creature! {shortCut.destNode}, {acreature.creatureTemplate.type}");
                            }

                            waitCounter = 0;
                            game.shortcuts.CreatureEnterFromAbstractRoom(creature,
                                player.world.GetAbstractRoom(shortCut.destinationCoord.room),
                                shortCut.destNode);



                        }
                    }
                }
            }
        }
    }

    public class FakeCreatureEntry : IBuffEntry
    {
        public static Shader displacementShader;
        public static Texture2D defaultDisplacementTexture;

        public static Texture2D defaultTurbulentTexture;

        public static FShader Turbulent;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FakeCreatureBuff, FakeCreatureBuffData, FakeCreatureHook>(FakeCreatureBuffData.FakeCreatureID);
            On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
        }

        public static void LoadAssets()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath(Path.Combine(FakeCreatureBuffData.FakeCreatureID.GetStaticData().AssetPath, "fakecreature")));
            Custom.rainWorld.Shaders.Add($"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain",
                FShader.CreateShader($"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain", bundle.LoadAsset<Shader>("AlphaBehindTerrain")));
            displacementShader = bundle.LoadAsset<Shader>("Displacement");
            defaultDisplacementTexture = bundle.LoadAsset<Texture2D>("T_FX_Tile_0141");
            defaultTurbulentTexture = bundle.LoadAsset<Texture2D>("T_FX_Tile_0137_Moon");

            Custom.rainWorld.Shaders.Add($"{FakeCreatureBuffData.FakeCreatureID}.Turbulent",
                Turbulent = FShader.CreateShader($"{FakeCreatureBuffData.FakeCreatureID}.Turbulent", bundle.LoadAsset<Shader>("Turbulent")));

            Shader.SetGlobalTexture("Turbulent_Tex",defaultTurbulentTexture);
            Shader.SetGlobalVector("Turbulent_SV",new Vector4(4,3,0,-0.1f));
        }

        private void StaticWorld_InitStaticWorld(On.StaticWorld.orig_InitStaticWorld orig)
        {
            orig();
            PostModsInit();
            On.StaticWorld.InitStaticWorld -= StaticWorld_InitStaticWorld;
        }

        public static void PostModsInit()
        {
            templates = new[]
            {
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedCentipede),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedCentipede),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedCentipede),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.RedLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.CyanLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.YellowLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.BlueLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.GreenLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.DaddyLongLegs),
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.DaddyLongLegs),

                };
        }
        public static CreatureTemplate[] templates;






    }

    internal class FakeCreatureHook
    {
        public static void HookOn()
        {
            On.Creature.Update += Creature_Update;
            On.Creature.SpitOutOfShortCut += Creature_SpitOutOfShortCut;
            On.Creature.SuckedIntoShortCut += Creature_SuckedIntoShortCut;
            On.Lizard.Collide += Lizard_Collide;
            On.Centipede.Collide += Centipede_Collide;
            On.DaddyLongLegs.Collide += DaddyLongLegs_Collide;
            On.Lizard.AttemptBite += Lizard_AttemptBite;
            On.Lizard.Bite += Lizard_Bite;
        }

        private static void Lizard_Bite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
        {
            if (modules.TryGetValue(self, out var module) && chunk.owner is Player)
            {
                module.SuckIntoShortCut();
                return;
            }

            orig(self, chunk);
        }

        private static void Lizard_AttemptBite(On.Lizard.orig_AttemptBite orig, Lizard self, Creature creature)
        {
            if (modules.TryGetValue(self, out var module) && creature is Player)
            {
                module.SuckIntoShortCut();
                return;
            }

            orig(self, creature);
        }

        private static void DaddyLongLegs_Collide(On.DaddyLongLegs.orig_Collide orig, DaddyLongLegs self,
            PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (modules.TryGetValue(self, out var module) && otherObject is Player)
            {
                module.SuckIntoShortCut();
                return;
            }

            orig(self, otherObject, myChunk, otherChunk);
        }

        private static void Centipede_Collide(On.Centipede.orig_Collide orig, Centipede self,
            PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (modules.TryGetValue(self, out var module) && otherObject is Player)
            {
                module.SuckIntoShortCut();
                return;
            }

            orig(self, otherObject, myChunk, otherChunk);
        }

        private static void Lizard_Collide(On.Lizard.orig_Collide orig, Lizard self, PhysicalObject otherObject,
            int myChunk, int otherChunk)
        {
            if (modules.TryGetValue(self, out var module) && otherObject is Player)
            {
                module.SuckIntoShortCut();
                return;
            }

            orig(self, otherObject, myChunk, otherChunk);
        }



        private static void Creature_SuckedIntoShortCut(On.Creature.orig_SuckedIntoShortCut orig, Creature self,
            IntVector2 entrancePos, bool carriedByOther)
        {
            var type = self.room.shortcutData(entrancePos).shortCutType;
            if (modules.TryGetValue(self, out var module) && (
                type == ShortcutData.Type.RoomExit || type == ShortcutData.Type.CreatureHole))
            {
                module.SuckIntoShortCut(false);
                return;
            }

            orig(self, entrancePos, carriedByOther);

        }

        private static void Creature_SpitOutOfShortCut(On.Creature.orig_SpitOutOfShortCut orig, Creature self,
            IntVector2 pos, Room newRoom, bool spitOutAllSticks)
        {
            orig(self, pos, newRoom, spitOutAllSticks);
            if (modules.TryGetValue(self, out var module))
                module.SpitOutShortCut();
        }

        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);
            if (modules.TryGetValue(self, out var module))
                module.Update();
        }

        public static ConditionalWeakTable<Creature, FakeCreatureModule> modules =
            new ConditionalWeakTable<Creature, FakeCreatureModule>();
    }

    internal class FakeCreatureModule
    {
        private WeakReference<Creature> creatureRef;

        public FakeCreatureModule(Creature creature)
        {
            creatureRef = new WeakReference<Creature>(creature);
            maxCounter = Random.Range(200, 500);
        }

        public void Update()
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;
            if (counter >= 0)
            {
                if (creature.inShortcut)
                    return;

                counter++;
                if (counter == maxCounter)
                {
                    creature.room.AddObject(new GhostEffect(creature.graphicsModule, 40, 1, 0.4f, null,
                        $"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain"));
                    creature.Destroy();
                }
            }

            if (creature.room == null)
                return;
        }

        public void SpitOutShortCut()
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;
            counter = 0;
        }

        public void Destroy()
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;
            creature.LoseAllGrasps();
            while (creature.grabbedBy.Any())
            {
                var grasp = creature.grabbedBy.First();
                grasp.grabber.ReleaseGrasp(grasp.grabber.grasps.IndexOf(grasp));
            }

            creature.Destroy();
        }

        public void SuckIntoShortCut(bool createEffect = true)
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;

            if (creature.graphicsModule != null)
            {
                creature.room.AddObject(new GhostEffect(creature.graphicsModule, 40, 1, 0.4f, null,
                    $"{FakeCreatureBuffData.FakeCreatureID}.AlphaBehindTerrain"));
                if (createEffect)
                {
                    creature.room.PlaySound(SoundID.SB_A14, 0f, 0.76f, 1f);
                    BuffPostEffectManager.AddEffect(new DisplacementEffect(0, 3, 0.3f, 1f, 1, 0.04f));
                }
            }

            Destroy();
        }

        public readonly int maxCounter;

        private int counter = -1;
    }
    public class DisplacementEffect : BuffPostEffectLimitTime
    {
        private float speed;
        private float maxInst;

        public DisplacementEffect(int layer, float duringTime, float enterTime, float fadeTime, float speed, float inst, Vector4? dispSV = null, Texture2D texture = null) : base(layer, duringTime, enterTime, fadeTime)
        {
            this.speed = speed;
            maxInst = inst;
            material = new Material(FakeCreatureEntry.displacementShader);
            material.SetVector("_DispSV", dispSV ?? new Vector4(0.24f, 0.114f, 0.149f, 0.184f));
            material.SetTextureOffset("_DispTex", new Vector2(Random.value, Random.value));
            material.SetTexture("_DispTex", texture ?? FakeCreatureEntry.defaultDisplacementTexture);
        }

        protected override float LerpAlpha => Mathf.Pow(base.LerpAlpha, 0.6f);

        public override void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            base.OnRenderImage(source, destination);
            material.SetFloat("_TimeSpeed", speed);
            material.SetFloat("_DispInst", maxInst * LerpAlpha);


            Graphics.Blit(source, destination, material);
        }
    }
}



