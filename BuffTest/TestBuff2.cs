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

        [JsonProperty] 
        public int count = 2;

    }
    public class TestBuff2 : Buff<TestBuff2, TestBuffData2>
    {
        public override BuffID ID => TestEntry.TestBuff2ID;

        public TestBuff2()
        {
            TestEntry.Log("Create TestBuff2");
        }

        public override bool Triggerable => counter <= 0;

        public override bool Active => !Triggerable;

        public int counter = 0;

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            counter--;
        }

        public override bool Trigger(RainWorldGame game)
        {
            Data.count--;
            counter = 40;
            return Data.count == 0;
        }
    }
}
