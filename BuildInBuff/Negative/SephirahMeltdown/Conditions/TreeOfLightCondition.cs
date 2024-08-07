using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MoreSlugcats;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class TreeOfLightCondition : Condition
    {
        public static event Action<int> OnMoveToNextPart; 


        public static readonly ConditionID TreeOfLight = new ConditionID(nameof(TreeOfLight), true);
        public override ConditionID ID => TreeOfLight;
        public override int Exp => 150;
        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }

        public Condition SetTargetCount(SlugcatStats state)
        {
            targetCount = (state.name == MoreSlugcatsEnums.SlugcatStatsName.Saint ? 3 : 5) * state.foodToHibernate;
            return this;
        }


        public override string DisplayProgress(InGameTranslator translator)
        {
            if (lastTot == targetCount && !Finished)
            {
                Finished = true;
                if (BuffCustom.TryGetGame(out var game) && Custom.rainWorld.BuffMode())
                    game.cameras[0].room.AddObject(new EndEffect(game.cameras[0].room));
                
            }

            return $"({lastTot}/{targetCount})";
        }


        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            if (game.AlivePlayers.Count != 0 && game.Players[0].state is PlayerState state)
            {
                if (eatCount + game.session.characterStats.foodToHibernate < targetCount && state.foodInStomach > 0)
                {
                    eatCount += state.foodInStomach;
                    AyinBuff.forceEnableSub = true;
                        if (game.AlivePlayers[0].realizedCreature is Player player)
                            player.SubtractFood(state.foodInStomach);
                    AyinBuff.forceEnableSub = false;
                }

                if (lastTot != eatCount + state.foodInStomach)
                {
                    if (Mathf.RoundToInt(lastTot / (targetCount / 10f)) !=
                        Mathf.RoundToInt((eatCount + state.foodInStomach) / (targetCount / 10f)))
                    {
                        OnMoveToNextPart?.Invoke(Mathf.RoundToInt((eatCount + state.foodInStomach) / (targetCount / 10f)));
                    }
                    lastTot = eatCount + state.foodInStomach;
                    onLabelRefresh?.Invoke(this);
                }
            }

            
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_TreeOfLight");
        }

        private int eatCount;
        private int lastTot = 0;

        [JsonProperty]
        public int targetCount;
    }

   


    internal class EndEffect : CosmeticSprite
    {
        public EndEffect(Room room)
        {
            this.room = room;
            foreach (var ply in room.game.Players.Select(i => i.realizedCreature as Player))
                if (ply != null)
                    ply.controller = new Player.NullController();

        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White")
            {
                anchorX = 0, anchorY = 0, width = Custom.rainWorld.screenSize.x, height = Custom.rainWorld.screenSize.y, alpha = 0
            };
            AddToContainer(sLeaser,rCam,rCam.ReturnFContainer("HUD2"));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].alpha = Mathf.Pow(Mathf.InverseLerp(20, 5 * 40 + 20, counter + timeStacker), 2);
        }

        private int counter;

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter++;

            if (counter == 5 * 40)
            {
                AyinPost.Instance.toColor = Color.clear;
                room.game.rainWorld.processManager.musicPlayer.GameRequestsSongStop(new StopMusicEvent()
                {
                    fadeOutTime = 4f,
                    prio = 100,
                    songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-1",
                    type = StopMusicEvent.Type.AllSongs
                });
            }
     
            if (counter == 6 * 40)
            {

                typeof(BuffPoolManager)
                    .GetMethod("CreateWinGamePackage", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(BuffPoolManager.Instance, Array.Empty<object>());
                room.game.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.BuffGameWinScreen, 3f);

            }
        

        }
    }

}
