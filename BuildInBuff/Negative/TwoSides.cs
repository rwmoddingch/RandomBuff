/*
        _            _            _           _            _          
       / /\         /\ \         /\ \        /\ \         /\ \     _  
      / /  \       /  \ \       /  \ \      /  \ \       /  \ \   /\_\
     / / /\ \__   / /\ \ \     / /\ \ \    / /\ \ \     / /\ \ \_/ / /
    / / /\ \___\ / / /\ \_\   / / /\ \_\  / / /\ \ \   / / /\ \___/ / 
    \ \ \ \/___// /_/_ \/_/  / / /_/ / / / / /  \ \_\ / / /  \/____/  
     \ \ \     / /____/\    / / /__\/ / / / /   / / // / /    / / /   
 _    \ \ \   / /\____\/   / / /_____/ / / /   / / // / /    / / /    
/_/\__/ / /  / / /______  / / /\ \ \  / / /___/ / // / /    / / /     
\ \/___/ /  / / /_______\/ / /  \ \ \/ / /____\/ // / /    / / /      
 \_____\/   \/__________/\/_/    \_\/\/_________/ \/_/     \/_/         

*/
using UnityEngine;
using Expedition;
using MoreSlugcats;
using RWCustom;
using RandomBuff;
using RandomBuffUtils;
using Smoke;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HotDogGains.Negative;

namespace BuiltinBuffs.Negative //命名空间在BuiltinBuffs的Negative下

{
    //对于有时长限制卡牌：
    class TwoSidesBuff : Buff<TwoSidesBuff, TwoSidesBuffData> { public override BuffID ID => TwoSidesIBuffEntry.twoSidesBuffID; }
    class TwoSidesBuffData : CountableBuffData
    {
        public override BuffID ID => TwoSidesIBuffEntry.twoSidesBuffID;

        public override int MaxCycleCount => 3;
    }


    internal class TwoSidesIBuffEntry : IBuffEntry
    {
        /*-----------------------------------------------------字段-----------------------------------------------------*/
        //设置BuffID
        public static BuffID twoSidesBuffID = new BuffID("TwoSides", true);

        private static bool throwForward = true;
        /*-----------------------------------------------------挂钩-----------------------------------------------------*/
        public static void HookOn()
        {
            //角色更新-判断投掷并进行转身动作
            On.Player.ThrowObject += Player_Throwturn;
        }
        /*-----------------------------------------------------方法-----------------------------------------------------*/
        //实现判断和自动转身
        private static void Player_Throwturn(On.Player.orig_ThrowObject orig,Player self,int grasp,bool eu)
        {
            //获取房间和位置
            var room = self.room;
            var pos = self.mainBodyChunk.pos;
            Vector2 vel = self.mainBodyChunk.vel;
            //作用和orig()差不多
            orig.Invoke(self, grasp, eu);
            if (throwForward)
            {
                //更新投掷方向
                throwForward = false;
            }
            else
            {
                //直接输入面朝方向的反方向
                self.input[0].x = -(self.flipDirection);
                //更新投掷方向
                throwForward = true;
            }


            /*条件判断，如果玩家“意识清醒”并且处在地面上站立或匍匐或者在杆子上。这部分暂时用不到。
            if (self.Consious)
            {
                bool player_CanThrowTurn =
                       self.bodyMode != Player.BodyModeIndex.Stunned
                    && self.bodyMode != Player.BodyModeIndex.Swimming
                    && self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut
                    && self.bodyMode != Player.BodyModeIndex.CorridorClimb
                    && self.bodyMode != Player.BodyModeIndex.WallClimb
                    &&(self.bodyMode == Player.BodyModeIndex.Stand
                    || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam
                    || self.animation== Player.AnimationIndex.HangFromBeam
                    || self.bodyMode == Player.BodyModeIndex.Crawl);               
                if (player_CanThrowTurn)
                {
                    //*一扭头*的动作
                    self.bodyChunks[1].vel.x += (self.flipDirection * -20);
                    //直接输入面朝方向的反方向
                    self.input[0].x = -(self.flipDirection);
                    //音效，目前为占位符
                    room.PlaySound(SoundID.Snail_Pop, pos);
                        
                }

            }
            */

        }
        public void OnEnable()
        {
            //注册BuffID
            BuffRegister.RegisterBuff<TwoSidesBuff,TwoSidesBuffData,TwoSidesIBuffEntry>(twoSidesBuffID);
        }
    }
}
