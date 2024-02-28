﻿using CustomSaveTx;
using RandomBuffUtils.BuffEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace RandomBuffUtils
{
    public static class BuffUtils
    {
        static bool everInit;
        private static bool canAccessLog =true;
        public static void OnEnable()
        {
            if (everInit)
                return;
            try
            {
                File.Create(AssetManager.ResolveFilePath("buffcustom.log")).Close();
            }
            catch (Exception e)
            {
                canAccessLog = false;
                Debug.LogException(e);
            }

            DeathPersistentSaveDataRx.HookOn();

            BuffRoomReachEvent.OnEnable();
            BuffRegionGateEvent.OnEnable();
            everInit = true;
        }

        public static void Log(object header, object m)
        {
            Debug.Log($"[RandomBuffUtils - {header}] {m}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcustom.log"), $"[Message - {header}]\t{m}\n");
        }

        public static void LogWarning(object header, object m)
        {
            Debug.LogWarning($"[RandomBuffUtils - {header}] {m}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcustom.log"), $"[Warning - {header}]\t{m}\n");
        }
        public static void LogError(object header, object m)
        {
            Debug.LogError($"[RandomBuffUtils - {header}] {m}");
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcustom.log"), $"[Error - {header}]\t{m}\n");
        }

        public static void LogException(object header, Exception m)
        {
            Debug.LogError($"[RandomBuffUtils - {header}] {m.Message}");
            Debug.LogException(m);
            if (canAccessLog)
                File.AppendAllText(AssetManager.ResolveFilePath("buffcustom.log"), $"[Fatal - {header}]\t{m.Message}\n");
            

        }

        public static string Compress(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var memoryStream = new MemoryStream())
            {
                using (var gZipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                {
                    msi.CopyTo(gZipStream);
                }
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public static string Decompress(string orig)
        {
            var bytes = Convert.FromBase64String(orig);

            using (var msi = new MemoryStream(bytes))
            using (var memoryStream = new MemoryStream())
            using (var gZipStream = new GZipStream(msi, CompressionMode.Decompress))
            {
                gZipStream.CopyTo(memoryStream);
                return Encoding.UTF8.GetString(memoryStream.ToArray());
            }
        }
    }
}