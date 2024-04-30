using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils
{
    public class SimpleRangeDamage : UpdatableAndDeletable
    {
        Creature killTagHolder;
        Creature.DamageType damageType;
        float killTagHolderDmgFactor;

        Vector2 pos;
        float rad;
        float damage;
        float stun;

        public SimpleRangeDamage(Room room, Creature.DamageType damageType, Vector2 pos, float rad, float damage, float stun, Creature killTagHolder, float killTagHolderDmgFactor)
        {
            this.room = room;
            this.pos = pos;
            this.rad = rad;
            this.damage = damage;
            this.killTagHolder = killTagHolder;
            this.killTagHolderDmgFactor = killTagHolderDmgFactor;
            this.damageType = damageType;
            this.stun = stun;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            for(int i = room.updateList.Count - 1;i >= 0; i--)
            {
                var obj = room.updateList[i];
                if (obj is Creature creature)
                {
                    if ((creature.DangerPos - pos).magnitude < rad)
                    {
                        float dmg = damage;
                        if (creature == killTagHolder)
                            dmg *= killTagHolderDmgFactor;

                        creature.Violence(killTagHolder?.firstChunk, null, creature.mainBodyChunk, null, damageType, dmg, stun);
                        BuffUtils.Log("SimpleRangeDamage", $"Violence Creature : {creature}, {damage}, {damageType}");
                    }
                }
            }
            Destroy();
        }
    }
}
