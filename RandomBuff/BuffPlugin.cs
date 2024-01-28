using BepInEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace RandomBuff
{
    [BepInPlugin(ModID, "RandomBuff", "1.0.0")]
    internal class BuffPlugin : BaseUnityPlugin
    {
        public const string ModID = "RandomBuff";


        public void Start()
        {
            On.RainWorld.OnModsInit += RainWorld_OnModsInit;
        }

        static bool inited;
        private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig.Invoke(self);
            if (inited)
                return;

            HooksApplier.ApplyHooks();
            inited = true;
        }

        public static void Log(string s)
        {
            Debug.Log($"[RandomBuff]{s}");
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
