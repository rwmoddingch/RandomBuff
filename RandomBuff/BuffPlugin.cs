using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff
{
    internal class BuffPlugin
    {

        public static void Log(string s)
        {
            Debug.Log($"[RandomBuff]{s}");
        }
    }
}
