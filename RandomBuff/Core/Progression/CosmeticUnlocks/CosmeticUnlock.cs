using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32;
using RandomBuff.Core.Progression.CosmeticUnlocks;
using RandomBuff.Render.Quest;
using RandomBuffUtils.FutileExtend;
using UnityEngine;

namespace RandomBuff.Core.Progression
{
    public class CosmeticUnlockID : ExtEnum<CosmeticUnlockID>
    {
        public CosmeticUnlockID(string value, bool register = false) : base(value, register)
        {
        }
        public static readonly CosmeticUnlockID FireWork = new("FireWork", true);//炸猫
        public static readonly CosmeticUnlockID Test = new("Test", true);
        public static readonly CosmeticUnlockID Crown = new("Crown", true);//全部
        public static readonly CosmeticUnlockID GlowingLeaf = new CosmeticUnlockID("GlowingLeaf", true);//黄猫

        public static readonly CosmeticUnlockID HoloScore = new CosmeticUnlockID("HoloScore", true);//红猫
        public static readonly CosmeticUnlockID FateRain = new CosmeticUnlockID("FateRain", true);//白猫
        public static readonly CosmeticUnlockID FoodBag = new CosmeticUnlockID("FoodBag", true);//胖猫
        public static readonly CosmeticUnlockID SoapSlug = new CosmeticUnlockID("SoapSlug", true);//水猫
        public static readonly CosmeticUnlockID MeteorSpear = new CosmeticUnlockID("MeteorSpear", true);//矛猫
        public static readonly CosmeticUnlockID AscendSparkle = new CosmeticUnlockID("AscendSparkle", true);//圣猫

    }

    public abstract partial class CosmeticUnlock
    {
        protected CosmeticUnlock()
        {
            questRenderer = new CosmeticQuestRenderer(this);
        }
        public abstract CosmeticUnlockID UnlockID { get; }
        public abstract string IconElement { get; }
        public abstract SlugcatStats.Name BindCat { get; }

        public virtual void StartGame(RainWorldGame game) { }

        public virtual void Update(RainWorldGame game) {}

        public virtual void Destroy() {}
    }

    public abstract partial class CosmeticUnlock
    {
        public static void Register<T>() where T : CosmeticUnlock, new()
        {
            Register(typeof(T));
        }
        internal static void Register(Type type)
        {
            if (Activator.CreateInstance(type) is CosmeticUnlock unlock)
            {
                if (cosmeticUnlocks.ContainsKey(unlock.UnlockID))
                    BuffPlugin.LogError($"Cosmetic Unlocks: same UnlockID {unlock.UnlockID}.");
                else
                    cosmeticUnlocks.Add(unlock.UnlockID,type);
            }
            else
            {
                BuffPlugin.LogError($"Cosmetic Unlocks: register type {type.FullName} is not CosmeticUnlock");
            }
        }

        internal static void Init()
        {
            Register<TestCosmeticUnlock>();
            Register<FireworkCosmetic>();
            Register<CrownCosmetic>();
            Register<GlowingLeafCosmetic>();
            Register<HoloScoreCosmetic>();
            Register<AscendSparkleCosmetic>();
            Register<FateRainCosmetic>();
            Register<FoodBagCosmetic>();
            Register<SoapSlugCosmetic>();
            Register<MeteorSpearCosmetic>();

            grown = MeshManager.LoadMesh("grown", AssetManager.ResolveFilePath("buffassets//meshs//grown.obj"));
            QuestRendererManager.AddProvider(new CosmeticQuestRendererProvider());
        }

        internal static CosmeticUnlock CreateInstance(string name,RainWorldGame game)
        {
            var re = Activator.CreateInstance(cosmeticUnlocks[new CosmeticUnlockID(name)]) as CosmeticUnlock;
            if(game != null)
            {
                try
                {
                    re.StartGame(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e, $"CosmeticUnlock: {name} StartGame Error!");
                    return null;
                }
            }

            return re;
        }

        internal static void LoadIconSprites()
        {
            string path = AssetManager.ResolveDirectory("");

            LoadImgOfName("BuffCosmetic_Crown");
            LoadImgOfName("BuffCosmetic_Firework");
            LoadImgOfName("BuffCosmetic_GlowingLeaf");
            LoadImgOfName("BuffCosmetic_HoloScore");
            LoadImgOfName("BuffCosmetic_FateRain");
            LoadImgOfName("BuffCosmetic_FoodBag");
            LoadImgOfName("BuffCosmetic_SoapSlug");
            LoadImgOfName("BuffCosmetic_MeteorSpear");
            LoadImgOfName("BuffCosmetic_AscendSparkle");

            void LoadImgOfName(string imgName)
            {
                string imgPath = $"buffassets/BuffCosmeticIcons/{imgName}";
                string totPath = AssetManager.ResolveFilePath(imgPath + ".png").Replace('/', Path.DirectorySeparatorChar);

                Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                texture2D = AssetManager.SafeWWWLoadTexture(ref texture2D, totPath, false, true);

                FAtlas fAtlas = new FAtlas(imgName, texture2D, FAtlasManager._nextAtlasIndex++, false);
                Futile.atlasManager.AddAtlas(fAtlas);

                BuffPlugin.Log($"FAtlasElement : {Futile.atlasManager.GetElementWithName(imgName).sourcePixelSize}");
            }
        }

        internal static readonly Dictionary<CosmeticUnlockID, Type> cosmeticUnlocks = new ();


        internal static FMesh.Mesh3DAsset grown;
    }

    public abstract partial class CosmeticUnlock : IQuestRenderer
    {
        CosmeticQuestRenderer questRenderer;
        public QuestRenderer Renderer => questRenderer;
    }

    internal class CosmeticQuestRendererProvider : QuestRendererProvider
    {
        public override IQuestRenderer Provide(QuestUnlockedType type, string id)
        {
            if (type != QuestUnlockedType.Cosmetic)
                return null;

            if(CosmeticUnlock.cosmeticUnlocks.TryGetValue(new CosmeticUnlockID(id), out var cosmeticType))
            {
                return Activator.CreateInstance(cosmeticType) as CosmeticUnlock;
            }

            return null;
        }
    }
}
