using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Missions.BuiltInMissions
{
    public class MissionTemplate : Mission
    {
        //偷懒用的模板示例，使用时需要继承接口: IMissionEntry
        
        //实际使用时请把false改为true，名字尽量独特避免撞车
        public static readonly MissionID templateID = new MissionID("Template", false);

        public override MissionID ID => templateID;

        public override SlugcatStats.Name bindSlug => null;

        public override Color textCol => Color.white;

        public override string missionName => "Nothing";

        public MissionTemplate() 
        {
            //记得往this.conditions和this.startBuffSet里分别加点Condition和BuffID
            //conditions里的Condition不要超过5个，超过的部分会被忽略
            //BuffID不要超过6个，超过的部分会被忽略
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new MissionTemplate());
        }
    }
}
