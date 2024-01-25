using System;
using System.Security.Permissions;
using BepInEx;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using UnityEngine;


#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace BuffTest
{


    public class TestEntry : IBuffEntry
    {
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<TestBuff,TestBuffData>(TestBuffID);
            On.RainWorldGame.Update += RainWorldGame_Update;
            Log("Test OnEnable");
        }


        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                Log("Create Test Buff!");
                BuffPoolManager.Instance.CreateBuff(TestEntry.TestBuffID);
            }
        }
        public static void Log(object msg)
        {
            BuffPlugin.Log($"<TEST BUFF> {msg}");
        }
        public static BuffID TestBuffID = new BuffID("TestBuff", true);
    }

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
    public class TestBuff : Buff<TestBuffData>
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
