using MonoMod.RuntimeDetour;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore;
using Object = UnityEngine.Object;
using System.Linq;
using Font = UnityEngine.Font;
using RWCustom;
using RandomBuff.Render.UI;


namespace RandomBuff.Render.CardRender
{
    internal static class CardRendererManager
    {
        static float maxDestroyTime = 120f;
        static List<BuffCardRendererBase> totalRenderers = new List<BuffCardRendererBase>();
        static List<BuffCardRenderer> inactiveCardRenderers = new List<BuffCardRenderer>();
        static List<SingleTextCardRenderer> inactiveSingleTextCardRenderer = new List<SingleTextCardRenderer>();

        public static int NextLegalID
        {
            get
            {
                int nextID = 0;
                while (true)
                {
                    bool anyIDMatched = false;
                    foreach(var render in totalRenderers)
                    {
                        if (render._id == nextID)
                            anyIDMatched = true;
                    }

                    if (!anyIDMatched)
                        return nextID;

                    nextID++;
                }
            }
        }

        public static BuffCardRenderer GetRenderer(BuffID buffID)
        {
            if (inactiveCardRenderers.Count == 0)
                return GetNewRenderer(buffID);

            float maxTimer = 0f;
            BuffCardRenderer mostInavtiveRenderer = null;
            foreach (var renderer in inactiveCardRenderers)//使用最先回收的渲染器
            {
                if (renderer.inactiveTimer >= maxTimer)
                {
                    maxTimer = renderer.inactiveTimer;
                    mostInavtiveRenderer = renderer;
                }
            }

            
            mostInavtiveRenderer.gameObject.SetActive(true);
            mostInavtiveRenderer.Init(mostInavtiveRenderer._id, BuffConfigManager.GetStaticData(buffID));
            inactiveCardRenderers.Remove(mostInavtiveRenderer);
            //BuffPlugin.LogDebug($"Get used renderer : {mostInavtiveRenderer._id}, {inactiveCardRenderers.Contains(mostInavtiveRenderer)}");
            return mostInavtiveRenderer;

            BuffCardRenderer GetNewRenderer(BuffID buffID)
            {
                try
                {
                    int id = NextLegalID;
                    var cardObj = new GameObject($"BuffCard_{id}");
                    cardObj.transform.position = new Vector3(id * 20, 0, 0);
                    var renderer = cardObj.AddComponent<BuffCardRenderer>();
                    totalRenderers.Add(renderer);

                    renderer.Init(id, BuffConfigManager.GetStaticData(buffID));
                    //BuffPlugin.LogDebug($"Get new card renderer of id {id}");
                    return renderer;
                }
                catch(Exception e)
                {
                    BuffPlugin.LogException(e, $"Render error : {buffID}");
                    return null;
                }
            }
        }

        public static SingleTextCardRenderer GetSingleTextRenderer(string text)
        {
            SingleTextCardRenderer result;
            if(inactiveSingleTextCardRenderer.Count > 0)
            {
                result = inactiveSingleTextCardRenderer.Pop();
                result.gameObject.SetActive(true);
                result.Init(result._id, null);
            }
            else
                result = GetNewRenderer(text);

            return result;

            SingleTextCardRenderer GetNewRenderer(string text)
            {
                int id = NextLegalID;
                var cardObj = new GameObject($"SingleTextCard_{id}");
                cardObj.transform.position = new Vector3(id * 20, 0, 0);
                SingleTextCardRenderer _cardRenderer = cardObj.AddComponent<SingleTextCardRenderer>();
                _cardRenderer.Init(id, null);
                BuffPlugin.LogDebug($"Get new SingleTextCardRenderer of id {id}");
                totalRenderers.Add(_cardRenderer);

                return _cardRenderer;
            }
        }

        public static void RecycleCardRenderer(BuffCardRendererBase buffCardRenderer)
        {
            if (buffCardRenderer is BuffCardRenderer cardRenderer)
            {
                cardRenderer.gameObject.SetActive(false);
                cardRenderer.Deacive();
                cardRenderer.inactiveTimer = 0f;
                if (!inactiveCardRenderers.Contains(cardRenderer))//不知道为什么，但有时候会出现重复回收的情况
                    inactiveCardRenderers.Add(cardRenderer);
            }
            else if(buffCardRenderer is SingleTextCardRenderer singleTextCardRenderer)
            {
                singleTextCardRenderer.gameObject.SetActive(false);
                if(!inactiveSingleTextCardRenderer.Contains(singleTextCardRenderer))
                    inactiveSingleTextCardRenderer.Add(singleTextCardRenderer);
            }
        }

        /// <summary>
        /// 维护一个有时间限制的对象池，当不活跃程度超过一定时间后，将会销毁该对象。
        /// </summary>
        /// <param name="deltaTime"></param>
        public static void UpdateInactiveRendererTimers(float deltaTime)
        {
            for (int i = inactiveCardRenderers.Count - 1; i >= 0; i--)
            {
                inactiveCardRenderers[i].inactiveTimer += deltaTime;
                if (inactiveCardRenderers[i].inactiveTimer > maxDestroyTime)
                {
                    var card = inactiveCardRenderers[i];
                    card.DestroyRenderer();

                    totalRenderers.Remove(card);
                    BuffPlugin.LogDebug($"Destroy inactive card renderer of id{card._id}");

                    inactiveCardRenderers.RemoveAt(i);
                    Object.Destroy(card.gameObject);
                }
            }
        }

