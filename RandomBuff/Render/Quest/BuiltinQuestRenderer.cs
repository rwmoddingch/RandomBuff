using Menu.Remix.MixedUI;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.Quest
{
    internal class DefaultQuestRenderer : QuestRenderer
    {
        string id;
        public DefaultQuestRenderer(string id) : base(null)
        {
            this.id = id;
        }


        public override void Init(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            if (mode == QuestRendererManager.Mode.NotificationBanner)
            {
                questLeaser.rect = new Vector2(LabelTest.GetWidth(id, true) + 20f, 20f);

                questLeaser.elements = new FNode[1];
                questLeaser.elements[0] = new FLabel(Custom.GetDisplayFont(), id) { anchorX = 0.5f, anchorY = 0.5f };
            }
            else if (mode == QuestRendererManager.Mode.QuestDisplay)
            {
                questLeaser.rect = new Vector2(LabelTest.GetWidth(id, false), 20f);

                questLeaser.elements = new FNode[1];
                questLeaser.elements[0] = new FLabel(Custom.GetFont(), id) { anchorX = 0.5f, anchorY = 0.5f };
            }
        }

        public override void Draw(QuestRendererManager.QuestLeaser questLeaser, float timeStacker, QuestRendererManager.Mode mode)
        {
            questLeaser.elements[0].alpha = questLeaser.smoothAlpha;
            questLeaser.elements[0].SetPosition(questLeaser.smoothCenterPos);
        }
    }

    internal class BuffCardQuestRenderer : QuestRenderer
    {
        BuffID id;
        BuffCard card;

        public BuffCardQuestRenderer(BuffCardQuest buffCardQuest ,BuffID id) : base(buffCardQuest)
        {
            this.id = id;
        }

        public override void Init(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            float scale = 0f;
            if (mode == QuestRendererManager.Mode.NotificationBanner)
                scale = BuffCard.normalScale * 0.5f;
            else if (mode == QuestRendererManager.Mode.QuestDisplay)
                scale = BuffCard.normalScale * 0.3f;

            card = new BuffCard(id);
            card.Scale = scale;
            questLeaser.elements = new FNode[1];
            questLeaser.elements[0] = card.Container;
            questLeaser.rect = new Vector2(CardBasicAssets.RenderTextureSize.x, CardBasicAssets.RenderTextureSize.y) * scale * BuffCard.interactiveScaleBound;
        }

        public override void Draw(QuestRendererManager.QuestLeaser questLeaser, float timeStacker, QuestRendererManager.Mode mode)
        {
            card.Alpha = questLeaser.smoothAlpha;
            card.Position = questLeaser.smoothCenterPos;
        }

        public override void ClearSprites(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            base.ClearSprites(questLeaser, mode);
            card.Destroy();
        }
    }
}
