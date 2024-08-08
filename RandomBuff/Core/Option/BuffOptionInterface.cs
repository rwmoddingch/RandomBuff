using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RandomBuffUtils.MixedUI;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.Option
{
    internal class BuffOptionInterface : OptionInterface
    {
        private const float YSize = 40;
        private const float YItemSize = 30;
        private const float XSpacing = 50;
        private static readonly Color CheatColor = new Color(0.85f, 0.35f, 0.4f);

        public static BuffOptionInterface Instance { get; private set; }

        public BuffOptionInterface()
        {
            CardSlotKey = config.Bind(nameof(CardSlotKey), KeyCode.Tab.ToString());
            KeyBindKey = config.Bind(nameof(KeyBindKey), KeyCode.CapsLock.ToString());
            
            ShowExceptionLog = config.Bind(nameof(ShowExceptionLog), true);

            EnableExpeditionExtend = config.Bind(nameof(EnableExpeditionExtend), true);
            EnableExpeditionModExtend = config.Bind(nameof(EnableExpeditionModExtend), false);

            CheatAllCards = config.Bind(nameof(CheatAllCards), false);
            CheatAllCosmetics = config.Bind(nameof(CheatAllCosmetics), false);

            DisableNotifyDialog = config.Bind(nameof(DisableNotifyDialog), false);
            Instance = this;

        }

        public static void SaveConfig()
        {
            Instance._SaveConfigFile();
        }

        public override void Initialize()
        {
            base.Initialize();

            cheatList.Clear();

            foreach (var configurable in config.configurables)
                configurable.Value.info = new ConfigurableInfo(BuffResourceString.Get($"Remix_{configurable.Key}_Desc", true));

            OpTab option = InitNewTab(BuffResourceString.Get("Remix_Option", true));
            OpTab cheat = InitNewTab(BuffResourceString.Get("Remix_Cheat", true), CheatColor);
        

            Tabs = new[] { option , cheat };
            
            const float initYIndex = 1.5f + 1f + 2f;
            float yIndex = initYIndex;

            //Options
            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_CardSlotKey", true),FLabelAlignment.Left),
                new OpKeyBinderEx(CardSlotKey, Vector2.zero, Vector2.zero));

            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_KeyBindKey", true), FLabelAlignment.Left),
                new OpKeyBinderEx(KeyBindKey, Vector2.zero, Vector2.zero));

            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_ShowExceptionLog", true),FLabelAlignment.Left),
                new OpCheckBox(ShowExceptionLog,Vector2.zero));

            yIndex++;


            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_EnableExpeditionExtend", true), FLabelAlignment.Left),
                mainExpedition = new OpCheckBox(EnableExpeditionExtend, Vector2.zero));


            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_EnableExpeditionModExtend", true), FLabelAlignment.Left),
               modExpedition = new OpCheckBox(EnableExpeditionModExtend, Vector2.zero));

            yIndex = initYIndex;

            //Cheats
            AppendItems(cheat,ref yIndex,
                cheatButton = new OpHoldButton(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_Cheat", true)){colorEdge = CheatColor});
            yIndex -= 1;
            AppendItems(cheat, ref yIndex,
                AppendToCheatList(
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_CheatAllCards", true), FLabelAlignment.Left) { color = CheatColor },
                new OpCheckBox(CheatAllCards, Vector2.zero) { colorEdge = CheatColor }));

            AppendItems(cheat, ref yIndex,
                AppendToCheatList(
                    new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_CheatAllCosmetics", true), FLabelAlignment.Left) { color = CheatColor },
                    new OpCheckBox(CheatAllCosmetics, Vector2.zero) { colorEdge = CheatColor }));

            var holdEvent = cheatButton.GetType().GetEvent("OnPressDone");
            holdEvent.AddEventHandler(cheatButton, Delegate.CreateDelegate(holdEvent.EventHandlerType,this,nameof(ShowCheatLayer)));

            foreach (var ele in cheatList)
                ele.Hide();

            //Credit
            option.AddItems(creditButton =new OpSimpleButton(new Vector2(option.CanvasSize.x / 2 - 75,30),new Vector2(150, YItemSize), 
                BuffResourceString.Get("Remix_Credit", true)));

       
            holdEvent = creditButton.GetType().GetEvent("OnClick");
            holdEvent.AddEventHandler(creditButton, Delegate.CreateDelegate(holdEvent.EventHandlerType, this, nameof(SwitchToCredit)));



        }


        public override void Update()
        {
            base.Update();
            if (!mainExpedition.GetValueBool() && !modExpedition.IsInactive)
            {
                if(modExpedition.GetValueBool())
                    modExpedition.SetValueBool(false);
                modExpedition.Deactivate();
            }
            else if (mainExpedition.GetValueBool() && modExpedition.IsInactive)
            {
                modExpedition.Reactivate();
            }
        }

        public void SwitchToCredit(UIfocusable trigger)
        {
            config.Save();
            Custom.rainWorld.processManager.RequestMainProcessSwitch(BuffEnums.ProcessID.CreditID);
        }

        public void ShowCheatLayer(UIfocusable trigger)
        {
            cheatButton.Hide();
            foreach(var ele in cheatList)
                ele.Show();
        }

        private OpTab InitNewTab(string name,Color? color = null)
        {
            color ??= MenuColorEffect.rgbMediumGrey;
            OpTab tab = new OpTab(this, name) { colorButton = color.Value };
            float yIndex = 1.5f;

            AppendItems(tab, 0, 600, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_Title", true),
                    FLabelAlignment.Center, true){color = color.Value});

            AppendItems(tab, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, "Version 1.0.0", FLabelAlignment.Left){color = color.Value},
                new OpLabel(Vector2.zero, Vector2.zero, "by: Team Nowhere", FLabelAlignment.Right) { color = color.Value });
            return tab;
        }

        private UIelement[] AppendToCheatList(params UIelement[] elements)
        {
            cheatList.AddRange(elements.Where(i => i != null));
            return elements;
        }

        private void AppendItems(OpTab tab,float overrideSpacing,float maxSizeX, ref float yIndex, params UIelement[] elements)
        {
            for (int i =0;i<elements.Length;i++)
            {
                var ele = elements[i];
                if(ele == null) continue;
                var size = 1;
                for(int j = i+1;j<elements.Length;j++)
                    if (elements[j] == null) size++;
                    else break;
                var sizeX = Mathf.Min(tab.CanvasSize.x / elements.Length * size - 2 * overrideSpacing, maxSizeX);

                ele.pos = new Vector2(tab.CanvasSize.x / elements.Length * i + overrideSpacing +
                                      ((tab.CanvasSize.x / elements.Length * size - 2 * overrideSpacing) - sizeX)/2 + 
                                      (ele is OpCheckBox ? sizeX - 24 : 0)/2,
                    tab.CanvasSize.y - yIndex * YSize);

                ele.size = new Vector2(sizeX, YItemSize);

                if(ele is UIconfig con)
                    con.description = con.cfgEntry.info.description;
            }
            
            tab.AddItems(elements.Where(i => i != null).ToArray());
            yIndex++;
        }

        private void AppendItems(OpTab tab, float maxSize, ref float yIndex, params UIelement[] elements) =>
            AppendItems(tab, XSpacing, maxSize, ref yIndex, elements);
        
        private void AppendItems(OpTab tab, ref float yIndex, params UIelement[] elements) =>
            AppendItems(tab, XSpacing,150,ref yIndex, elements);
        


        public Configurable<string> CardSlotKey { get; private set; }
        public Configurable<string> KeyBindKey { get; private set; }
        public Configurable<bool> ShowExceptionLog { get; private set; }

        public Configurable<bool> CheatAllCards { get; private set; }
        public Configurable<bool> CheatAllCosmetics { get; private set; }

        public Configurable<bool> EnableExpeditionExtend {get; private set; }
        public Configurable<bool> EnableExpeditionModExtend { get; private set; }
        public Configurable<bool> DisableNotifyDialog { get; private set; }

        private OpCheckBox mainExpedition, modExpedition;

        private OpHoldButton cheatButton;
        private readonly List<UIelement> cheatList = new();

        private OpSimpleButton creditButton;
    }
}
