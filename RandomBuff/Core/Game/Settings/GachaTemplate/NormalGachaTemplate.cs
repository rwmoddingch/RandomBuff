using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    internal class NormalGachaTemplate : BaseGachaTemplate
    {
        public override GachaTemplateID ID => GachaTemplateID.Normal;

        public override void NewGame()
        {
            CurrentPacket = new CachaPacket() { positive = (SPSelect, SPShow, SPCount), negative = (SNSelect, SNShow, SNCount) };
        }

        public override void SessionEnd()
        {
            CurrentPacket = new CachaPacket() { positive = (PSelect, PShow, PCount), negative = (NSelect, NShow, NCount) };
        }

        public override bool NeedRandomStart => RandomStart;

        [JsonProperty]
        public int PSelect = 1;
        [JsonProperty]
        public int PShow = 3;
        [JsonProperty]
        public int PCount = 1;

        [JsonProperty]
        public int NSelect = 1;
        [JsonProperty]
        public int NShow = 3;
        [JsonProperty]
        public int NCount = 1;

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
