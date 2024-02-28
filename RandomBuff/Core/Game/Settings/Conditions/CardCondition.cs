using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
    internal class CardCondition : Condition
    {
        public override ConditionID ID => ConditionID.Card;

        public override float Exp => needCard * 50;

        public override void GachaEnd(List<BuffID> picked, List<BuffID> allCards)
        {
            currentCard = allCards.Count;
            BuffPlugin.LogDebug($"GachaEnd Refresh Card Count: {currentCard}");
        }

        public override void SetRandomParameter(float difficulty, List<Condition> sameConditions = null)
        {
            needCard = (int)Random.Range(Mathf.Lerp(5, 10, difficulty), Mathf.Lerp(10, 15, difficulty));
            BuffPlugin.LogDebug($"Add Card Condition {needCard}");
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"({currentCard}/{needCard})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(translator.Translate("Collect {0} cards"), needCard);
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            var count = BuffPoolManager.Instance.GetAllBuffIds().Count;
            if (count != currentCard)
            {
                currentCard = count;
                onLabelRefresh?.Invoke(this);
                Finished = count >= needCard;
            }
        }

        [JsonProperty]
        public int needCard;

        [JsonProperty] 
        public int currentCard;

    }
}
