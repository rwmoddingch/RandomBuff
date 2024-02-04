using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.BuffMenu
{
    internal class GachaMenuHooks : HooksApplier
    {
        public static ProcessManager.ProcessID GachaMenuID = new ProcessManager.ProcessID("GachaMenu", true);

        public static void HooksOn()
        {
            On.ProcessManager.RequestMainProcessSwitch_ProcessID_float += ProcessManager_RequestMainProcessSwitch_ProcessID_float;
        }

        /// <summary>
        /// 检测在其他地方申请的进程切换。如果符合条件，则替换将要切换的进程并进入GachaMenu
        /// </summary>
        private static void ProcessManager_RequestMainProcessSwitch_ProcessID_float(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID_float orig, ProcessManager self, ProcessManager.ProcessID ID, float fadeOutSeconds)
        {
            if (self.currentMainLoop is RainWorldGame game && !game.wasAnArtificerDream && ID == ProcessManager.ProcessID.SleepScreen)
            {
                ID = GachaMenuID;
            }
            orig.Invoke(self, ID, fadeOutSeconds);
        }
    }
}
