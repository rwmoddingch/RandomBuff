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
        public static BuffFormatVersion saveVersion = new ("a-0.0.5");

        public static BuffFormatVersion outDateVersion = new("a-0.0.3");

        internal static ManualLogSource LogInstance { get; private set; }

        internal static BuffPlugin Instance { get; private set; }

        public const string ModId = "randombuff";

        public void OnEnable()
        {
            LogInstance = this.Logger;
            Instance = this;
            try
            {
                On.RainWorld.OnModsInit += RainWorld_OnModsInit;
                //On.RainWorld.PostModsInit += RainWorld_PostModsInit;

                try
                {
                    if (!isLoaded)
                    {
                        if (File.Exists(Application.streamingAssetsPath + Path.DirectorySeparatorChar + "randombuff.log"))
                            File.Delete("randombuff.log");

                        File.Create(Application.streamingAssetsPath + Path.DirectorySeparatorChar + "buffcore.log").Close();
                    }

                }
                catch (Exception e)
                {
                    canAccessLog = false;
                    Logger.LogFatal(e.ToString() + "\n" + e.StackTrace);
                    UDebug.LogException(e);
                }
                OnModsInit();
                On.RWCustom.Custom.Log += Custom_Log;
                On.RWCustom.Custom.LogImportant += Custom_LogImportant;
            }
            catch (Exception e)
            {
                Logger.LogFatal(e.ToString() + "\n" + e.StackTrace);
                UDebug.LogException(e);
            }
        }

        private void Custom_Log(On.RWCustom.Custom.orig_Log orig, string[] values)
        {
            BuffPlugin.Log(string.Concat("[RainWorld]",values));
        }

        private void Custom_LogImportant(On.RWCustom.Custom.orig_LogImportant orig, string[] values)
        {
            BuffPlugin.Log(string.Concat("[RainWorld]", values));
        }

        private void Update()
        {
            CardRendererManager.UpdateInactiveRendererTimers(Time.deltaTime);
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            BuffPlugin.Log("RainWorld_OnModsInit 1");
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                LogException(e);
            }
            BuffPlugin.Log("RainWorld_OnModsInit 2");

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
                    BuffRegister.BuildAllDataStaticWarpper();
                    BuffConfigManager.InitTemplateStaticData();
                    isPostLoaded = true;
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }

            BuffPlugin.Log("RainWorld_OnModsInit 3");


        }

        private void OnModsInit()
        {
            BuffPlugin.Log("OnModsInit 1");
            try
            {
                if (!isLoaded)
                {
                    BuffPlugin.Log("OnModsInit 2");
                    float a = 1f;
                    float b = 2f;

                    int c = 1;
                    int d = 2;

                    Log($"[Random Buff], version: {saveVersion}, {System.DateTime.Now}");

                    if (File.Exists(AssetManager.ResolveFilePath("buff.dev")))
                    {
                        DevEnabled = true;
                        LogWarning("Debug Enable");
                    }
                    CardBasicAssets.LoadAssets();
                    BuffResourceString.Init();

                    GachaTemplate.Init();
                    Condition.Init();
                    TypeSerializer.Init();

                    BuffFile.OnModsInit();
                    CoreHooks.OnModsInit();
                    BuffRegister.InitAllBuffPlugin();
                    Helper.DynamicImitator.SupportAndCreate(typeof(float));
                    Log($"Helper.DynamicImitator add : {Helper.DynamicImitator.Addition(1f, 2f)}");

                    BuffUtils.OnEnable();
                    if (DevEnabled)
                        On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
                    isLoaded = true;

                    var result = a * b;
                    var result1 = a + b;

                    var result2 = c + d;
                    var result3 = c * d;
                    //TODO : 测试用
                    //TestTypeSerializer();
                    DevEnabled = true;

                    Cardpedia.CardpediaMenuHooks.Hook();
                    CardpediaMenuHooks.LoadAsset();
                    BuffPlugin.Log("OnModsInit 3");
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
            BuffPlugin.Log("OnModsInit 4");
        }

        //private void TestTypeSerializer()
        //{
        //    Color color = Color.cyan;
        //    var se = TypeSerializer.GetSerializer<Color>().Serialize(color);
        //    Log(se);
        //    Log(TypeSerializer.GetSerializer<Color>().Deserialize(se));
        //    Log(TypeSerializer.GetSerializer<IntVector2>().Serialize(new IntVector2(1,2)));

        //}

        private void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig(self, dt);
            if (self.rainWorld.BuffMode() && Directory.Exists("Debug") && DevEnabled)
            {
                if (Input.GetKeyDown(KeyCode.K))
                {
                    BuffPoolManager.Instance.GameSetting.SaveGameSettingToPath("Debug/gameSetting.txt");
                    BuffPoolManager.Instance.CreateBuff(new BuffID("DeathFreeMedallion"));
                }
                if (Input.GetKeyDown(KeyCode.L))
                {
                    var gameSetting = new GameSetting(self.StoryCharacter);
                    var template = new MissionGachaTemplate();
                    gameSetting.gachaTemplate = template;
                    gameSetting.GetRandomCondition();
                    gameSetting.GetRandomCondition();

                    var re = BuffConfigManager.buffTypeTable.Values.ToList();
                    template.cardPick.Add(0,new List<string>{ re[0][Random.Range(0, re[0].Count)].value , re[0][Random.Range(0, re[0].Count)].value });
                    template.cardPick.Add(1, new List<string>{ re[1][Random.Range(0, re[1].Count)].value, re[1][Random.Range(0, re[1].Count)].value });
                    gameSetting.SaveGameSettingToPath("Debug/MissionSetting.txt");
                }
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
                    BuffRegister.BuildAllDataStaticWarpper();
                    BuffConfigManager.InitTemplateStaticData();
                    isPostLoaded = true;
                }
            }
            catch (Exception e)
            {
                LogException(e);
            }
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
            Debug.Log($"[RandomBuff] {message}");
            if(canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Message]\t{message}\n");
           
        }

        internal static void LogDebug(object message)
        {
            if (DevEnabled)
            {
                Debug.Log($"[RandomBuff] {message}");
                if (canAccessLog)
                    File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Debug]\t\t{message}\n");
            }

        }

        internal static void LogWarning(object message)
        {
            Debug.LogWarning($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Warning]\t{message}\n");
        }

        internal static void LogError(object message)
        {
            Debug.LogError($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Error]\t\t{message}\n");
        }

        internal static void LogFatal(object message)
        {
            Debug.Log($"[RandomBuff] {message}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{message}\n");

        }

        internal static void LogException(Exception e)
        {
            Debug.LogException(e);
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{e.ToString()}/*\n{e.StackTrace}*/\n");
          
        }

        internal static void LogException(Exception e,object m)
        {
            Debug.LogException(e);
            if (canAccessLog)
            {
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{e.Message}\n");
                File.AppendAllText(AssetManager.ResolveFilePath("buffcore.log"), $"[Fatal]\t\t{m}\n");
            }
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
