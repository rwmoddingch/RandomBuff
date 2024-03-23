using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public abstract class Mission
    {
        public abstract MissionID ID { get; }

        public abstract SlugcatStats.Name bindSlug { get; }

        public abstract Color textCol { get; }

        public abstract string missionName { get; }

        public List<Condition> conditions = new();

        public List<BuffID> startBuffSet = new();

        public bool Finished
        {
            get
            {
                if (conditions.Count == 0) return true;
                bool fin = true;
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (!conditions[i].Finished)
                    {
                        fin = false;
                        break;
                    }
                }
                return fin;
            }
        }

    }
}
