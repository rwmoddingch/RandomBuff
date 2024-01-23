﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using BepInEx;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Hooks;
using RandomBuff.Core.SaveData;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

//添加友元方便调试
[assembly: InternalsVisibleTo("BuffTest")]

namespace RandomBuff
{
    [BepInPlugin("randombuff", "Random Buff", "1.0.0")]
    class BuffPlugin : BaseUnityPlugin
    {
        public const string saveVersion = "a-0.0.1";

        public void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            try
            {
                orig(self);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            try
            {
                if (!isLoaded)
                {
               

                    if(File.Exists(AssetManager.ResolveFilePath("randomBuff.log")))
                        File.Delete(AssetManager.ResolveFilePath("randomBuff.log"));
                    File.Create(AssetManager.ResolveFilePath("randomBuff.log")).Close();

                    Log($"[Random Buff], version: {saveVersion}");

                    if (File.Exists(AssetManager.ResolveFilePath("buff.dev")))
                    {
                        DevEnabled = true;
                        LogWarning("Debug Enable");
                    }

                    BuffFile.OnModsInit();
                    CoreHooks.OnModsInit();
                    BuffRegister.InitAllBuffPlugin();
               
                    isLoaded = true;
                }
            }
            catch (Exception e)
            {

                Debug.LogException(e);
            }
        }

        private static bool isLoaded = false;


        internal static bool DevEnabled { get; private set; }


        public static void Log(object message)
        {
            Debug.Log($"[RandomBuff] {message}");
            File.AppendAllText(AssetManager.ResolveFilePath("randomBuff.log"), $"[Message]\t{message}\n");
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning($"[RandomBuff] {message}");
            File.AppendAllText(AssetManager.ResolveFilePath("randomBuff.log"), $"[Warning]\t{message}\n");
        }

        public static void LogError(object message)
        {
            Debug.LogError($"[RandomBuff] {message}");
            File.AppendAllText(AssetManager.ResolveFilePath("randomBuff.log"), $"[Error]\t{message}\n");
        }
    }
}
