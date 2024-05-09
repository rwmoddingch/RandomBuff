using Mono.Cecil;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuffUtils
{
    public static class ExplosionSpawner
    {
        public static void SpawnDamageOnlyExplosion(Creature source ,Vector2 pos, Room room, Color color, float radMulti, float damage = 2f)
        {
            room.AddObject(new SootMark(room, pos, 80f, true));
            room.AddObject(new DamageOnlyExplosion(room, source, pos, 2, 250f * radMulti, 6.2f * radMulti, damage, 280f, 0.25f, source, 0.7f, 160f, 1f, source.abstractCreature));
            room.AddObject(new Explosion.ExplosionLight(pos, 280f * radMulti, 1f, 7, color));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f * radMulti, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, pos, 14, 30f * radMulti, 9f, 7f * radMulti, 170f * radMulti, color));
            room.AddObject(new ShockWave(pos, 330f * radMulti, 0.045f * radMulti, 5, false));

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
                    room.AddObject(new Spark(pos + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(color, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                }
                room.AddObject(new Explosion.FlashingSmoke(pos + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), color, Random.Range(3, 11)));
            }
            room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f * radMulti);
            room.PlaySound(SoundID.Bomb_Explode, pos);
        }
   
    }
}
