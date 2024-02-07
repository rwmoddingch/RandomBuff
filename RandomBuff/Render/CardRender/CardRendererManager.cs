using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal static class CardRendererManager
    {
        public static Font titleFont;
        public static Font discriptionFont;

        static float maxDestroyTime = 5f;
        static int currentID;
        static Queue<BuffCardRenderer> buffCards = new Queue<BuffCardRenderer>();
        static List<BuffCardRenderer> inactiveCardRenderers = new List<BuffCardRenderer>();

        public static BuffCardRenderer GetRenderer(BuffID buffID)
        {
            if (inactiveCardRenderers.Count == 0)
                return GetNewRenderer(buffID);

            float maxTimer = 0f;
            BuffCardRenderer mostInavtiveRenderer = null;
            foreach (var renderer in inactiveCardRenderers)//使用最先回收的渲染器
            {
                if (renderer.inactiveTimer > maxTimer)
                {
                    maxTimer = renderer.inactiveTimer;
                    mostInavtiveRenderer = renderer;
                }
            }

            mostInavtiveRenderer.gameObject.SetActive(true);
            return mostInavtiveRenderer;

            BuffCardRenderer GetNewRenderer(BuffID buffID)
            {
                var cardObj = new GameObject($"BuffCard_{currentID}");
                cardObj.transform.position = new Vector3(currentID * 20, 0, 0);
                var renderer = cardObj.AddComponent<BuffCardRenderer>();

                renderer.Init(currentID, BuffConfigManager.GetStaticData(buffID));

                currentID++;
                return renderer;
            }
        }

        public static void RecycleCardRenderer(BuffCardRenderer buffCardRenderer)
        {
            buffCardRenderer.gameObject.SetActive(false);
            buffCardRenderer.inactiveTimer = 0f;
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
                    BuffPlugin.Log($"Destroy inactive card renderer of id{card._id}");

                    inactiveCardRenderers.RemoveAt(i);
                    Object.Destroy(card.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// 卡牌渲染基础资源类
    /// </summary>
    public static class CardBasicAssets
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
        public static Font TitleFont { get; private set; }

        /// <summary>
        /// 卡牌介绍使用的字体
        /// </summary>
        public static Font DiscriptionFont { get; private set; }

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

        /// <summary>
        /// 卡牌渲染的贴图尺寸
        /// </summary>
        public static Vector2Int RenderTextureSize = new Vector2Int(600, 1000);

        /// <summary>
        /// 从文件中加载资源
        /// </summary>
        public static void LoadAssets()
        {
            //加载assetbundle资源
            string path = AssetManager.ResolveFilePath("buffassets/assetbundles/randombuffbuiltinbundle");
            BuffPlugin.Log(File.Exists(path));
            AssetBundle ab = AssetBundle.LoadFromFile(path);

            var allAssets = ab.LoadAllAssets();

            List<Font> loadedFonts = new List<Font>();
            List<Shader> loadedShaders = new List<Shader>();

            foreach (var asset in allAssets)
            {
                if (asset is Font)
                    loadedFonts.Add((Font)asset);

                if (asset is Shader)
                    loadedShaders.Add((Shader)asset);
                BuffPlugin.Log($"Load asset from assetbundle : {asset.name}");
            }

            BuffPlugin.Log(Shader.Find("Unlit/Color"));

            DiscriptionFont = loadedFonts[0];
            TitleFont = loadedFonts[1];

            CardHighlightShader = loadedShaders[1];
            CardTextShader = loadedShaders[2];
            CardBasicShader = loadedShaders[0];

            //加载其他资源
            MoonBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/moonback").texture;
            FPBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/fpback").texture;
            SlugBack = Futile.atlasManager.LoadImage("buffassets/cardbacks/slugback").texture;
        }
    }
}