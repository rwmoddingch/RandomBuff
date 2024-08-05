using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    public class NormalGachaTemplate : GachaTemplate
    {
        public override GachaTemplateID ID => GachaTemplateID.Normal;

        public NormalGachaTemplate()
        {
            TemplateDescription = "GachaTemplate_Desc_Normal";
        }

        public NormalGachaTemplate(bool noPick)
        {
            TemplateDescription = "GachaTemplate_Desc_Normal";

            if (noPick)
            {
                NShow = NSelect = NCount = PSelect = PCount = PShow = 0;
            }
        }

        public override string TemplateDetail => base.TemplateDetail + string.Format(BuffResourceString.Get("GachaTemplate_Detail_Normal"),
            PSelect * PCount, NSelect * NCount);

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
