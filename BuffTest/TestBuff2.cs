using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuffTest
{
    public class TestBuffData2 : BuffData
    {
        public override BuffID ID => TestEntry.TestBuff2ID;

        public TestBuffData2()
        {
            TestEntry.Log("Create TestBuffData2");
        }

        public override void DataLoaded(bool newData)
        {
        }

    }
    public class TestBuff2 : Buff<TestBuffData2>
    {
        public override BuffID ID => TestEntry.TestBuff2ID;

        public TestBuff2()
        {
            TestEntry.Log("Create TestBuff2");
        }

    }
}
