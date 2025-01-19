using Mono.Cecil.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using JetBrains.Annotations;
using RandomBuff.Core.SaveData;
using MonoMod.Utils;
using RWCustom;
using UnityEngine;
using System.Runtime.InteropServices.ComTypes;
using Modding.Expedition;
using MoreSlugcats;
using RandomBuff;
using RandomBuff.Core.Option;
using System.Security.Policy;
using System.Runtime.Serialization;
using System.Security.Permissions;

#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618
namespace BuiltinBuffs.Expeditions
{
    public class ExpeditionExtend : IBuffEntry
    {

        #region Option

        private static readonly HashSet<string> UseLess = new HashSet<string>()
        { "bur-doomed", "unl-passage", "unl-lantern", "unl-bomb", "unl-electric", };

        private static readonly Dictionary<string, List<SlugcatStats.Name>> BuiltInPerks = new Dictionary<string, List<SlugcatStats.Name>>()
        {
            {
                "unl-explosionimmunity",
                new List<SlugcatStats.Name>() { MoreSlugcatsEnums.SlugcatStatsName.Artificer }
            },
            { "unl-explosivejump", new List<SlugcatStats.Name>() { MoreSlugcatsEnums.SlugcatStatsName.Artificer } },
            { "unl-crafting", new List<SlugcatStats.Name>() { MoreSlugcatsEnums.SlugcatStatsName.Artificer } },
            { "unl-backspear", new List<SlugcatStats.Name>() { SlugcatStats.Name.Red } },
            { "unl-dualwield", new List<SlugcatStats.Name>() { MoreSlugcatsEnums.SlugcatStatsName.Spear } },
            { "unl-agility", new List<SlugcatStats.Name>() { MoreSlugcatsEnums.SlugcatStatsName.Rivulet } },
        };


        private static readonly HashSet<string> Triggerable = new HashSet<string>()
        { "unl-gun", "unl-vulture", "unl-sing", "unl-karma" };


        private static readonly Dictionary<string, AbstractPhysicalObject.AbstractObjectType> SimplySpawn = new Dictionary<string, AbstractPhysicalObject.AbstractObjectType>()
        {
            {"unl-gun" , MoreSlugcatsEnums.AbstractObjectType.JokeRifle},
            {"unl-vulture", AbstractPhysicalObject.AbstractObjectType.VultureMask},
            {"unl-sing" , MoreSlugcatsEnums.AbstractObjectType.SingularityBomb},
        };

        private static readonly Dictionary<string, int> Stackable = new Dictionary<string, int>()
        {
            { "unl-agility", 2 },
            { "unl-explosionimmunity", 3 }
        };

        private static readonly Dictionary<string, Type> HookTypes = new Dictionary<string, Type>()
        {
            { "unl-explosionimmunity", typeof(ExplosionImmunityHook)} ,
            { "unl-agility", typeof(AgilityHook) }
        };

        private static bool OnlyEnglish(string str)
        {
            return str.ToLower().All(i =>
                (i >= 'a' && i <= 'z') || (i >= '0' && i <= '9') || i == '.' || i == ',');
        }

        public static bool IsUselessID(string str)
        {
            return UseLess.Contains(str);
        }

        public static bool IsTriggerable(string str)
        {
            return Triggerable.Contains(str);
        }

        public static int IsStackable(string str)
        {
            if (Stackable.TryGetValue(str, out var value))
                return value;
            return -1;
        }

        public static Type GetHookType(string str)
        {
            if(HookTypes.TryGetValue(str,out var type))
                return type;
            return null;
        }

        public static List<SlugcatStats.Name> GetConflictName(string id)
        {
            if (CustomPerks.PerkForID(id) is CustomPerk perk)
            {
                var list = new List<SlugcatStats.Name>();
                foreach (var name in SlugcatStats.Name.values.entries.Select(i => new SlugcatStats.Name(i)))
                    if (!perk.AvailableForSlugcat(name))
                        list.Add(name);
                return list;
            }
            else if (BuiltInPerks.TryGetValue(id, out var list))
                return list;
            return new List<SlugcatStats.Name>();
        }

        public static AbstractPhysicalObject.AbstractObjectType GetSimplySpawn(string id)
        {
            if (SimplySpawn.TryGetValue(id, out var type))
                return type;
            return null;
        }

