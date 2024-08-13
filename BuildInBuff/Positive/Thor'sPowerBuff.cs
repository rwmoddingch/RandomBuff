using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class Thor_sPowerBuff : Buff<Thor_sPowerBuff, Thor_sPowerBuffData>
    {
        static float LightningColorHue = 200f / 360f;
        static Color LightningColor = Custom.HSL2RGB(LightningColorHue, 1f, 0.5f);

        public override BuffID ID => Thor_sPowerBuffEntry.Thor_sPowerBuffID;

        public override bool Triggerable => !triggered;

        public override bool Active => !triggered;
        bool triggered;

        public override bool Trigger(RainWorldGame game)
        {
            if (triggered)
                return false;

            if(game.FirstAlivePlayer.realizedCreature != null && game.FirstAlivePlayer.realizedCreature.room != null)
            {
                var room = game.FirstAlivePlayer.realizedCreature.room;
                CreateShock(game.FirstAlivePlayer.realizedCreature.DangerPos, room);

                List<Spear> spearToReplace = new List<Spear>();
                foreach(var obj in room.updateList)
                {
                    if(obj is Spear spear)
                    {
                        spearToReplace.Add(spear);
                    }
                }

                var creatureList = room.updateList.Where(obj => obj is Creature).Select(obj => obj as Creature).ToList();

                foreach (var creature in creatureList)
                {
                    if (creature.grasps != null)
                    {
                        for (int i = 0; i < creature.grasps.Length; i++)
                        {
                            if (creature.grasps[i] == null)
                                continue;

                            var grasp = creature.grasps[i];
                            if (grasp.grabbed is Spear spear)
                            {
                                spearToReplace.Remove(spear);

                                grasp.Release();
                                spear.Destroy();

                                AbstractSpear abSpear = new AbstractSpear(room.world, null, creature.abstractCreature.pos, room.game.GetNewID(), false, true);
                                abSpear.RealizeInRoom();

                                creature.Grab(abSpear.realizedObject, i, 0, Creature.Grasp.Shareability.CanOnlyShareWithNonExclusive, 0.5f, true, false);
                                continue;
                            }
                        }
                    }
                }


                foreach (var spear in spearToReplace)
                {
                    AbstractSpear abSpear = new AbstractSpear(room.world, null, spear.abstractPhysicalObject.pos, room.game.GetNewID(), false, true);
                    abSpear.RealizeInRoom();

                    spear.Destroy();
                }

                triggered = true;
                return false;
            }

            return false;
        }

        public void CreateShock(Vector2 pos, Room room)
        {
            room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, pos, 2f, 1.4f - Random.value * 0.4f);
            room.PlaySound(SoundID.Zapper_Zap, pos, 1f, 1.4f - Random.value * 0.4f);
            room.PlaySound(SoundID.Bomb_Explode, pos, 1f, 1.4f - Random.value * 0.4f);

            var Lightning = new LightningBolt(pos + Vector2.up * 700f, pos, 0, Mathf.Lerp(1f, 1.6f, Random.value), 0.25f, 0.5f, LightningColorHue, true);
            Lightning.intensity = 1f;

            room.AddObject(Lightning);
            room.AddObject(new ShockWave(pos, 220f, 0.01f, 10));


            room.AddObject(new Explosion.ExplosionLight(pos, 320f, 1f, 7, LightningColor));
            room.AddObject(new Explosion.ExplosionLight(pos, 220f, 1f, 3, new Color(1f, 1f, 1f)));
            room.ScreenMovement(pos, default, 0.5f);

            for (int i = 4; i < 10; i++)
            {
                room.AddObject(new Spark(pos, Custom.RNV() * Mathf.Lerp(4f, 28f, Random.value), LightningColor, null, 20, 40));
            }
        }
    }

    internal class Thor_sPowerBuffData : BuffData
    {
        public override BuffID ID => Thor_sPowerBuffEntry.Thor_sPowerBuffID;
    }

    internal class Thor_sPowerBuffEntry : IBuffEntry
    {
        public static BuffID Thor_sPowerBuffID = new BuffID("Thor_sPower", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<Thor_sPowerBuff, Thor_sPowerBuffData, Thor_sPowerBuffEntry>(Thor_sPowerBuffID);
        }
    }
}
