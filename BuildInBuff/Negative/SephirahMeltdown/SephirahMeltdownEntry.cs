using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuiltinBuffs.Negative.SephirahMeltdown.Conditions;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.SaveData;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class SephirahMeltdownEntry : IBuffEntry
    {
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ChesedBuff, ChesedBuffData, ChesedBuffHook>(ChesedBuffData.Chesed);
            BuffRegister.RegisterBuff<HodBuff, HodBuffData, HodBuff>(HodBuffData.Hod);
            BuffRegister.RegisterBuff<HokmaBuff, HokmaBuffData, HokmaHook>(HokmaBuffData.Hokma);
            BuffRegister.RegisterBuff<MalkuthBuff, MalkuthBuffData, MalkuthHook>(MalkuthBuffData.Malkuth);
            BuffRegister.RegisterBuff<NetzachBuff, NetzachBuffData, NetzachHook>(NetzachBuffData.Netzach);
            BuffRegister.RegisterBuff<TipherethBuff, TipherethBuffData, TipherethHook>(TipherethBuffData.Tiphereth);
            BuffRegister.RegisterBuff<YesodBuff, YesodBuffData, YesodBuff>(YesodBuffData.Yesod);
            BuffRegister.RegisterBuff<BinahBuff, BinahBuffData, BinahHook>(BinahBuffData.Binah);

        }

        public static void LoadAssets()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("buffassets/assetbundles/sephirah"));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.Yesod", FShader.CreateShader("SephirahMeltdownEntry.Yesod",
                bundle.LoadAsset<Shader>("Yesod")));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.GrayCast", FShader.CreateShader("SephirahMeltdownEntry.GrayCast",
                bundle.LoadAsset<Shader>("GrayCast")));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.BinahWave", FShader.CreateShader("SephirahMeltdownEntry.BinahWave",
                bundle.LoadAsset<Shader>("BinahWave")));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.Bar", FShader.CreateShader("SephirahMeltdownEntry.Bar",
                bundle.LoadAsset<Shader>("BinahBar")));
            Futile.atlasManager.LoadAtlas(Path.Combine(ChesedBuffData.Chesed.GetStaticData().AssetPath, "ChesedTex"));
            Futile.atlasManager.LoadAtlas(Path.Combine(BinahBuffData.Binah.GetStaticData().AssetPath, "BinahTex"));

            BinahScreenEffect = bundle.LoadAsset<Shader>("BinahScreenEffect");
            BinahScreenEffectTexture = bundle.LoadAsset<Texture2D>("CameraFilterPack_Blizzard1");

            BuffSounds.LoadSound(BinahAtkStone, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk_Stone"));
            BuffSounds.LoadSound(BinahAtkFairy, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk_Fairy"));
            BuffSounds.LoadSound(BinahAtkStrike, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk_Strike"));

            BuffSounds.LoadSound(BinahAtkFinalStart, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk4_CastStart"));
            BuffSounds.LoadSound(BinahAtkFinalLoop, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk4_CastLoop"));
            BuffSounds.LoadSound(BinahAtkFinalEnd, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk4_CastEnd"));
            BuffSounds.LoadSound(BinahAtkFinalMake, BinahBuffData.Binah.GetStaticData().AssetPath, new BuffSoundGroupData(),
                new BuffSoundData("Binah_Atk4_make"));
        }

        public static readonly SoundID BinahAtkStone = new SoundID(nameof(BinahAtkStone), true);
        public static readonly SoundID BinahAtkFairy = new SoundID(nameof(BinahAtkFairy), true);
        public static readonly SoundID BinahAtkStrike = new SoundID(nameof(BinahAtkStrike), true);


        public static readonly SoundID BinahAtkFinalStart = new SoundID(nameof(BinahAtkFinalStart), true);
        public static readonly SoundID BinahAtkFinalLoop = new SoundID(nameof(BinahAtkFinalLoop), true);
        public static readonly SoundID BinahAtkFinalEnd = new SoundID(nameof(BinahAtkFinalEnd), true);
        public static readonly SoundID BinahAtkFinalMake = new SoundID(nameof(BinahAtkFinalMake), true);


        public static Shader BinahScreenEffect;
        public static Texture2D BinahScreenEffectTexture;

    }

    internal abstract class SephirahMeltdownBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 4;

        public override void CycleEnd()
        {
            base.CycleEnd();
            if (CycleUse == MaxCycleCount && !BuffConfigManager.IsItemLocked(QuestUnlockedType.Card, ID.value) &&
                BuffPoolManager.Instance.GameSetting.MissionId != SephirahMeltdownsMission.SephirahMeltdowns.value)
                Reward();
            
            CycleUse = Mathf.Min(CycleUse, MaxCycleCount);
        }



        public virtual void Reward()
        {
            SlugcatStats.Name name;
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                name = game.StoryCharacter;
            else
                name = Custom.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
            BuffDataManager.Instance.GetOrCreateBuffData(
                BuffPicker.GetNewBuffsOfType(name, 1, BuffType.Positive)[0].BuffID,
                true);
        }
    }
}
