﻿using RandomBuff.Render.Quest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class CosmeticQuestRenderer : QuestRenderer
    {
        public CosmeticQuestRenderer(IQuestRenderer owner) : base(owner)
        {
        }

        public override void Init(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            if(mode == QuestRendererManager.Mode.NotificationBanner)
            {
                questLeaser.rect = new Vector2(120, 120);
                questLeaser.elements = new FNode[1];
                questLeaser.elements[0] = new FSprite((owner as CosmeticUnlock).IconElement) { scale = 1.5f };
            }
            else
            {
                questLeaser.rect = new Vector2(80, 80f);
                questLeaser.elements = new FNode[1];
                questLeaser.elements[0] = new FSprite((owner as CosmeticUnlock).IconElement) { scale = 1f };
            }
        }

        public override void Draw(QuestRendererManager.QuestLeaser questLeaser, float timeStacker, QuestRendererManager.Mode mode)
        {
            base.Draw(questLeaser, timeStacker, mode);
            questLeaser.elements[0].alpha = questLeaser.smoothAlpha;
            questLeaser.elements[0].SetPosition(questLeaser.smoothCenterPos);
        }
    }
}
