using CustomSaveTx;
using RandomBuffUtils.BuffEvents;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils
{
    public static class BuffUtils
    {
        static bool everInit;
        public static void OnEnable()
        {
            if (everInit)
                return;
            
            DeathPersistentSaveDataRx.HookOn();

            BuffRoomReachEvent.OnEnable();
            BuffRegionGateEvent.OnEnable();
            everInit = true;
        }

        public static void Log(object header, object m)
        {
            Debug.Log($"[RandomBuffUtils - {header}] {m}");
        }

        public static void LogWarning(object header, object m)
        {
            Debug.LogWarning($"[RandomBuffUtils - {header}] {m}");
        }
        public static void LogError(object header, object m)
        {
            Debug.LogError($"[RandomBuffUtils - {header}] {m}");
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
