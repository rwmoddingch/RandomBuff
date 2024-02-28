using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.BuffMenu
{
    internal class BuffGameManual : ManualDialog
    {
        public static Dictionary<string, int> topicKeys = new Dictionary<string, int>
        {
            { "introduction", 1 },
            { "gameplay", 1 },
            { "buff", 1 },
            { "condition", 1 },
            { "gamemode", 1 }
        };

        public BuffGameManual(ProcessManager manager, Dictionary<string, int> topics)
            : base(manager, topics)
        {
            this.currentTopic = this.topics.Keys.ElementAt(0);
            this.pageNumber = 0;
            this.GetManualPage(this.currentTopic, this.pageNumber);
        }

        public override string TopicName(string topic)
        {
            if (topic == "introduction")
                return "RandomBuff Introduction";
            else if (topic == "gameplay")
                return "RandomBuff GamePlay";
            else if (topic == "buff")
                return "Random Buff";
            else if (topic == "conditon")
                return "Game Conditions";
            else if (topic == "gamemode")
                return "Game Modes";
            return "NULL";
        }
    }
}
