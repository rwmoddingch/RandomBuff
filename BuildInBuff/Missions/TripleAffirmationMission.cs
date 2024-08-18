using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative;
using BuiltinBuffs.Positive;
using HarmonyLib;
using HotDogBuff.Negative;
using MonoMod.Cil;
using MoreSlugcats;
using Newtonsoft.Json;
using RandomBuff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Game.Settings.Missions;
using RandomBuffUtils;
using UnityEngine;

namespace BuiltinBuffs.Missions
{
    internal class TripleAffirmationMission : Mission, IMissionEntry
    {

        public static readonly MissionID TripleAffirmation = new MissionID(nameof(TripleAffirmation), true);
        public override MissionID ID => TripleAffirmation;
        public override SlugcatStats.Name BindSlug => MoreSlugcatsEnums.SlugcatStatsName.Saint;
        public override Color TextCol => new Color(0.66667f, 0.9451f, 0.33725f);
        public override string MissionName => BuffResourceString.Get("Mission_Display_TripleAffirmation");

        public TripleAffirmationMission()
        {
            gameSetting = new GameSetting(BindSlug)
            {
                conditions = new List<Condition>()
                {
                    new AscendCondition(){targetRegionCount = 4},
                    new AscendOracleCondition(),
                    new AbyssCondition()
                },
            };
            startBuffSet.AddRange(new []
            {
                HellIBuffEntry.hellBuffID,
                LavaImmuneBuffEntry.LavaImmune,
                ByeByeWeaponBuffEntry.ByeByeWeaponID
            });
        }

        public void RegisterMission()
        {
            BuffRegister.RegisterCondition<AscendCondition>(AscendCondition.Ascend, "Ascend", true);
            BuffRegister.RegisterCondition<AscendOracleCondition>(AscendOracleCondition.AscendOracle, "AscendOracle", true);
            BuffRegister.RegisterCondition<AbyssCondition>(AbyssCondition.Abyss, "Abyss", true);
  

            MissionRegister.RegisterMission(TripleAffirmation,new TripleAffirmationMission());
        }
    }


 
    public class AscendOracleCondition : Condition
    {

        public static readonly ConditionID AscendOracle = new ConditionID(nameof(AscendOracle), true);
        public override ConditionID ID => AscendOracle;
        public override int Exp => 200;

        [JsonProperty] 
        private bool ascendFp;

