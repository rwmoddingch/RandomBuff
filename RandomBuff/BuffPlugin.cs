
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Option;
using Steamworks;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.ExceptionTracker;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Wawa;


#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618


//添加友元方便调试
[assembly: InternalsVisibleTo("BuiltinBuffs")]
[assembly: InternalsVisibleTo("ExpeditionExtend")]
[assembly: InternalsVisibleTo("BombExpand")]
[assembly: InternalsVisibleTo("LobotomyCorporationPack")]
namespace RandomBuff
{
    [BepInPlugin(ModId, "Random Buff", ModVersion)]
    internal partial class BuffPlugin : BaseUnityPlugin
    {
        public static BuffFormatVersion saveVersion = new("a-0.0.6");

        public static BuffFormatVersion outDateVersion = new("a-0.0.3");

        public static BuffOptionInterface Option { get; private set; }

        public const string ModId = "randombuff";

        public const string ModVersion = "1.1.2";

        public static string CacheFolder { get; private set; }

        public static string SaveFolder { get; private set; }

        internal static ManualLogSource LogInstance { get; private set; }
        internal static BuffPlugin Instance { get; private set; }



        private static bool isLoaded = false;
        private static bool canAccessLog = true;

        public static string GameVersion;

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

            var replace = File.ReadAllLines("doorstop_config.ini").First(i => 
                i.Contains("enabledVersionPath=")).Replace("enabledVersionPath=","");
            GameVersion = File.ReadAllText(replace);
            
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
                    HookILCursor();
                    
                    basePath = ModManager.ActiveMods.First(i => i.id == ModId).basePath;
                    Log($"Version: {ModVersion},Game Version:{GameVersion}," +
                        $" Current save version: {saveVersion}, {DateTime.Now}");

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
                    WawaSaveData.OnModsInit();
                    

                    BuffConfigManager.InitBuffPluginInfo();


                    QuestUnlockedType.Init();
                    Wawa.Wawa.Init();

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
                                $"Random Buff,TEST Build: 2025_03_19\nUSER: {SteamUser.GetSteamID().GetAccountID().m_AccountID},{SteamFriends.GetPersonaName()}",
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
        private static void CleanAllBuffs()
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

            BuffRegister.InitAllBuffPlugins();
            BuffRegister.BuildAllBuffConfigWarpper();
            BuffConfigManager.InitBuffStaticData();

            BuffCore.AfterBuffReloadedInternal(BuffCore.GetAllEnabledBuffPlugins());
            BuffRegister.CheckAndRemoveInvalidBuff();


            GachaTemplate.Init();
            Condition.Init();
            QuestCondition.Init();

            BuffConfigManager.InitTemplateStaticData();
            MissionRegister.RegisterAllMissions();

            BuffConfigManager.InitQuestData();

            BuffRegister.LoadBuffPluginAsset();
            
        }

        /// <summary>
        /// 更新启用列表
        /// </summary>
        /// <param name="list"></param>
        internal static void UpdateNewEnableList(string[] list)
        {
            EnabledPlugins = list.ToHashSet();
            File.WriteAllLines((SaveFolder + Path.AltDirectorySeparatorChar + "EnableBuffPlugins.txt"), list);
        }

        /// <summary>
        /// 禁用buffPlugin
        /// </summary>
        /// <param name="name"></param>
        internal static void DisablePlugin(string name)
        {
            EnabledPlugins.Remove(name);
        }
        
        /// <summary>
        /// 启用buffPlugin
        /// </summary>
        /// <param name="name"></param>
        internal static void EnablePlugin(string name)
        {
            EnabledPlugins.Add(name);
        }

