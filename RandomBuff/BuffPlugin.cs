
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using BepInEx;
using RandomBuff.Cardpedia;
using BepInEx.Logging;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Hooks;
using RandomBuff.Core.SaveData;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuff.Render.CardRender;
using RandomBuffUtils;
using UnityEngine;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.CosmeticUnlocks;
using RandomBuff.Core.Progression.Quest.Condition;
using RandomBuff.Render.UI.Component;
using RandomBuff.Render.Quest;
using Kittehface.Framework20;
using RandomBuff.Core.Option;
using Steamworks;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.ExceptionTracker;
using RandomBuff.Core.Progression.Quest;


#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


//添加友元方便调试
[assembly: InternalsVisibleTo("BuiltinBuffs")]
[assembly: InternalsVisibleTo("ExpeditionExtend")]
[assembly: InternalsVisibleTo("BombExpand")]
namespace RandomBuff
{
    [BepInPlugin(ModId, "Random Buff", ModVersion)]
    internal partial class BuffPlugin : BaseUnityPlugin
    {
        public static BuffFormatVersion saveVersion = new("a-0.0.6");

        public static BuffFormatVersion outDateVersion = new("a-0.0.3");

        public static BuffOptionInterface Option { get; private set; }

        public const string ModId = "randombuff";

        public const string ModVersion = "1.1.0";

        public static string CacheFolder { get; private set; }


        internal static ManualLogSource LogInstance { get; private set; }
        internal static BuffPlugin Instance { get; private set; }



        private static bool isLoaded = false;
        private static bool canAccessLog = true;

#if TESTVERSION
        internal static bool DevEnabled => true;
        private FStage devVersion;
#else
        internal static bool DevEnabled => false;
#endif


        public void OnEnable()
        {
            LogInstance = this.Logger;
            Instance = this;

            try
            {
                On.RainWorld.OnModsInit += RainWorld_OnModsInit;
                Option = new BuffOptionInterface();


            }
            catch (Exception e)
            {
                Logger.LogFatal(e.ToString());
            }
        }




        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {

            try
            {
                if (!isLoaded)
                {
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

                    basePath = ModManager.ActiveMods.First(i => i.id == ModId).basePath;
                    Log($"Version: {ModVersion}, Current save version: {saveVersion}, {DateTime.Now}");



                    CheckBuffPluginVersion();


#if TESTVERSION
                    Log($"!!!!TEST BUILD!!!!");

#endif
                    Application.logMessageReceived += Application_logMessageReceived;

                    BuffUIAssets.LoadUIAssets();
                    CardBasicAssets.LoadAssets();
                    CosmeticUnlock.LoadIconSprites();
                    CardpediaMenuHooks.LoadAsset();

                    BuffUtils.OnEnable();

                    BuffResourceString.Init();

                    InputAgency.Init();
                    TypeSerializer.Init();
                    CosmeticUnlock.Init();
                    QuestRendererManager.Init();

                    BuffFile.OnModsInit();
                    CoreHooks.OnModsInit();
                    SoapBubblePool.Hook();
                    AnimMachine.Init();


                    BuffConfigManager.InitBuffPluginInfo();


                    QuestUnlockedType.Init();


                    MachineConnector.SetRegisteredOI(ModId, Option);
                    StartCoroutine(ExceptionTracker.LateCreateExceptionTracker());

                    ReloadAllBuffs();
#if TESTVERSION
                    On.StaticWorld.InitCustomTemplates += orig =>
                    {
                        orig();

                        if (devVersion == null)
                        {
                            TMProFLabel label = new TMProFLabel(CardBasicAssets.TitleFont,
                                $"Random Buff, Build: 2024_08_24\nUSER: {SteamUser.GetSteamID().GetAccountID().m_AccountID},{SteamFriends.GetPersonaName()}",
                                new Vector2(1000, 200), 0.4f)
                            {
                                Alignment = TMPro.TextAlignmentOptions.BottomLeft,
                                Pivot = new Vector2(0f, 0f),
                                y = 5,
                                x = 5,
                                alpha = 0.3f
                            };

                            Futile.AddStage(devVersion = new FStage("BUFF_DEV"));
                            devVersion.AddChild(label);
                        }

                    };
                    foreach (var file in Directory.GetFiles(UserData.GetPersistentDataPath(), "sav*"))
                    {
                        if (int.TryParse(Path.GetFileName(file).Substring(3), out var slot))
                        {
                            if (slot >= 100)
                            {
                                if (!File.Exists($"{UserData.GetPersistentDataPath()}/buffMain{slot - 1}"))
                                    File.Copy(file, $"{UserData.GetPersistentDataPath()}/buffMain{slot - 1}");
                                File.Delete(file);
                            }
                        }
                    }

#endif
                    isLoaded = true;

                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
        }

    }

    internal partial class BuffPlugin 
    {

        private static HashSet<string> EnabledPlugins { get; set; }

        private static string basePath;

        /// <summary>
        /// 清除全部Buff
        /// </summary>
        internal static void CleanAllBuffs()
        {
            MissionRegister.CleanAll();
            BuffRegister.CleanAll();
            BuffHookWarpper.CleanAll();
            BuffConfigManager.CleanAll();
            QuestCondition.CleanAll();

        }

        /// <summary>
        /// 重新加载全部Buff
        /// </summary>
        internal static void ReloadAllBuffs()
        {
            BuffPlugin.Log("Reload All buff plugins");
            CleanAllBuffs();

            LoadEnabledPlugins();

            BuffRegister.InitAllBuffPlugin();
            BuffRegister.BuildAllBuffConfigWarpper();
            BuffConfigManager.InitBuffStaticData();
            BuffRegister.CheckAndRemoveInvalidBuff();


            GachaTemplate.Init();
            Condition.Init();
            QuestCondition.Init();

            BuffConfigManager.InitTemplateStaticData();
            MissionRegister.RegisterAllMissions();

            BuffConfigManager.InitQuestData();

            BuffRegister.LoadBuffPluginAsset();

        }

        internal static void UpdateNewEnableList(string[] list)
        {
            File.WriteAllLines((basePath + Path.AltDirectorySeparatorChar + "EnablePlugins"), list);
        }

        private void Update()
        {
            CardRendererManager.UpdateInactiveRendererTimers(Time.deltaTime);
            ExceptionTracker.Singleton?.Update();
            BuffExceptionTracker.Singleton?.RawUpdate();

            SoapBubblePool.UpdateInactiveItems();
            FakeFoodPool.UpdateInactiveItems();

        }


        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && BuffOptionInterface.Instance.ShowExceptionLog.Value)
                ExceptionTracker.TrackExceptionNew(stackTrace, condition);

        }



