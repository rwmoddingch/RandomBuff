using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Record;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI.ExceptionTracker;
using RandomBuffUtils;
using UnityEngine;

namespace RandomBuff.Core.Game
{
    /// <summary>
    /// 游戏局内逻辑控制
    /// 每局游戏会重新创建
    ///
    /// 哈哈没有外部接口！
    /// </summary>
    internal sealed partial class BuffPoolManager
    {
        /// <summary>
        /// 获取ID对应的Buff
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IBuff GetBuff(BuffID id)
        {
            if(buffDictionary.ContainsKey(id))
                return buffDictionary[id];
            return null;
        }

        /// <summary>
        /// 获取ID对应的BuffData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public BuffData GetBuffData(BuffID id)
        {
            if (cycleDatas.ContainsKey(id))
                return cycleDatas[id];
            return null;
        }

        /// <summary>
        /// 获取ID对应的Buff
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool TryGetBuff(BuffID id, out IBuff buff)
        {
            buff = null;
            if (buffDictionary.ContainsKey(id))
            {
                buff = buffDictionary[id];
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取全部启用的BuffID
        /// 外部方法
        /// </summary>
        /// <returns></returns>
        public List<BuffID> GetAllBuffIds()
        {
            return cycleDatas.Keys.ToList();
        }
    }


    /// <summary>
    /// 游戏局内逻辑控制
    /// 每局游戏会重新创建
    ///
    /// 内部接口
    /// </summary>
    internal sealed partial class BuffPoolManager
    {
        public static BuffPoolManager Instance { get; private set; }
        public RainWorldGame Game { get;}


        private readonly Dictionary<BuffID, IBuff> buffDictionary = new();

        private List<IBuff> buffList = new();

        private List<CosmeticUnlock> cosmeticList = new();

        internal readonly Dictionary<BuffID, TemporaryBuffPool> temporaryBuffPools = new Dictionary<BuffID, TemporaryBuffPool>();

        private InGameRecord record;

        public bool isInitHud = false;

        private BuffPoolManager(RainWorldGame game)
        {

#if TESTVERSION
            if (File.Exists(AssetManager.ResolveFilePath("hotreload.txt")))
            {
                foreach (var id in File.ReadAllLines(AssetManager.ResolveFilePath("hotreload.txt")).Select(i => new BuffID(i)))
                {
                    if (BuffConfigManager.ContainsId(id))
                    {
                        BuffPlugin.Log($"Hot reload {id}");
                        var staticData = id.GetStaticData();
                        Futile.atlasManager.UnloadImage(staticData.FaceName);
                        Futile.atlasManager.LoadImage(staticData.FaceName);

                    }
                }
            }
#endif
            Game = game;
            record = new InGameRecord();
            BuffPlugin.Log("Clone all data to CycleData");
            foreach (var data in BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter))
                cycleDatas.Add(data.Key, data.Value.Clone());

            Instance = this;

            GameSetting = BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).Clone();
            GameSetting.EnterGame(Game);
          
            BuffPlugin.Log($"------Enter Game,TemplateID: {GameSetting.gachaTemplate.ID}, Difficulty: {GameSetting.Difficulty}, Condition Count: {GameSetting.conditions.Count}{(GameSetting.MissionId is null ? "" : $", Mission: {GameSetting.MissionId}")}------");


            foreach(var condition in GameSetting.conditions)
                BuffPlugin.LogDebug($"---condition: {condition.ID}");

            foreach (var data in BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter))
            {
                CreateBuff(data.Key);
            }

            BuffDataManager.Instance.CleanMalnourishedData();


            foreach (var value in CosmeticUnlockID.values.entries.Where(BuffConfigManager.IsCosmeticCanUse))
            {
                var unlock = CosmeticUnlock.CreateInstance(value,game);
                if(unlock != null)
                    cosmeticList.Add(unlock);
            }


        }


        internal Dictionary<BuffID, BuffData> GetDataDictionary()
        {
            return cycleDatas;
        }


