using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kittehface.Framework20;
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
                BuffPlugin.Log($"Loading save data, Slot : {UsedSlot = CurrentSlot}");
                UserData.Mount(rainWorld.playerHandler.profile, $"buffsave{UsedSlot}");
            }

            LoadFailedFallBack();
        }


        public void SaveFile()
        {
            if (buffCoreFile != null)
            {
                BuffPlugin.Log("Saving buff data");
                buffCoreFile.Set<string>("buff-data", BuffDataManager.Instance.ToStringData(), UserData.WriteMode.Immediate);
                return;
            }
            BuffPlugin.LogError("Failed to save buff data");
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
                    !buffCoreFile.Contains("buff-data"))
                {
                    LoadFailedFallBack();
                }
                BuffPlugin.Log($"Buff file version : [{buffCoreFile.Get<string>("buff-version")}], current version : {BuffPlugin.saveVersion}");

                //LoadConfig(buffCoreFile.Get<string>("buff-config"), buffCoreFile.Get<int>("buff-version"));
                BuffDataManager.LoadData(buffCoreFile.Get<string>("buff-data"), buffCoreFile.Get<string>("buff-version"));

                buffCoreFile.Set<string>("buff-version", BuffPlugin.saveVersion);
                Platform.NotifyUserDataReadCompleted(this);
            }
            else
            {
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

        private static int CurrentSlot => rainWorld.options.saveSlot >= 0
            ? (rainWorld.options.saveSlot + 1) : Mathf.Abs(rainWorld.options.saveSlot);

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
                Instance = new BuffFile();
            }
        }
    }

}