        public static void DestroyAllInactiveRenderer()
        {
            for (int i = inactiveCardRenderers.Count - 1; i >= 0; i--)
            {
                var card = inactiveCardRenderers[i];
                totalRenderers.Remove(card);
                BuffPlugin.LogDebug($"Force destroy inactive card renderer of id{card._id}");

                inactiveCardRenderers.RemoveAt(i);
                Object.Destroy(card.gameObject);
            }
        }
    }

    /// <summary>
    /// 卡牌渲染基础资源类
    /// </summary>
    internal static class CardBasicAssets
    {
        /// <summary>
        /// 卡牌高光shader
        /// </summary>
        public static Shader CardHighlightShader { get; private set; }

        /// <summary>
        /// 卡牌字体shader
        /// </summary>
        public static Shader CardTextShader { get; private set; }

        /// <summary>
        /// 卡牌shader
        /// </summary>
        public static Shader CardBasicShader { get; private set; }

        public static Shader CardBasicTransprent { get; private set; }
        /// <summary>
        /// 卡牌标题的字体
        /// </summary>
        public static TMP_FontAsset TitleFont { get; private set; }
        public static Font TitleOrigFont { get; private set; }

        /// <summary>
        /// 卡牌介绍使用的字体
        /// </summary>
        public static TMP_FontAsset DiscriptionFont { get; private set; }
        public static Font DiscriptionOrigFont { get; private set; }

        public static TMP_Settings TMP_settings { get; private set; }

        /// <summary>
        /// 正面增益的卡背
        /// </summary>
        public static Texture MoonBack { get; private set; }

        /// <summary>
        /// 负面增益的卡背
        /// </summary>
        public static Texture FPBack { get; private set; }

        /// <summary>
        /// 中性增益的卡背
        /// </summary>
        public static Texture SlugBack { get; private set; }

        public static Texture TextBack { get; private set; }

        public static Texture MissingFaceTexture { get; private set; }

        public static string MissingFace { get; private set; }

        public static Color PositiveColor { get; } = Helper.GetRGBColor(27, 178, 196);
        public static Color NegativeColor { get; } = Helper.GetRGBColor(183, 56, 73);
        public static Color DualityColor { get; } = Helper.GetRGBColor(177, 170, 187);

        /// <summary>
        /// 卡牌渲染的贴图尺寸
        /// </summary>
        public static readonly Vector2Int RenderTextureSize = new Vector2Int(600, 1000);


        static string GB2312;//加载完字体后销毁节省内存
        static bool[] corounteFlags = new bool[2] { false, false };

        public static bool PauseLoadFont { get; set; }

        /// <summary>
        /// 从文件中加载资源
        /// </summary>
        public static void LoadAssets()
        {
            //调整TextMeshPro
            _ = new Hook(typeof(TMP_Settings).GetProperty("instance").GetGetMethod(), TMP_Settings_instance_get);

            //加载assetbundle资源
            string path = AssetManager.ResolveFilePath("buffassets/assetbundles/randombuffbuiltinbundle");
            string path2 = AssetManager.ResolveFilePath("buffassets/assetbundles/textmeshpro");
            string path3 = AssetManager.ResolveFilePath($"buffassets/assetbundles/font");
            AssetBundle bundle1 = AssetBundle.LoadFromFile(path3);
            TitleFont = bundle1.LoadAsset<TMP_FontAsset>("ZhankuAsset");
            DiscriptionFont = bundle1.LoadAsset<TMP_FontAsset>("NotoSansHans-RegularAsset");


            //加载TextMeshPro
            AssetBundle ab2 = AssetBundle.LoadFromFile(path2);
            var allAssets2 = ab2.LoadAllAssets();

            try
            {
                foreach (var asset in allAssets2)
                {
                    if (asset is TMP_Settings settings)
                        TMP_settings = settings;
                }
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e);
            }
            TMP_Settings.instance.m_warningsDisabled = false;



            //加载自带资源
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            var allAssets = ab.LoadAllAssets();

            List<Shader> loadedShaders = new List<Shader>();

            foreach (var asset in allAssets)
            {
                if (asset is Shader shader)
                    loadedShaders.Add(shader);
                BuffPlugin.Log($"Load asset from assetbundle : {asset.name}");
            }

            CardHighlightShader = loadedShaders[2];
            CardTextShader = loadedShaders[3];
            CardBasicShader = loadedShaders[0];
            CardBasicTransprent = loadedShaders[1];

            //加载其他资源
            MoonBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/moonback").texture;
            FPBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/fpback").texture;
            SlugBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/slugback").texture;
            TextBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/textback").texture;

            var atlas = Futile.atlasManager.LoadImage("buffassets/cardbacks/missing");
            MissingFace = atlas.name;
            MissingFaceTexture = atlas.texture;

            BuffPlugin.Log($"tex name : {FPBack.name}, {FPBack.texelSize}");
        }

        public static TMP_Settings TMP_Settings_instance_get(Func<TMP_Settings> orig)
        {
            if (TMP_Settings.s_Instance == null)
                TMP_Settings.s_Instance = TMP_settings;
            return TMP_Settings.s_Instance;
        }

    }
}