        #endregion

        public void OnEnable()
        {
            BuffPlugin.Log($"Current Expedition Extend state: Mod:{BuffOptionInterface.Instance.EnableExpeditionModExtend.Value}");
            try
            {
                if(!Futile.atlasManager.DoesContainAtlas(PositiveTexture))
                    Futile.atlasManager.LoadImage(PositiveTexture);

                if (!Futile.atlasManager.DoesContainAtlas(NegativeTexture))
                    Futile.atlasManager.LoadImage(NegativeTexture);

                ExpeditionProgression.SetupPerkGroups();
                ExpeditionProgression.SetupBurdenGroups();

                InitExpeditionType();
                dynamicAssembly = BuffBuilder.FinishGenerate(typeof(ExpeditionExtend).Assembly.GetName().Name).First();

                ExpeditionCoreHooks.OnModsInit();
                BuffCore.AfterBuffReloaded += BuffCoreOnAfterBuffReloaded;

            }
            catch (Exception e)
            {
                BuffUtils.LogException("BuffExtend", e);
            }
        }

        private void BuffCoreOnAfterBuffReloaded(BuffPluginInfo[] enabledplugins)
        {
            RegisterExpeditionType(dynamicAssembly);
            dynamicAssembly = null;
            BuffCore.AfterBuffReloaded -= BuffCoreOnAfterBuffReloaded;
        }

        private Assembly dynamicAssembly;
        private const string NegativeTexture = "buffinfos/ExpeditionExtend/expedition/expeditionNegative";
        private const string PositiveTexture = "buffinfos/ExpeditionExtend/expedition/expeditionPositive";






        private static void InitExpeditionType()
        {
            foreach (var group in ExpeditionProgression.perkGroups)
            {
                if (!BuffOptionInterface.Instance.EnableExpeditionModExtend.Value && group.Key != "expedition" &&
                    group.Key != "moreslugcats")
                    continue;
                
                foreach (var id in group.Value)
                {
                  
                    if (IsUselessID(id)) continue;
                    var re = BuffBuilder.GenerateBuffType(typeof(ExpeditionExtend).Assembly.GetName().Name, id,
                        true, (il) => BuildILBuffCtor(il, id));
                    re.buffType.DefineMethodOverride("Destroy", typeof(void), Type.EmptyTypes,
                        Mono.Cecil.MethodAttributes.Public, (il) => BuildILDestroy(il, id));
                    if (Triggerable.Contains(id))
                    {
                        re.buffType.DefineMethodOverride("Trigger", typeof(bool),new [] {typeof(RainWorldGame)},
                            Mono.Cecil.MethodAttributes.Public, (il) => BuildILTrigger(il, id));
                    }
                }
            }

            foreach (var group in ExpeditionProgression.burdenGroups)
            {
                if (!BuffOptionInterface.Instance.EnableExpeditionModExtend.Value && group.Key != "expedition" &&
                    group.Key != "moreslugcats")
                    continue;
                foreach (var id in group.Value)
                {
                    if (IsUselessID(id)) continue;
                    var re = BuffBuilder.GenerateBuffType(typeof(ExpeditionExtend).Assembly.GetName().Name, id,
                   true, (il) => BuildILBuffCtor(il, id));
                    re.buffType.DefineMethodOverride("Destroy", typeof(void), Type.EmptyTypes,
                        Mono.Cecil.MethodAttributes.Public, (il) => BuildILDestroy(il, id));
                    if (Triggerable.Contains(id))
                    {
                        re.buffType.DefineMethodOverride("Trigger", typeof(bool), new[] { typeof(RainWorldGame) },
                            Mono.Cecil.MethodAttributes.Public, (il) => BuildILTrigger(il, id));
                    }
                }   
            }
        }

        private static void SetProperty(BuffStaticData data, string name, object value)
        {
            typeof(BuffStaticData).GetProperty(name).SetMethod.Invoke(data, new[] { value });
        }

        private static void RegisterExpeditionType(Assembly ass)
        {
            var ctor = typeof(BuffStaticData).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic).First(i => i.GetParameters().Length == 0);


