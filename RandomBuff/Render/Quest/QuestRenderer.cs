using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }

    public class QuestRendererManager
    {
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
                foreach(var element in elements)
                    element.RemoveFromContainer();
            }
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
}
