using RandomBuff.Core.Progression.Quest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Render.Quest
{
    internal class FreePickQuestProvider : QuestRendererProvider
    {
        public override IQuestRenderer Provide(QuestUnlockedType type, string id)
        {
            return type == QuestUnlockedType.FreePick ? new FreePickQuest(id) : null;
        }

        public override string GetRewardTitle(QuestUnlockedType type, string id)
        {
            return BuffResourceString.Get("Notification_FreePickReward");
        }
    }

    internal class FreePickQuest : IQuestRenderer
    {
        public FreePickQuest(string id)
        {
            Renderer = new FreePickQuestRender(id);
        }
        public QuestRenderer Renderer { get; }
    }
}
