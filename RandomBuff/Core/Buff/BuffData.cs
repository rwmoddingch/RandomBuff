 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff的数据类，包括动态数据和静态数据
    /// </summary>
    public abstract class BuffData
    {
        public virtual BuffID ID => BuffID.None;

        public BuffData()
        {
            SetUp();
        }

        /// <summary>
        /// 初始化方法，用来设置存档数据入口和设置数据入口
        /// </summary>
        public virtual void SetUp()
        {

        }
    }
}
