using Menu;
using RandomBuff.Credit.CreditObject;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit
{
    internal class BuffCreditMenu : Menu.Menu
    {
        RainEffect rainEffect;
        public float time { get; private set; }

        public BuffCreditMenu(ProcessManager manager)
            : base(manager, BuffEnums.ProcessID.CreditID)
        {
            pages.Add(new Page(this, null, "main", 0));
            rainEffect = new RainEffect(this, pages[0]);
            pages[0].subObjects.Add(rainEffect);

            AnimMachine.GetDelayCmpnt(40 * 10, autoDestroy: true).BindActions(OnAnimFinished: (t) =>
            {
                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            });
        }

        public void EndCredit()
        {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
        }

        public override void RawUpdate(float dt)
        {
            base.RawUpdate(dt);
            time += dt;
            rainEffect.rainFade = Custom.SCurve(Mathf.InverseLerp(0f, 6f, time), 0.8f) * 0.5f;
        }

        public override void Update()
        {
            base.Update();
            if (UnityEngine.Random.value < 0.00625f)
            {
                rainEffect.LightningSpike(Mathf.Pow(UnityEngine.Random.value, 2f) * 0.85f, Mathf.Lerp(20f, 120f, UnityEngine.Random.value));
            }
        }
    }


    
}
