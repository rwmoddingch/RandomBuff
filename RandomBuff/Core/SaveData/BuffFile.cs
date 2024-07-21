using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Kittehface.Framework20;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Hooks;
using RWCustom;
using UnityEngine;
using static RainWorld;

namespace RandomBuff.Core.SaveData
{

    /// <summary>
    /// 保存格式版本
    /// </summary>
    public sealed class BuffFormatVersion
    {
        private readonly char buildType;
        private readonly int mainVersion;
        private readonly int subVersion;
        private readonly int minVersion;

        public BuffFormatVersion(char buildType, int mainVersion, int subVersion, int minVersion)
        {
            this.buildType = buildType;
            this.mainVersion = mainVersion;
            this.subVersion = subVersion;
            this.minVersion = minVersion;
        }

        public BuffFormatVersion(string serVersion)
        {
            try
            {
                buildType = serVersion[0];
                var split = serVersion.Substring(2).Split('.');
                mainVersion = int.Parse(split[0]);
                subVersion = int.Parse(split[1]);
                minVersion = int.Parse(split[2]);
            }
            catch (Exception e)
            {
                BuffPlugin.LogInstance.LogFatal(e);
                this.buildType = BuffPlugin.saveVersion.buildType;
                this.mainVersion = BuffPlugin.saveVersion.mainVersion;
                this.subVersion = BuffPlugin.saveVersion.subVersion;
                this.minVersion = BuffPlugin.saveVersion.minVersion;


            }

        }

        public override string ToString()
        {
            return $"{buildType}-{mainVersion}.{subVersion}.{minVersion}";
        }

        public static bool operator==(BuffFormatVersion a, BuffFormatVersion b)
        {
            if(a is null|| b is null) return false;
            return a.buildType == b.buildType && a.mainVersion == b.mainVersion && a.subVersion == b.subVersion && a.minVersion== b.minVersion;
        }



        public static bool operator !=(BuffFormatVersion a, BuffFormatVersion b)
        {
            return !(a == b);
        }


        public static bool operator >(BuffFormatVersion a, BuffFormatVersion b)
        {
            if (a is null || b is null) return false;
            if (a.mainVersion == b.mainVersion)
            {
                if (a.subVersion == b.subVersion)
                {
                    return a.minVersion > b.minVersion;
                }
                return a.subVersion > b.subVersion;
            }
            return a.mainVersion > b.mainVersion;
        }

        public static bool operator <(BuffFormatVersion a, BuffFormatVersion b)
        {
            if(a is null || b is null)  return false;
            if (a.mainVersion == b.mainVersion)
            {
                if (a.subVersion == b.subVersion)
                {
                    return a.minVersion < b.minVersion;
                }
                return a.subVersion < b.subVersion;
            }
            return a.mainVersion < b.mainVersion;
        }

        public static bool operator <=(BuffFormatVersion a, BuffFormatVersion b)
        {
            return a < b || a == b;
        }

        public static bool operator >=(BuffFormatVersion a, BuffFormatVersion b)
        {
            return a > b || a == b;
        }
    }

    /// <summary>
    /// 一个存档读取接口 实际不用修改
    /// 会在更改存档时重新创建
    /// </summary>
    sealed partial class BuffFile
    {
        public int UsedSlot { get; private set; } = -1;

        public enum BuffFileLoadState
        {
            BeforeLoad,
            Loading,
            AfterLoad
        }

        public BuffFileLoadState LoadState { get; set; } = BuffFileLoadState.BeforeLoad;

        private UserData.File buffCoreFile;

        private BuffFile()
        {
            buffCoreFile = null;
            if (Platform.initialized)
            {
                LoadFile();
                return;
            }

            Platform.OnRequestUserDataRead += Platform_OnRequestUserDataRead;
        }

        public void LoadFile()
        {
            if (Profiles.ActiveProfiles.Count > 0)
            {
                UserData.OnFileMounted += UserData_OnFileMounted;
                BuffPlugin.Log($"Loading save data, Slot : {UsedSlot = CurrentSlot}, " +
                               $"Main game slot: {Custom.rainWorld.options.saveSlot}");
                UserData.Mount(Profiles.ActiveProfiles[0], $"buffsave{UsedSlot}");
                return;
            }

            LoadFailedFallBack();
        }

