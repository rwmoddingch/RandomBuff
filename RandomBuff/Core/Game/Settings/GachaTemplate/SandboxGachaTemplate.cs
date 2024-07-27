using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    internal class SandboxGachaTemplate : GachaTemplate
    {
        public override GachaTemplateID ID => GachaTemplateID.SandBox;

        public SandboxGachaTemplate()
        {
            TemplateDescription = "GachaTemplate_Desc_Sandbox";
            PocketPackMultiply = 0;
            ExpMultiply = 0f;
        }


        public override void NewGame()
        {
            CurrentPacket = new CachaPacket() {positive = (0, 0, 0), negative = (0, 0, 0) };
        }

        public override void SessionEnd(RainWorldGame game)
        {
            CurrentPacket = new CachaPacket() { positive = (0, 0, 0), negative = (0, 0, 0) };
            BuffPlugin.LogDebug($"Session End: {CurrentPacket.positive}, {CurrentPacket.negative}");
        }


        public override bool NeedRandomStart => false;

    }
}
