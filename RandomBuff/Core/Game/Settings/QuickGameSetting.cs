using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Game.Settings
{
    internal class QuickGameSetting : BaseGameSetting
    {
        public override BuffSettingID ID { get; }


        public override bool NeedRandomStart => true;


        public QuickGameSetting()
        {
        }

        public override void EnterGame()
        {
     
            inGame = true;
            counter = 0;
            if (newGame)
            {
                newGame = false;
                counter = 40 * 30 - 1;
            }
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if (BuffHud.Instance == null)
                return;
            counter++;
            if (counter >= 40 * 30)
            {
                
                if (queue.Count == 10)
                {
                    BuffPoolManager.Instance.RemoveBuff(queue.Peek());
                    BuffHud.Instance.RemoveCard(queue.Dequeue());
                }

                BuffID buffId;
                if (isPositive) 
                    buffId = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1, 
                        BuffType.Positive)[0].BuffID;
                else
                    buffId = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1, 
                        BuffType.Negative, BuffType.Duality)[0].BuffID;

                BuffPlugin.LogDebug($"Quick Mode : New Buff {buffId}");

                BuffHud.Instance.AppendNewCard(buffId);
                BuffPoolManager.Instance.CreateBuff(buffId);
                queue.Enqueue(buffId);
                counter = 0;
            }
            
        }
        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket();
            inGame = false;
            BuffPlugin.Log($"QuickGameSetting : SessionEnd! isPositive: {isPositive}");
        }

        public override void NewGame()
        {
            newGame = true;
            CurrentPacket = new CachaPacket();
        }

        private int counter;
        private bool inGame;
        private bool newGame;

        [JsonProperty]
        public Queue<BuffID> queue = new ();

        [JsonProperty] 
        public int pointer = 0;

        [JsonProperty]
        public bool isPositive = true;

    }
}
