using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class CardCondition : Condition
    {
        public override ConditionID ID => ConditionID.Card;

        public override float Exp => needCard * 50;

        public override void GachaEnd(List<BuffID> picked, List<BuffID> allCards)
        {
            currentCard = allCards.Count;
            if (!all)
                currentCard = allCards.Count(i => BuffConfigManager.buffTypeTable[type].Contains(i));
            BuffPlugin.LogDebug($"GachaEnd Refresh Card Count: {currentCard}, type: {type}, all: {all}");
        }


        public override bool SetRandomParameter(SlugcatStats.Name name, float difficulty,
            List<Condition> sameConditions = null)
        {
            sameConditions ??= new List<Condition>();
            List<string> list = new() { "all", "Positive", "Negative", "Duality" };
            if (sameConditions.Any(i => (i as CardCondition).all))
                list.Remove("all");

            foreach (var condition in sameConditions.Select(i => i as CardCondition))
                if(!condition.all && list.Contains(condition.type.ToString()))
                    list.Remove(condition.type.ToString());

            var current = list[Random.Range(0, list.Count)];
            if (current == "all")
                all = true;
            else
                type = (BuffType)Enum.Parse(typeof(BuffType),current);
            
            needCard = (int)Random.Range(Mathf.Lerp(5, 10, difficulty), Mathf.Lerp(10, 15, difficulty)) / (all ? 1: 2);
            BuffPlugin.LogDebug($"Add Card Condition {needCard}:{current}");
            return list.Count != 1;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCard}/{needCard})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            var type = all ? "all types" : this.type.ToString();
            return string.Format(translator.Translate("Collect {0} {1} cards"), needCard, type);
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            timer++;
            if (timer == 5)
            {
                int count = 0;
                if (all)
                    count = BuffPoolManager.Instance.GetAllBuffIds().Count;
                else
                    count = BuffPoolManager.Instance.GetAllBuffIds()
                        .Count(i => BuffConfigManager.buffTypeTable[type].Contains(i));
                if (count != currentCard)
                {
                    currentCard = count;
                    onLabelRefresh?.Invoke(this);
                    Finished = count >= needCard;
                }
                timer = 0;
            }
        }

        private int timer = 0;

        [JsonProperty]
        public int needCard;

        [JsonProperty] 
        public int currentCard;

        [JsonProperty]
        public bool all;
        
        [JsonProperty]
        public BuffType type;

    }
}
