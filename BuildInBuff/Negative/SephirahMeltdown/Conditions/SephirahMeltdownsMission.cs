using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using HotDogGains.Negative;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown.Conditions
{
    internal class SephirahMeltdownsMission : Mission, IMissionEntry
    {
        public static readonly MissionID SephirahMeltdowns = new MissionID(nameof(SephirahMeltdowns), true);
        public override MissionID ID => SephirahMeltdowns;
        public override SlugcatStats.Name BindSlug { get; }
        public override Color TextCol => Color.white;
        public override string MissionName => BuffResourceString.Get("Mission_Display_Meltdown");


        public SephirahMeltdownsMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new MeltdownHuntCondition(){killCount = 1,minConditionCycle = 4,maxConditionCycle = 8,type = BuffConfigManager.ContainsId(new BuffID("bur-pursued")) ? CreatureTemplate.Type.RedCentipede : CreatureTemplate.Type.KingVulture},
                    new BinahCondition(){minConditionCycle =8, maxConditionCycle = 12},
                    new FixedCycleCondition() {SetCycle = 12},
                    new DeathCondition(){deathCount = 20}
                },
                gachaTemplate = new SephirahMeltdownsTemplate()
                {
                    cardPick = new Dictionary<int, List<string>>()
                    {
                        {4, new List<string>()
                        {
                            TipherethBuffData.Tiphereth.value,
                            ChesedBuffData.Chesed.value,
                            BuffConfigManager.ContainsId(new BuffID("bur-pursued")) ?  "bur-pursued" : ArmedKingVultureBuffEntry.ArmedKingVultureID.value 
                        }},
                        {8, new List<string>()
                        {
                            HokmaBuffData.Hokma.value,
                            BinahBuffData.Binah.value
                        }},
                    }, 
                    NCount = 0, NSelect = 0, NShow = 0,
                    PCount = 0, PSelect = 0, PShow = 0,
                    PocketPackMultiply = 0,
                    ForceStartPos = "CC_S03"
                }
            };
            startBuffSet.Add(MalkuthBuffData.Malkuth);
            startBuffSet.Add(YesodBuffData.Yesod);
            startBuffSet.Add(NetzachBuffData.Netzach);
            startBuffSet.Add(HodBuffData.Hod);
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<MeltdownHuntCondition>(MeltdownHuntCondition.MeltdownHunt, "Meltdown Hunt",true);
            BuffRegister.RegisterCondition<BinahCondition>(BinahCondition.Binah, "Binah", true);
            BuffRegister.RegisterCondition<TreeOfLightCondition>(TreeOfLightCondition.TreeOfLight, "TreeOfLight", true);

            MissionRegister.RegisterMission(SephirahMeltdowns, new SephirahMeltdownsMission());
            BuffRegister.RegisterGachaTemplate<SephirahMeltdownsTemplate>(SephirahMeltdownsTemplate.SephirahMeltdowns);
        }

    }

    internal class SephirahMeltdownsTemplate : MissionGachaTemplate
    {
        public static readonly GachaTemplateID SephirahMeltdowns=
            new GachaTemplateID(nameof(SephirahMeltdowns), true);

        public override GachaTemplateID ID => SephirahMeltdowns;

        public SephirahMeltdownsTemplate()
        {
            TemplateDescription = "GachaTemplate_Desc_SephirahMeltdowns";
            PocketPackMultiply = 0;
            ExpMultiply *= 1.5f;
        }

        public override void EnterGame(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.cycleNumber == 4)
                BuffUtils.Log("SephirahMeltdown", "Add bur-pursued at 4 cycles");
            if (game.GetStorySession.saveState.cycleNumber != 12)
            {
                MusicEvent musicEvent = new MusicEvent
                {
                    fadeInTime = 1f,
                    roomsRange = -1,
                    cyclesRest = 0,
                    volume = 0.13f,
                    prio = 10f,
                    stopAtDeath = true,
                    stopAtGate = false,
                    loop = true,
                    maxThreatLevel = 10,
                    songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/SephirahMissionSong",

                };

                game.rainWorld.processManager.musicPlayer?.GameRequestsSong(musicEvent);
            }

            else
            {
                MusicEvent musicEvent = new MusicEvent
                {
                    fadeInTime = 1f,
                    roomsRange = -1,
                    cyclesRest = 0,
                    volume = 0.13f,
                    prio = 10f,
                    stopAtDeath = true,
                    stopAtGate = false,
                    loop = true,
                    maxThreatLevel = 10,
                    songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-1",

                };
                game.rainWorld.processManager.musicPlayer?.GameRequestsSong(musicEvent);
            }


            On.Music.MusicPlayer.GameRequestsSong += MusicPlayer_GameRequestsSong;
            base.EnterGame(game);
        }

        private void MusicPlayer_GameRequestsSong(On.Music.MusicPlayer.orig_GameRequestsSong orig, Music.MusicPlayer self, MusicEvent musicEvent)
        {
            var ghost = self.threatTracker?.ghostMode ?? -1;
            orig(self,musicEvent);
            if (ghost != -1)
                self.threatTracker.ghostMode = ghost;
            
        }

        public override void SessionEnd(RainWorldGame game)
        {
            if (game.GetStorySession.saveState.cycleNumber == 7)
            {
                if(BuffCore.GetAllBuffIds().Contains(new BuffID("bur-pursued")))
                    BuffPoolManager.Instance.RemoveBuffAndData(new BuffID("bur-pursued"));
                else
                    BuffPoolManager.Instance.RemoveBuffAndData(ArmedKingVultureBuffEntry.ArmedKingVultureID);

                BuffUtils.Log("SephirahMeltdown","Remove bur-pursued at 8 cycles");
            }
            CurrentPacket = (game.GetStorySession.saveState.cycleNumber + 1) % 4 == 0 ?
                new CachaPacket() { negative = (0, 0, 0), positive = (2, 6, 1) } : new CachaPacket();
            if (game.GetStorySession.saveState.cycleNumber == 11 &&
                BuffPoolManager.Instance.GameSetting.conditions.Count(i => !i.Finished) == 1)
            {
                BuffUtils.Log("SephirahMeltdown", "Add Final Test");
                BuffPoolManager.Instance.GameSetting.conditions.RemoveAll(i => i is DeathCondition);
                BuffPoolManager.Instance.GameSetting.conditions.Add(
                    new TreeOfLightCondition().SetTargetCount(game.session.characterStats));
                AyinBuffData.Ayin.CreateNewBuff();
            }
            game.rainWorld.processManager.musicPlayer?.GameRequestsSongStop(new StopMusicEvent()
            {
                fadeOutTime = 0.5f,
                prio = 100,
                songName = $"BUFF_{AyinBuffData.Ayin.GetStaticData().AssetPath}/Ayin-1",
                type = StopMusicEvent.Type.AllSongs
            });
            game.rainWorld.processManager.musicPlayer?.GameRequestsSongStop(new StopMusicEvent()
            {
                fadeOutTime = 0.5f,
                prio = 100,
                songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/Binah-Garion",
                type = StopMusicEvent.Type.AllSongs
            });
            game.rainWorld.processManager.musicPlayer?.GameRequestsSongStop(new StopMusicEvent()
            {
                fadeOutTime = 0.5f,
                prio = 100,
                songName = $"BUFF_{BinahBuffData.Binah.GetStaticData().AssetPath}/SephirahMissionSong",
                type = StopMusicEvent.Type.AllSongs
            });
            On.Music.MusicPlayer.GameRequestsSong -= MusicPlayer_GameRequestsSong;
        }

    }
}
