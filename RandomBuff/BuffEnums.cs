using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff
{
    public static class BuffEnums
    {
        public static class ProcessID
        {
            public static readonly ProcessManager.ProcessID BuffGameMenu = new("BuffGameMenu");
            public static readonly ProcessManager.ProcessID BuffGameWinScreen = new ProcessManager.ProcessID("BuffGameWinScreen", true);
            public static readonly ProcessManager.ProcessID Cardpedia = new ProcessManager.ProcessID("Cardpedia", true);
            public static readonly ProcessManager.ProcessID CreditID = new ProcessManager.ProcessID("RandomBufdCredit", true);

            public static readonly ProcessManager.ProcessID UnstackMenu = new("UnstackMenu", true);
            public static readonly ProcessManager.ProcessID StackMenu = new("StackMenu", true);
            public static readonly ProcessManager.ProcessID GachaMenuID = new("GachaMenu", true);


        }
    }
}
