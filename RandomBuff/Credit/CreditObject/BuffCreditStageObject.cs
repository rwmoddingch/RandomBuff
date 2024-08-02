using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit.CreditObject
{
    internal class BuffCreditStageObject : PositionedMenuObject
    {
        public bool ableToRemove;
        public float inStageEnterTime;
        public float lifeTime;

        public BuffCreditStage CreditStage => owner as BuffCreditStage;

        public float LifeParam => (CreditStage.StageTime - inStageEnterTime) / lifeTime;

        public BuffCreditStageObject(Menu.Menu menu, BuffCreditStage owner, Vector2 pos, float inStageEnterTime, float lifeTime) : base(menu, owner, pos)
        {
            this.inStageEnterTime = inStageEnterTime;
            this.lifeTime = lifeTime;
        }

        public virtual void RequestRemove()
        {
        }
    }

    internal class BuffCreditStage : PositionedMenuObject
    {
        List<BuffCreditStageObject> objects = new List<BuffCreditStageObject>();
        public bool AllStageObjectRemoved
        {
            get
            {
                bool allAbleToRemove = true;
                foreach(var obj in objects)
                {
                    if(!obj.ableToRemove)
                    {
                        allAbleToRemove = false;
                        break;
                    }
                }
                return allAbleToRemove;
            }
        }

        float enterStageTime;
        public float StageTime => CreditMenu.Time - enterStageTime;

        public BuffCreditMenu CreditMenu => menu as BuffCreditMenu;

        public BuffCreditStage(Menu.Menu menu, MenuObject owner, float enterStageTime) : base(menu, owner, Vector2.zero)
        {
            this.enterStageTime = enterStageTime;
        }

        public void AddObjectToStage(BuffCreditStageObject creditsObject)
        {
            objects.Add(creditsObject);
            subObjects.Add(creditsObject);
        }

        public void RequestRemove()
        {
            foreach(var obj in objects)
            {
                obj.RequestRemove();
            }
        }
    }
}
