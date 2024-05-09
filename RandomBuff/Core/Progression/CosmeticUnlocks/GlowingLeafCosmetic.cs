using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class GlowingLeafCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.GlowingLeaf;

        public override string IconElement => "Futile_White";

        public override SlugcatStats.Name BindCat => SlugcatStats.Name.Yellow;
    }
}
