using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Positive;
using HUD;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class EatMoreBuff : Buff<EatMoreBuff, EatMoreBuffData>
    {
        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;

        public EatMoreBuff()
        {
            if (BuffCustom.TryGetGame(out var game) &&
                game.session is StoryGameSession session &&
                game.Players[0].realizedCreature is Player player)
            {
              
                var vec = SlugcatStats.SlugcatFoodMeter(game.StoryCharacter);
                session.characterStats.foodToHibernate = vec.y;
                session.characterStats.maxFood = vec.x;

                player.slugcatStats.foodToHibernate = vec.y;
                player.slugcatStats.maxFood = vec.x;
                game.cameras[0].hud.foodMeter.survivalLimit = vec.y;
                game.cameras[0].hud.foodMeter.survLimTo = vec.y;
            }
        }
    }

    class EatMoreBuffData : BuffData
    {
        public override BuffID ID => EatMoreIBuffEntry.eatMoreBuffID;
    }

    class EatMoreIBuffEntry : IBuffEntry
    {
        public static BuffID eatMoreBuffID = new BuffID("EatMore", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<EatMoreBuff,EatMoreBuffData,EatMoreIBuffEntry>(eatMoreBuffID);
        }

        public static void LongLifeCycleHookOn()
        {
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;
        }

   

        private static IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            IntVector2 origFoodRequirement = orig(slugcat);
            var data = eatMoreBuffID.GetBuffData();
            int newHibernateRequirement = origFoodRequirement.y + data.StackLayer;
            int newMaxFoodRequirement = Mathf.Max(newHibernateRequirement, origFoodRequirement.x);

            return new IntVector2(newMaxFoodRequirement, newHibernateRequirement);
        }
    }
}