        /// <summary>
        /// 减少游戏内BuffData的堆叠层数
        /// 层数为0或非堆叠自动删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool UnstackBuff(BuffID id,bool removeBuffInstance = true)
        {
            
            if (!cycleDatas.ContainsKey(id))
            {
                BuffPlugin.LogError($"unstack buff not found: {id}");
                return false;
           
            }

            if (BuffConfigManager.GetStaticData(id).Stackable)
            {
                BuffPlugin.Log($"UnStack buff : {Game.StoryCharacter}:{id}");
                try
                {
                    cycleDatas[id].UnStack();
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e,$"Exception in BuffPoolManager:UnstackBuff:{id}");
                }
                if (cycleDatas[id].StackLayer == 0)
                {
                    if(removeBuffInstance)
                        RemoveBuffAndData(id);
                    else
                        RemoveData(id);
                    return true;
                }
            }
            else
            {
                if (removeBuffInstance)
                    RemoveBuffAndData(id);
                else
                    RemoveData(id);
                return true;
            }

            return false;
        }


        /// <summary>
        /// 局内创建BuffData
        /// </summary>
        /// <param name="id"></param>
        internal BuffData CreateNewBuffData(BuffID id)
        {
            if (cycleDatas.ContainsKey(id))
            {
                BuffPlugin.Log($"Already contains BuffData {id} in {Game.StoryCharacter} game, stack More");
                cycleDatas[id].Stack();

                if (GameSetting.gachaTemplate is not SandboxGachaTemplate)
                {
                    BuffPlayerData.Instance.SlotRecord.AddCard(id.GetStaticData().BuffType);
                    record.AddCard(id.GetStaticData().BuffType);
                }

                return cycleDatas[id];
            }

            try
            {
                var re = (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(id));
                BuffHookWarpper.EnableBuff(id, HookLifeTimeLevel.UntilQuit);
                re.Stack();
                BuffPlayerData.Instance.SlotRecord.AddCard(id.GetStaticData().BuffType);
                record.AddCard(id.GetStaticData().BuffType);
                cycleDatas.Add(id, re);
                return re;
            }
            catch (Exception ex)
            {
                BuffPlugin.LogException(ex,$"Exception at BuffPoolManger:CreateNewBuffData:{id}");
                return null;
            }
        }

        /// <summary>
        /// 创建新的BuffPool
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        internal static BuffPoolManager LoadGameBuff(RainWorldGame game)
        {
            BuffPlugin.Log($"New game, character: {game.StoryCharacter}, Slot: {game.rainWorld.options.saveSlot}, " +
                           $"buff count: {BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter).Count}");
            return new BuffPoolManager(game);
        }


        bool lastbuttonPressed;
        internal void Update(RainWorldGame game)
        {
            if (game.GamePaused)
                return;

            if(!lastbuttonPressed && Input.GetKey(KeyCode.P) && Input.GetKey(KeyCode.A) && BuffPlugin.DevEnabled)
            {
                game.Win(false);
            }

            lastbuttonPressed = Input.GetKey(KeyCode.P) && Input.GetKey(KeyCode.A);



            foreach (var buff in buffList)
            {
                try
                {
                    buff.Update(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke buff Update of {buff.ID}");
                    ExceptionTracker.TrackException(e, $"Exception happened when invoke buff Update of {buff.ID}");
                }
            }


            foreach (var cosmetic in cosmeticList)
            {
                try
                {
                    cosmetic.Update(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke cosmeticUnlock Update of {cosmetic.UnlockID}");
                    ExceptionTracker.TrackException(e, $"Exception happened when invoke cosmeticUnlock Update of {cosmetic.UnlockID}");
                }
            }
            GameSetting.InGameUpdate(game);
        }



        /// <summary>
        /// 周期结束后自动销毁
        /// 若 NeedDeletion == true 则移除Buff
        /// </summary>
        internal void Destroy()
        {
            BuffPlugin.Log("DESTROY BUFF POOL!");
            foreach (var buff in buffList)
            {
                try
                {
                    BuffHookWarpper.DisableBuff(buff.ID, HookLifeTimeLevel.InGame);
                    buff.Destroy();
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Destroy of {buff.ID}");
                    ExceptionTracker.TrackException(e, $"Exception happened when invoke gain Destroy of {buff.ID}");
                }
            }
            foreach (var cosmetic in cosmeticList)
                cosmetic.Destroy();
            cosmeticList.Clear();
            Instance = null;
            if (PlayerUtils.owners.Count > 0)
            {
                int num = PlayerUtils.owners.Count;
                for (int i = 0; i < num; i++)
                    PlayerUtils.RemovePart(PlayerUtils.owners[0]);
                
            }

            GameSetting.OnDestroy();
        }


        /// <summary>
        /// 周期结束后的移除或更新
        /// 若 NeedDeletion == true 则移除Buff
        /// </summary>
        internal void WinGame(bool malnourished)
        {
            if (Game.manager.upcomingProcess != null)
                return;

            BuffPlugin.Log("------Win Game & Cycle End------");

            BuffPlugin.Log("Clear out temporary buff pools");
            foreach(var pool in temporaryBuffPools.Values)
            {
                pool.Destroy();
            }
            temporaryBuffPools.Clear();

            GameSetting.SessionEnd(Game);
            GameSetting.inGameRecord += record;

            //先进行CycleEnd
            BuffDataManager.Instance.WinGame(this, cycleDatas, GameSetting, malnourished);

            //后处理删除
            for (int i = buffList.Count-1;i>=0;i--)
            {
                var buff = buffList[i];
                try
                {
                    if (GetBuffData(buff.ID).NeedDeletion)
                        UnstackBuff(buff.ID);

                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke buff Destroy of {buff.ID}");
                    ExceptionTracker.TrackException(e, $"Exception happened when invoke buff Destroy of {buff.ID}");
                }
            }

            BuffFile.Instance.SaveFile();

            if (GameSetting.Win || (Input.GetKey(KeyCode.P) && Input.GetKey(KeyCode.A) && BuffPlugin.DevEnabled))
            {
                CreateWinGamePackage();
                
                Game.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.BuffGameWinScreen,0.6f);
            }
        }


        /// <summary>
        /// 添加Buff实例
        /// 理论上不应出现添加BuffDataManager不存在的实例的情况
        /// </summary>
        /// <param name="id"></param>
        /// <param name="needStack"></param>
        /// <returns></returns>
        internal IBuff CreateBuff(BuffID id, bool needStack = false)
        {
            BuffPlugin.Log($"Create buff instance: {id}");
            if (buffDictionary.ContainsKey(id))
            {
                BuffPlugin.LogWarning($"Buff: {id} Already Contain");
                if(needStack && id.GetStaticData().Stackable)
                    GetBuffData(id).Stack();
                return buffDictionary[id];
            }

            if (!cycleDatas.ContainsKey(id))
            {
                BuffPlugin.Log($"Buff: {id} Not Contain in CycleData");
                if (CreateNewBuffData(id) == null)
                {
                    BuffPlugin.LogError($"Create BuffData {id} Failed!");
                    return null;
                }
            }
            var type = BuffRegister.GetBuffType(id);
            if (type == null)
            {
                BuffPlugin.LogError($"Type of Buff ID Not Found: {id}");
                return null;
            }

            try
            {
                var buff = (IBuff)Activator.CreateInstance(type);
                buffDictionary.Add(id, buff);
                buffList.Add(buff);
                BuffHookWarpper.EnableBuff(id, HookLifeTimeLevel.InGame);
                return buff;
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"Exception in BuffPoolManager:CreateBuff:{id}");
                cycleDatas.Remove(id);
                return null;
            }
        }


        internal bool TriggerBuff(BuffID id, bool ignoreCheck = false)
        {
            BuffPlugin.Log($"Trigger Buff: {id}, ignoreCheck: {ignoreCheck}");
            if (TryGetBuff(id, out var buff) && ((BuffConfigManager.GetStaticData(id).Triggerable && buff.Triggerable) || ignoreCheck))
            {
                bool re = false;
                try
                {
                    re = buff.Trigger(Game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e,$"Exception in BuffPoolManager:TriggerBuff:{id}");
                }

                BuffHud.Instance.TriggerCard(buff.ID);

                if (GameSetting.gachaTemplate is not SandboxGachaTemplate)
                {
                    record.ActiveCard();
                    BuffPlayerData.Instance.SlotRecord.ActiveCard();
                }


                if (re)
                {
                    return UnstackBuff(buff.ID);
                }
            }
            return false;
        }

        /// <summary>
        /// 游戏进行中删除Buff
        /// </summary>
        /// <param name="id"></param>
        internal void RemoveBuff(BuffID id)
        {
            if (!buffDictionary.ContainsKey(id))
            {
                BuffPlugin.LogError($"remove buff not found: {id}");
                return;
            }
            BuffHookWarpper.DisableBuff(id, HookLifeTimeLevel.InGame);
            BuffHookWarpper.DisableBuff(id, HookLifeTimeLevel.UntilQuit);

            try
            {
                buffDictionary[id].Destroy();

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception in BuffPoolManager:RemoveBuff:{id}");
            }
            finally
            {
                buffList.Remove(buffDictionary[id]);
                buffDictionary.Remove(id);
            }
        }

        /// <summary>
        /// 移除游戏内BuffData和Buff
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal void RemoveBuffAndData(BuffID id)
        {
            RemoveBuff(id);
            RemoveData(id);
            BuffHud.Instance.RemoveCard(id);
        }

        private void RemoveData(BuffID id)
        {
            BuffPlugin.Log($"Remove buff data : {Game.StoryCharacter}-{id}");
            cycleDatas.Remove(id);
        }

        internal TemporaryBuffPool GetTemporaryBuffPool(BuffID id)
        {
            if(temporaryBuffPools.ContainsKey(id))
                return temporaryBuffPools[id];

            TemporaryBuffPool result = new TemporaryBuffPool(this);
            temporaryBuffPools.Add(id, result);
            return result;
        }

        /// 轮回内的临时数据
        private readonly Dictionary<BuffID, BuffData> cycleDatas = new();
        internal GameSetting GameSetting { get; private set; }
    }

    /// <summary>
    /// 游戏局内逻辑控制
    /// 每局游戏会重新创建
    /// 
    /// 游戏结算的数据传递
    /// </summary>
    internal sealed partial class BuffPoolManager
    {
        public WinGamePackage winGamePackage;

        private void CreateWinGamePackage()
        {
            winGamePackage = new WinGamePackage
            {
                missionId = GameSetting.MissionId,
                saveState = Game.GetStorySession.saveState,
                expMultiply = GameSetting.gachaTemplate.ExpMultiply,
            };
            foreach (var buff in buffDictionary.Keys)
                winGamePackage.winWithBuffs.Add(buff);
            foreach(var condition in GameSetting.conditions) 
                winGamePackage.winWithConditions.Add(condition);

            winGamePackage.sessionRecord = Game.GetStorySession.playerSessionRecords[0];
            winGamePackage.buffRecord = new InGameTimerRecord() { totTime = SpeedRunTimer.TimeFormat(SpeedRunTimer.GetCampaignTimeTracker(GameSetting.name).TotalFixedTimeSpan) };
            winGamePackage.buffRecord.totCard +=  GameSetting.inGameRecord.totCard;
            winGamePackage.buffRecord.totPositiveCard += GameSetting.inGameRecord.totPositiveCard;
            winGamePackage.buffRecord.totDualityCard += GameSetting.inGameRecord.totDualityCard;
            winGamePackage.buffRecord.totNegativeCard += GameSetting.inGameRecord.totNegativeCard;
            winGamePackage.buffRecord.totTriggerCount += GameSetting.inGameRecord.totTriggerCount;

            if (ModManager.CoopAvailable)
            {
                for (int i = 1; i < Game.GetStorySession.playerSessionRecords.Length; i++)
                {
                    if (Game.GetStorySession.playerSessionRecords[i].kills != null && Game.GetStorySession.playerSessionRecords[i].kills.Count > 0)
                    {
                        winGamePackage.sessionRecord.kills.AddRange(Game.GetStorySession.playerSessionRecords[i].kills);
                    }
                }
            }
        }
    }

    public class TemporaryBuffPool
    {
        internal BuffPoolManager poolManager;
        public List<BuffID> managedIDs = new List<BuffID>();
        public List<BuffID> allBuffIDs => poolManager.GetAllBuffIds();

        internal TemporaryBuffPool(BuffPoolManager poolManager)
        {
            this.poolManager = poolManager;
        }

        public bool CreateTemporaryBuff(BuffID id, bool needStack = false)
        {
            if (allBuffIDs.Contains(id))
                return false;

            poolManager.CreateBuff(id, needStack);
            BuffHud.Instance?.AppendNewCard(id);
            managedIDs.Add(id);

            return true;
        }

        public void RemoveTemporaryBuffAndData(BuffID id)
        {
            if (!managedIDs.Contains(id))
                return;
            managedIDs.Remove(id);
            poolManager.RemoveBuffAndData(id);
        }

        internal void Destroy()
        {
            for(int i = managedIDs.Count - 1; i >= 0; i--)
            {
                RemoveTemporaryBuffAndData(managedIDs[i]);
            }
        }
    }

    public class WinGamePackage
    {
        public List<BuffID> winWithBuffs = new List<BuffID>();
        public List<Condition> winWithConditions = new List<Condition>();
        public PlayerSessionRecord sessionRecord;
        public SaveState saveState;
        public InGameTimerRecord buffRecord;

        public float expMultiply;
        public string missionId;
    }
}
