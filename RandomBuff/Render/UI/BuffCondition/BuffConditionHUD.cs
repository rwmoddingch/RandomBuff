using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Render.UI.Component;
using RandomBuff.Render.UI.Notification;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.BuffCondition
{
    internal class BuffConditionHUD
    {
        public FContainer Container { get; } = new();
        internal List<ConditionInstance> instances = new();
        internal MissionDisplay missionDisplay;
        FlagBanner flagBanner;

        public Vector2 TopLeft => new Vector2(0, Custom.rainWorld.screenSize.y);
        public Mode currentMode = Mode.Refresh;

        public bool flagMode;
        Mode origMode = Mode.Refresh;
        
        public BuffConditionHUD()
        {
            var gameSetting = BuffPoolManager.Instance.GameSetting;
            if (!string.IsNullOrEmpty(gameSetting.MissionId) && MissionRegister.TryGetMission(new MissionID(gameSetting.MissionId), out var mission))
            {
                missionDisplay = new MissionDisplay(this, mission, 0);
            }
            int indexIncreasement = missionDisplay == null ? 0 : 1;
            foreach (var condition in gameSetting.conditions)
            {
                condition.BindHudFunction(OnCompleted, OnUncompleted, OnLabelRefresh);
                instances.Add(new ConditionInstance(this, condition, instances.Count + indexIncreasement));
            }
            flagBanner = new FlagBanner(this);
            UpdateFlagMode();
        }

        public void OnCompleted(Condition condition)
        {
            foreach (var conditionalInstance in instances)
            {
                conditionalInstance.Complete(condition);
            }
            UpdateFlagMode();
        }

        public void OnUncompleted(Condition condition)
        {
            foreach(var conditionalInstance in instances)
            {
                conditionalInstance.Uncomplete(condition);
            }
            UpdateFlagMode();
        }

        public void OnLabelRefresh(Condition condition)
        {
            foreach(var conditionInstance in instances)
            {
                conditionInstance.Refresh(condition);
            }
        }

        void UpdateFlagMode()
        {
            var gameSetting = BuffPoolManager.Instance.GameSetting;
            if (gameSetting.conditions.Count == 0)
                return;

            bool allFinished = true;
            foreach (var condition in gameSetting.conditions)
            {
                allFinished &= condition.Finished;
            }

            if(flagMode != allFinished)
            {
                flagMode = allFinished;
                if (flagMode)
                    ChangeMode(Mode.Refresh);
                else
                    ChangeMode(origMode);
            }
        }

        public void Update()
        {
            for(int i = instances.Count - 1; i >= 0; i--)
                instances[i].Update();
            flagBanner.Update();
            missionDisplay?.Update();
        }

        public void DrawSprites(float timeStacker)
        {
            for (int i = instances.Count - 1; i >= 0; i--)
                instances[i].DrawSprites(timeStacker);
            flagBanner.DrawSprites(timeStacker);
            missionDisplay?.GrafUpdate(timeStacker);
        }

        public void Destroy()
        {
            for (int i = instances.Count - 1; i >= 0; i--)
                instances[i].Destroy();
            flagBanner.Destroy();
            missionDisplay?.Destroy();
        }

        public void ChangeMode(Mode newMode)
        {
            if (flagMode)
            {
                origMode = newMode;
                currentMode = Mode.Refresh;
            }
            else
            {
                if (newMode == currentMode)
                    return;

                if (currentMode == Mode.Refresh && newMode == Mode.Alway)
                {
                    missionDisplay?.SetShow();
                    foreach (var instance in instances)
                    {
                        instance.SetShow();
                    }
                }

                currentMode = newMode;
            }
        }

        internal class MissionDisplay
        {
            BuffConditionHUD conditionHUD;

            CardTitle cardTitle;

            Color currentColor;
            Color targetColor;

            Vector2 hoverPos;
            Vector2 hidePos;
            Vector2 pos;
            Vector2 lastPos;

            string mission;

            bool _show;
            bool Show
            {
                get => _show;
                set
                {
                    if(value != _show)
                    {
                        _show = value;
                        cardTitle.RequestSwitchTitle(_show ? mission : "");
                    }
                }
            }
            TickAnimCmpnt showAnim;

            public MissionDisplay(BuffConditionHUD conditionHUD, Mission bindMission, int index)
            {
                this.conditionHUD = conditionHUD;
                hoverPos = conditionHUD.TopLeft + new Vector2(20, -30 * index) + new Vector2(0f, - 20f);
                mission = bindMission.MissionName;
                showAnim = AnimMachine.GetTickAnimCmpnt(0, ConditionInstance.MaxShowAnimTimer + ConditionInstance.MaxStayDisplayTimer, autoStart: false).AutoPause().BindActions(OnAnimFinished: OnTickAnimFinish);

                cardTitle = new CardTitle(conditionHUD.Container, 0.07f, hoverPos, 0.5f, 0f, 10, 2);
                Show = true;
            }

            public void Update()
            {
                cardTitle.Update();
                if(conditionHUD.currentMode == Mode.Alway && showAnim.enable)
                {
                    showAnim.current = ConditionInstance.MaxShowAnimTimer;
                    showAnim.SetEnable(false);
                }
                else if(conditionHUD.currentMode == Mode.Refresh && !showAnim.enable)
                {
                    showAnim.SetEnable(true);
                }
            }

            void OnTickAnimFinish(TickAnimCmpnt tickAnimCmpnt)
            {
                Show = false;
            }

            public void GrafUpdate(float timeStacker)
            {
                cardTitle.GrafUpdate(timeStacker);
            }

            public void SetShow()
            {
                showAnim.Reset();
                Show = true;
            }

            public void Destroy()
            {
                cardTitle.Destroy();
            }
        }

        internal class ConditionInstance
        {
            public static int MaxShowAnimTimer = 40;
            public static int MaxStayDisplayTimer = 120;
            public static int MaxFlashTimer = 10;
            public static Color normalColor = Color.white * 0.6f + Color.black * 0.4f;
            public static Color completeColor = Color.green * 0.8f + Color.blue * 0.2f;
            public static Color uncompleteColor = Color.red * 0.8f + Color.yellow * 0.2f;
            public static Color highLightColor = Color.white;

            BuffConditionHUD conditionHUD;
            Condition bindCondition;
            int index;

            FSprite flatLight;
            FLabel textLabel;

            //状态变量
            public bool show;
            bool animFinished;
            int showTimer;
            int lastShowTimer;
            float ShowFactor => showTimer / (float)MaxShowAnimTimer;
            float LastShowFactor => lastShowTimer / (float)MaxShowAnimTimer;

            int stayDisplayTimer;

            int lastFlashCounter;
            int flashCounter;
            float FlashFactor => flashCounter / (float)MaxFlashTimer;

            bool complete;
            bool setComplete;
            bool needColorRefresh;

            string setText;

            Color currentColor;
            Color targetColor;

            Vector2 hoverPos;
            Vector2 hidePos;
            Vector2 pos;
            Vector2 lastPos;

            public ConditionInstance(BuffConditionHUD conditionHUD, Condition bindCondition, int index)
            {
                this.conditionHUD = conditionHUD;
                this.bindCondition = bindCondition;
                this.index = index;

                hoverPos = conditionHUD.TopLeft + new Vector2(20, -30 * index);
                hidePos = hoverPos + Vector2.right * 100f;

                InitSprites();
                Refresh(bindCondition, true);
                if (bindCondition.Finished)
                    Complete(bindCondition);
            }

            public void Refresh(Condition condition, bool forceUpdateText = false)
            {
                if (bindCondition != condition)
                    return;

                setText = condition.DisplayName(Custom.rainWorld.inGameTranslator) + " " +
                             condition.DisplayProgress(Custom.rainWorld.inGameTranslator);

                if ((show && animFinished) || forceUpdateText)
                    SetText(setText);

                RefreshColor(complete);
                BuffPlugin.Log($"{condition.ID} RefreshColor to false in Refresh, setComplete {setComplete}");
            }

            public void Complete(Condition condition)
            {
                if (bindCondition != condition)
                    return;
                BuffPlugin.Log("Complete");
                RefreshColor(true);
                BuffPlugin.Log($"{bindCondition.ID} RefreshColor to true in Complete, setComplete {setComplete}");
            }

            public void Uncomplete(Condition condition)
            {
                if (bindCondition != condition)
                    return;
                BuffPlugin.Log("UnComplete");
                RefreshColor(false);
                BuffPlugin.Log($"{bindCondition.ID} RefreshColor to false in UnComplete, setComplete {setComplete}");
            }

            void Flash()
            {
                flashCounter = MaxFlashTimer;
            }

            void SetText(string text)
            {
                textLabel.text = text;
                flatLight.scaleX = textLabel.textRect.width / 10f;
                flatLight.scaleY = textLabel.textRect.height / 15f;
            }

            void RefreshColor(bool complete)
            {
                setComplete = complete;
                if (show)
                {
                    if (animFinished)//动画完成，可以直接刷新颜色
                    {
                        stayDisplayTimer = MaxStayDisplayTimer;//重置显示时间
                        InternalRefreshColor(complete);
                        BuffPlugin.Log($"{bindCondition.ID} InternalRefreshColor to {complete} in RefreshColor, setComplete {setComplete}");
                    }
                    else//动画未完成稍后刷新颜色
                        needColorRefresh = true;
                }
                else
                {
                    show = true;
                    animFinished = false;
                    needColorRefresh = true;
                }
            }

            public void SetShow()
            {
                if (!show)
                    animFinished = false;
                show = true;
                stayDisplayTimer = MaxStayDisplayTimer;
            }

            void InternalRefreshColor(bool newCompleteState)
            {
                currentColor = highLightColor;
                if(newCompleteState)
                {
                    targetColor = completeColor;
                }
                else
                {
                    targetColor = normalColor;
                    if (complete)//曾经完成，设置为未完成表明发生了状态变化，否则为普通刷新
                        currentColor = uncompleteColor;
                }
                complete = newCompleteState;
            }

            public void InitSprites()
            {
                flatLight = new FSprite("Futile_White", true)
                {
                    color = Color.white,
                    alpha = 0f,
                    anchorX = 0f,
                    anchorY = 1f,
                    shader = Custom.rainWorld.Shaders["FlatLight"]
                };
                textLabel = new FLabel(Custom.GetDisplayFont(), "")
                {
                    anchorX = 0,
                    anchorY = 1f,
                    alignment = FLabelAlignment.Left
                };
                conditionHUD.Container.AddChild(flatLight);
                conditionHUD.Container.AddChild(textLabel);
            }

            public void Update()
            {
                lastShowTimer = showTimer;
                lastPos = pos;
                lastFlashCounter = flashCounter;


                if (show)
                {
                    //展开过程
                    if(showTimer < MaxShowAnimTimer)
                    {
                        showTimer++;
                        animFinished = false;
                        UpdatePosition();
                    }
                    else if(showTimer == MaxShowAnimTimer)
                    {
                        showTimer++;
                        animFinished = true;
                        UpdatePosition();
                        Flash();
                        if (conditionHUD.currentMode == Mode.Refresh)
                        {
                            stayDisplayTimer = MaxStayDisplayTimer;
                            if(needColorRefresh)
                            {
                                SetText(setText);
                                InternalRefreshColor(setComplete);
                                //BuffPlugin.Log($"InternalRefreshColor to {setComplete} in Update");
                                needColorRefresh = false;
                            }
                        }
                    }
                    else
                    {
                        //刷新模式无元素改变后自动隐藏
                        if(conditionHUD.currentMode == Mode.Refresh)
                        {
                            if (stayDisplayTimer > 0)
                                stayDisplayTimer--;
                            else
                            {
                                show = false;
                            }
                        }
                    }
                }
                else
                {
                    if (showTimer > 0)
                    {
                        showTimer--;
                        animFinished = false;
                        UpdatePosition();
                    }
                    else if(showTimer == 0)
                    {
                        showTimer--;
                        animFinished = true;
                        UpdatePosition();
                    }
                }

                if(flashCounter > 0)
                {
                    flashCounter--;
                }

                if (currentColor != targetColor)
                {
                    currentColor = Color.Lerp(currentColor, targetColor, 0.25f);

                    textLabel.color = currentColor;
                    flatLight.color = currentColor;

                    if (Mathf.Abs(currentColor.r - targetColor.r) < 0.01f && Mathf.Abs(currentColor.g - targetColor.g) < 0.01f && Mathf.Abs(currentColor.b - targetColor.b) < 0.01f){
                        currentColor = targetColor;
                    }
                }

                void UpdatePosition()
                {
                    pos = Vector2.Lerp(hidePos, hoverPos, Helper.LerpEase(ShowFactor));
                }
            }

            public void DrawSprites(float timeStacker)
            {
                if(pos != lastPos)
                {
                    Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
                    textLabel.SetPosition(smoothPos);
                    flatLight.SetPosition(smoothPos);
                }
                if(lastShowTimer != showTimer)
                {
                    float smoothAlpha = Mathf.Lerp(LastShowFactor, ShowFactor, timeStacker);
                    textLabel.alpha = smoothAlpha;
                }
                if(lastFlashCounter != flashCounter)
                {
                    flatLight.alpha = FlashFactor;
                }
            }

            public void Destroy()
            {
                textLabel.RemoveFromContainer();
            }
        }

        class FlagBanner
        {
            static int showCounter = 80;
            static Vector2 flagRect = new Vector2(300f, 100f);

            BuffConditionHUD hud;

            RandomBuffFlag flag;
            RandomBuffFlagRenderer renderer;

            FLabel flagInfo;
            CardTitle cardTitle;

            int lastCounter;
            int counter;
            float ShowFactor => counter / (float)showCounter;
            float LastShowFactor => lastCounter / (float)showCounter;

            float textAlpha;
            float lastTextAlpha;
            float setTextAlpha;

            Vector2 flagHangPos;

            public FlagBanner(BuffConditionHUD hud)
            {
                this.hud = hud;

                Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
                Vector2 middleScreen = screenSize / 2f;
                flagHangPos = new Vector2(middleScreen.x , screenSize.y) - new Vector2(flagRect.x / 2, 0);

                flag = new RandomBuffFlag(new IntVector2(40, 20), flagRect);
                renderer = new RandomBuffFlagRenderer(flag, RandomBuffFlagRenderer.FlagType.OuterTriangle, RandomBuffFlagRenderer.FlagColorType.Golden) { pos = flagHangPos, customAlpha = true, alpha = 0f, lastAlpha = 0f};

                hud.Container.AddChild(renderer.container);
                flagInfo = new FLabel(Custom.GetDisplayFont(), BuffResourceString.Get("BuffConditionHUD_WinInfo")) { anchorX = 0.5f, anchorY = 1f, alpha = 0f };
                hud.Container.AddChild(flagInfo);
                cardTitle = new CardTitle(hud.Container, BuffCard.normalScale * 0.25f, flagHangPos + new Vector2(flagRect.x / 2f, -40f));
            }

            public void Update()
            {
                if(counter > 0)
                {
                    flag.Update();
                    renderer.Update();
                    cardTitle.Update();
                }

                bool lateFlagMode = hud.flagMode;
                foreach(var conditionInstanec in hud.instances)
                {
                    lateFlagMode &= !conditionInstanec.show;
                }

                renderer.Show = lateFlagMode;
                lastCounter = counter;
                if (lateFlagMode && counter < showCounter)
                {
                    counter++;
                    if (counter == showCounter)
                    {
                        cardTitle.RequestSwitchTitle(BuffResourceString.Get("BuffConditionHUD_WinTitle"));
                        setTextAlpha = 1f;
                    }
                }
                else if (!lateFlagMode && counter > 0)
                {
                    if(setTextAlpha != 0)
                    {
                        setTextAlpha = 0f;
                        cardTitle.RequestSwitchTitle("");
                    }
                    counter--;
                }

                lastTextAlpha = textAlpha;
                if(textAlpha != setTextAlpha)
                {
                    textAlpha = Mathf.Lerp(textAlpha, setTextAlpha, 0.15f);
                    if(Mathf.Approximately(textAlpha, setTextAlpha))
                        textAlpha = setTextAlpha;
                }
            }

            public void DrawSprites(float timeStacker)
            {
                if(counter > 0 && (lastCounter > 0|| hud.flagMode))
                {
                    renderer.GrafUpdate(timeStacker);
                    cardTitle.GrafUpdate(timeStacker);

                    float smoothT = Mathf.Lerp(LastShowFactor, ShowFactor, timeStacker);

                    Vector2 smoothHangPos = Vector2.Lerp(flagHangPos + Vector2.up * 300f, flagHangPos, Helper.LerpEase(smoothT));
                    renderer.pos = smoothHangPos;
                    renderer.alpha = ShowFactor;
                    renderer.lastAlpha = LastShowFactor;

                    flagInfo.SetPosition(smoothHangPos + new Vector2(flagRect.x / 2f, -60f));
                    flagInfo.alpha = Mathf.Lerp(lastTextAlpha, textAlpha, timeStacker);
                }
            }

            public void Destroy()
            {
                renderer.container.RemoveAllChildren();
                renderer.container.RemoveFromContainer();
            }
        }

        internal enum Mode
        {
            Alway,
            Refresh
        }
    }
}
