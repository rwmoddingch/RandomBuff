using Menu;
using RandomBuff.Core.BuffMenu;
using RandomBuff.Core.Game.Settings.Missions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RandomBuff.Render.UI.Component.MissionEffect;

namespace RandomBuff.Render.UI.Component
{
    public class BuffMissionEffectContainer : PositionedMenuObject
    {
        FContainer container;

        List<MissionEffect> activeEffects = new List<MissionEffect>();
        float index;

        public bool update;

        internal BuffMissionEffectContainer(Menu.Menu menu, BuffNewGameMissionPage owner) : base(menu, owner, Vector2.zero)
        {
            container = new FContainer();
            owner.Container.AddChild(container);
        }

        public void SetMission(Mission mission)
        {
            MissionEffect newEffect;
            if (mission is IOwnMissionEffect provider)
                newEffect = provider.InitEffect(this);
            else
                newEffect = new MissionEffect(this);
            container.AddChild(newEffect.Container);
            activeEffects.Add(newEffect);
        }

        public override void Update()
        {
            base.Update();

            if(activeEffects.Count > 1 && index >= activeEffects.Count - 1)
            {
                for (int i = 0; i < activeEffects.Count - 1; i++)//清除淡出动画显示完成的效果
                {
                    var effectToRemove = activeEffects[0];
                    activeEffects.RemoveAt(0);

                    effectToRemove.RemoveSprites();
                }
                index = 0f;
            }

            if (!update)
                return;

            if(activeEffects.Count > 1)
                index = Mathf.Min(activeEffects.Count - 1, index + (activeEffects.Count - 1) / 40f);

            for (int i = 0;i < activeEffects.Count; i++)
            {
                var effect = activeEffects[i];

                effect.LastAlpha = effect.Alpha;
                effect.Alpha = Mathf.Clamp01(1f - Mathf.Abs(index - i));

                effect.Update(DrawPos(1f));
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            if (!update)
                return;

            foreach (var effect in activeEffects)
            {
                effect.GrafUpdate(DrawPos(timeStacker), timeStacker);
            }
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            foreach (var effect in activeEffects)
            {
                effect.RemoveSprites();
            }
            activeEffects.Clear();
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            container = null;
        }
    }

    public class MissionEffect
    {
        public BuffMissionEffectContainer effectContainer;
        public FContainer Container { get; private set; } = new FContainer();

        public float Alpha {  get; internal set; }
        public float LastAlpha { get; internal set; }

        public MissionEffect(BuffMissionEffectContainer effectContainer)
        {
            this.effectContainer = effectContainer;
        }

        public virtual void Update(Vector2 zeroPoint)
        {
        }

        public virtual void GrafUpdate(Vector2 smoothZeroPoint, float t)
        {
        }

        public virtual void RemoveSprites()
        {
            Container.RemoveFromContainer();
            Container.RemoveAllChildren();
            Container = null;
        }
    
        public interface IOwnMissionEffect
        {
            public MissionEffect InitEffect(BuffMissionEffectContainer container);
        }
    }
}
