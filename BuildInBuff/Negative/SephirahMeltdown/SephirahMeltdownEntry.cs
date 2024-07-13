using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RandomBuff.Core.Progression;
using RandomBuff.Core.SaveData;
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
        }

        public static void LoadAssets()
        {
            AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("buffassets/assetbundles/sephirah"));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.Yesod", FShader.CreateShader("SephirahMeltdownEntry.Yesod",
                bundle.LoadAsset<Shader>("Yesod")));
            Custom.rainWorld.Shaders.Add("SephirahMeltdownEntry.GrayCast", FShader.CreateShader("SephirahMeltdownEntry.GrayCast",
                bundle.LoadAsset<Shader>("GrayCast")));
            Futile.atlasManager.LoadAtlas(Path.Combine(ChesedBuffData.Chesed.GetStaticData().AssetPath, "ChesedTex"));
        }
    }

    internal abstract class SephirahMeltdownBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 4;

        public override void CycleEnd()
        {
            base.CycleEnd();
            if (CycleUse == MaxCycleCount && !BuffConfigManager.IsItemLocked(QuestUnlockedType.Card, ID.value))
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
