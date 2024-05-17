using Mono.Cecil;
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
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class CountdownToDeathBuff : Buff<CountdownToDeathBuff, CountdownToDeathBuffData>
    {
        public override BuffID ID => CountdownToDeathBuffEntry.countdownToDeathBuffID;

        bool startExplode;
        List<AbstractCreature> finishList = new List<AbstractCreature>();
        public CountdownToDeathBuff()
        {
            MyTimer = new DownCountBuffTimer((timer, game) =>
            {
                BuffUtils.Log(ID, "CountdownToDeathBuff triggered");
                startExplode = true;
            }, 200);
        }

        public override bool Trigger(RainWorldGame game)
        {
            return true;
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (!startExplode)
                return;

            if (finishList.Count == game.Players.Count)
            {
                TriggerSelf(true);
                return;
            }

            foreach (var player in game.Players)
            {
                if (player.realizedCreature == null || player.realizedCreature.room == null)
                    continue;
                if (finishList.Contains(player))
                    continue;

                finishList.Add(player);
                var room = player.realizedCreature.room;
                Vector2 pos = player.realizedCreature.DangerPos;
                var source = player.realizedCreature;
                room.AddObject(new SootMark(room, pos, 80f, true));
                room.AddObject(new Explosion(room, null, pos, 2, 250f, 6.2f, 10f, 280f, 0.25f, source, 0.7f, 160f, 1f));
                room.AddObject(new Explosion.ExplosionLight(pos, 280f, 1f, 7, Color.red));
                room.AddObject(new Explosion.ExplosionLight(pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                room.AddObject(new ExplosionSpikes(room, pos, 14, 30f, 9f, 7f, 170f, Color.red));
                room.AddObject(new ShockWave(pos, 330f, 0.045f, 5, false));

                for (int i = 0; i < 25; i++)
                {
                    Vector2 a = Custom.RNV();
                    if (room.GetTile(pos + a * 20f).Solid)
                    {
                        if (!room.GetTile(pos - a * 20f).Solid)
                        {
                            a *= -1f;
                        }
                        else
                        {
                            a = Custom.RNV();
                        }
                    }
                    for (int j = 0; j < 3; j++)
                    {
                        room.AddObject(new Spark(pos + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(Color.red, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                    }
                    room.AddObject(new Explosion.FlashingSmoke(pos + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), Color.red, Random.Range(3, 11)));
                }
                room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f);
                room.PlaySound(SoundID.Bomb_Explode, pos);
            }
        }
    }

    internal class CountdownToDeathBuffData : CountableBuffData
    {
        public override BuffID ID => CountdownToDeathBuffEntry.countdownToDeathBuffID;

        public override int MaxCycleCount => 1;
    }

    internal class CountdownToDeathBuffEntry : IBuffEntry
    {
        public static BuffID countdownToDeathBuffID = new BuffID("CountdownToDeath", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<CountdownToDeathBuff, CountdownToDeathBuffData, CountdownToDeathBuffEntry>(countdownToDeathBuffID);
        }
    }
}
