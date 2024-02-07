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



        public QuickGameSetting()
        {
            for(int i=0;i<10;i++)
                queue[i] = BuffID.None;
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
            counter++;
            if (counter >= 40 * 30)
            {
                if (queue[pointer] != BuffID.None)
                    BuffPoolManager.Instance.RemoveBuff(queue[pointer]);
                if (isPositive) 
                    queue[pointer] = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1, 
                        BuffType.Positive)[0].BuffID;
                else
                    queue[pointer] = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1, 
                        BuffType.Negative, BuffType.Duality)[0].BuffID;
                BuffPlugin.LogDebug($"Quick Mode : New Buff {queue[pointer]}");

                BuffPoolManager.Instance.CreateBuff(queue[pointer++]);

                pointer = pointer % queue.Length;
                counter = 0;
            }
            
        }
        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket();
            inGame = false;
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
        public BuffID[] queue = new BuffID[10];

        [JsonProperty] 
        public int pointer = 0;

        [JsonProperty]
        public bool isPositive = true;

    }
}