        [JsonProperty] 
        private bool ascendMoon;


        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            if (ascendFp != game.GetStorySession.saveState.deathPersistentSaveData.ripPebbles ||
                ascendMoon != game.GetStorySession.saveState.deathPersistentSaveData.ripMoon)
            {
                ascendFp = game.GetStorySession.saveState.deathPersistentSaveData.ripPebbles;
                ascendMoon = game.GetStorySession.saveState.deathPersistentSaveData.ripMoon;
                Finished = ascendMoon && ascendFp;
                onLabelRefresh?.Invoke(this);
            }
           
        }


        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (Finished || (!ascendMoon && !ascendFp))
                return "";
            else
                return $"({(ascendMoon ? "Moon" : "")}{(ascendFp ? "FP" : "")})";
        }

        public override string DisplayName(InGameTranslator translator)
        {
            return BuffResourceString.Get("DisplayName_AscendOracle");
        }
    }

    public class AscendCondition : Condition
    {

        public static readonly ConditionID Ascend = new ConditionID(nameof(Ascend), true);
        public override ConditionID ID => Ascend;


        public override int Exp => targetRegionCount * 100;


        [JsonProperty]
        private Dictionary<string,int> ascendCreatures = new Dictionary<string,int>();

        [JsonProperty]
        private HashSet<string> finishedRegions = new HashSet<string>();

        [JsonProperty] 
        public int targetRegionCount;

        private bool isAscended;

        private bool IsIgnoreCreature(CreatureTemplate.Type type)
        {
            if (type == CreatureTemplate.Type.SeaLeech ||
                type == CreatureTemplate.Type.Leech ||
                type == CreatureTemplate.Type.Fly ||
                type == CreatureTemplate.Type.Spider)
                return true;
            return false;
        }

        private int GetCreatureSpawnCount(SaveState state, World world)
        {
            return world.spawners.OfType<World.SimpleSpawner>().Count(i => !IsIgnoreCreature(i.creatureType))
                + world.lineages.Count(i => !IsIgnoreCreature(i.CurrentType(state)));
        }

        public override void EnterGame(RainWorldGame game)
        {
            base.EnterGame(game);
            On.SaveState.AddCreatureToRespawn += SaveState_AddCreatureToRespawn;
            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint1;
        }

        private void Player_ClassMechanicsSaint1(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {
            orig(self);
            isAscended = false;
        }

        private void Player_ClassMechanicsSaint(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(i => i.MatchCallOrCallvirt<Creature>("Die"));
            c.EmitDelegate<Action>(() => isAscended = true);
        }


        private void SaveState_AddCreatureToRespawn(On.SaveState.orig_AddCreatureToRespawn orig, SaveState self, AbstractCreature critter)
        {
            if (!isAscended)
                orig(self, critter);
            else
            {
                if(!ascendCreatures.ContainsKey(critter.world.name))  ascendCreatures.Add(critter.world.name,0);
                ++ascendCreatures[critter.world.name];

            
                if (!finishedRegions.Contains(critter.world.game.world.name) &&
                    ascendCreatures.GetValueSafe(critter.world.game.world.name) == 
                    GetCreatureSpawnCount(critter.world.game.GetStorySession.saveState, critter.world.game.world))
                {
                    finishedRegions.Add(critter.world.game.world.name);
                    if (finishedRegions.Count == targetRegionCount)
                        Finished = true;
                }
                onLabelRefresh?.Invoke(this);
                isAscended = false;
            }

        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                var targetCount = (GetCreatureSpawnCount(game.GetStorySession.saveState, game.world));
                if(finishedRegions.Contains(game.world.name))
                    return $"({finishedRegions.Count}/{targetRegionCount}) ({targetCount}/{targetCount})";
                return $"({finishedRegions.Count}/{targetRegionCount}) ({ascendCreatures.GetValueSafe(game.world.name)}/{targetCount})";
            }

            return $"({finishedRegions.Count}/{targetRegionCount})";

        }

        public override void SessionEnd(SaveState save)
        {
            base.SessionEnd(save);
            On.SaveState.AddCreatureToRespawn -= SaveState_AddCreatureToRespawn;
            IL.Player.ClassMechanicsSaint -= Player_ClassMechanicsSaint;
            On.Player.ClassMechanicsSaint -= Player_ClassMechanicsSaint1;

        }

        public override string DisplayName(InGameTranslator translator)
        {
            return string.Format(BuffResourceString.Get("DisplayName_Ascend"),targetRegionCount);
        }
    }

    public class AbyssCondition : Condition
    {
        public static readonly ConditionID Abyss = new ConditionID(nameof(Abyss), true);


        [JsonProperty] 
        private bool isHidden = true;


        public override ConditionID ID => Abyss;

        public override int Exp => 50;

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }


        public override void HookOn()
        {
            base.HookOn();
            On.Room.Loaded += Room_Loaded;
        }

        private void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            if (self.roomSettings.DangerType == RoomRain.DangerType.AerieBlizzard ||
                self.roomSettings.DangerType == MoreSlugcatsEnums.RoomRainDangerType.Blizzard)
                self.roomSettings.DangerType = RoomRain.DangerType.Flood;
            if (self.world.rainCycle.CycleProgression >= 0.99f)
                self.water = true;

            self.roomSettings.effects.RemoveAll(i => i.type == RoomSettings.RoomEffect.Type.HeatWave
                                                     || i.type == RoomSettings.RoomEffect.Type.FireFlies
                                                     || i.type == RoomSettings.RoomEffect.Type.FairyParticles
                                                     || i.type == RoomSettings.RoomEffect.Type.Coldness);
            
            if(self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.VoidMelt))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.VoidMelt, 0.2f,
                false));
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.Bloom))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.Bloom, 0.2f,
                    false));

            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.DarkenLights))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.DarkenLights, 0.2f,
                    false));
            self.roomSettings.placedObjects.RemoveAll(i => i.type == PlacedObject.Type.FairyParticleSettings);

            //var setting = new PlacedObject(PlacedObject.Type.FairyParticleSettings, null);
            //setting.data = new PlacedObject.FairyParticleData(setting)
            //{
            //    colorHmax = 0.14f,
            //    colorSmax = 1,
            //    colorLmax = 0.52f,
            //    colorHmin = 0.19f,
            //    colorSmin = 1f,
            //    colorLmin = 0.43f,
            //    dirLerpType = FairyParticle.LerpMethod.SIN_IO,
            //    dirDevMax = 19,
            //    dirDevMin = 0,
            //    dirMax = 23,
            //    dirMin = 0,
            //    scaleMax = 2,
            //    scaleMin = 1,
            //    glowStrength = 100,
            //    glowRad = 8,
            //    speedLerpType = FairyParticle.LerpMethod.SIN_IO,
            //    numKeyframes = 9,
            //    alphaTrans = 0.27f,
            //    spriteType = PlacedObject.FairyParticleData.SpriteType.Leaf,
            //    absPulse = true,
            //    pulseRate = 0,
            //    pulseMax = 37,
            //    pulseMin = 7,
            //    interpTrans = 0.7f,
            //    interpDurMax = 180,
            //    interpDurMin = 60,
            //    interpDistMax = 100,
            //    interpDistMin = 40,
                
            //};
            //self.roomSettings.placedObjects.Add(setting);
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.HeatWave))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.HeatWave, 0.3f,
                false));
            //if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.FairyParticles))
            //    self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.FairyParticles, 0.13f,
            //false));
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.FireFlies))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.FireFlies, 1f,
                false));
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.LethalWater))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LethalWater, 1f,
                    false));
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.LightBurn))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.LightBurn, 0.4f,
                    false));
            if (self.roomSettings.effects.All(i => i.type != RoomSettings.RoomEffect.Type.WaterViscosity))
                self.roomSettings.effects.Add(new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.WaterViscosity, 0.2f,
                    false));
            orig(self);
            if (self.abstractRoom.name == "SB_E05SAINT")
                self.AddObject(new MissionEnding(self));

       
        }

    

        public override void InGameUpdate(RainWorldGame game)
        {
            base.InGameUpdate(game);
            if (isHidden && BuffPoolManager.Instance.GameSetting.conditions.All(i => i.Finished || i is AbyssCondition))
            {
                isHidden = false;
                onLabelRefresh?.Invoke(this);
                foreach (var room in game.world.activeRooms)
                {
                    if(room.abstractRoom.name == "SB_E05SAINT")
                        room.AddObject(new MissionEnding(room));
                }
            }
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return "";
        }


        public override string DisplayName(InGameTranslator translator)
        {
            if (isHidden)
                return "? ? ?";
            return BuffResourceString.Get("DisplayName_Abyss");
        }

        public void Finish()
        {
            Finished = true;
        }
    }


    public class MissionEnding : UpdatableAndDeletable
    {
        public MissionEnding(Room room)
        {
            this.room = room;
            for (int i = 0; i < room.roomSettings.effects.Count; i++)
            {
                if (room.roomSettings.effects[i].type == RoomSettings.RoomEffect.Type.VoidSpawn)
                {
                    this.StoredEffect = room.roomSettings.effects[i];
                    return;
                }
            }
            this.clearedSpawn = false;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            for (int i = 0; i < this.room.game.Players.Count; i++)
            {
                if (this.room.game.Players[i].realizedCreature != null && (this.room.game.Players[i].realizedCreature as Player).room == this.room)
                {
                    Player player = this.room.game.Players[i].realizedCreature as Player;
                    player.allowOutOfBounds = true;
                    if (player.mainBodyChunk.pos.x < -248f)
                    {
                        player.SuperHardSetPosition(new Vector2(this.room.RoomRect.right + 232f, player.mainBodyChunk.pos.y));
                    }
                    if (player.mainBodyChunk.pos.x > this.room.RoomRect.right + 248f)
                    {
                        player.SuperHardSetPosition(new Vector2(-232f, player.mainBodyChunk.pos.y));
                    }
                    if (player.KarmaCap >= 9)
                    {
                        if (this.room.game.cameras[0].paletteBlend != this.target_blend)
                        {
                            if (Mathf.Abs(this.room.game.cameras[0].paletteBlend - this.target_blend) < 0.01f)
                            {
                                this.room.game.cameras[0].ChangeFadePalette(this.room.game.cameras[0].paletteB, this.target_blend);
                            }
                            else
                            {
                                this.room.game.cameras[0].ChangeFadePalette(this.room.game.cameras[0].paletteB, Mathf.Lerp(this.room.game.cameras[0].paletteBlend, this.target_blend, 0.1f));
                            }
                        }
                        if (player.mainBodyChunk.pos.y < -118f)
                        {
                            this.target_blend = Mathf.Clamp(this.target_blend + 0.1f, 0f, 1f);
                            this.room.game.cameras[0].ChangeFadePalette(this.room.game.cameras[0].paletteB, Mathf.Clamp(this.room.game.cameras[0].paletteBlend + 0.05f, 0f, 1f));
                            player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, 742f));
                            player.Stun(300);
                            this.ClearAllVoidSpawn();
                            this.StoredEffect.amount = 0f;
                            for (int j = 0; j < player.bodyChunks.Length; j++)
                            {
                                player.bodyChunks[j].vel.y = Mathf.Clamp(player.bodyChunks[j].vel.y, -15f, 15f);
                            }
                        }
                        if (player.mainBodyChunk.pos.y > 768f && this.target_blend > 0f)
                        {
                            if (this.target_blend < 1f)
                            {
                                this.target_blend = Mathf.Clamp(this.target_blend - 0.1f, 0f, 1f);
                            }
                            player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, -102f));
                            player.Stun(300);
                            this.ClearAllVoidSpawn();
                            this.StoredEffect.amount = 0f;
                        }
                        if (this.target_blend >= 0.9f && this.blackFade == null)
                        {
                            player.hideGodPips = true;
                        }
                        if (this.target_blend == 1f && this.fadeObj == null)
                        {
                            this.fadeObj = new FadeOut(this.room, Color.white, 130f, false);
                            this.room.AddObject(this.fadeObj);
                        }
                        if (this.fadeObj != null && this.fadeObj.IsDoneFading() && this.blackFade == null)
                        {
                            this.karmaSymbolWait++;
                        }
                        if (this.karmaSymbolWait > 40 && this.blackFade == null)
                        {
                            if (this.karmaObj == null)
                            {
                                this.karmaObj = new KarmaVectorX(this.room.game.cameras[0].pos + this.room.game.cameras[0].sSize / 2f, 600f, 20f, 1f);
                                this.karmaObj.segments = new Vector2[48, 3];
                                this.karmaObj.color = new Color(1f, 1f, 1f);
                                this.karmaObj.container = "HUD";
                                this.room.AddObject(this.karmaObj);
                            }
                            this.phaseTimer++;
                            float num = Math.Min(1f, (float)this.phaseTimer / 130f);
                            this.karmaObj.color = new Color(1f - num, 1f - num, 1f - num);
                        }
                        if (this.fadeObj != null && this.blackFade == null && this.phaseTimer > 130)
                        {
                            this.phaseTimer = 0;
                            this.karmaObj.Destroy();
                            this.karmaObj = new KarmaVectorX(this.room.game.cameras[0].pos + this.room.game.cameras[0].sSize / 2f, 600f, 20f, 1f);
                            this.karmaObj.segments = new Vector2[48, 3];
                            this.karmaObj.color = new Color(0f, 0f, 0f);
                            this.karmaObj.container = "Bloom";
                            this.blackFade = new FadeOut(this.room, Color.black, 130f, false);
                            typeof(BuffPoolManager)
                                .GetMethod("CreateWinGamePackage", BindingFlags.NonPublic | BindingFlags.Instance)
                                .Invoke(BuffPoolManager.Instance, Array.Empty<object>());
                            room.game.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.BuffGameWinScreen, 3f);
                            BuffPoolManager.Instance.GameSetting.conditions.OfType<AbyssCondition>().FirstOrDefault()?.Finish();

                            this.room.AddObject(this.blackFade);
                            this.room.AddObject(this.karmaObj);
                        }
                        if (this.blackFade != null && this.karmaObj != null)
                        {
                            this.karmaObj.color = new Color(this.blackFade.fade, this.blackFade.fade, this.blackFade.fade);
                        }
                        if (this.blackFade != null && this.blackFade.IsDoneFading())
                        {
                            this.phaseTimer++;
                            this.karmaObj.alpha = Mathf.Max(0f, 1f - (float)this.phaseTimer / 20f);
                        }
                        if (this.blackFade != null && this.blackFade.IsDoneFading() && this.phaseTimer > 20 && !this.loadStarted)
                        {
                            this.loadStarted = true;
                            this.room.world.game.globalRain.ResetRain();
                            return;
                        }
                    }
                    else
                    {
                        if (player.mainBodyChunk.pos.y > this.room.RoomRect.top + 48f)
                        {
                            player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, 798f));
                        }
                        if (player.mainBodyChunk.pos.y < 782f)
                        {
                            if (player.mainBodyChunk.pos.x > 300f && player.mainBodyChunk.pos.x < 620f)
                            {
                                if (player.mainBodyChunk.pos.x < 460f)
                                {
                                    player.SuperHardSetPosition(new Vector2(300f, this.room.RoomRect.top + 32f));
                                }
                                else
                                {
                                    player.SuperHardSetPosition(new Vector2(620f, this.room.RoomRect.top + 32f));
                                }
                            }
                            else
                            {
                                player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, this.room.RoomRect.top + 32f));
                            }
                            for (int k = 0; k < player.bodyChunks.Length; k++)
                            {
                                player.bodyChunks[k].vel.y = Mathf.Clamp(player.bodyChunks[k].vel.y, -15f, 15f);
                            }
                        }
                    }
                }
            }
        }

        private void ClearAllVoidSpawn()
        {
            if (this.clearedSpawn)
            {
                return;
            }
            this.clearedSpawn = true;
            for (int i = 0; i < this.room.updateList.Count; i++)
            {
                if (this.room.updateList[i] is VoidSpawn)
                {
                    this.room.updateList[i].slatedForDeletetion = true;
                }
            }
        }

        public float target_blend;

        public bool loadStarted;

        public FadeOut fadeObj;

        private RoomSettings.RoomEffect StoredEffect;

        private bool clearedSpawn;

        public KarmaVectorX karmaObj;

        public FadeOut blackFade;

        public int phaseTimer;

        public int karmaSymbolWait;
    }
}
