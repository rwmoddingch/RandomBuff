using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.CosmeticQuest
{
    internal abstract class CosmeticQuest
    {
        public virtual SlugcatStats.Name BindSlugcat { get; }
        public virtual string QuestID { get; }

        public virtual void HooksOn()
        {
        }

        public virtual void HooksOff()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void GrafUpdate(float timeStacker)
        {
        }
    }
}
