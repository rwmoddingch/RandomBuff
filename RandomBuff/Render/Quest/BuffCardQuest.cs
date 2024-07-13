using RandomBuff.Core.Buff;
using RandomBuff.Core.Progression;
using RandomBuff.Core.SaveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression.Quest;

namespace RandomBuff.Render.Quest
{
    internal class BuffCardQuest : IQuestRenderer
    {
        BuffCardQuestRenderer questRenderer;
        public QuestRenderer Renderer => questRenderer;
        
        public BuffCardQuest(BuffID id)
        {
            questRenderer = new BuffCardQuestRenderer(this, id);
        }
    }

    internal class BuffCardQuestProvider : QuestRendererProvider
    {
        public override IQuestRenderer Provide(QuestUnlockedType type, string id)
        {
            if (type != QuestUnlockedType.Card)
                return null;

            var buffID = new BuffID(id);
            if(BuffConfigManager.ContainsId(buffID))
            {
                return new BuffCardQuest(buffID);
            }
            return null;
        }
    }
}
