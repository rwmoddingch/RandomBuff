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

        public SandboxGachaTemplate() { }

        public SandboxGachaTemplate(bool noPick)
        {
            if (noPick)
            {
                NShow = NSelect = NCount = PSelect = PCount = PShow = 0;
            }
            ExpMultiply = 0f;
        }

        public override void NewGame()
        {
            CurrentPacket = new CachaPacket() { positive = (SPSelect, SPShow, SPCount), negative = (SNSelect, SNShow, SNCount) };
        }

        public override void SessionEnd(RainWorldGame game)
        {
            CurrentPacket = new CachaPacket() { positive = (PSelect, PShow, PCount), negative = (NSelect, NShow, NCount) };
            BuffPlugin.LogDebug($"Session End: {CurrentPacket.positive}, {CurrentPacket.negative}");
        }


        public override bool NeedRandomStart => RandomStart;

        [JsonProperty]
        public int PSelect = 0;
        [JsonProperty]
        public int PShow = 0;
        [JsonProperty]
        public int PCount = 0;

        [JsonProperty]
        public int NSelect = 0;
        [JsonProperty]
        public int NShow = 0;
        [JsonProperty]
        public int NCount = 0;

        [JsonProperty]
        public int SPSelect = 0;
        [JsonProperty]
        public int SPShow = 0;
        [JsonProperty]
        public int SPCount = 0;

        [JsonProperty]
        public int SNSelect = 0;
        [JsonProperty]
        public int SNShow = 0;
        [JsonProperty]
        public int SNCount = 0;

        [JsonProperty]
        public bool RandomStart = false;
    }
}
