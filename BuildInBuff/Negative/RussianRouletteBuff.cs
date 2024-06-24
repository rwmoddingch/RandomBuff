using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuff.SpecificBuffModule;
using RandomBuff.Core.Game;

namespace BuiltinBuffs.Negative
{
    internal class RussianRouletteBuff : Buff<RussianRouletteBuff, RussianRouletteBuffData>, ICutPlayerBodyPartBuff
    {
        //假设这个buff可能会切除玩家头部，下循环玩家头部会“长回来”
        public static CutPlayerBodyPartData RussianRouletteCutData = new CutPlayerBodyPartData(new CuttingData(true), new CuttingData(false),
            new CuttingData(false), new CuttingData(false), new CuttingData(false));
        public override BuffID ID => RussianRouletteBuffEntry.RussianRoulette;
        public CutPlayerBodyPartData CutPlayerBodyPartData => RussianRouletteCutData;
        //重置头部的已切割信息
        public void ResetCuttingData()
        {
            if (PlayerCuttingBuffManager.playerCuttingBuffs.ContainsKey(ID))
            {
                PlayerCuttingBuffManager.playerCuttingBuffs[ID].headCut.cutPlayerNumber.Clear();
            }
        }
        public RussianRouletteBuff()
        {
            PlayerCuttingBuffManager.RegisterPlayerCuttingBuff(ID, CutPlayerBodyPartData);
            ResetCuttingData();
        }
        public override void Destroy()
        {
            base.Destroy();
            if (PlayerCuttingBuffManager.playerCuttingBuffs.ContainsKey(ID))
            {
                PlayerCuttingBuffManager.playerCuttingBuffs.Remove(ID);
            }
        }
    }

    class RussianRouletteBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 3;
        public override BuffID ID => RussianRouletteBuffEntry.RussianRoulette;
    }

    class RussianRouletteBuffEntry : IBuffEntry
    {
        public static BuffID RussianRoulette = new BuffID("RussianRoulette", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RussianRouletteBuff, RussianRouletteBuffData, RussianRouletteBuffEntry>(RussianRoulette);
        }
        public static void HookOn()
        {On.Weapon.Thrown += Weapon_Thrown;}
        private static void Weapon_Thrown(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if (!(thrownBy is Player)) return;
            var playerStillHasHead = true;
            var tempPool = BuffPoolManager.Instance.GetTemporaryBuffPool(RussianRoulette);
            for (int i = 0; i < tempPool.allBuffIDs.Count; i++)
            {
                var id = tempPool.allBuffIDs[i];
                //判断是否有其他会切掉头部的buff已经把该玩家头部切掉了
                if (PlayerCuttingBuffManager.playerCuttingBuffs.TryGetValue(id, out var data) && data.headCut.willCut && 
                    data.headCut.cutPlayerNumber.Contains((thrownBy as Player).playerState.playerNumber))
                {
                    playerStillHasHead = false;
                }
            }
            if (!playerStillHasHead) return;
            if (thrownBy != null && UnityEngine.Random.value < 0.16666667f)
            {
                if(PlayerCuttingBuffManager.playerCuttingBuffs.TryGetValue(RussianRoulette, out var data))
                {
                    //切掉该玩家的头部并记录被切掉头部的玩家编号
                    data.headCut.cutPlayerNumber.Add((thrownBy as Player).playerState.playerNumber);
                    var result = new SharedPhysics.CollisionResult(thrownBy, thrownBy.firstChunk, null, true, thrownBy.firstChunk.pos);
                    self.HitSomething(result, true);
                    if (self.room != null)
                    {
                        self.room.PlaySound(SoundID.Bomb_Explode, self.firstChunk);
                    }
                }                
            }            
        }
    }
}
