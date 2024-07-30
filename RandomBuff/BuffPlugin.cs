using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using BepInEx;
using RandomBuff.Cardpedia;
using BepInEx.Logging;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Hooks;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuff.Render.CardRender;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using UDebug = UnityEngine.Debug;
using Random = UnityEngine.Random;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.CosmeticUnlocks;
using RandomBuff.Core.Progression.Quest.Condition;
using RandomBuff.Render.UI.Component;
using RandomBuffUtils.FutileExtend;
using RandomBuff.Render.Quest;
using System.Drawing;
using Steamworks;
using RandomBuff.Render.UI;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

//添加友元方便调试
[assembly: InternalsVisibleTo("BuiltinBuffs")]
[assembly: InternalsVisibleTo("BuffTest")]

namespace RandomBuff
{
    [BepInPlugin(ModId, "Random Buff", "1.0.0")]
    internal class BuffPlugin : BaseUnityPlugin
    {
        public static BuffFormatVersion saveVersion = new ("a-0.0.6");

        public static BuffFormatVersion outDateVersion = new("a-0.0.3");

        internal static ManualLogSource LogInstance { get; private set; }

        internal static BuffPlugin Instance { get; private set; }

        public static bool AllCardDisplay { get; private set; }

        public const string ModId = "randombuff";

        public void OnEnable()
        {
            LogInstance = this.Logger;
            Instance = this;
            try
            {
                On.RainWorld.OnModsInit += RainWorld_OnModsInit;
                On.RainWorld.PostModsInit += RainWorld_PostModsInit;
           
            }
            catch (Exception e)
            {
                Logger.LogFatal(e.ToString());
            }
        }

        private void Update()
        {
            CardRendererManager.UpdateInactiveRendererTimers(Time.deltaTime);
            ExceptionTracker.Singleton?.Update();
            
            SoapBubblePool.UpdateInactiveItems();
            FakeFoodPool.UpdateInactiveItems();
        }

        private FStage devVersion;

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {

            try
            {
                if (!isLoaded)
                {
                    if(File.Exists(AssetManager.ResolveFilePath("randombuff.log")))
                        File.Delete("randombuff.log");
                     
                    File.Create(AssetManager.ResolveFilePath("buffcore.log")).Close();
                }
            }
            catch (Exception e)
            {
                canAccessLog = false;
                Logger.LogFatal(e.ToString());
                UnityEngine.Debug.LogException(e);
            }
          
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                LogException(e);
            }

            OnModsInit();
        }

