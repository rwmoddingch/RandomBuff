using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Progression;
using RandomBuff.Render.Quest;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public abstract class Mission : IQuestRenderer
    {
        public abstract MissionID ID { get; }

        public abstract SlugcatStats.Name BindSlug { get; }

        public abstract Color TextCol { get; }

        public abstract string MissionName { get; }


        public List<BuffID> startBuffSet = new();

        public GameSetting GameSetting => gameSetting;

        MissionQuestRenderer questRenderer;
        public virtual QuestRenderer Renderer => questRenderer;

        protected GameSetting gameSetting = new(null);

        public Mission()
        {
            questRenderer = new MissionQuestRenderer(this);
        }

        /// <summary>
        /// 验证依赖是否完整
        /// </summary>
        /// <returns></returns>
        public bool VerifyId()
        {
            foreach (var id in startBuffSet)
            {
                if (!BuffID.values.entries.Contains(id.value))
                    return false;
            }

            return gameSetting.IsValid;
        }

    }


    internal class MissionQuestRenderer : DefaultQuestRenderer
    {
        public MissionQuestRenderer(Mission mission) : base(mission.MissionName)
        {
        }
    }

    internal class MissionQuestRendererProvider : QuestRendererProvider
    {
        public override IQuestRenderer Provide(QuestUnlockedType type, string id)
        {
            if (type != QuestUnlockedType.Mission)
                return null;

            if (MissionRegister.TryGetMission(new MissionID(id), out var mission))
                return mission;
            return null;
        }
    }
}
