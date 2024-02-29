using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class ThundThrowBuff : Buff<ThundThrowBuff, ThundThrowBuffData>
    {
        public override BuffID ID => ThundThrowBuffEntry.thundTHrowBuffID;
    }

    internal class ThundThrowBuffData : BuffData
    {
        public override BuffID ID => ThundThrowBuffEntry.thundTHrowBuffID;
    }

    internal class ThundThrowBuffEntry : IBuffEntry
    {
        static float LightningColorHue = 255f / 350f;
        static Color LightningColor = Custom.HSL2RGB(LightningColorHue, 1f, 0.5f);
        public static BuffID thundTHrowBuffID = new BuffID("ThundThrow", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ThundThrowBuff, ThundThrowBuffData, ThundThrowBuffEntry>(thundTHrowBuffID);
        }

        public static void HookOn()
        {
            On.Weapon.ChangeMode += Weapon_ChangeMode;
        }

        private static void Weapon_ChangeMode(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            if (!(self is Rock rock))
            {
                orig.Invoke(self, newMode);
                return;
            }

            if(self.mode == Weapon.Mode.Thrown && newMode == Weapon.Mode.Free)
            {
                self.room.PlaySound(SoundID.Death_Lightning_Spark_Spontaneous, self.firstChunk.pos, 2f, 1.4f - Random.value * 0.4f);
                self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.4f - Random.value * 0.4f);
                self.room.PlaySound(SoundID.Bomb_Explode, self.firstChunk.pos, 1f, 1.4f - Random.value * 0.4f);

                var Lightning = new LightningBolt(self.firstChunk.pos + Vector2.up * 700f,self.firstChunk.pos , 0, Mathf.Lerp(0.4f, 0.6f, Random.value),0.25f, 0.5f , LightningColorHue, true);
                Lightning.intensity = 1f;

                self.room.AddObject(Lightning);
                self.room.AddObject(new ShockWave(self.firstChunk.pos, 80f, 0.01f, 10));
                self.room.AddObject(new Explosion(self.room, self, self.firstChunk.pos, 7, 80f, 6.2f, 0.9f, 280f, 0.25f, self.thrownBy, 0.7f, 160f, 1f));

                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 180f, 1f, 7, LightningColor));
                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 130f, 1f, 3, new Color(1f, 1f, 1f)));
                self.room.ScreenMovement(self.firstChunk.pos, default, 0.5f);
                self.blink = 10;


                for(int i = 4; i < 10; i++)
                {
                    self.room.AddObject(new Spark(self.firstChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 28f, Random.value), LightningColor, null, 20, 40));
                }
                
            }
            orig.Invoke(self, newMode);
        }
    }
}
