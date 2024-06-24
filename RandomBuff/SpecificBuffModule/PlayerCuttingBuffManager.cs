using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;

namespace RandomBuff.SpecificBuffModule
{
    public partial class CutPlayerBodyPartData
    {
        public CuttingData headCut;
        public CuttingData tailCut;
        public CuttingData leftArmCut;
        public CuttingData rightArmCut;
        public CuttingData legsCut;
        /// <summary>
        /// 用来记录Buff切割玩家所有部位的信息
        /// </summary>
        /// <param name="headCut"></param>
        /// <param name="leftArmCut"></param>
        /// <param name="rightArmCut"></param>
        /// <param name="tailCut"></param>
        /// <param name="legsCut"></param>
        public CutPlayerBodyPartData(CuttingData headCut, CuttingData leftArmCut, CuttingData rightArmCut, CuttingData tailCut, CuttingData legsCut)
        {
            this.headCut = headCut;
            this.leftArmCut = leftArmCut;
            this.rightArmCut = rightArmCut;
            this.tailCut = tailCut;
            this.legsCut = legsCut;
        }
    }

    public class CuttingData
    {
        /// <summary>
        /// Buff包含切除这部分肢体的效果
        /// </summary>
        /// <param name="willCut"></param>
        public bool willCut;
        /// <summary>
        /// 已被切掉该部位的玩家编号
        /// </summary>
        public List<int> cutPlayerNumber;
        /// <summary>
        /// willCut: Buff包含切除这部分肢体的效果; 
        /// alreadyCut: Buff切除这部分肢体的效果已生效
        /// </summary>
        public CuttingData(bool willCut)
        {
            cutPlayerNumber = new List<int>();
            this.willCut = willCut;
        }
    }

    /// <summary>
    /// 可能切割玩家部位的Buff继承这个接口
    /// </summary>
    public interface ICutPlayerBodyPartBuff
    {        
        CutPlayerBodyPartData CutPlayerBodyPartData { get; }

        //手动在继承接口的Buff类的ctor里面调用，用来重置CuttingData（一般是把alreadyCut设为false
        void ResetCuttingData();
    }

    public partial class PlayerCuttingBuffManager
    {
        public static Dictionary<BuffID, CutPlayerBodyPartData> playerCuttingBuffs = new Dictionary<BuffID, CutPlayerBodyPartData>();
        /// <summary>
        /// 注册切割玩家部位的Buff信息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="cutPlayerBodyPartData"></param>
        public static void RegisterPlayerCuttingBuff(BuffID id, CutPlayerBodyPartData cutPlayerBodyPartData)
        {
            if (!playerCuttingBuffs.ContainsKey(id))
            {
                playerCuttingBuffs.Add(id, cutPlayerBodyPartData);
            }
        }
    }
}
