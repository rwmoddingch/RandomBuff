using System;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
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

        public override void Update(RainWorldGame game)
        {

            base.Update(game);
            foreach (var player in game.Players)
            {
                if (player.realizedCreature?.room != null &&
                    player.realizedCreature?.room == game.cameras[0].room)
                {
                    foreach (var shortCut in player.realizedCreature.room.shortcuts.Where(i =>
                                 i.shortCutType == ShortcutData.Type.RoomExit &&
                                 Custom.DistLess(i.StartTile.ToVector2() * 20f, player.realizedCreature.DangerPos,
                                     200)))
                    {
                        var speed = player.realizedCreature.mainBodyChunk.vel.magnitude *
                                    Custom.VecToDeg(Custom.DirVec(player.realizedCreature.DangerPos,
                                        shortCut.StartTile.ToVector2() * 20f - new Vector2(10, 10)));

                        if (Random.value < Custom.LerpMap(Custom.Dist(
                                shortCut.StartTile.ToVector2() * 20f - new Vector2(10, 10),
                                player.realizedCreature.DangerPos), 60, 150, 0.08f, 0.02f, 0.4f) / 20f *
                            Custom.LerpMap(speed, 0, 10, 1, 2.3f))
                        {
                            if (Random.value < 0.5f)
                            {
                                AbstractCreature acreature = new AbstractCreature(player.world,
                                    FakeCreatureEntry.templates[Random.Range(0, FakeCreatureEntry.templates.Length)],
                                    null, player.pos, game.GetNewID());
                                acreature.Realize();
                                var creature = acreature.realizedCreature;
                                creature.inShortcut = true;
                                if (Random.value > 0.01f)
                                {
                                    var module = new FakeCreatureModule(creature);
                                    FakeCreatureHook.modules.Add(creature, module);
                                    Debug.Log(
                                        $"Create creature with fake module! {shortCut.destNode}, {acreature.creatureTemplate.type}, {module.maxCounter}");
                                }
                                else
                                {
                                    Debug.Log(
                                        $"Wow! Create creature! {shortCut.destNode}, {acreature.creatureTemplate.type}");
                                }

                                game.shortcuts.CreatureEnterFromAbstractRoom(creature,
                                    player.world.GetAbstractRoom(shortCut.destinationCoord.room),
                                    shortCut.destNode);

                            }

                        }
                    }
                }
            }
        }
    }

    public class FakeCreatureEntry : IBuffEntry
    {
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FakeCreatureBuff, FakeCreatureBuffData, FakeCreatureHook>(FakeCreatureBuffData.FakeCreatureID);
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            On.StaticWorld.InitStaticWorld += StaticWorld_InitStaticWorld;
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






        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (!self.rainWorld.BuffMode())
                return;
        }


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
            if (modules.TryGetValue(self, out var module))
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
            maxCounter = Random.Range(200, 600);
        }

        public void Update()
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;
            if (counter >= 0)
            {
                counter++;
                if (counter == maxCounter)
                    creature.Destroy();
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

        public void SuckIntoShortCut(bool createShadow = true)
        {
            if (!creatureRef.TryGetTarget(out var creature))
                return;

            if (creature.graphicsModule != null && createShadow)
            {
                creature.room.AddObject(new GhostEffect(creature.graphicsModule, 40, 1, 0.4f));
                creature.room.PlaySound(SoundID.SB_A14, 0f, 0.76f, 1f);

            }

            Destroy();
        }

        public readonly int maxCounter;

        private int counter = -1;
    }
}
