using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuffUtils;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class BinahCondition : IntervalCondition
    {
        public static readonly ConditionID Binah = new ConditionID(nameof(Binah), true);

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            BinahGlobalManager.OnBinahDie += BinahGlobalManager_OnBinahDie;

        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);
            BinahGlobalManager.OnBinahDie += BinahGlobalManager_OnBinahDie;

        }

        private void BinahGlobalManager_OnBinahDie()
        {
            Finished = true;
        }

        public override ConditionID ID => Binah;
        public override int Exp => 600;



        public override string InRangeDisplayName()
        {
      
            return BuffResourceString.Get("DisplayName_MeltDownBinah");
        }

        public override string InRangeDisplayProgress()
        {
            return string.Empty;
        }
    }
}
