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

namespace RandomBuff.Render.CardRender
{
    internal static class CardRendererManager
    {
        public static Font titleFont;
        public static Font discriptionFont;

        static float maxDestroyTime = 60f;
        static List<BuffCardRenderer> totalRenderers = new List<BuffCardRenderer>();
        static List<BuffCardRenderer> inactiveCardRenderers = new List<BuffCardRenderer>();

        static int NextLegalID
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
            BuffPlugin.Log($"Get used renderer : {mostInavtiveRenderer._id}, {inactiveCardRenderers.Contains(mostInavtiveRenderer)}");
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
                    BuffPlugin.LogDebug($"Get new card renderer of id {id}");
                    return renderer;
                }
                catch(Exception e)
                {
                    BuffPlugin.LogException(e, $"Render error : {buffID}");
                    return null;
                }
            }
        }

        public static void RecycleCardRenderer(BuffCardRenderer buffCardRenderer)
        {
            buffCardRenderer.gameObject.SetActive(false);
            buffCardRenderer.inactiveTimer = 0f;
            if(!inactiveCardRenderers.Contains(buffCardRenderer))//不知道为什么，但有时候会出现重复回收的情况
                inactiveCardRenderers.Add(buffCardRenderer);
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
        /// 
        /// </summary>
        public static Texture SlugBack { get; private set; }

        public static Texture TextBack { get; private set; }

        /// <summary>
        /// 卡牌渲染的贴图尺寸
        /// </summary>
        public static Vector2Int RenderTextureSize = new Vector2Int(600, 1000);

        static Dictionary<string, Shader> LoadedShaders = new Dictionary<string, Shader>();

        static string GB2312;//加载完字体后销毁节省内存
        static bool[] corounteFlags = new bool[2] { false, false };

        /// <summary>
        /// 从文件中加载资源
        /// </summary>
        public static void LoadAssets()
        {
            //调整TextMeshPro
            Hook tmp_settings_get_hook = new Hook(typeof(TMP_Settings).GetProperty("instance").GetGetMethod(), TMP_Settings_instance_get);

            //加载assetbundle资源
            string path = AssetManager.ResolveFilePath("buffassets/assetbundles/randombuffbuiltinbundle");
            string path2 = AssetManager.ResolveFilePath("buffassets/assetbundles/textmeshpro");
            string path3 = AssetManager.ResolveFilePath($"buffassets/assetbundles/GB2312.txt");

            GB2312 = BuffResource.CommonlyUsed;
            //var lines = File.ReadAllLines(path2, Encoding.UTF8);

            //加载TextMeshPro
            TMP_FontAsset testAsset = null;
            AssetBundle ab2 = AssetBundle.LoadFromFile(path2);
            var allAssets2 = ab2.LoadAllAssets();
            Shader shader1 = null;

            List<Font> fontsToCreate = new List<Font>();
            
            try
            {
                foreach (var asset in allAssets2)
                {
                    if (asset is TMP_Settings)
                        TMP_settings = (TMP_Settings)asset;

                    if (asset is Shader shader)
                    {
                        Debug.Log($"Loaded shader : {shader.name}");
                        BuffPlugin.Log($"Loaded shader : {shader.name}");
                        LoadedShaders.Add(shader.name, shader);
                        if (shader.name == "TextMeshPro/Distance Field SSD")
                        {
                            shader1 = (Shader)shader;
                        }
                    }

                    if(asset is Font fontAsset)
                    {
                        fontsToCreate.Add(fontAsset);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            TMP_Settings.instance.m_warningsDisabled = false;

            foreach(var font in fontsToCreate)
            {
                testAsset = CreateFontAsset(font);

                testAsset.material.EnableKeyword("UNDERLAY_ON");
                testAsset.material.SetColor("_UnderlayColor", Color.black);
                testAsset.material.SetFloat("_UnderlayDilate", 1f);
                testAsset.material.SetFloat("_UnderlaySoftness", 1f);

                testAsset.name = font.name;
                Debug.Log($"charaters in font : {testAsset.characterTable.Count}, {testAsset.name}");
                if(testAsset.name == "Zhanku")
                {
                    TitleFont = testAsset;
                    TitleOrigFont = font;
                    BuffPlugin.Instance.StartCoroutine(LoadCharacterForFont(testAsset, 0));
                }
                else
                {
                    DiscriptionFont = testAsset;
                    DiscriptionOrigFont = font;
                    BuffPlugin.Instance.StartCoroutine(LoadCharacterForFont(testAsset, 1));
                }
            }

            //加载自带资源
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            var allAssets = ab.LoadAllAssets();

            List<TMP_FontAsset> loadedFonts = new List<TMP_FontAsset>();
            List<Shader> loadedShaders = new List<Shader>();

            foreach (var asset in allAssets)
            {
                if (asset is Shader)
                    loadedShaders.Add((Shader)asset);
                BuffPlugin.Log($"Load asset from assetbundle : {asset.name}");
            }

            CardHighlightShader = loadedShaders[1];
            CardTextShader = loadedShaders[2];
            CardBasicShader = loadedShaders[0];

            //加载其他资源
            MoonBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/moonback").texture;
            FPBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/fpback").texture;
            SlugBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/slugback").texture;
            TextBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/textback").texture;

            BuffPlugin.Log($"tex name : {FPBack.name}, {FPBack.texelSize}");
        }

        public static TMP_Settings TMP_Settings_instance_get(Func<TMP_Settings> orig)
        {
            if (TMP_Settings.s_Instance == null)
                TMP_Settings.s_Instance = TMP_settings;
            return TMP_Settings.s_Instance;
        }

        public static TMP_FontAsset CreateFontAsset(Font font)
        {
            return CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 4096, 4096);
        }

        public static TMP_FontAsset CreateFontAsset(Font font, int samplingPointSize, int atlasPadding, GlyphRenderMode renderMode, int atlasWidth, int atlasHeight, AtlasPopulationMode atlasPopulationMode = AtlasPopulationMode.Dynamic, bool enableMultiAtlasSupport = true)
        {
            FontEngine.InitializeFontEngine();
            if (FontEngine.LoadFontFace(font, samplingPointSize) != 0)
            {
                Debug.LogWarningFormat("Unable to load font face for [" + font.name + "]. Make sure \"Include Font Data\" is enabled in the Font Import Settings.", font);
                return null;
            }

            TMP_FontAsset tMP_FontAsset = ScriptableObject.CreateInstance<TMP_FontAsset>();
            tMP_FontAsset.m_Version = "1.1.0";
            tMP_FontAsset.faceInfo = FontEngine.GetFaceInfo();
            if (atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                tMP_FontAsset.sourceFontFile = font;
            }

            tMP_FontAsset.atlasPopulationMode = atlasPopulationMode;
            tMP_FontAsset.atlasWidth = atlasWidth;
            tMP_FontAsset.atlasHeight = atlasHeight;
            tMP_FontAsset.atlasPadding = atlasPadding;
            tMP_FontAsset.atlasRenderMode = renderMode;
            tMP_FontAsset.atlasTextures = new Texture2D[1];
            Texture2D texture2D = new Texture2D(0, 0, TextureFormat.Alpha8, mipChain: false);
            tMP_FontAsset.atlasTextures[0] = texture2D;
            tMP_FontAsset.isMultiAtlasTexturesEnabled = enableMultiAtlasSupport;
            int num;
            if ((renderMode & (GlyphRenderMode)16) == (GlyphRenderMode)16)
            {
                num = 0;
                Material material = new Material(LoadedShaders["TextMeshPro/Mobile/Bitmap"]/*ShaderUtilities.ShaderRef_MobileBitmap*/);
                material.SetTexture(ShaderUtilities.ID_MainTex, texture2D);
                material.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                material.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight); 
                tMP_FontAsset.material = material;
            }
            else
            {
                num = 1;
                Material material2 = new Material(LoadedShaders["TextMeshPro/Mobile/Distance Field"]/*ShaderUtilities.ShaderRef_MobileSDF*/);
                material2.SetTexture(ShaderUtilities.ID_MainTex, texture2D);
                material2.SetFloat(ShaderUtilities.ID_TextureWidth, atlasWidth);
                material2.SetFloat(ShaderUtilities.ID_TextureHeight, atlasHeight);
                material2.SetFloat(ShaderUtilities.ID_GradientScale, atlasPadding + num);
                material2.SetFloat(ShaderUtilities.ID_WeightNormal, tMP_FontAsset.normalStyle);
                material2.SetFloat(ShaderUtilities.ID_WeightBold, tMP_FontAsset.boldStyle);
                tMP_FontAsset.material = material2;
            }

            tMP_FontAsset.freeGlyphRects = new List<GlyphRect>(8)
            {
                new GlyphRect(0, 0, atlasWidth - num, atlasHeight - num)
            };
            tMP_FontAsset.usedGlyphRects = new List<GlyphRect>(8);
            tMP_FontAsset.ReadFontAssetDefinition();
            return tMP_FontAsset;
        }
    
        public static IEnumerator LoadCharacterForFont(TMP_FontAsset fontToLoad, int index)
        {
            int targetFrameRate = Mathf.Clamp(Custom.rainWorld.options.fpsCap + 30,60,120);
            int pointer = 0;

            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;

            
            while (pointer < GB2312.Length)
            {
                fontToLoad.HasCharacter(GB2312[pointer], true, true);
                //Debug.Log($"{GB2312[pointer]} {fontToLoad.HasCharacter(GB2312[pointer], false, true)}");
                end = DateTime.Now;

                if((end - start).TotalSeconds >= 1f / targetFrameRate)
                {
                    //BuffPlugin.Log($"CheckCharacter at {fontToLoad.name}, {pointer} / {GB2312.Length}");
                    start = DateTime.Now;
                    yield return null;
                }
                pointer++;
            }

            BuffPlugin.Log($"CheckCharacter at {fontToLoad.name} finish, total charater : {fontToLoad.characterTable.Count}");
            corounteFlags[index] = true;
            

            if(corounteFlags.All(i=>i))
            {
                GB2312 = "";
                corounteFlags = null;
                BuffPlugin.Log($"CheckCharacter allFinisehd");
            }

            yield break;
        }
    }
}