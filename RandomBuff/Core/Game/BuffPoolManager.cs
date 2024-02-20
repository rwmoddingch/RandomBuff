using System;
using System.Collections.Generic;
using System.Linq;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Game
{
    /// <summary>
    /// 游戏局内逻辑控制
    /// 每局游戏会重新创建
    ///
    /// 外部接口
    /// </summary>
    public sealed partial class BuffPoolManager
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
    public sealed partial class BuffPoolManager
    {
        public static BuffPoolManager Instance { get; private set; }
        public RainWorldGame Game { get;}


        private readonly Dictionary<BuffID, IBuff> buffDictionary = new();

        private List<IBuff> buffList = new();

        private BuffPoolManager(RainWorldGame game)
        {

            Game = game;

            BuffPlugin.Log("Clone all data to CycleData");
            foreach (var data in BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter))
                cycleDatas.Add(data.Key, data.Value.Clone());

            GameSetting = BuffDataManager.Instance.GetGameSetting(game.StoryCharacter).Clone();
            GameSetting.EnterGame();
          
            BuffPlugin.Log($"Enter Game,TemplateID: {GameSetting.gachaTemplate.ID}, Difficulty: {GameSetting.Difficulty}, Condition Count: {GameSetting.conditions.Count}");

            foreach (var data in BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter))
                CreateBuff(data.Key);


        }


        internal Dictionary<BuffID, BuffData> GetDataDictionary()
        {
            return cycleDatas;
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
            BuffHookWarpper.DisableBuff(id);
            buffDictionary[id].Destroy();
            buffDictionary.Remove(id);
            UnstackBuff(id);
        }

        /// <summary>
        /// 减少游戏内BuffData的堆叠层数
        /// 层数为0或非堆叠自动删除
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal void UnstackBuff(BuffID id)
        {
            
            if (!cycleDatas.ContainsKey(id))
            {
                BuffPlugin.LogError($"unstack buff not found: {id}");
                return;
            }

            if (BuffConfigManager.GetStaticData(id).Stackable)
            {
                BuffPlugin.LogError($"UnStack buff : {Game.StoryCharacter}");
                cycleDatas[id].UnStack();
                if (cycleDatas[id].StackLayer == 0)
                    RemoveBuffData(id);
            }
            else
            {
                RemoveBuffData(id);
            }
        }


        /// <summary>
        /// 移除游戏内BuffData
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private void RemoveBuffData(BuffID id)
        {
            BuffPlugin.Log($"Remove buff : {Game.StoryCharacter}-{id}");
            cycleDatas.Remove(id);
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
                return cycleDatas[id];
            }
            var re = (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(id));
            cycleDatas.Add(id, re);
            return re;
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
            return Instance = new BuffPoolManager(game);
        }

   

        internal void Update(RainWorldGame game)
        {
            if (game.GamePaused)
                return;

            foreach (var buff in buffList)
            {
                try
                {
                    buff.Update(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Update of {buff.ID}");
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
                    BuffHookWarpper.DisableBuff(buff.ID);
                    buff.Destroy();
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Destroy of {buff.ID}");
                }
            }
            Instance = null;
        }


        /// <summary>
        /// 周期结束后的移除或更新
        /// 若 NeedDeletion == true 则移除Buff
        /// </summary>
        internal void WinGame()
        {
            if (Game.manager.upcomingProcess != null)
                return;

            BuffPlugin.Log("------Win Game & Cycle End------");

            foreach (var buff in buffList)
            {
                try
                {
                    if (GetBuffData(buff.ID).NeedDeletion)
                        UnstackBuff(buff.ID);
                    
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Destroy of {buff.ID}");
                }
            }
            GameSetting.SessionEnd(Game);
            BuffDataManager.Instance.WinGame(this, cycleDatas, GameSetting);
            BuffFile.Instance.SaveFile();

            if (GameSetting.Win)
            {
                Game.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics,0.6f);
                Game.manager.upcomingProcess = ProcessManager.ProcessID.Statistics;
            }

        }


        /// <summary>
        /// 添加Buff实例
        /// 理论上不应出现添加BuffDataManager不存在的实例的情况
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal IBuff CreateBuff(BuffID id)
        {
            BuffPlugin.Log($"Create buff instance: {id}");
            if (buffDictionary.ContainsKey(id))
            {
                BuffPlugin.LogWarning($"Buff: {id} Already Contain");
                return buffDictionary[id];
            }

            if (!cycleDatas.ContainsKey(id))
            {
                BuffPlugin.Log($"Buff: {id} Not Contain in CycleData");
                CreateNewBuffData(id);
            }
            var type = BuffRegister.GetBuffType(id);
            if (type == null)
            {
                BuffPlugin.LogError($"Type of Buff ID Not Found: {id}");
                return null;
            }

            var buff = (IBuff)Activator.CreateInstance(type);
            BuffHookWarpper.EnableBuff(id);
            buffDictionary.Add(id, buff);
            buffList.Add(buff);
            return buff;
        }


        internal bool TriggerBuff(BuffID id, bool ignoreCheck = false)
        {
            var re = false;
            BuffPlugin.Log($"Trigger Buff: {id}, ignoreCheck: {ignoreCheck}");
            if (TryGetBuff(id, out var buff) && ((BuffConfigManager.GetStaticData(id).Triggerable && buff.Triggerable) || ignoreCheck))
            {
                BuffHud.Instance.TriggerCard(id);
                if ((re = buff.Trigger(Game)) == true)
                {
                    RemoveBuff(buff.ID);
                }
            }

            return re;
        }

        /// 轮回内的临时数据
        private readonly Dictionary<BuffID, BuffData> cycleDatas = new();
        internal GameSetting GameSetting { get; private set; }
    }
}
