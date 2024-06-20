using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace HotDogGains.Duality
{
    class SisyphusBuff : Buff<SisyphusBuff, SisyphusBuffData> { public override BuffID ID => SisyphusBuffEntry.SisyphusID; }
    class SisyphusBuffData : BuffData { public override BuffID ID => SisyphusBuffEntry.SisyphusID; }
    class SisyphusBuffEntry : IBuffEntry
    {
        public static BuffID SisyphusID = new BuffID("SisyphusID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SisyphusBuff, SisyphusBuffData, SisyphusBuffEntry>(SisyphusID);
        }
        public static void HookOn()
        {
            On.Player.SlugcatGrab += Player_SlugcatGrab;
            On.Player.ReleaseObject += Player_ReleaseObject;
        }

        private static void Player_SlugcatGrab(On.Player.orig_SlugcatGrab orig, Player self, PhysicalObject obj, int graspUsed)
        {
            orig.Invoke(self, obj, graspUsed);
            if (obj != null&&obj is Rock)
            {
                RandomBuffUtils.BuffUtils.Log(SisyphusID, "SaveRoom and pos");
                var rockData = (obj as Rock).SisyphusData();
                rockData.saveRoom = obj.room;
                rockData.savePos = obj.firstChunk.pos;
                rockData.saveRegion = obj.room.world.regionState.regionName;
            }
        }

        private static void Player_ReleaseObject(On.Player.orig_ReleaseObject orig, Player self, int grasp, bool eu)
        {
            Rock rock = self.grasps[grasp].grabbed as Rock;
            orig.Invoke(self, grasp, eu);
            if (rock.SisyphusData().saveRoom!=null&&rock.SisyphusData().saveRegion==self.room.world.regionState.regionName)
            {

                RandomBuffUtils.BuffUtils.Log(SisyphusID, "Warp rock and player");
                var data = rock.SisyphusData();
                Warp(self, data.saveRoom, data.savePos);
                Warp(rock, data.saveRoom, data.savePos);
                SisyphusBuff.Instance.TriggerSelf(true);

                
                if (self.room.game.cameras!=null&& self.room.game.cameras.Length>0)
                {
                    var microphone = self.room.game.cameras[0].virtualMicrophone;

                    for (int i = microphone.soundObjects.Count - 1; i >= 0; i--)
                    {
                        if (microphone.soundObjects[i].soundData.soundID == Enums.Sounds.sisyphus)
                        {
                            microphone.soundObjects[i].Destroy();
                        }
                    }
                    microphone.PlaySound(Enums.Sounds.sisyphus, 0f, 1, 1);

                }
            }

        }

        public static void Warp(PhysicalObject obj,Room newRoom,Vector2 newPos)
        {
            if (obj.room!=newRoom)
            {
                obj.abstractPhysicalObject.Move(newRoom.GetWorldCoordinate(newPos));
                obj.PlaceInRoom(newRoom);
                obj.abstractPhysicalObject.pos = newRoom.GetWorldCoordinate(newPos);
            }
            else
            {
                foreach (var item in obj.bodyChunks)
                {
                    item.HardSetPosition(newPos);
                }

            }
            
        }
    }
    public static class Sisyphus
    {
        public static ConditionalWeakTable<Rock, ExRock> modules = new ConditionalWeakTable<Rock, ExRock>();
        public  static ExRock SisyphusData(this Rock rock)=>modules.GetValue(rock,(Rock r)=>new ExRock(rock));
    }
    public class ExRock
    {
        Rock rock;

        public string saveRegion;
        public Room saveRoom;
        public Vector2 savePos;

        public ExRock(Rock rock)
        {
            this.rock = rock;
        }
    }
}