using RandomBuff.Core.Progression;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression.Quest;
using UnityEngine;

namespace RandomBuff.Render.Quest
{
    public abstract class QuestRenderer
    {
        public IQuestRenderer owner;
        public QuestRenderer(IQuestRenderer owner)
        {
            this.owner = owner;
        }

        public virtual void Init(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {

        }

        public virtual void Update(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {

        }

        public virtual void Draw(QuestRendererManager.QuestLeaser questLeaser, float timeStacker, QuestRendererManager.Mode mode)
        {

        }

        public virtual void ClearSprites(QuestRendererManager.QuestLeaser questLeaser, QuestRendererManager.Mode mode)
        {
            foreach (var element in questLeaser.elements)
                element.RemoveFromContainer();
        }
    }

    public class QuestRendererManager
    {
        static List<QuestRendererProvider> providers = new List<QuestRendererProvider>();

        public FContainer Container { get; private set; }
        public readonly Mode mode;

        List<QuestLeaser> leasers = new List<QuestLeaser>();

        public QuestRendererManager(FContainer container, Mode mode)
        {
            Container = container;
            this.mode = mode;
        }

        public QuestLeaser AddQuestToRender(IQuestRenderer quest)
        {
            var renderer = quest.Renderer;
            QuestLeaser leaser = new QuestLeaser(renderer, this);
            renderer.Init(leaser, mode);
            leaser.AddToContainer();
            leasers.Add(leaser);

            return leaser;
        }

        public QuestLeaser AddQuestToRender(QuestUnlockedType type, string id)
        {
            foreach(var provider in providers)
            {
                var result = provider.Provide(type, id);
                if (result != null)
                    return AddQuestToRender(result);
            }
            var renderer = new DefaultQuestRenderer(id);
            QuestLeaser leaser = new QuestLeaser(renderer, this);
            renderer.Init(leaser, mode);
            leaser.AddToContainer();
            leasers.Add(leaser);

            return leaser;
        }

        public QuestRendererProvider GetProvider(QuestUnlockedType type, string id)
        {
            foreach (var provider in providers)
            {
                var result = provider.Provide(type, id);
                if (result != null)
                    return provider;
            }
            return null;
        }


        public void Update()
        {
            foreach(var leaser in leasers)
            {
                leaser.questRenderer.Update(leaser, mode);
            }
        }

        public void GrafUpdate(float timeStacker)
        {
            foreach(var leaser in leasers)
            {
                leaser.questRenderer.Draw(leaser, timeStacker, mode);
            }
        }

        public void Destroy()
        {
            foreach(var leaser in leasers)
            {
                leaser.ClearAllElements();
            }
            leasers.Clear();
        }

        public void DestroyLeaser(QuestLeaser leaser)
        {
            leaser.ClearAllElements();
            leasers.Remove(leaser);
        }

        public class QuestLeaser
        {
            public QuestRenderer questRenderer;
            public QuestRendererManager rendererManager;

            public FNode[] elements;
            public Vector2 rect;

            public Vector2 smoothCenterPos;
            public float smoothAlpha;

            public QuestLeaser(QuestRenderer questRenderer, QuestRendererManager rendererManager)
            {
                this.questRenderer = questRenderer;
                this.rendererManager = rendererManager;
            }

            public void AddToContainer()
            {
                foreach(var element in elements)
                {
                    rendererManager.Container.AddChild(element);
                }
            }

            public void ClearAllElements()
            {
                questRenderer.ClearSprites(this, rendererManager.mode);
            }
        }

        public static void AddProvider(QuestRendererProvider provider)
        {
            providers.Add(provider);
        }

        public static void Init()
        {
            AddProvider(new BuffCardQuestProvider());
        }
    
        public enum Mode
        {
            NotificationBanner,
            QuestDisplay
        }
    }

    public interface IQuestRenderer
    {
        public QuestRenderer Renderer {get;} 
    }

    public abstract class QuestRendererProvider
    {
        public virtual IQuestRenderer Provide(QuestUnlockedType type, string id)
        {
            return null;
        }

        public virtual string GetRewardTitle(QuestUnlockedType type, string id)
        {
            throw new NotImplementedException();
        }
    }
}

