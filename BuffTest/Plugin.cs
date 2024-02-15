using System;
using System.Security.Permissions;
using BepInEx;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Hooks;
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
            BuffRegister.RegisterBuff<TestBuff, TestBuffData>(TestBuffID);
            BuffRegister.RegisterBuff<TestBuff2, TestBuffData2>(TestBuff2ID);
            On.RainWorldGame.Update += RainWorldGame_Update;
            Log("Test OnEnable 2");
        }


        private void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (self.rainWorld.BuffMode() && BuffPlugin.DevEnabled)
            {
                if (Input.GetKeyDown(KeyCode.LeftControl))
                {
                    Log("Create Test Buff2!");
                    BuffPoolManager.Instance.CreateBuff(TestBuff2ID);
                    BuffHud.Instance.AppendNewCard(TestBuff2ID);
                }
            }
        }

        public static void Log(object msg)
        {
            BuffPlugin.Log($"<TEST BUFF> {msg}");
        }
        public static BuffID TestBuffID = new BuffID("TestBuff", true);
        public static BuffID TestBuff2ID = new BuffID("TestBuff2", true);

    }


}
