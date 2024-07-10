using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    internal class MissionGachaTemplate : NormalGachaTemplate
    {
        public override GachaTemplateID ID => GachaTemplateID.Mission;

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            if (cardPick.TryGetValue(game.GetStorySession.saveState.cycleNumber, out var list))
            {
                foreach (var rawId in list)
                {
                    var id = BuffRegister.GetDataType(rawId).id;
                    if (id == null)
                    {
                        BuffPlugin.LogWarning($"MissionGachaTemplate: can't find BuffID:{rawId}");
                        continue;
                    }

                    if (BuffPoolManager.Instance.GetBuffData(id) != null)
                    {
                        if (!BuffConfigManager.GetStaticData(id).Stackable)
                            BuffPlugin.Log($"MissionGachaTemplate: already contains BuffID:{rawId}");
                        else
                            BuffPoolManager.Instance.CreateBuff(id, true);
                        continue;
                    }
                    BuffPoolManager.Instance.CreateBuff(id, true);
                    BuffHud.Instance?.AppendNewCard(id);
                }
            }
        }


        [JsonProperty]
        public Dictionary<int,List<string>> cardPick = new ();
    }
}
