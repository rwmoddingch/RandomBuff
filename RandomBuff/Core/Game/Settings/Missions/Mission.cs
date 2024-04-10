using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public abstract class Mission
    {
        public abstract MissionID ID { get; }

        public abstract SlugcatStats.Name BindSlug { get; }

        public abstract Color TextCol { get; }

        public abstract string MissionName { get; }


        public List<BuffID> startBuffSet = new();

        public GameSetting GameSetting => gameSetting;

        protected GameSetting gameSetting = new(null);


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
}
