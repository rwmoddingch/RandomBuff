using Menu.Remix.MixedUI;
using RandomBuff.Core.Game.Settings.Missions;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.Quest
{
    internal class MissionQuestRenderer : QuestRenderer
    {
        public MissionQuestRenderer(Mission mission) : base(mission)
        {
        }


        public override void Init(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            string missionName = (owner as Mission).MissionName;
            if(mode == QuestRendererManager.Mode.NotificationBanner)
            {
                
                questLeaser.rect = new Vector2(LabelTest.GetWidth(missionName, true) + 20f, 0f);

                questLeaser.elements = new FNode[1];
                questLeaser.elements[0] = new FLabel(Custom.GetDisplayFont(), missionName) { anchorX = 0.5f, anchorY = 0.5f };
            }
        }

        public override void Draw(QuestRendererManager.QuestLeaser questLeaser, float timeStacker, QuestRendererManager.Mode mode)
        {
            if(mode == QuestRendererManager.Mode.NotificationBanner)
            {
                questLeaser.elements[0].alpha = questLeaser.smoothAlpha;
                questLeaser.elements[0].SetPosition(questLeaser.smoothCenterPos);
            }
        }
    }
}
