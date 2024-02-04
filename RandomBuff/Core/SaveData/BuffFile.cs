using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Kittehface.Framework20;
using Newtonsoft.Json;
using RWCustom;
using UnityEngine;

namespace RandomBuff.Core.SaveData
{
    /// <summary>
    /// 一个存档读取接口 实际不用修改
    /// 会在更改存档时重新创建
    /// </summary>
    sealed partial class BuffFile
    {
        public int UsedSlot { get; private set; } = -1;

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
            if (rainWorld?.playerHandler?.profile != null)
            {
                UserData.OnFileMounted += UserData_OnFileMounted;
                BuffPlugin.Log($"Loading save data, Slot : {UsedSlot = CurrentSlot}, " +
                               $"Main game slot: {Custom.rainWorld.options.saveSlot}");
                UserData.Mount(rainWorld.playerHandler.profile, $"buffsave{UsedSlot}");
                return;
            }

            LoadFailedFallBack();
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        public void SaveFile()
        {
            if (buffCoreFile != null)
            {
                BuffPlugin.Log($"Saving buff data at slot {Instance.UsedSlot}");
                buffCoreFile.Set<string>("buff-data", BuffDataManager.Instance.ToStringData(), UserData.WriteMode.Immediate);
                buffCoreFile.Set<string>("buff-config", BuffConfigManager.Instance.ToStringData(), UserData.WriteMode.Immediate);
                buffCoreFile.Set<string>("buff-collect", JsonConvert.SerializeObject(buffCollect), UserData.WriteMode.Immediate);
                return;
            }
            BuffPlugin.LogError($"Failed to save buff data at slot {Instance.UsedSlot}");
        }

        /// <summary>
        /// 会在切回主界面自动调用
        /// 保存配置文件
        /// </summary>
        public void SaveConfigFile()
        {
            if (buffCoreFile != null)
            {
                BuffPlugin.Log($"Save config file at slot {Instance.UsedSlot}");
                buffCoreFile.Set<string>("buff-config", BuffConfigManager.Instance.ToStringData(), UserData.WriteMode.Immediate);
                return;
            }
            BuffPlugin.LogError($"Failed to save buff data at slot {Instance.UsedSlot}");
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
        }

        private void BuffCoreFile_OnReadCompleted(UserData.File file, UserData.Result result)
        {
            buffCoreFile.OnReadCompleted -= BuffCoreFile_OnReadCompleted;
            if (result.Contains(UserData.Result.FileNotFound))
            {
                buffCoreFile.OnWriteCompleted += BuffCoreFile_OnWriteCompleted_NewFile;
                LoadFailedFallBack();
                buffCoreFile.Write();
                return;
            }

            if (result.IsSuccess())
            {
                if (!buffCoreFile.Contains("buff-config")  ||
                    !buffCoreFile.Contains("buff-version") ||
                    !buffCoreFile.Contains("buff-data")    ||
                    !buffCoreFile.Contains("buff-collect"))
                {
                    LoadFailedFallBack();
                }
                BuffPlugin.Log($"Buff file version : [{buffCoreFile.Get<string>("buff-version")}], current version : {BuffPlugin.saveVersion}");

                BuffConfigManager.LoadConfig(buffCoreFile.Get<string>("buff-config"), buffCoreFile.Get<string>("buff-version"));
                BuffDataManager.LoadData(buffCoreFile.Get<string>("buff-data"), buffCoreFile.Get<string>("buff-version"));

                buffCollect = JsonConvert.DeserializeObject<List<string>>(buffCoreFile.Get<string>("buff-collect"));

                //更新格式版本
                buffCoreFile.Set<string>("buff-version", BuffPlugin.saveVersion);

                Platform.NotifyUserDataReadCompleted(this);
            }
            else
            {
                BuffPlugin.LogError("BuffCoreFile read failed");
                LoadFailedFallBack();
            }
        }

        private void BuffCoreFile_OnWriteCompleted_NewFile(UserData.File file, UserData.Result result)
        {
            buffCoreFile.OnWriteCompleted -= BuffCoreFile_OnWriteCompleted_NewFile;
            Platform.NotifyUserDataReadCompleted(this);
            if (result.IsFailure())
                throw new Exception("Create Buff File Failed!"); //TODO : 添加异常处理
        }

        private void LoadFailedFallBack()
        {
            if (buffCoreFile == null)
                return;
            buffCoreFile.Set<string>("buff-version", BuffPlugin.saveVersion);
            buffCoreFile.Set<string>("buff-config", "");
            buffCoreFile.Set<string>("buff-data", "");
            buffCoreFile.Set<string>("buff-collect", "");
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

        private static int CurrentSlot => rainWorld.options.saveSlot >= 100
            ? (rainWorld.options.saveSlot)
            : rainWorld.options.saveSlot + 100;

        public static void OnModsInit()
        {
            On.PlayerProgression.ctor += PlayerProgression_ctor;
            BuffPlugin.Log("Buff File Hook Loaded");
        }

        private static void PlayerProgression_ctor(On.PlayerProgression.orig_ctor orig, PlayerProgression self, RainWorld rainWorld, bool tryLoad, bool saveAfterLoad)
        {
            orig(self,rainWorld, tryLoad, saveAfterLoad);
            if (Instance?.UsedSlot != CurrentSlot)
            {
                if (Instance != null)
                {
                    BuffPlugin.Log($"Save last slot file at slot {Instance.UsedSlot}, Before load file at Slot {CurrentSlot}");
                    Instance.SaveFile();
                }

                Instance = new BuffFile();
            }
            else if (Instance != null &&
                     Custom.rainWorld.options.saveSlot < 100)
            {
                Instance.SaveConfigFile();
            }
        }

        public void AddCollect(string buffID)
        {
            BuffPlugin.Log($"Add buff collect to Save Slot {CurrentSlot}");
            if(!buffCollect.Contains(buffID))
                buffCollect.Add(buffID);
        }

        public List<string> buffCollect = new();

    }

}