        private void CheckBuffPluginVersion()
        {
            CacheFolder = basePath + Path.AltDirectorySeparatorChar + "buffcaches";
            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);

            if (File.Exists(Path.Combine(CacheFolder, "buffVersion")))
            {
                bool hasDeleteAll = false;
                var lines = File.ReadAllLines(Path.Combine(CacheFolder, "buffVersion")).ToList();
                var lastVersion = lines.ToDictionary(i => i.Split('|')[0], i => i.Split('|')[1]);

                foreach (var mod in ModManager.ActiveMods.Where(i =>
                             Directory.Exists(Path.Combine(i.basePath, "buffplugins")) ||
                             Directory.Exists(Path.Combine(i.basePath, "buffassets"))))
                {
                    if (lastVersion.TryGetValue(mod.id, out var version))
                    {
                        if (version != mod.version)
                        {
                            lines.Add($"{mod.id}|{mod.version}");
                            lines.Remove($"{mod.id}|{version}");
                            BuffPlugin.Log(
                                $"Enabled mod version changed : [{mod.id},{mod.version}], last version:{version}");
                            if (!hasDeleteAll)
                            {
                                foreach (var all in Directory.GetFiles(CacheFolder, $"*"))
                                {
                                    File.Delete(all);
                                    hasDeleteAll = true;
                                }
                            }
                        }
                    }
                    else
                    {
                        lines.Add($"{mod.id}|{mod.version}");
                        BuffPlugin.Log($"New enable mod : [{mod.id},{mod.version}");
                    }
                }

                File.WriteAllLines(Path.Combine(CacheFolder, "buffVersion"), lines);
            }
            else
            {
                foreach (var all in Directory.GetFiles(CacheFolder, $"*"))
                    File.Delete(all);
                File.WriteAllLines(Path.Combine(CacheFolder, "buffVersion"), ModManager.ActiveMods.Where(i =>
                        Directory.Exists(Path.Combine(i.basePath, "buffplugins")) ||
                        Directory.Exists(Path.Combine(i.basePath, "buffassets")))
                    .Select(i => $"{i.id}|{i.version}")
                    .ToArray());
            }

        }

        private static void LoadEnabledPlugins()
        {
            if (!File.Exists(basePath + Path.AltDirectorySeparatorChar + "EnablePlugins"))
            {
                File.WriteAllLines((basePath + Path.AltDirectorySeparatorChar + "EnablePlugins"), new[]
                {
                    "BuiltinBuffs",
                    "ExpeditionExtend"
                });
            }

            EnabledPlugins = File.ReadAllLines(basePath + Path.AltDirectorySeparatorChar + "EnablePlugins").ToHashSet();
            foreach (var id in EnabledPlugins)
                BuffPlugin.Log($"Enabled Buff Plugins: [{id}]");
            
        }
    }




    internal partial class BuffPlugin
    {

        internal static bool IsPluginsEnabled(string assemblyName)
        {
            BuffPlugin.LogDebug($"Try {assemblyName}, {EnabledPlugins.Contains(assemblyName)}");
            return EnabledPlugins.Contains(assemblyName);
        }


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

}
