using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using UnityEngine;
using Mono.Cecil.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using Random = UnityEngine.Random;


namespace BuiltinBuffs.Negative
{
    internal class GiveALightBuff : Buff<GiveALightBuff, GiveALightBuffData>
    {
        public override BuffID ID => GiveALightIBuffEntry.GiveALightBuffID;
    }

    internal class GiveALightBuffData : BuffData
    {
        public override BuffID ID => GiveALightIBuffEntry.GiveALightBuffID;
    }

    internal class GiveALightIBuffEntry : IBuffEntry
    {
        public static BuffID GiveALightBuffID = new BuffID("GiveALight", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<GiveALightBuff, GiveALightBuffData, GiveALightIBuffEntry>(GiveALightBuffID);
        }

        public static void HookOn()
        {
            IL.LizardSpit.Update += LizardSpit_Update;
        }

        private static void LizardSpit_Update(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchCallvirt<Creature>("Violence")
            ))
            {
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate <Action<LizardSpit>>((self) =>
                {
                    BuffPlugin.Log("LizardSpit hit chunk");
                    Vector2 vector = self.pos;
                    self.room.AddObject(new SootMark(self.room, vector, 80f, true));
                    self.room.AddObject(new DamageOnlyExplosion(self.room, self.lizard, vector, 2, 125f, 6.2f, 0.2f, 120f, 0.25f, self.lizard, 0.7f, 160f, 1f, self.lizard.abstractCreature));
                    self.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, self.lizard.effectColor));
                    self.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                    self.room.AddObject(new ExplosionSpikes(self.room, vector, 14, 30f, 9f, 7f, 170f, self.lizard.effectColor));
                    self.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5, false));
                    for (int i = 0; i < 25; i++)
                    {
                        Vector2 a = Custom.RNV();
                        if (self.room.GetTile(vector + a * 20f).Solid)
                        {
                            if (!self.room.GetTile(vector - a * 20f).Solid)
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
                            self.room.AddObject(new Spark(vector + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(self.lizard.effectColor, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                        }
                        self.room.AddObject(new Explosion.FlashingSmoke(vector + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), self.lizard.effectColor, Random.Range(3, 11)));
                    }
                    self.room.ScreenMovement(new Vector2?(vector), default(Vector2), 1.3f);
                    self.room.PlaySound(SoundID.Bomb_Explode, vector);
                });
            }
            else
            {
                Debug.LogException(new Exception("LizardSpit_Update cant find!"));
            }
        }
    }
}
