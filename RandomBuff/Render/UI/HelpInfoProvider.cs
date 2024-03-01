using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    public class HelpInfoProvider
    {
        static int MaxCounter = 10;
        static float FloatMaxCounter = 40f;
        static Vector2 HidePos = new Vector2(0, 80f);
        static Vector2 ShowPos = new Vector2(50f, 80f);
        public delegate bool CustomHelpInfoProvider(HelpInfoID ID, out string helpInfo, params object[] Params);

        static List<CustomHelpInfoProvider> providers = new();
        public static event CustomHelpInfoProvider CustomProviders
        {
            add { providers.Add(value); }
            remove { providers.Remove(value); }
        }

        BuffCardSlot slot;

        internal FLabel[] labels = new FLabel[2];
        int currentShowLabel;

        int[][] counters = new int[2][];

        HelpInfoID currentID;

        string info;
        int currentUsingLabel;

        internal HelpInfoProvider(BuffCardSlot slot)
        {
            this.slot = slot;

            for(int i = 0; i < labels.Length; i++)
            {
                labels[i] = new FLabel(RWCustom.Custom.GetDisplayFont(), "")
                {
                    alignment = FLabelAlignment.Left,

                    anchorX = 0,
                    anchorY = 0,

                    x = 50f,
                    y = 50f,
                    scale = 0.8f
                };
                slot.Container.AddChild(labels[i]);

                //target-current-last
                counters[i] = new int[3] { 0, 0, 0 };
            }

            UpdateHelpInfo(HelpInfoID.None);
        }

        public void UpdateHelpInfo(HelpInfoID ID, bool forceUpdate = false, params object[] Params)
        {
            if (currentID == ID && !forceUpdate)
                return;

            currentID = ID;
            foreach(var invoker in providers)
            {
                if (invoker.Invoke(ID, out string info, Params))
                {
                    SetText(info);
                    return;
                }
            }

            info = " ";

            if (ID == HelpInfoID.None)
            {
                SetText(info);
                return;
            }
        }

        void SetText(string text)
        {
            counters[currentUsingLabel][0] = 0;

            currentUsingLabel = 1 - currentUsingLabel;
            labels[currentUsingLabel].text = text;
            counters[currentUsingLabel][0] = MaxCounter;
        }

        public void Update()
        {
            for(int i = 0; i < labels.Length; i++)
            {
                counters[i][2] = counters[i][1];
                if (counters[i][0] != counters[i][1])
                {
                    if (counters[i][0] > counters[i][1])
                        counters[i][1]++;
                    else
                        counters[i][1]--;
                }
            }
        }

        public void GrafUpdate(float timeStacker)
        {
            for(int i = 0;i < labels.Length;i++)
            {
                //if (counters[i][2] == counters[i][0])
                //    continue;

                float f = Mathf.Lerp(counters[i][2] / FloatMaxCounter, counters[i][1] / FloatMaxCounter, timeStacker);
                f = Helper.LerpEase(f);
                labels[i].SetPosition(Vector2.Lerp(HidePos, ShowPos, f));
                labels[i].alpha = f;
            }
        }

        public class HelpInfoID : ExtEnum<HelpInfoID>
        {
            public HelpInfoID(string id, bool register = false) : base(id, register)
            {
            }

            public static HelpInfoID None = new HelpInfoID("None", true);
        }
    }
}
