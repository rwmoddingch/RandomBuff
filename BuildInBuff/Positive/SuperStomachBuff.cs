using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using System.Runtime.InteropServices;

namespace BuiltinBuffs.Positive
{
    internal class SuperStomachBuff : Buff<SuperStomachBuff, SuperStomachBuffData>
    {
        public override BuffID ID => SuperStomachBuffEntry.SuperStomach;
    }

    class SuperStomachBuffData : BuffData
    {
        public override BuffID ID => SuperStomachBuffEntry.SuperStomach;

        [JsonProperty] public string storageData_1;
        [JsonProperty] public string storageData_2;
        [JsonProperty] public string storageData_3;
        [JsonProperty] public string storageData_4;
    }

    class SuperStomachBuffEntry : IBuffEntry
    {
        public static BuffID SuperStomach = new BuffID("SuperStomach", true);
        public static ConditionalWeakTable<Player, BigStomach> bigStomach = new ConditionalWeakTable<Player, BigStomach>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperStomachBuff, SuperStomachBuffData, SuperStomachBuffEntry>(SuperStomach);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
            On.Player.GrabUpdate += Player_GrabUpdate;
            On.Player.BiteEdibleObject += Player_BiteEdibleObject;
            On.Player.MaulingUpdate += Player_MaulingUpdate;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.SlugcatHand.Update += SlugcatHand_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
        }
        
        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            try
            {
                var buffData = BuffCore.GetBuffData(SuperStomach);
                if (buffData != null)
                {
                    for (int i = 0; i < self.Players.Count; i++)
                    {
                        if (self.Players[i].realizedCreature == null) continue;
                        switch (i)
                        {
                            case 0:
                                {
                                    if (bigStomach.TryGetValue(self.Players[i].realizedCreature as Player, out var module0))
                                    {
                                        if (module0.objectsInStomach.Count > 0)
                                        {
                                            (buffData as SuperStomachBuffData).storageData_1 = module0.ToSaveString;
                                        }
                                    }
                                    continue;
                                }
                            case 1:
                                {
                                    if (bigStomach.TryGetValue(self.Players[i].realizedCreature as Player, out var module1))
                                    {
                                        if (module1.objectsInStomach.Count > 0)
                                        {
                                            (buffData as SuperStomachBuffData).storageData_2 = module1.ToSaveString;
                                        }
                                    }
                                    continue;
                                }
                            case 2:
                                {
                                    if (bigStomach.TryGetValue(self.Players[i].realizedCreature as Player, out var module2))
                                    {
                                        if (module2.objectsInStomach.Count > 0)
                                        {
                                            (buffData as SuperStomachBuffData).storageData_3 = module2.ToSaveString;
                                        }
                                    }
                                    continue;
                                }
                            case 3:
                                {
                                    if (bigStomach.TryGetValue(self.Players[i].realizedCreature as Player, out var module3))
                                    {
                                        if (module3.objectsInStomach.Count > 0)
                                        {
                                            (buffData as SuperStomachBuffData).storageData_4 = module3.ToSaveString;
                                        }
                                    }
                                    continue;
                                }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            orig(self, malnourished);
        }

        private static void Player_BiteEdibleObject(On.Player.orig_BiteEdibleObject orig, Player self, bool eu)
        {
            orig(self, eu);
            if (bigStomach.TryGetValue(self, out var module))
            {
                module.regurgitateCounter = 0;
                module.swallowCounter = 0;
            }
        }

        private static void Player_MaulingUpdate(On.Player.orig_MaulingUpdate orig, Player self, int graspIndex)
        {
            orig(self, graspIndex);
            if (bigStomach.TryGetValue(self, out var module))
            {
                module.regurgitateCounter = 0;
                module.swallowCounter = 0;
            }
        }

        private static void Player_GrabUpdate(On.Player.orig_GrabUpdate orig, Player self, bool eu)
        {
            orig(self, eu);
            if (bigStomach.TryGetValue(self, out var module) && self.slugcatStats.name != MoreSlugcatsEnums.SlugcatStatsName.Spear)
            {
                if (module.regurgitateCounter > 0) { module.regurgitateCounter--; }

                bool pckpHold = self.input[0].pckp && self.input[0].y == 0;
                bool canRegurgitate = true;
                bool shouldUpdate = true;
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] != null && self.grasps[i].grabbed != null && self.grasps[i].grabbed is IPlayerEdible && self.FoodInStomach < self.MaxFoodInStomach)
                    {
                        canRegurgitate = false;
                        shouldUpdate = false;
                        break;
                    }
                }
                if (shouldUpdate)
                {
                    if (self.objectInStomach != null && module.objectsInStomach.Count < 3 && pckpHold)
                    {
                        for (int i = 0; i < self.grasps.Length; i++)
                        {
                            if (self.grasps[i] != null && self.grasps[i].grabbed != null && self.CanBeSwallowed(self.grasps[i].grabbed))
                            {
                                canRegurgitate = false;
                                if (module.swallowCounter < 90) module.swallowCounter++;
                                self.swallowAndRegurgitateCounter = 0;
                                if (module.swallowCounter >= 90)
                                {
                                    SwallowToExtraStomach(self, i, module);
                                    if (self.graphicsModule != null) (self.graphicsModule as PlayerGraphics).swallowing = 20;
                                    module.swallowCounter = 0;
                                    break;
                                }
                            }
                        }

                        if (canRegurgitate && pckpHold && module.objectsInStomach.Count > 0)
                        {
                            self.swallowAndRegurgitateCounter = 0;
                            module.regurgitateCounter += 2;
                            self.Blink(5);
                            if (module.regurgitateCounter > (90 - 6f * Mathf.Pow(module.objectsInStomach.Count, 1.4f)))
                            {
                                RegurgitateObjInExtraStomach(self, module);
                            }
                        }
                    }
                    else
                    {
                        if (module.swallowCounter > 0) module.swallowCounter--;

                        for (int i = 0; i < self.grasps.Length; i++)
                        {
                            if (self.grasps[i] != null && self.grasps[i].grabbed != null && self.CanBeSwallowed(self.grasps[i].grabbed))
                            {
                                canRegurgitate = false;
                                break;
                            }
                        }

                        if (canRegurgitate && pckpHold && module.objectsInStomach.Count > 0)
                        {
                            self.swallowAndRegurgitateCounter = 0;
                            module.regurgitateCounter += 2;
                            self.Blink(5);
                            if (module.regurgitateCounter > (90 - 6f * Mathf.Pow(module.objectsInStomach.Count, 1.4f)))
                            {
                                RegurgitateObjInExtraStomach(self, module);
                            }
                        }
                    }
                }                
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (bigStomach.TryGetValue(self.player, out var module))
            {
                if (module.regurgitateCounter > 2)
                {
                    float num10 = Mathf.InverseLerp(0f, 110f, (float)module.regurgitateCounter);
                    float num11 = (float)module.regurgitateCounter / Mathf.Lerp(30f, 15f, num10);
                    if (self.player.standing)
                    {
                        self.drawPositions[0, 0].y += Mathf.Sin(num11 * 3.14159274f * 2f) * num10 * 2f;
                        self.drawPositions[1, 0].y += -Mathf.Sin((num11 + 0.2f) * 3.14159274f * 2f) * num10 * 3f;
                    }
                    else
                    {
                        self.drawPositions[0, 0].y += Mathf.Sin(num11 * 3.14159274f * 2f) * num10 * 3f;
                        self.drawPositions[0, 0].x += Mathf.Cos(num11 * 3.14159274f * 2f) * num10 * 1f;
                        self.drawPositions[1, 0].y += Mathf.Sin((num11 + 0.2f) * 3.14159274f * 2f) * num10 * 2f;
                        self.drawPositions[1, 0].x += -Mathf.Cos(num11 * 3.14159274f * 2f) * num10 * 3f;
                    }
                }
            }
        }

        private static void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
        {
            orig(self);
            if (bigStomach.TryGetValue(self.owner.owner as Player, out var module) && module.swallowCounter > 10)
            {
                int num3 = -1;
                int num4 = 0;
                while (num3 < 0 && num4 < 2)
                {
                    if ((self.owner.owner as Player).grasps[num4] != null && (self.owner.owner as Player).CanBeSwallowed((self.owner.owner as Player).grasps[num4].grabbed))
                    {
                        num3 = num4;
                    }
                    num4++;
                }
                if (num3 == self.limbNumber)
                {
                    float num5 = Mathf.InverseLerp(10f, 90f, module.swallowCounter);
                    if (num5 < 0.5f)
                    {
                        self.relativeHuntPos *= Mathf.Lerp(0.9f, 0.7f, num5 * 2f);
                        self.relativeHuntPos.y = self.relativeHuntPos.y + Mathf.Lerp(2f, 4f, num5 * 2f);
                        self.relativeHuntPos.x = self.relativeHuntPos.x * Mathf.Lerp(1f, 1.2f, num5 * 2f);
                    }
                    else
                    {
                        (self.owner as PlayerGraphics).blink = 5;
                        self.relativeHuntPos = new Vector2(0f, -4f) + Custom.RNV() * 2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                        (self.owner as PlayerGraphics).head.vel += Custom.RNV() * 2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                        self.owner.owner.bodyChunks[0].vel += Custom.RNV() * 0.2f * UnityEngine.Random.value * Mathf.InverseLerp(0.5f, 1f, num5);
                    }
                }
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            try
            {
                if (!bigStomach.TryGetValue(self, out var data))
                {
                    bigStomach.Add(self, new BigStomach());
                    bigStomach.TryGetValue(self, out var module);
                    var buffData = BuffCore.GetBuffData(SuperStomach);
                    if (buffData != null && self.room != null && self.room.world != null)
                    {
                        if (self.IsJollyPlayer)
                        {
                            switch (self.playerState.playerNumber)
                            {
                                case 1:
                                    {
                                        if (!string.IsNullOrEmpty((buffData as SuperStomachBuffData).storageData_1))
                                        {
                                            module.AbsObjFromString(self.room.world, (buffData as SuperStomachBuffData).storageData_1);
                                            (buffData as SuperStomachBuffData).storageData_1 = string.Empty;
                                        }
                                        break;
                                    }
                                case 2:
                                    {
                                        if (!string.IsNullOrEmpty((buffData as SuperStomachBuffData).storageData_2))
                                        {
                                            module.AbsObjFromString(self.room.world, (buffData as SuperStomachBuffData).storageData_2);
                                            (buffData as SuperStomachBuffData).storageData_2 = string.Empty;
                                        }
                                        break;
                                    }
                                case 3:
                                    {
                                        if (!string.IsNullOrEmpty((buffData as SuperStomachBuffData).storageData_3))
                                        {
                                            module.AbsObjFromString(self.room.world, (buffData as SuperStomachBuffData).storageData_3);
                                            (buffData as SuperStomachBuffData).storageData_3 = string.Empty;
                                        }
                                        break;
                                    }
                                case 4:
                                    {
                                        if (!string.IsNullOrEmpty((buffData as SuperStomachBuffData).storageData_4))
                                        {
                                            module.AbsObjFromString(self.room.world, (buffData as SuperStomachBuffData).storageData_4);
                                            (buffData as SuperStomachBuffData).storageData_4 = string.Empty;
                                        }
                                        break;
                                    }
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty((buffData as SuperStomachBuffData).storageData_1))
                            {
                                module.AbsObjFromString(self.room.world, (buffData as SuperStomachBuffData).storageData_1);
                            }
                        }
                    }
                }

                /*
                if (bigStomach.TryGetValue(self, out var _module))
                {
                    _module.swallowCooldown--;
                    if (_module.swallowCooldown <= 0 && Input.GetKey(KeyCode.L) && self.room != null && self.room.world != null)
                    {
                        _module.swallowCooldown = 40;
                        if (_module.objectsInStomach.Count < 3)
                        {
                            AbstractPhysicalObject absObj;
                            if (UnityEngine.Random.value < 0.5f)
                            {
                                absObj = new AbstractPhysicalObject(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID());
                            }
                            else
                            {
                                absObj = new AbstractSpear(self.room.world, null, new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID(), true);
                            }
                            _module.objectsInStomach.Add(absObj);
                        }
                    }

                    if (_module.objectsInStomach.Count > 0 && Input.GetKey(KeyCode.M) && self.room != null)
                    {
                        var absObj = _module.objectsInStomach.Pop();
                        absObj.world = self.room.world;
                        absObj.pos = new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0);
                        self.room.abstractRoom.AddEntity(absObj);
                        absObj.RealizeInRoom();
                        absObj.realizedObject.firstChunk.HardSetPosition(self.firstChunk.pos + 30f * Vector2.up);
                    }
                }
                */
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
        }

        public static void SwallowToExtraStomach(Player self, int grasp, BigStomach module)
        {
            if (grasp < 0 || self.grasps[grasp] == null)
            {
                return;
            }

            if (self.room != null && self.room.game != null)
            {
                AbstractPhysicalObject abstractPhysicalObject = self.grasps[grasp].grabbed.abstractPhysicalObject;
                if (abstractPhysicalObject is AbstractSpear)
                {
                    (abstractPhysicalObject as AbstractSpear).stuckInWallCycles = 0;
                }
                module.objectsInStomach.Add(abstractPhysicalObject);
                if (ModManager.MMF && self.room.game.session is StoryGameSession)
                {
                    (self.room.game.session as StoryGameSession).RemovePersistentTracker(abstractPhysicalObject);
                }
                self.ReleaseGrasp(grasp);
                abstractPhysicalObject.realizedObject.RemoveFromRoom();
                abstractPhysicalObject.Abstractize(self.abstractCreature.pos);
                abstractPhysicalObject.Room.RemoveEntity(abstractPhysicalObject);
            }
        }

        public static void RegurgitateObjInExtraStomach(Player self, BigStomach module)
        {
            if (self.room != null && self.room.world != null)
            {
                var absObj = module.objectsInStomach.Pop();
                absObj.world = self.room.world;
                absObj.pos = new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0);
                self.room.abstractRoom.AddEntity(absObj);
                absObj.RealizeInRoom();
                absObj.realizedObject.firstChunk.HardSetPosition(self.firstChunk.pos);
                module.regurgitateCounter = 0;

                Vector2 a = Custom.DirVec(self.bodyChunks[1].pos, self.bodyChunks[0].pos);
                for (int i = 0; i < 3; i++)
                {
                    self.room.AddObject(new WaterDrip(self.firstChunk.pos + Custom.RNV() * UnityEngine.Random.value * 1.5f, Custom.RNV() * 3f * UnityEngine.Random.value + a * Mathf.Lerp(2f, 6f, UnityEngine.Random.value), false));
                }
                self.room.PlaySound(SoundID.Slugcat_Regurgitate_Item, self.mainBodyChunk);

                if (self.FreeHand() != -1 && self.bodyMode != Player.BodyModeIndex.Crawl && self.bodyMode != Player.BodyModeIndex.CorridorClimb)
                {
                    self.SlugcatGrab(absObj.realizedObject, self.FreeHand());
                }
            }
        }
    }

    public class BigStomach
    {
        public List<AbstractPhysicalObject> objectsInStomach;
        //public int swallowCooldown;
        public int swallowCounter;
        public int regurgitateCounter;

        public BigStomach()
        {
            objectsInStomach = new List<AbstractPhysicalObject>();
        }

        public string ToSaveString
        {
            get
            {
                if (objectsInStomach.Count > 0)
                {
                    string str = string.Empty;
                    for (int i = 0; i < objectsInStomach.Count; i++)
                    {
                        str += objectsInStomach[i].ToString();
                        if (i < objectsInStomach.Count - 1)
                        {
                            str += "<sSoA>";
                        }
                    }
                    UnityEngine.Debug.Log("Generate SStomach String: " + str);
                    return str;
                }
                return string.Empty;
            }
        }

        public void AbsObjFromString(World world, string data)
        {
            string[] array = Regex.Split(data, "<sSoA>");
            for (int i = 0; i < array.Length; i++)
            {
                var absObj = SaveState.AbstractPhysicalObjectFromString(world, array[i]);
                if (absObj != null) objectsInStomach.Add(absObj);
            }
        }
    }
}
