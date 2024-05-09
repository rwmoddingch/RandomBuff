using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class FireworkCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.FireWork;

        public override string IconElement => "Futile_White";

        public override SlugcatStats.Name BindCat => MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer;
    }
}
