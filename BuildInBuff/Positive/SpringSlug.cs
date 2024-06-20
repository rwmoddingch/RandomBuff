using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HotDogGains.Positive
{
    class SpringSlugBuff : Buff<SpringSlugBuff, SpringSlugBuffData> { public override BuffID ID => SpringSlugBuffEntry.SpringSlugID; }
    class SpringSlugBuffData : BuffData { public override BuffID ID => SpringSlugBuffEntry.SpringSlugID; }
    class SpringSlugBuffEntry : IBuffEntry
    {
        public static BuffID SpringSlugID = new BuffID("SpringSlugID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpringSlugBuff, SpringSlugBuffData, SpringSlugBuffEntry>(SpringSlugID);
        }
        public static void HookOn()
        {
            On.Player.TerrainImpact += Player_TerrainImpact;
        }

        private static void Player_TerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, RWCustom.IntVector2 direction, float speed, bool firstContact)
        {
            orig.Invoke(self, chunk, direction, speed, firstContact);
            if (speed>16)
            {
                BuffUtils.Log(SpringSlugID, "SlugSpring");
                for (int i = 0; i < self.bodyChunks.Length; i++)
                {
                    self.bodyChunks[i].vel = -direction.ToVector2() * speed*0.9f;
                }
            }
        }
    }
}