        public void DeleteAllFile()
        {
            LoadFailedFallBack(true); 
            if(File.Exists(Application.persistentDataPath + "/sav" + (CurrentSlot+1)))
                File.Delete(Application.persistentDataPath + "/sav" + (CurrentSlot + 1));
            BuffPlugin.Log($"Delete save data {Application.persistentDataPath + "/sav" + (CurrentSlot + 1)}");
            SaveFile();
            
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public void SaveFile()
        {
            if (buffCoreFile != null)
            {
                BuffPlugin.Instance.StartCoroutine(InternalSaveFile());
                return;
            }
            BuffPlugin.LogError($"Failed to save buff data at slot {Instance.UsedSlot}");
        }

        IEnumerator InternalSaveFile()
        {
            int waitTimeOutCounter = 400;
            BuffPlugin.Log($"!!Saving buff data at slot {Instance.UsedSlot}!!");

            List<string> saveTitle = new()
            {
                "buff-data",
                "buff-config",
                "buff-player",
                "buff-version"
            };
            List<string> saveData = new()
            {
                BuffDataManager.Instance.ToStringData(),
                BuffConfigManager.Instance.ToStringData(),
                BuffPlayerData.Instance.ToStringData(),
                BuffPlugin.saveVersion.ToString()
            };
            for (int i = 0; i < saveTitle.Count; i++)
            {
                while (UserData.Busy)
                {
                    waitTimeOutCounter--;
                    if (waitTimeOutCounter == 0)
                    {
                        BuffPlugin.LogError($"save buff data at slot {Instance.UsedSlot} TIME OUT");
                        yield break;
                    }
                    yield return new WaitForFixedUpdate();
                }
                buffCoreFile.Set(saveTitle[i], saveData[i]);
                BuffPlugin.Log($"saved buff data at slot {Instance.UsedSlot}, Title: {saveTitle[i]}");
            }

            BuffPlugin.Log($"!!Finish saved buff data at slot {Instance.UsedSlot}!!");

        }

        /// <summary>
        /// 会在切回主界面自动调用
        /// 保存配置文件
        /// </summary>
        public void SaveConfigFile()
        {
            //哦对我觉得没什么影响于是就直接全保存了.jpg
            if(BuffDataManager.Instance != null &&
               BuffConfigManager.Instance != null)
                SaveFile();
        }

        private void Platform_OnRequestUserDataRead(List<object> pendingUserDataReads)
        {
            Platform.OnRequestUserDataRead -= Platform_OnRequestUserDataRead;
            pendingUserDataReads.Add(this);
            LoadFile();
        }

        private void UserData_OnFileMounted(UserData.File file, UserData.Result result)
        {
            UserData.OnFileMounted -= UserData_OnFileMounted;
            LoadState = BuffFileLoadState.Loading;
            if (result.IsSuccess())
            {
                buffCoreFile = file;

                buffCoreFile.OnReadCompleted += BuffCoreFile_OnReadCompleted;
                buffCoreFile.Read();
                return;
            }
            BuffPlugin.LogError($"File Mounted Failed, STATE NUM: {result}");
            Platform.NotifyUserDataReadCompleted(this);
            LoadFailedFallBack();
            OnBuffReadCompleted?.Invoke();
        }


        private void BuffCoreFile_OnReadCompleted(UserData.File file, UserData.Result result)
        {
            buffCoreFile.OnReadCompleted -= BuffCoreFile_OnReadCompleted;

            if (result.Contains(UserData.Result.FileNotFound))
            {
                buffCoreFile.OnWriteCompleted += BuffCoreFile_OnWriteCompleted_NewFile;
                buffCoreFile.Write();
                return;
            }

            if (result.IsSuccess())
            {
                if (!buffCoreFile.Contains("buff-config")  ||
                    !buffCoreFile.Contains("buff-version") ||
                    !buffCoreFile.Contains("buff-data")    ||
                    !buffCoreFile.Contains("buff-player"))
                {
                    LoadFailedFallBack();
                    return;
                }
                var fileVersion = new BuffFormatVersion(buffCoreFile.Get("buff-version"));

                if (fileVersion < BuffPlugin.outDateVersion)
                {
                    BuffPlugin.LogError("OutDate version, clean all data!");
                    LoadFailedFallBack(true);
                    return;
                }
                BuffPlugin.Log($"Buff file version : [{fileVersion}], current version : [{BuffPlugin.saveVersion}]");
                BuffConfigManager.LoadConfig(buffCoreFile.Get("buff-config"), fileVersion);
                BuffDataManager.LoadData(buffCoreFile.Get("buff-data"), fileVersion);
                BuffPlayerData.LoadBuffPlayerData(buffCoreFile.Get("buff-player"), fileVersion);

                Platform.NotifyUserDataReadCompleted(this);
                LoadState = BuffFileLoadState.AfterLoad;
            }
            else
            {
                BuffPlugin.LogError($"Unhandled Exception : State ID :{result}");
                LoadFailedFallBack();
            }

            OnBuffReadCompleted?.Invoke();

        }

        private void BuffCoreFile_OnWriteCompleted_NewFile(UserData.File file, UserData.Result result)
        {
            buffCoreFile.OnWriteCompleted -= BuffCoreFile_OnWriteCompleted_NewFile;
            BuffPlugin.Log($"Create new save slot file At {UsedSlot}");
            Platform.NotifyUserDataReadCompleted(this);
            LoadFailedFallBack();

            OnBuffReadCompleted?.Invoke();

            buffCoreFile.Write();
            if (result.IsFailure())
                throw new Exception("Create Buff File Failed!"); //TODO : 添加异常处理
        }

        private void LoadFailedFallBack(bool forceDelete = false)
        {
            if (buffCoreFile == null)
                return;
            if (!buffCoreFile.Contains("buff-version") || forceDelete)
                buffCoreFile.Set("buff-version", BuffPlugin.saveVersion.ToString());
            if (!buffCoreFile.Contains("buff-config") || forceDelete)
                buffCoreFile.Set("buff-config", "");
            if (!buffCoreFile.Contains("buff-data") || forceDelete)
                buffCoreFile.Set("buff-data", "");
            if (!buffCoreFile.Contains("buff-player") || forceDelete)
                buffCoreFile.Set("buff-player", "");
            BuffConfigManager.LoadConfig(buffCoreFile.Get("buff-config"), BuffPlugin.saveVersion);
            BuffDataManager.LoadData(buffCoreFile.Get("buff-data"), BuffPlugin.saveVersion);
            BuffPlayerData.LoadBuffPlayerData(buffCoreFile.Get("buff-player"), BuffPlugin.saveVersion);
            LoadState = BuffFileLoadState.AfterLoad;
        }
    }