        private void Update()
        {
            CardRendererManager.UpdateInactiveRendererTimers(Time.deltaTime);
            ExceptionTracker.Singleton?.Update();
            BuffExceptionTracker.Singleton?.RawUpdate();

            SoapBubblePool.UpdateInactiveItems();
            FakeFoodPool.UpdateInactiveItems();
            if (Input.GetKey(KeyCode.Z) && Input.GetKeyDown(KeyCode.K) && DevEnabled)
            {
                TestHookForBuff();
                TestHookForConditionForBuff();
            }
        }
        
        
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && BuffOptionInterface.Instance.ShowExceptionLog.Value)
                ExceptionTracker.TrackExceptionNew(stackTrace, condition);

        }

        private void CheckBuffPluginVersion()
        {
            CacheFolder = basePath + Path.AltDirectorySeparatorChar + "buffcaches";
            SaveFolder = Application.persistentDataPath + Path.AltDirectorySeparatorChar + "RandomBuff";
            
            if (!Directory.Exists(CacheFolder))
                Directory.CreateDirectory(CacheFolder);
            if (!Directory.Exists(SaveFolder))
                Directory.CreateDirectory(SaveFolder);


            if (File.Exists(Path.Combine(SaveFolder, "BuffPluginVersion.txt")))
            {
                bool hasDeleteAll = false;
                var lines = File.ReadAllLines(Path.Combine(SaveFolder, "BuffPluginVersion.txt")).ToList();
                Dictionary<string, string> lastVersion;

                try
                {
                    lastVersion = lines.ToDictionary(i => i.Split('|')[0], i => i.Split('|')[1]);
                }
                catch (Exception _)
                {
                    lastVersion = new();
                    LogError("Corrupted BuffPluginVersion.txt");
                    lines.Clear();
                    foreach (var all in Directory.GetFiles(CacheFolder, $"*"))
                    {
                        File.Delete(all);
                        hasDeleteAll = true;
                    }
                    
                }

                foreach (var mod in ModManager.ActiveMods.Where(i =>
                             Directory.Exists(Path.Combine(i.BestMatchedPath("buffplugins"), "buffplugins")) ||
                             Directory.Exists(Path.Combine(i.BestMatchedPath("buffassets"), "buffassets"))))
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

                File.WriteAllLines(Path.Combine(SaveFolder, "BuffPluginVersion.txt"), lines);
            }
            else
            {
                foreach (var all in Directory.GetFiles(CacheFolder, $"*"))
                    File.Delete(all);
                File.WriteAllLines(Path.Combine(SaveFolder, "BuffPluginVersion.txt"), ModManager.ActiveMods.Where(i =>
                        Directory.Exists(Path.Combine(i.BestMatchedPath("buffplugins"), "buffplugins")) ||
                        Directory.Exists(Path.Combine(i.BestMatchedPath("buffassets"), "buffassets")))
                    .Select(i => $"{i.id}|{i.version}")
                    .ToArray());
            }

        }

        private static void LoadEnabledPlugins()
        {
            if (!File.Exists(SaveFolder + Path.AltDirectorySeparatorChar + "EnableBuffPlugins.txt"))
            {
                File.WriteAllLines((SaveFolder + Path.AltDirectorySeparatorChar + "EnableBuffPlugins.txt"), new[]
                {
                    "BuiltinBuffs",
                    "BombExpand",
                    "ExpeditionExtend",
                });
            }

            EnabledPlugins = File.ReadAllLines(SaveFolder + Path.AltDirectorySeparatorChar + "EnableBuffPlugins.txt").ToHashSet();
            foreach (var id in EnabledPlugins)
                BuffPlugin.Log($"Enabled Buff Plugins: [{id}]");
            
        }
    }




    internal partial class BuffPlugin
    {

        internal static bool IsPluginsEnabled(string assemblyName)
        {
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

    internal partial class BuffPlugin
    {
        void HookILCursor()
        {
                            
            _ = new Hook(typeof(ILCursor).GetMethod(nameof(ILCursor.TryGotoNext),new []{typeof(MoveType),typeof(Func<Instruction, bool>[])}),
                (Func<ILCursor,MoveType, Func<Instruction, bool>[], bool> orig,
                    ILCursor self, MoveType moveType,
                    params  Func<Instruction, bool>[] predicates) =>
                {
                    if (!orig(self,moveType,predicates))
                        if (BuffUtils.ForceGoto)
                            throw new KeyNotFoundException();
                        else
                            return false;
                    return true;
                });
            _ = new Hook(typeof(ILCursor).GetMethod(nameof(ILCursor.TryGotoPrev),new []{typeof(MoveType),typeof(Func<Instruction, bool>[])}),
                (Func<ILCursor,MoveType, Func<Instruction, bool>[], bool> orig,
                    ILCursor self, MoveType moveType,
                    params Func<Instruction, bool>[] predicates) =>
                {
                    if (!orig(self, moveType, predicates))
                        if (BuffUtils.ForceGoto)
                            throw new KeyNotFoundException();
                        else
                            return false;
                    return true;
                });
        }
        void TestHookForBuff()
        {
            string str = "";
            foreach (var a in BuffID.values.entries)
            {
                var (re, msg) = TestSingle(a);
                if (re)
                {
                    str +=msg;
                }
            }
            (bool re, string message) TestSingle(string a)
            {
                bool re = false;
                string message = $"{a}\n";
                BuffID b = new(a);
                try
                {
                    BuffHookWarpper.EnableBuff(b, HookLifeTimeLevel.InGame);
          
                }
                catch (Exception e)
                {
                    message += "----INGAME----\n" + e.ToString() + "\n";
                    re = true;
                }
                try
                {
                    BuffHookWarpper.EnableBuff(b, HookLifeTimeLevel.UntilQuit);
                }
                catch (Exception e)
                {
                    message +=  "----UntilQuit----\n" + e.ToString() + "\n";
                    re = true;
                }
                try
                {
                    BuffHookWarpper.DisableBuff(b, HookLifeTimeLevel.InGame);
                }
                catch (Exception e) { }
                try
                {
                    BuffHookWarpper.DisableBuff(b, HookLifeTimeLevel.UntilQuit);
                }
                catch (Exception e) { }

                BuffPlugin.Log($"{a}, {re}");
                return (re, message);
            }
            File.WriteAllText("TestOutput.txt",str);
        }

        void TestHookForConditionForBuff()
        {
            string str = "";
            foreach (var a in ConditionID.values.entries)
            {
                var (re, msg) = TestSingleC(a);
                if (re)
                {
                    str +=msg;
                }
            }
            (bool re, string message) TestSingleC(string a)
            {
                bool re = false;
                string message = $"{a}\n";
                var c = (Condition)Activator.CreateInstance(BuffRegister.GetConditionType(new(a)).Type);
                try
                {
                    c.HookOn();
          
                }
                catch (Exception e)
                {
                    message += "----INGAME----\n" + e.ToString() + "\n";
                    re = true;
                }
           
                try
                {
                    c.DisableHook();
                }
                catch (Exception e) { }
                BuffPlugin.Log($"{a}, {re}");
                return (re, message);
            }
            File.WriteAllText("TestOutputC.txt",str);
        }

       
    }
}