            foreach (var group in ExpeditionProgression.perkGroups)
            {
                if (!BuffOptionInterface.Instance.EnableExpeditionModExtend.Value && group.Key != "expedition" &&
                    group.Key != "moreslugcats")
                    continue;
                foreach (var id in group.Value)
                {
                    if (IsUselessID(id)) continue;
                    var staticData = (BuffStaticData)ctor.Invoke(Array.Empty<object>());
                    SetProperty(staticData, "BuffID", new BuffID(id));
                    SetProperty(staticData, "BuffType", BuffType.Positive);
                    SetProperty(staticData, "FaceName", PositiveTexture);
                    SetProperty(staticData, "Color", Custom.hexToColor("2EFFFF"));

                    SetProperty(staticData, "Triggerable", IsTriggerable(id));
                    SetProperty(staticData, "Stackable", IsStackable(id) != -1);

                    if (IsStackable(id) != -1)
                        SetProperty(staticData, "MaxStackLayers", IsStackable(id));

                    SetProperty(staticData, "MultiLayerFace", true);
                    SetProperty(staticData, "FaceLayer", 3);
                    SetProperty(staticData, "MaxFaceDepth", 1.0f);
                    SetProperty(staticData, "FaceBackgroundColor", Custom.hexToColor("020B0B"));

                    var conflict = GetConflictName(id);
                    SetProperty(staticData, "Conflict", conflict.Select(i => i.value).ToHashSet());

                    staticData.CardInfos.Add(Custom.rainWorld.inGameTranslator.currentLanguage, new BuffStaticData.CardInfo()
                    {
                        BuffName = ForceUnlockedAndLoad(ExpeditionProgression.UnlockName, id),
                        Description = (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
                        $"(来自:{Custom.rainWorld.inGameTranslator.Translate(ModManager.ActiveMods.FirstOrDefault(i => i.id == group.Key)?.name ?? group.Key)})\n" :
                        $"(From:{Custom.rainWorld.inGameTranslator.Translate(ModManager.ActiveMods.FirstOrDefault(i => i.id == group.Key)?.name ?? group.Key)})\n") +
                        ForceUnlockedAndLoad(ExpeditionProgression.UnlockDescription, id),
                    });
                    BuffRegister.InternalRegisterBuff(staticData.BuffID, ass.GetType($"{typeof(ExpeditionExtend).Assembly.GetName().Name}.{id}Buff", true),
                        ass.GetType($"{typeof(ExpeditionExtend).Assembly.GetName().Name}.{id}BuffData"), GetHookType(id), typeof(ExpeditionExtend).Assembly.GetName());
                    BuffRegister.RegisterStaticData(staticData);

                }
            }
            foreach (var group in ExpeditionProgression.burdenGroups)
            {
                if (!BuffOptionInterface.Instance.EnableExpeditionModExtend.Value && group.Key != "expedition" &&
                    group.Key != "moreslugcats")
                    continue;
                foreach (var id in group.Value)
                {
                    if (IsUselessID(id)) continue;
                    var staticData = (BuffStaticData)ctor.Invoke(Array.Empty<object>());
                    SetProperty(staticData, "BuffID", new BuffID(id));
                    SetProperty(staticData, "BuffType", BuffType.Negative);
                    SetProperty(staticData, "FaceName", NegativeTexture);
                    SetProperty(staticData, "Color", Custom.hexToColor("FF462E"));

                    SetProperty(staticData, "Triggerable", IsTriggerable(id));
                    SetProperty(staticData, "Stackable", IsStackable(id) != -1);

                    if(IsStackable(id) != -1)
                        SetProperty(staticData, "MaxStackLayers", IsStackable(id));


                    SetProperty(staticData, "MultiLayerFace", true);
                    SetProperty(staticData, "FaceLayer", 3);
                    SetProperty(staticData, "MaxFaceDepth", 1.0f);
                    SetProperty(staticData, "FaceBackgroundColor", Custom.hexToColor("0B0302"));


                    var conflict = GetConflictName(id);
                    SetProperty(staticData, "Conflict", conflict.Select(i => i.value).ToHashSet());

                    var name = ForceUnlockedAndLoad(ExpeditionProgression.BurdenName, id);
                    staticData.CardInfos.Add(OnlyEnglish(name) ? InGameTranslator.LanguageID.English :
                        Custom.rainWorld.inGameTranslator.currentLanguage, new BuffStaticData.CardInfo()
                        {
                            BuffName = name,
                            Description = (Custom.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Chinese ?
                                              $"(来自:{Custom.rainWorld.inGameTranslator.Translate(ModManager.ActiveMods.FirstOrDefault(i => i.id == group.Key)?.name ?? group.Key)})\n" :
                                              $"(From:{Custom.rainWorld.inGameTranslator.Translate(ModManager.ActiveMods.FirstOrDefault(i => i.id == group.Key)?.name ?? group.Key)})\n")
                            + ForceUnlockedAndLoad(ExpeditionProgression.BurdenManualDescription, id),
                        });
                    BuffRegister.InternalRegisterBuff(staticData.BuffID, ass.GetType($"{typeof(ExpeditionExtend).Assembly.GetName().Name}.{id}Buff", true),
                        ass.GetType($"{typeof(ExpeditionExtend).Assembly.GetName().Name}.{id}BuffData"), GetHookType(id), typeof(ExpeditionExtend).Assembly.GetName());
                    BuffRegister.RegisterStaticData(staticData);

                }
            }
        }

 

