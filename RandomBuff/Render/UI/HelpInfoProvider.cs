using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Render.UI
{
    public class HelpInfoProvider
    {
        public delegate bool CustomHelpInfoProvider(HelpInfoID ID, out string helpInfo, params object[] Params);

        static List<CustomHelpInfoProvider> providers = new();
        public static event CustomHelpInfoProvider CustomProviders
        {
            add { providers.Add(value); }
            remove { providers.Remove(value); }
        }

        BuffCardSlot slot;
        internal FLabel label;

        HelpInfoID currentID;

        string info;
        int displayLength;

        internal HelpInfoProvider(BuffCardSlot slot)
        {
            this.slot = slot;
            slot.Container.AddChild(label = new FLabel(RWCustom.Custom.GetDisplayFont(), "")
            {
                alignment = FLabelAlignment.Left,

                anchorX = 0,
                anchorY = 0,

                x = 50f,
                y = 50f,
                scale = 0.8f
            });
            UpdateHelpInfo(HelpInfoID.None);
        }

        public void UpdateHelpInfo(HelpInfoID ID, params object[] Params)
        {
            if (currentID == ID)
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

            info = "    ";

            if (ID == HelpInfoID.None)
            {
                SetText(info);
                return;
            }
        }

        void SetText(string text)
        {
            info = text;
            displayLength = 1;
        }

        public void Update()
        {
            if(displayLength < info.Length)  
            {
                displayLength++;
                label.text = info.Substring(0, displayLength);
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
