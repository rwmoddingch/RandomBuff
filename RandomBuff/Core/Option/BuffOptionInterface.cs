using Menu;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI.BuffPack;
using RandomBuffUtils;
using RandomBuffUtils.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RandomBuff.Core.Option
{
    internal class BuffOptionInterface : OptionInterface
    {
        private const float YSize = 35;
        private const float YItemSize = 30;
        private const float XSpacing = 50;
        private static readonly Color CheatColor = new Color(0.85f, 0.35f, 0.4f);
        private static readonly Color gradientA = Custom.hexToColor("7BFFD2");
        private static readonly Color gradientB = Custom.hexToColor("A67BFF");


        public PackButton[] packButtons;

        private bool[] isChanged;
        private string[] pluginInfoNames;

        public bool HasAnyChanged()
        {
            return isChanged.Any(i => i);
        }


        public static BuffOptionInterface Instance { get; private set; }

        public BuffOptionInterface()
        {
            CardSlotKey = config.Bind(nameof(CardSlotKey), KeyCode.Tab.ToString());
            KeyBindKey = config.Bind(nameof(KeyBindKey), KeyCode.CapsLock.ToString());
            
            ShowExceptionLog = config.Bind(nameof(ShowExceptionLog), true);

            EnableExpeditionModExtend = config.Bind(nameof(EnableExpeditionModExtend), false);

            CheatAllCards = config.Bind(nameof(CheatAllCards), false);
            CheatAllCosmetics = config.Bind(nameof(CheatAllCosmetics), false);

            DisableNotifyDialog = config.Bind(nameof(DisableNotifyDialog), false);
            DisableCardSlotText = config.Bind(nameof(DisableCardSlotText), false);
            CosmeticForEverySlug = config.Bind(nameof(CosmeticForEverySlug), false);
            DisableCardPocketConflict = config.Bind(nameof(DisableCardPocketConflict), false);
            EnableDevChat = config.Bind(nameof(EnableDevChat), false);
            ShowUnlockWawaFuncNotification = config.Bind(nameof(ShowUnlockWawaFuncNotification), true);
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

            const float initYIndex = 1.5f + 1f + 2f;
            float yIndex = initYIndex;

            List<OpTab> initTab = new List<OpTab>();
            OpTab option = InitNewTab(BuffResourceString.Get("Remix_Option", true));
            initTab.Add(option);
            OpTab pack = InitNewTab(BuffResourceString.Get("Remix_BuffPack", true));
            initTab.Add(pack);
            OpTab cheat = InitNewTab(BuffResourceString.Get("Remix_Cheat", true), CheatColor);
            initTab.Add(cheat);
            if(BuffConfigManager.IsItemLocked(QuestUnlockedType.Cosmetic, "Crown") || true)
            {
                OpTab wawa = InitNewTab(BuffResourceString.Get("Remix_Wawa", true));
                initTab.Add(wawa);

                AppendItems(wawa, ref yIndex, new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_EnableDevChat", true), FLabelAlignment.Left), new OpCheckBox(EnableDevChat, Vector2.zero));
            }

            Tabs = initTab.ToArray();
            
           

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

            AppendItems(option, ref yIndex, new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_CosmeticForEverySlug", true), FLabelAlignment.Left), new OpCheckBox(CosmeticForEverySlug, Vector2.zero));

            AppendItems(option, ref yIndex, new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_DisableCardPocketConflict", true), FLabelAlignment.Left) { color = CheatColor }, new OpCheckBox(DisableCardPocketConflict, Vector2.zero) { colorEdge = CheatColor });

            yIndex++;

            
            AppendItems(option, ref yIndex,
                new OpLabel(Vector2.zero, Vector2.zero, BuffResourceString.Get("Remix_EnableExpeditionModExtend", true), FLabelAlignment.Left),
                new OpCheckBox(EnableExpeditionModExtend, Vector2.zero));

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


            float sizeY = 0f;
            

            var infos = BuffConfigManager.GetSortedPluginInfos();
            packButtons = packButtons = new PackButton[infos.Count()];
            isChanged = new bool[infos.Count()];
            pluginInfoNames = new string[infos.Count()];

            int i = 0;
            foreach (var pluginInfo in infos)
            {
                
                var button = new PackButton(Vector2.zero, new Vector2(525f, 120f), pluginInfo, true, pluginInfo.AssemblyName !=  "BuiltinBuffs") { Enabled = pluginInfo.Enabled };

                button.ToggleCallBack += (self) =>
                {
                    int buttonIndex = packButtons.IndexOf(self);
                    isChanged[buttonIndex] = !isChanged[buttonIndex];

                    if (self.Enabled)
                    {
                        foreach (var dependentPack in self.pluginInfo.Dependencies)
                        {
                            int newIndex = -1;
                            for (int j = 0;j < pluginInfoNames.Length; j++)
                            {
                                if (pluginInfoNames[j].Equals(dependentPack))
                                    newIndex = j;
                            }
                            if(newIndex != -1)
                            {
                                isChanged[newIndex] = packButtons[newIndex].Enabled == false;
                                packButtons[newIndex].Enabled = true;
                            }
                   
                        }
                    }
                    else
                    {
                        for (int l = 0; l < packButtons.Length; l++)
                        {
                            var button = packButtons[l];
                            if (button.pluginInfo.Dependencies.Contains(self.pluginInfo.AssemblyName))
                            {
                                isChanged[l] = button.Enabled == true;
                                button.Enabled = false;
                            }
                        }
                    }
                };
                packButtons[i] = button;
                pluginInfoNames[i] = pluginInfo.AssemblyName;
                sizeY += 120f + 20f;
                BuffPlugin.Log(pluginInfo.AssemblyName);
                i++;
            }

            

            sizeY = Mathf.Max(sizeY, 560f);

            OpScrollBox scrollBox;
            pack.AddItems(new OpLabel(20f, 470f, Regex.Replace(BuffResourceString.Get("Remix_BuffPackDescription", true),"<LINE>","\n")));
            pack.AddItems(scrollBox = new OpScrollBox(new Vector2(20f, 20f), new Vector2(560f, 420f), sizeY, hasSlideBar: true));

            float anchorY = sizeY;
            foreach(var button in packButtons)
            {
                anchorY -= button.size.y;
                anchorY -= 10f;
                button.SetPos(new Vector2(10f, anchorY));
                scrollBox.AddItems(button);
                anchorY -= 10f;
            }
        }

        public override void Update()
        {
            base.Update();
            if(Tabs.Length > 3)
            {
                Color gradient = Color.Lerp(gradientA, gradientB, Mathf.Sin(Time.time * Mathf.PI * 0.2f));
                Tabs[3].colorButton = gradient;
                Tabs[3].colorCanvas = gradient;
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
                new OpLabel(Vector2.zero, Vector2.zero, $"Version {BuffPlugin.ModVersion}", FLabelAlignment.Left){color = color.Value},
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

        public Configurable<bool> EnableExpeditionModExtend { get; private set; }
        public Configurable<bool> DisableNotifyDialog { get; private set; }

        public Configurable<bool> DisableCardSlotText { get; private set; }

        public Configurable<bool> CosmeticForEverySlug { get; private set; }
        public Configurable<bool> DisableCardPocketConflict { get; private set; }
        public Configurable<bool> EnableDevChat { get; private set; }
        public Configurable<bool> ShowUnlockWawaFuncNotification { get; private set; }


        private OpHoldButton cheatButton;
        private readonly List<UIelement> cheatList = new();

        private OpSimpleButton creditButton;
    }
}
