using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace HotDogGains.Negative
{
    class PupaLizardBuff : Buff<PupaLizardBuff, PupaLizardBuffData>{public override BuffID  ID => PupaLizardBuffEntry.PupaLizardID;}
    class PupaLizardBuffData :BuffData{public override BuffID ID => PupaLizardBuffEntry.PupaLizardID;}
    class PupaLizardBuffEntry : IBuffEntry
    {
        public static BuffID PupaLizardID = new BuffID("PupaLizardID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PupaLizardBuff,PupaLizardBuffData,PupaLizardBuffEntry>(PupaLizardID);
        }
            public static void HookOn()
        {
            IL.DangleFruit.Stalk.Update += Stalk_Update;
        }

        private static void Stalk_Update(MonoMod.Cil.ILContext il)
        {
            var c = new ILCursor(il);

            //修改每口的间隔
            if (c.TryGotoNext(MoveType.Before,
                i => i.MatchLdarg(0),
                i => i.MatchLdnull(),
                i => i.MatchStfld<DangleFruit.Stalk>("fruit")
                ))
            {
                //Debug.Log("找到了蓝果变蜥蜴");
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Action<DangleFruit.Stalk>>(
                   (stalk) =>
                   {
                       if (Random.value>0.5f)
                       {
                           //Debug.Log("变蜥蜴");
                           var fruit = stalk.fruit;
                           var room = fruit.room;
                           var lizard = new AbstractCreature(fruit.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.BlueLizard), null, room.GetWorldCoordinate(fruit.firstChunk.pos), room.game.GetNewID());
                           lizard.RealizeInRoom();
                           room.PlaySound(SoundID.Water_Nut_Swell,lizard.realizedCreature.firstChunk);
                           room.AddObject(new ShockWave(lizard.realizedCreature.firstChunk.pos, 30, 1, 10));
                           stalk.fruit.Destroy();

                       }
                   }
               );

            }
        }
        public static void SpawnLizard()
        {

        }
    }
}