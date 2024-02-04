using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.GachaMenu
{
    internal class GachaMenu : Menu.Menu
    {
        public GachaMenu(ProcessManager manager, ProcessManager.ProcessID processID) : base(manager, processID)
        {
            pages.Add(new Menu.Page(this, null, "GachaMenu", 0));
            pages[0].subObjects.Add(new Menu.SimpleButton(this, pages[0], "Exit", "ExitButton", new Vector2(1300, 50f), new Vector2(100f, 30f)));
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if(message == "ExitButton")
            {
                ShutDownProcess();
            }
        }
    }
}