    /// <summary>
    /// 静态成员变量
    /// 包括一些hook
    /// </summary>
    sealed partial class BuffFile
    {
        public static BuffFile Instance { get; private set; }

        private static RainWorld rainWorld => Custom.rainWorld;

        private static int CurrentSlot => rainWorld.BuffMode()
            ? (rainWorld.options.saveSlot)
            : rainWorld.options.saveSlot >= 0 ?
                rainWorld.options.saveSlot + 100 :
                -rainWorld.options.saveSlot + 99;

        public static void OnModsInit()
        {
            On.PlayerProgression.ctor_RainWorld_bool_bool_string += PlayerProgression_ctor_RainWorld_bool_bool_string;
            On.PlayerProgression.Update += PlayerProgression_Update;
            BuffPlugin.Log("Buff File Hook Loaded");
        }

        private static void PlayerProgression_ctor_RainWorld_bool_bool_string(On.PlayerProgression.orig_ctor_RainWorld_bool_bool_string orig, PlayerProgression self, RainWorld rainWorld, bool tryLoad, bool saveAfterLoad, string overrideBaseDir)
        {
            orig.Invoke(self, rainWorld, tryLoad, saveAfterLoad, overrideBaseDir);
            if (Instance?.UsedSlot != CurrentSlot)
            {
                if (Instance != null)
                {
                    BuffPlugin.Log($"Save last slot file at slot {Instance.UsedSlot}, Before load file at Slot {CurrentSlot}");
                    Instance.SaveFile();
                }

                Instance = new BuffFile();
                return;
            }
            else if (Instance != null &&
                     Custom.rainWorld.options.saveSlot < 100)
            {
                Instance.SaveConfigFile();
            }
            OnBuffReadCompleted?.Invoke();

            if (self.progressionLoaded)
                OnFileReadCompleted?.Invoke();
            else
                waitLoad.Add(self);
        }

        private static void PlayerProgression_Update(On.PlayerProgression.orig_Update orig, PlayerProgression self)
        {
            orig(self);
            if (waitLoad.Contains(self) && self.progressionLoaded)
            {
                OnFileReadCompleted?.Invoke();
                waitLoad.Remove(self);
            }
        }

        private static event Action OnFileReadCompleted;
        private static event Action OnBuffReadCompleted;

        private static List<PlayerProgression> waitLoad = new ();

        /// <summary>
        /// 当存档完全读取完毕的回调
        /// </summary>
        public class BuffFileCompletedCallBack
        {
            private int state;
            private Action action;

            public BuffFileCompletedCallBack(Action action)
            {
                this.action = action;
                OnFileReadCompleted += BuffFileCompletedCallBack_OnFileReadCompleted;
                OnBuffReadCompleted += BuffFileCompletedCallBack_OnBuffReadCompleted;
            }

            private void BuffFileCompletedCallBack_OnBuffReadCompleted()
            {
                OnBuffReadCompleted -= BuffFileCompletedCallBack_OnBuffReadCompleted;
                state++;
                if (state == 2)
                    action.Invoke();
            }

            private void BuffFileCompletedCallBack_OnFileReadCompleted()
            {
                OnFileReadCompleted -= BuffFileCompletedCallBack_OnFileReadCompleted;
                state++;
                if (state == 2)
                    action.Invoke();
            }
        }

    }

}
