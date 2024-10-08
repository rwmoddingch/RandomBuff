﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff的ID，和内置的ExtEnum用法相同。
    /// </summary>
    public class BuffID : ExtEnum<BuffID>
    {
        public static BuffID None;
        static BuffID()
        {
            None = new BuffID("None", true);
        }

        public BuffID(string value, bool register = false) : base(value, register)
        {
        }
    }

    public enum BuffType
    {
        Positive,
        Negative,
        Duality
    }

    public enum BuffProperty
    {
        Normal,
        Special
    }
}


