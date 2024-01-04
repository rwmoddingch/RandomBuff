using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff基类
    /// </summary>
    public abstract class Buff<TData> where TData : BuffData
    {
        internal TData buffData;
        /// <summary>
        /// 数据类属性，只读
        /// </summary>
        public virtual TData Data { get => buffData; }


        public Buff()
        {
        }
    }
}
