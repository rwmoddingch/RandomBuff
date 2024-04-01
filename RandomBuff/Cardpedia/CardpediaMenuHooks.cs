using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using Menu;

namespace RandomBuff.Cardpedia
{
    public static class CardpediaMenuHooks
    {
        private static MainMenu menu;
        public static Shader InvertColor;
        public static Shader SquareBlinking;
        public static Shader UIBlur;
        public static Shader FoldingUp;
        public static Shader LeftFoldUp;
        public static Shader RightFoldUp;
        public static Shader FoldableTextHorizontal;
        public static Shader FoldableTextVertical;
        public static Shader UIBlurFoldable;
        public static Shader FoldablePicVertical;
        public static Texture2D RBNoiseTex;

        public static void Hook()
        {
            On.Menu.MainMenu.ctor += MainMenu_ctor;
        }

        private static void MainMenu_ctor(On.Menu.MainMenu.orig_ctor orig, MainMenu self, ProcessManager manager, bool showRegionSpecificBkg)
        {
            orig(self, manager, showRegionSpecificBkg);
            float buttonWidth = MainMenu.GetButtonWidth(self.CurrLang);
            Vector2 pos = new Vector2(683f - buttonWidth / 2f, 0f);
            Vector2 size = new Vector2(buttonWidth, 30f);
            SimpleButton collectionButton = new SimpleButton(self, self.pages[0], self.Translate("CARDPEDIA"), "CARDPEDIA", pos, size);
            menu = self;
            self.AddMainMenuButton(collectionButton, new Action(CollectionButtonPressed), 0);
        }

        public static void CollectionButtonPressed()
        {
            if (menu != null)
            {
                menu.manager.RequestMainProcessSwitch(BuffEnums.ProcessID.Cardpedia);
                menu.PlaySound(SoundID.MENU_Switch_Page_In);
            }
        }

        public static void LoadAsset()
        {
            RainWorld rainWorld = Custom.rainWorld;
            Futile.atlasManager.LoadImage("buffassets/illustrations/BlankScreen");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Negative_Flat");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Negative_Fill");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Duality_Flat");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Duality_Fill");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Positive_Flat");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Positive_Fill");
            Futile.atlasManager.LoadImage("buffassets/illustrations/TitleShadow_Cardpedia");
            Futile.atlasManager.LoadImage("buffassets/illustrations/TitleFlat_Cardpedia");
            Futile.atlasManager.LoadImage("buffassets/illustrations/UIBlock");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Type_Chi");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Stack_Chi");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Trigger_Chi");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Description_Chi");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Confliction_Chi");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Type_Eng");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Stack_Eng");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Trigger_Eng");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Description_Eng");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/Confliction_Eng");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/SmallShadow");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/LongShadow");
            Futile.atlasManager.LoadImage("buffassets/illustrations/Titles/LongShadow_200");
            Futile.atlasManager.LoadImage("buffassets/illustrations/SlugLoading_Main");
            Futile.atlasManager.LoadImage("buffassets/illustrations/SlugLoading_Cards");
            Futile.atlasManager.LoadImage("buffassets/illustrations/SlugLoading_UI");
            Futile.atlasManager.LoadImage("buffassets/missionicons/missionicon_general");
            string path = AssetManager.ResolveFilePath("buffassets/assetbundles/shadertest");           
            AssetBundle ab = AssetBundle.LoadFromFile(path);
            InvertColor = ab.LoadAsset<Shader>("Assets/InvertColor.shader");
            SquareBlinking = ab.LoadAsset<Shader>("Assets/SquareBlinking.shader");
            UIBlur = ab.LoadAsset<Shader>("Assets/UIBlur.shader");
            FoldingUp = ab.LoadAsset<Shader>("Assets/FoldingUp.shader");
            LeftFoldUp = ab.LoadAsset<Shader>("Assets/LeftFoldUp.shader");
            RightFoldUp = ab.LoadAsset<Shader>("Assets/RightFoldUp.shader");
            FoldableTextHorizontal = ab.LoadAsset<Shader>("Assets/FoldableTextHorizontal.shader");
            FoldableTextVertical = ab.LoadAsset<Shader>("Assets/FoldableTextVertical.shader");
            UIBlurFoldable = ab.LoadAsset<Shader>("Assets/UIBlurFoldable.shader");
            FoldablePicVertical = ab.LoadAsset<Shader>("Assets/FoldablePicVertical.shader");
            RBNoiseTex = (ab.LoadAsset("Assets/Textures/RBNoiseTex.png")) as Texture2D;           
            if (SquareBlinking != null && RBNoiseTex != null)
            {
                Shader.SetGlobalTexture("_RBNoiseTex",RBNoiseTex);
                Shader.SetGlobalFloat("_Resolution",0.04f);
                Shader.SetGlobalFloat("_BlinkAlpha",0.2f);
                rainWorld.Shaders.Add("SquareBlinking", FShader.CreateShader("SquareBlinking", CardpediaMenuHooks.SquareBlinking));
            }
            rainWorld.Shaders.Add("InvertColor", FShader.CreateShader("InvertColor", CardpediaMenuHooks.InvertColor));
            rainWorld.Shaders.Add("UIBlur", FShader.CreateShader("UIBlur", CardpediaMenuHooks.UIBlur));
            rainWorld.Shaders.Add("FoldingUp", FShader.CreateShader("FoldingUp", CardpediaMenuHooks.FoldingUp));
            rainWorld.Shaders.Add("LeftFoldUp", FShader.CreateShader("LeftFoldUp", CardpediaMenuHooks.LeftFoldUp));
            rainWorld.Shaders.Add("RightFoldUp", FShader.CreateShader("RightFoldUp", CardpediaMenuHooks.RightFoldUp));
            rainWorld.Shaders.Add("FoldableTextHorizontal", FShader.CreateShader("FoldableTextHorizontal", CardpediaMenuHooks.FoldableTextHorizontal));
            rainWorld.Shaders.Add("FoldableTextVertical", FShader.CreateShader("FoldableTextVertical", CardpediaMenuHooks.FoldableTextVertical));
            rainWorld.Shaders.Add("UIBlurFoldable", FShader.CreateShader("UIBlurFoldable", CardpediaMenuHooks.UIBlurFoldable));
            rainWorld.Shaders.Add("FoldablePicVertical", FShader.CreateShader("FoldablePicVertical", CardpediaMenuHooks.FoldablePicVertical));
            Shader.SetGlobalColor("_BlurColor",Color.white);
            Shader.SetGlobalFloat("_ColorBias",0.10f);
            Shader.SetGlobalFloat("_BlurSize",5f);
            Shader.SetGlobalColor("_SetColor", Color.white);
        }
    }
}