        private static string ForceUnlockedAndLoad(Func<string, string> orig, string key)
        {
            if (ExpeditionData.unlockables == null)
                ExpeditionData.unlockables = new List<string>();
            bool contains = ExpeditionData.unlockables.Contains(key);
            if (!contains) ExpeditionData.unlockables.Add(key);
            var re = orig(key);
            if (!contains) ExpeditionData.unlockables.Remove(key);
            return re;
        }

   


        public static void ExpeditionBuffDestroy(RuntimeBuff buff, string id)
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                switch (id)
                {
                    case "unl-backspear":
                        foreach (var ply in game.Players.Select(i => i.realizedCreature as Player))
                            if (ply != null && ply.slugcatStats.name != SlugcatStats.Name.Red)
                            {
                                ply.spearOnBack.DropSpear();
                                ply.spearOnBack = null;
                            }
                        break;
                    case "bur-pursued":
                        ExpeditionGame.burdenTrackers.RemoveAll(i => i is ExpeditionGame.PursuedTracker);
                        break;
                    case "unl-agility":
                        PlayerUtils.UndoAll(buff);
                        break;
                    case "unl-explosionimmunity":
                        ExplosionImmunityHook.IgnoreStun = false;
                        break;
                }
            }
        }


        public static bool ExpeditionBuffTrigger(RuntimeBuff buff, string id)
        {
            if (GetSimplySpawn(id) is AbstractPhysicalObject.AbstractObjectType type &&
                BuffCustom.TryGetGame(out var game))
            {
                foreach (var ply in game.AlivePlayers)
                {
                    if (ply.realizedCreature is Player player && player.Consious && player.room != null &&
                        !player.inShortcut)
                    {
                        SandboxGameSession sandBox =
                            (SandboxGameSession)FormatterServices.GetUninitializedObject(typeof(SandboxGameSession));
                        sandBox.game = (RainWorldGame)FormatterServices.GetUninitializedObject(typeof(RainWorldGame));

                        sandBox.game.overWorld = (OverWorld)FormatterServices.GetUninitializedObject(typeof(OverWorld));
                        sandBox.game.overWorld.activeWorld =
                            (World)FormatterServices.GetUninitializedObject(typeof(World));
                        sandBox.game.world.abstractRooms = new AbstractRoom[1];
                        sandBox.game.world.abstractRooms[0] =
                            (AbstractRoom)FormatterServices.GetUninitializedObject(typeof(AbstractRoom));
                        sandBox.game.world.abstractRooms[0].entities = new List<AbstractWorldEntity>();
                        sandBox.SpawnItems(
                            new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, type, 0),
                            ply.pos, ply.world.game.GetNewID());
                        var obj = sandBox.game.world.abstractRooms[0].entities.Pop() as AbstractPhysicalObject;
                        obj.world = ply.world;
                        obj.pos = ply.pos;
                        ply.Room.AddEntity(obj);
                        obj.RealizeInRoom();
                        if (player.CanIPickThisUp(obj.realizedObject))
                            player.SlugcatGrab(obj.realizedObject, player.FreeHand());
                        else
                        {
                            foreach (var chunk in obj.realizedObject.bodyChunks)
                                chunk.HardSetPosition(player.DangerPos);
                        }

                        return true;
                    }
                }

                return false;
            }

            if (id == "unl-karma" && BuffCustom.TryGetGame(out var game1))
            {
                if (!game1.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma)
                {
                    game1.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma = true;
                    game1.cameras[0].hud.karmaMeter.reinforceAnimation = 0;
                    return true;
                }

                return false;
            }
            return false;
        }

        public static void ExpeditionBuffCtor(RuntimeBuff buff, string id)
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                switch (id)
                {
                    case "unl-backspear":
                        foreach(var ply in game.Players.Select(i => i.realizedCreature as Player))
                            if (ply != null)
                                ply.spearOnBack = new Player.SpearOnBack(ply);
                        break;
                    case "unl-agility":
                        if (ModManager.CoopAvailable)
                        {
                            foreach (var stat in game.GetStorySession.characterStatsJollyplayer.Where(i => i != null))
                                OverrideForAgility(stat);
                        }
                        OverrideForAgility(game.session.characterStats);
                        break;
                    case "bur-pursued":
                        if (ExpeditionGame.burdenTrackers.All( i => !(i is ExpeditionGame.PursuedTracker)))
                            ExpeditionGame.burdenTrackers.Add(new ExpeditionGame.PursuedTracker(game));
                        break;
                    case "unl-explosionimmunity":
                        ExplosionImmunityHook.IgnoreStun = false;
                        break;
                    case "unl-glow":
                        game.GetStorySession.saveState.theGlow = true;
                        foreach (var ply in game.Players.Select(i => i.realizedCreature as Player))
                            if(ply != null)
                                ply.glowing = true;
                        break;
                }

                void OverrideForAgility(SlugcatStats stat)
                {
                    stat.Modify(PlayerUtils.Max, "lungsFac", 0.15f, buff);
                    stat.Modify(PlayerUtils.Max, "runspeedFac", 1.75f, buff);
                    stat.Modify(PlayerUtils.Max, "poleClimbSpeedFac", 1.8f, buff);
                    stat.Modify(PlayerUtils.Max, "corridorClimbSpeedFac", 1.6f, buff);
                }
            }
        }


        #region Some IL

        public static void AddUnique(List<string> self, string item)
        {
            if (!self.Contains(item))
                self.Add(item);
        }

        private static void BuildILDestroy(ILProcessor il, string item)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(RuntimeBuff).GetMethod(nameof(RuntimeBuff.Destroy), BindingFlags.Public | BindingFlags.Instance));
            il.Emit(OpCodes.Ldsfld, typeof(ExpeditionCoreHooks).GetField(nameof(ExpeditionCoreHooks.activeUnlocks), BindingFlags.Static | BindingFlags.Public));

            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(List<string>).GetMethod(nameof(List<string>.Remove), new[] { typeof(string) }));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(ExpeditionExtend).GetMethod(nameof(ExpeditionExtend.ExpeditionBuffDestroy), new[] { typeof(RuntimeBuff), typeof(string) }));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);

        }
        private static void BuildILTrigger(ILProcessor il, string item)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(ExpeditionExtend).GetMethod(nameof(ExpeditionExtend.ExpeditionBuffTrigger), new[] { typeof(RuntimeBuff), typeof(string) }));
            il.Emit(OpCodes.Ret);

        }
        private static void BuildILBuffCtor(ILProcessor il, string item)
        {
            il.Emit(OpCodes.Ldsfld, typeof(ExpeditionCoreHooks).GetField(nameof(ExpeditionCoreHooks.activeUnlocks), BindingFlags.Static | BindingFlags.Public));
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(ExpeditionExtend).GetMethod(nameof(ExpeditionExtend.AddUnique), new[] { typeof(List<string>), typeof(string) }));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldstr, item);
            il.Emit(OpCodes.Callvirt, typeof(ExpeditionExtend).GetMethod(nameof(ExpeditionExtend.ExpeditionBuffCtor), new[] { typeof(RuntimeBuff), typeof(string) }));

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(RuntimeBuff).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).First());
            il.Emit(OpCodes.Ret);
        }

        #endregion
    }

}
