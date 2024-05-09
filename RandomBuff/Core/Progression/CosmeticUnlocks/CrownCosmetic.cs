using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class CrownCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.Crown;

        public override string IconElement => "BuffCosmetic_Crown";

        public override SlugcatStats.Name BindCat => null;
    }
}
