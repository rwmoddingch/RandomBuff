﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData;
using UnityEngine;

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
            foreach (var data in BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter))
            {
                CreateBuff(data.Key);
            }
        }

        /// <summary>
        /// 创建新的BuffPool
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        internal static BuffPoolManager LoadGameBuff(RainWorldGame game)
        {
            BuffPlugin.Log($"New game, character: {game.StoryCharacter}, " +
                           $"buff count: {BuffDataManager.Instance.GetDataDictionary(game.StoryCharacter).Count}");
            return Instance = new BuffPoolManager(game);
        }

   

        internal void Update(RainWorldGame game)
        {
            foreach (var buff in buffList)
            {
                try
                {
                    buff.Update(game);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Update of {buff.ID}");
                }
            }
        }



        /// <summary>
        /// 周期结束后自动销毁
        /// 若 NeedDeletion == true 则移除Buff
        /// </summary>
        internal void Destroy()
        {
            BuffPlugin.Log("Destroy buff pool");
            foreach (var buff in buffList)
            {
                try
                {
                    BuffHookWarpper.DisableBuff(buff.ID);
                    buff.Destroy();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
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
                    if (buff.NeedDeletion)
                        BuffDataManager.Instance.RemoveBuffData(buff.ID);
                    
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    BuffPlugin.LogError($"Exception happened when invoke gain Destroy of {buff.ID}");
                }
            }
            BuffDataManager.Instance.WinGame(this);
            BuffFile.Instance.SaveFile();
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

            if (!BuffDataManager.Instance.GetDataDictionary(Game.StoryCharacter).ContainsKey(id))
            {
                BuffPlugin.LogWarning($"Buff: {id} Not Contain in BuffDataManager");
                BuffDataManager.Instance.GetOrCreateBuffData(id, true);
            }
            var type = BuffRegister.GetBuffType(id);
            if (type == null)
            {
                BuffPlugin.LogError($"Type of Buff ID Not Found: {id}");
                return null;
            }

            var buff = (IBuff)Activator.CreateInstance(type);
            buffDictionary.Add(id, buff);
            buffList.Add(buff);
            return buff;
        }
    }
}