        private void OnModsInit()
        {
            try
            {
                if (!isLoaded)
                {
                    Log($"[Random Buff], version: {saveVersion}, {System.DateTime.Now}");

                    if (File.Exists(AssetManager.ResolveFilePath("buff.dev")))
                    {
                        DevEnabled = true;
                        LogWarning("Debug Enable");
                    }

                    if (File.Exists(AssetManager.ResolveFilePath("buffallcards.txt")))
                    {
                        AllCardDisplay = true;
                        LogWarning("Displayed all cards");

                    }

                    Application.logMessageReceived += Application_logMessageReceived;

                    BuffUIAssets.LoadUIAssets();

                    CardBasicAssets.LoadAssets();
                    CosmeticUnlock.LoadIconSprites();
                    BuffResourceString.Init();

                    GachaTemplate.Init();
                    Condition.Init();
                    InputAgency.Init();
                    TypeSerializer.Init();
                    QuestCondition.Init();
                    CosmeticUnlock.Init();
                    QuestRendererManager.Init();

                    BuffFile.OnModsInit();
                    CoreHooks.OnModsInit();
                    BuffRegister.InitAllBuffPlugin();


                    BuffUtils.OnEnable();

                    //TODO : 测试用
                    //DevEnabled = true;

                    CardpediaMenuHooks.Hook();
                    CardpediaMenuHooks.LoadAsset();
                    SoapBubblePool.Hook();

                    AnimMachine.Init();

                    StartCoroutine(ExceptionTracker.LateCreateExceptionTracker());

                    isLoaded = true;

                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

      

        private void RainWorld_PostModsInit(On.RainWorld.orig_PostModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                LogException(e);
            }
            try
            {
                if (!isPostLoaded)
                {
                    if (!isLoaded)
                    {
                        LogError("Fallback Call OnModsInit");
                        OnModsInit();
                        if (!isLoaded)
                        {
                            LogFatal("Can't call OnModsInit !!!!!!");
                            return;
                        }
                    }
                    //延迟加载以保证其他plugin的注册完毕后再加载
                    BuffConfigManager.InitBuffStaticData();
                    BuffConfigManager.InitTemplateStaticData();
                    BuffRegister.LoadBuffPluginAsset();

                    //这个会用到template数据（嗯
                    MissionRegister.RegisterAllMissions();
                    BuffConfigManager.InitQuestData();

                    BuffRegister.BuildAllDataStaticWarpper();

                    /****************************************/

                    On.StaticWorld.InitCustomTemplates += orig =>
                    {
                        orig();
                        TMProFLabel label = new TMProFLabel(CardBasicAssets.TitleFont, $"Random Buff, Build: 2024_07_29_2\nUSER: {SteamUser.GetSteamID().GetAccountID().m_AccountID},{SteamFriends.GetPersonaName()}", new Vector2(1000,200), 0.4f)
                        {
                            Alignment = TMPro.TextAlignmentOptions.BottomLeft,
                            Pivot = new Vector2(0f, 0f),
                            y = 5,
                            x = 5,
                            alpha = 0.3f
                        };
                        Futile.AddStage(devVersion = new FStage("BUFF_DEV"));
                        devVersion.AddChild(label);
                    };
                    /****************************************/
                    isPostLoaded = true;
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }


        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception)
                ExceptionTracker.TrackExceptionNew(stackTrace,condition);
            
        }

        private static bool isLoaded = false;
        private static bool isPostLoaded = false;
        private static bool canAccessLog = true;

        internal static bool DevEnabled { get; private set; }



        /// <summary>
        /// 会额外保存到../RainWorld_Data/StreamingAssets/buffcore.log
        /// </summary>
        /// <param name="message"></param>
        internal static void Log(object message)
        {
            UnityEngine.Debug.Log($"[RandomBuff] {message}");
            if(canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Message]\t{message}\n");
           
        }

        internal static void LogDebug(object message)
        {
            if (DevEnabled)
            {
                UnityEngine.Debug.Log($"[RandomBuff] {message}");
            }
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Debug]\t\t{message}\n");

        }

        internal static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Warning]\t{message}\n");
        }

        internal static void LogError(object message)
        {
            UnityEngine.Debug.LogError($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Error]\t\t{message}\n");
        }

        internal static void LogFatal(object message)
        {
            UnityEngine.Debug.LogError($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{message}\n");

        }

        internal static void LogException(Exception e)
        {
            UnityEngine.Debug.LogException(e);
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{e.Message}\n{e.StackTrace}\n");
        }

        internal static void LogException(Exception e,object m)
        {
            UnityEngine.Debug.LogException(e);
            if (canAccessLog)
            {
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{e.Message}\n");
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"       \t\t{m}\n");
            }
            UnityEngine.Debug.LogError(m);
        }
    }

    /// <summary>
    /// 用来简化应用钩子的过程（懒得自己写了）
    /// 继承这个类并且编写一个名为 HooksOn 的公共静态方法即可
    /// </summary>
    internal class HooksApplier
    {
        internal static void ApplyHooks()
        {
            var applierType = typeof(HooksApplier);
            var types = applierType.Assembly.GetTypes().Where(a => a.BaseType == applierType && a != applierType);
            foreach (var t in types)
            {
                try
                {
                    t.GetMethod("HooksOn", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
