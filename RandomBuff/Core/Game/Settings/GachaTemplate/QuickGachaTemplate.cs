using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    internal class QuickGachaTemplate : Game.GachaTemplate
    {
        public override GachaTemplateID ID => GachaTemplateID.Quick;


        public override bool NeedRandomStart => true;


        public QuickGachaTemplate()
        {
        }

        public override void EnterGame()
        {
            if (newGame)
            {
                newGame = false;
                counter = 40 * Time;
            }
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if (BuffHud.Instance == null)
                return;

            counter++;

            if (counter >= 40 * Time)
            {

                if (queue.Count == MaxCount)
                {
                    BuffHud.Instance.RemoveCard(queue.Dequeue());
                    BuffPoolManager.Instance.RemoveBuff(queue.Peek());
                }

                BuffID buffId;
                if (isPositive)
                    buffId = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1,
                        BuffType.Positive)[0].BuffID;
                else
                    buffId = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1,
                        BuffType.Negative, BuffType.Duality)[0].BuffID;

                BuffPlugin.LogDebug($"Quick Mode : New Buff {buffId}");

                BuffPoolManager.Instance.CreateBuff(buffId);
                BuffHud.Instance.AppendNewCard(buffId);
                queue.Enqueue(buffId);
                counter = 0;
            }

        }
        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket();
            BuffPlugin.Log($"QuickGameSetting : SessionEnd! isPositive: {isPositive}");
        }

        public override void NewGame()
        {
            BuffPlugin.LogDebug("New Quick Game");
            newGame = true;
            CurrentPacket = new CachaPacket();
        }

        private int counter;

        [JsonProperty]
        private bool newGame;

        [JsonProperty]
        public Queue<BuffID> queue = new();

        [JsonProperty]
        public int pointer = 0;

        [JsonProperty]
        public bool isPositive = true;

        [JsonProperty]
        public int Time = 30;

        [JsonProperty]
        public int MaxCount = 10;

    }
}
