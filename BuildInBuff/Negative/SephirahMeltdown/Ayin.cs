using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class AyinBuffData : BuffData
    {
        public static readonly BuffID Ayin = new BuffID("Ayin",true);
        public override BuffID ID => Ayin;

        
    }

    internal class AyinBuff : Buff<AyinBuff, AyinBuffData>
    {
        public override BuffID ID => AyinBuffData.Ayin;

        public float Deg(float timeStacker) => Mathf.Lerp(lastDeg, deg, timeStacker);

        public AyinBuff()
        {
            forceEnableSub = false;
            TreeOfLightCondition.OnMoveToNextPart += TreeOfLightCondition_OnMoveToNextPart;
            if (BuffCustom.TryGetGame(out var game))
                (game.Players[0].state as PlayerState).foodInStomach = 0;
        }

        private void TreeOfLightCondition_OnMoveToNextPart(int obj)
        {
            fromDeg = deg;
            toDeg = Custom.LerpMap(obj, 0, 5, 0, 180);
            moveCounter = 0;
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (moveCounter < 40)
                moveCounter++;
            lastDeg = deg;
            deg = Mathf.Lerp(fromDeg, toDeg, Helper.EaseOutElastic(moveCounter / 40f));
       
        }

        private int moveCounter;

        private float lastDeg;
        private float deg;
        private float fromDeg;
        private float toDeg;


        public override void Destroy()
        {
            base.Destroy();
            TreeOfLightCondition.OnMoveToNextPart -= TreeOfLightCondition_OnMoveToNextPart;
        }

        public static bool forceEnableSub = false;
    }

    internal class AyinHook
    {
        public static void HookOn()
        {
            On.RoomCamera.DrawUpdate += RoomCamera_DrawUpdate;
            On.RegionGate.customKarmaGateRequirements += RegionGate_customKarmaGateRequirements;

            On.RainCycle.Update += RainCycle_Update;
            On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;

            On.Player.SubtractFood += Player_SubtractFood;

            On.RainWorldGame.Update += RainWorldGame_Update;

            counter = 0;
        }

        private static int counter = 0;

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            
            counter++;
            if (counter == 40)
            {
                foreach (var room in self.world.activeRooms)
                    if (room.updateList.All(i => !(i is CreatureEnd)))
                        room.AddObject(new CreatureEnd(room));
                counter = 0;
            }

        }

        private static void Player_SubtractFood(On.Player.orig_SubtractFood orig, Player self, int sub)
        {
            if(AyinBuff.forceEnableSub)
                orig(self, sub);
        }

        private static float CreatureCommunities_LikeOfPlayer(On.CreatureCommunities.orig_LikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber)
        {
            var re = orig(self,commID, region, playerNumber);
            return Mathf.Max(re, 0.75f);
        }

        private static void RainCycle_Update(On.RainCycle.orig_Update orig, RainCycle self)
        {
            orig(self);
            self.timer = 500;
            self.preTimer = 0;
        }

        private static void RegionGate_customKarmaGateRequirements(On.RegionGate.orig_customKarmaGateRequirements orig, RegionGate self)
        {
            orig(self);
            self.karmaRequirements[0] = RegionGate.GateRequirement.OneKarma;
            self.karmaRequirements[1] = RegionGate.GateRequirement.OneKarma;
            self.unlocked = true;

        }

        private static void RoomCamera_DrawUpdate(On.RoomCamera.orig_DrawUpdate orig, RoomCamera self, float timeStacker, float timeSpeed)
        {
            orig(self, timeStacker, timeSpeed);

            var localCenter = (self.followAbstractCreature?.realizedCreature?.DangerPos - self.pos) / Custom.rainWorld.screenSize ??
                              new Vector2(0.5F, 0.5f);
            var rect = Shader.GetGlobalVector(RainWorld.ShadPropSpriteRect);
            var border = Custom.LerpMap(Mathf.Abs(90 - AyinBuff.Instance.Deg(timeStacker)), 90,0, 0.5f, 0.15f);
            var xOffset = Mathf.Max(0, Mathf.Abs(localCenter.x - 0.5f) - border) * Mathf.Sign(localCenter.x - 0.5f);
            for (int i = 0; i < 11; i++)
            {
                self.SpriteLayers[i].rotation = 0;
                self.SpriteLayers[i].SetPosition(0, xOffset * self.sSize.y);
                self.SpriteLayers[i].RotateAroundPointRelative(self.sSize * new Vector2(0.5f,0.5f), AyinBuff.Instance.Deg(timeStacker));
            }


            (rect.x, rect.y, rect.z, rect.w) = (rect.x , rect.y  + xOffset * (rect.w - rect.y), rect.z , rect.w + xOffset * (rect.w - rect.y));
            Shader.SetGlobalVector(RainWorld.ShadPropSpriteRect, rect);

            Shader.SetGlobalFloat("Buff_screenRotate", AyinBuff.Instance.Deg(timeStacker) * Mathf.Deg2Rad);
            if(self.levelGraphic.shader != Custom.rainWorld.Shaders["SephirahMeltdownEntry.LevelColorRotation"])
                self.levelGraphic.shader = Custom.rainWorld.Shaders["SephirahMeltdownEntry.LevelColorRotation"];

        }
    }

    internal class CreatureEnd : UpdatableAndDeletable
    {
        public CreatureEnd(Room room)
        {
            this.room = room;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            foreach (var crit in room.abstractRoom.creatures.Select(i => i.realizedCreature))
            {
                if (crit == null || crit is Player || crit.inShortcut || crit.Template.smallCreature || crit.Template.type == CreatureTemplate.Type.Overseer) continue;
                crit.Stun(200);

            }
        }
    }
}
