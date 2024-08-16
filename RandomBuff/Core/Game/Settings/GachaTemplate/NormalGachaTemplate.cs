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

        public override string TemplateDetail => base.TemplateDetail + (nCount * nSelect + pSelect * pSelect != 0 ? string.Format(BuffResourceString.Get("GachaTemplate_Detail_Normal"),
            PSelect * PCount, NSelect * NCount) : string.Empty);

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


        private void ChangeDesc()
        {
            if (nCount * nSelect + pSelect * pSelect == 0)
            {
                if (TemplateDescription == "GachaTemplate_Desc_Normal")
                    TemplateDescription = "GachaTemplate_Desc_Normal_NoPick";
            }
            else if (TemplateDescription == "GachaTemplate_Desc_Normal_NoPick")
                TemplateDescription = "GachaTemplate_Desc_Normal";
        }

        [JsonProperty]
        public int PSelect
        {
            get => pSelect;
            set
            {
                pSelect = value;
                ChangeDesc();
            }
        }


        [JsonProperty]
        public int PCount
        {
            get => pCount;
            set
            {
                pCount = value;
                ChangeDesc();
            }
        }


        [JsonProperty]
        public int PShow { get; set; } = 3;

        [JsonProperty]
        public int NSelect
        {
            get => nSelect;
            set
            {
                nSelect = value;
                ChangeDesc();
            }
        }

        [JsonProperty]
        public int NCount
        {
            get => nCount;
            set
            {
                nCount = value;
                ChangeDesc();

            }
        }

        [JsonProperty]
        public int NShow { get; set; } = 3;

        [JsonProperty]
        public int SPSelect { get; set; } = 0;
        [JsonProperty]
        public int SPShow { get; set; } = 0;
        [JsonProperty]
        public int SPCount { get; set; } = 0;

        [JsonProperty]
        public int SNSelect { get; set; } = 0;
        [JsonProperty]
        public int SNShow { get; set; } = 0;
        [JsonProperty]
        public int SNCount { get; set; } = 0;


        private int pSelect = 1;
        private int pCount = 1;


        private int nSelect = 1;
        private int nCount = 1;

        [JsonProperty]
        public bool RandomStart { get; set; } = false;
    }
}
