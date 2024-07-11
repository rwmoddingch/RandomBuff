using System.Collections.Generic;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    public class MissionTemplate : Mission
    {
        //偷懒用的模板示例，使用时需要继承接口: IMissionEntry
        
        //实际使用时请把false改为true，名字尽量独特避免撞车
        public static readonly MissionID templateID = new MissionID("Template", false);

        public override MissionID ID => templateID;

        public override SlugcatStats.Name BindSlug => null;

        public override Color TextCol => Color.white;

        public override string MissionName => "Nothing";


        /// <summary>
        /// 在构造函数里重新创建gameSetting,添加条件信息及抽卡信息
        /// </summary>
        public MissionTemplate()
        {
            return;
            //示例
            gameSetting = new GameSetting(BindSlug, "Normal" /*抽卡模版选择 会读取bufftemplates文件夹内的json文件做配置*/)
            {
                conditions = new List<Condition>()
                { new AchievementCondition() {achievementID = WinState.EndgameID.Chieftain} },
                gachaTemplate = new NormalGachaTemplate(){ExpMultiply = 1.2f}, 
                //如果想自己设定特殊抽卡模版但不想写json的话可以直接这样构建
            };
        }

        public void RegisterMission()
        {
            MissionRegister.RegisterMission(ID, new MissionTemplate());
        }
    }
}
