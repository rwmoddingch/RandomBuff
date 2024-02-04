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
    public class TestBuffData : BuffData
    {
        public override BuffID ID => TestEntry.TestBuffID;

        public TestBuffData()
        {
            TestEntry.Log("Create TestBuffData");
        }

        public override void DataLoaded(bool newData)
        {
            TestEntry.Log($"Data Loaded, Count: {count}, Static Data: {TestConfig}");
        }

        public override void CycleEnd()
        {
            count++;
            TestEntry.Log($"Cycle End, Count: {count}, Static Data: {TestConfig}");
        }

        [JsonProperty]
        public int count;

        [CustomStaticConfig]
        public int TestConfig { get; }
    }
    public class TestBuff : Buff<TestBuff,TestBuffData>
    {
        public override BuffID ID => TestEntry.TestBuffID;

        public TestBuff()
        {
            TestEntry.Log("Create TestBuff");
        }

        public override bool Trigger(RainWorldGame game)
        {
            return false;
        }

        public override void Update(RainWorldGame game)
        {
        }

        public override void Destroy()
        {
            TestEntry.Log("Test Buff Destroy");
        }
    }
}
