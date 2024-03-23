using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Menu;
using RWCustom;
using RandomBuff.Cardpedia.InfoPageRender;
using System.ComponentModel;
using RandomBuff.Cardpedia.Elements;
using RandomBuff.Core.Buff;
using System.Runtime.CompilerServices;
using RandomBuff.Cardpedia.PediaPage;

namespace RandomBuff.Cardpedia
{
    internal class CardpediaMenu : Menu.Menu, CheckBox.IOwnCheckBox
    {
        public static CardpediaMenu Instance;

        //public TextBoxManager textBoxManager;
        //public CardSheetManager cardSheetManager;
        public ConfigManager configManager;

        public CardSheetPage sheetPage;

        private FSprite loadingSprite_Main;
        private FSprite loadingSprite_UI;
        private FSprite loadingSprite_Cards;
        private FSprite loadingSprite_Dark;

        private FSprite darkSprite;
        private FSprite darkSprite_Upper;
        public FSprite blurSprite;

        public FSprite progressRing_Negative;
        public FSprite progressRing_Positive;
        public FSprite progressRing_Duality;
        public FLabel progress_Negative;
        public FLabel progress_Positive;
        public FLabel progress_Duality;

        private FSprite titleShadow_Cardpedia;
        private FSprite titleFlat_Cardpedia;

        public SimpleButton backButton;
        public BigArrowButton leftFlipButton;
        public BigArrowButton rightFlipButton;
        public BigFancyButton negaButton;
        public BigFancyButton dualButton;
        public BigFancyButton posiButton;

        public BuffType currentType
        {
            set
            {
                sheetPage.RefreshSheet(value);
                sheetPage.Show = true;
                //textBoxManager.currentType = value;
                //textBoxManager.titleBack.color = textBoxManager.currentType == BuffType.Negative ? new Color(0.6f, 0f, 0.05f) :
                //(textBoxManager.currentType == BuffType.Positive ? new Color(0f, 0.6f, 0.4f) : new Color(0.5f, 0.5f, 0.5f));
                //textBoxManager.InitEmptyInfo();

                //cardSheetManager.currentType = value;
                //cardSheetManager.displayingCard.element = Futile.atlasManager.GetElementWithName("buffassets/cardbacks/" +
                //    (cardSheetManager.currentType == BuffType.Negative ? "fpback" : (cardSheetManager.currentType == BuffType.Positive ? "moonback" : "slugback")));
                //cardSheetManager.displayingCard.scale = 0.35f * (600f / cardSheetManager.displayingCard.element.sourcePixelSize.x);
            }
        }
        public bool inited;
        public bool fullyLoaded;
        public bool BrowsingCards;
        public bool textBoxLoaded;
        public bool cardSheetLoaded;
        public float switchCount;
        public float lastAlpha;
        public float SetAlpha;
        public Vector2 lastScale;
        public Vector2 SetScale;
        public Vector2 lastTitlePos;
        public Vector2 SetTitlePos;

        public CardpediaMenu(ProcessManager manager) : base(manager, CardpediaMenuHooks.Cardpedia)
        {
            BrowsingCards = false;
            switchCount = 1f;

            pages.Add(new Page(this, null, "main", 0));
            scene = new InteractiveMenuScene(this, null, Menu.MenuScene.SceneID.Empty);
            pages[0].subObjects.Add(scene);


            //加载画面
            Vector2 centerPos = new Vector2(683, 393);
            string path = "buffassets/illustrations/SlugLoading_";
            loadingSprite_Dark = new FSprite("pixel");
            loadingSprite_Dark.scale = 2000f;
            loadingSprite_Dark.color = new Color(0.01f, 0.01f, 0.01f);
            loadingSprite_Dark.SetPosition(centerPos);
            cursorContainer.AddChild(loadingSprite_Dark);
            loadingSprite_Main = new FSprite(path + "Main");
            loadingSprite_UI = new FSprite(path + "UI");
            loadingSprite_Cards = new FSprite(path + "Cards");
            loadingSprite_Cards.alpha = 0f;
            loadingSprite_Main.SetPosition(centerPos);
            loadingSprite_UI.SetPosition(centerPos);
            loadingSprite_Cards.SetPosition(centerPos);
            cursorContainer.AddChild(loadingSprite_Main);
            cursorContainer.AddChild(loadingSprite_UI);
            cursorContainer.AddChild(loadingSprite_Cards);
        }

        public void InitMenuElements()
        {
            //Bling bling 背景
            darkSprite = new FSprite("buffassets/illustrations/BlankScreen", true);
            //darkSprite.color = new Color(0.2f, 0.2f, 0.2f);
            //darkSprite.scaleX = 1378f;
            //darkSprite.scaleY = 800f;
            darkSprite.SetPosition(new Vector2(683, 393));
            //darkSprite.alpha = 0.5f;
            darkSprite.shader = manager.rainWorld.Shaders["SquareBlinking"];
            pages[0].Container.AddChild(darkSprite);

            var linearGradient = new FSprite("LinearGradient200");
            linearGradient.scaleX = 1376f;
            linearGradient.scaleY = 8f;
            linearGradient.color = new Color(0f, 0f, 0f);
            //linearGradient.alpha = 0.8f;
            linearGradient.SetPosition(new Vector2(683f, linearGradient.scaleY * 99));
            pages[0].Container.AddChild(linearGradient);

            //类型板块按钮
            negaButton = new BigFancyButton("Negative", this, pages[0], new Vector2(0, 0), new Vector2(1, 1));
            dualButton = new BigFancyButton("Duality", this, pages[0], new Vector2(0, 0), new Vector2(1, 1));
            posiButton = new BigFancyButton("Positive", this, pages[0], new Vector2(0, 0), new Vector2(1, 1));
            pages[0].subObjects.Add(negaButton);
            pages[0].subObjects.Add(dualButton);
            pages[0].subObjects.Add(posiButton);

            //进度圆环
            progressRing_Negative = new FSprite("Futile_White");
            progressRing_Negative.scale = 5f;
            progressRing_Negative.color = new Color(0.9f, 0f, 0.10f);
            progressRing_Negative.alpha = 0.5f;
            progressRing_Negative.SetPosition(negaButton.fillSprite.GetPosition() - new Vector2(452f, 0));
            progressRing_Negative.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            var backRing_Nega = new FSprite("Futile_White");
            backRing_Nega.scale = 5f;
            backRing_Nega.color = new Color(0.45f, 0f, 0.05f);
            backRing_Nega.alpha = 1f;
            backRing_Nega.SetPosition(progressRing_Negative.GetPosition());
            backRing_Nega.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            pages[0].Container.AddChild(backRing_Nega);
            progressRing_Positive = new FSprite("Futile_White");
            progressRing_Positive.scale = 5f;
            progressRing_Positive.color = new Color(0f, 1f, 0.85f);
            progressRing_Positive.alpha = 0.5f;
            progressRing_Positive.SetPosition(posiButton.fillSprite.GetPosition() - new Vector2(452f, 0));
            progressRing_Positive.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            var backRing_Posi = new FSprite("Futile_White");
            backRing_Posi.scale = 5f;
            backRing_Posi.color = new Color(0f, 0.5f, 0.42f);
            backRing_Posi.alpha = 1f;
            backRing_Posi.SetPosition(progressRing_Positive.GetPosition());
            backRing_Posi.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            pages[0].Container.AddChild(backRing_Posi);
            progressRing_Duality = new FSprite("Futile_White");
            progressRing_Duality.scale = 5f;
            progressRing_Duality.color = new Color(0.85f, 0.85f, 0.85f);
            progressRing_Duality.alpha = 0.5f;
            progressRing_Duality.SetPosition(dualButton.fillSprite.GetPosition() - new Vector2(452f, 0));
            progressRing_Duality.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            var backRing_Dual = new FSprite("Futile_White");
            backRing_Dual.scale = 5f;
            backRing_Dual.color = new Color(0.3f, 0.3f, 0.3f);
            backRing_Dual.alpha = 1f;
            backRing_Dual.SetPosition(progressRing_Duality.GetPosition());
            backRing_Dual.shader = this.manager.rainWorld.Shaders["HoldButtonCircle"];
            pages[0].Container.AddChild(backRing_Dual);
            pages[0].Container.AddChild(progressRing_Negative);
            pages[0].Container.AddChild(progressRing_Positive);
            pages[0].Container.AddChild(progressRing_Duality);

            progress_Negative = new FLabel(Custom.GetFont(), (progressRing_Negative.alpha * 100f).ToString() + "%");
            progress_Negative.color = progressRing_Negative.color;
            progress_Negative.SetPosition(progressRing_Negative.GetPosition());
            progress_Negative.scale = 1.5f;
            progress_Positive = new FLabel(Custom.GetFont(), (progressRing_Positive.alpha * 100f).ToString() + "%");
            progress_Positive.color = progressRing_Positive.color;
            progress_Positive.SetPosition(progressRing_Positive.GetPosition());
            progress_Positive.scale = 1.5f;
            progress_Duality = new FLabel(Custom.GetFont(), (progressRing_Duality.alpha * 100f).ToString() + "%");
            progress_Duality.color = progressRing_Duality.color;
            progress_Duality.SetPosition(progressRing_Duality.GetPosition());
            progress_Duality.scale = 1.5f;
            pages[0].Container.AddChild(progress_Negative);
            pages[0].Container.AddChild(progress_Positive);
            pages[0].Container.AddChild(progress_Duality);

            //卡牌阅览界面背景和边框
            darkSprite_Upper = new FSprite("pixel", true);
            darkSprite_Upper.scale = 1400f;
            darkSprite_Upper.color = Color.black;
            darkSprite_Upper.SetPosition(new Vector2(683, 393));
            darkSprite_Upper.alpha = 0.6f;
            pages[0].Container.AddChild(darkSprite_Upper);

            leftFlipButton = new BigArrowButton(this, pages[0], "LEFTFLIP", new Vector2(200f, 120f), 3);
            pages[0].subObjects.Add(leftFlipButton);

            rightFlipButton = new BigArrowButton(this, pages[0], "RIGHTFLIP", new Vector2(1100f, 120f), 1);
            pages[0].subObjects.Add(rightFlipButton);

            backButton = new SimpleButton(this, pages[0], Translate("BACK"), "BACK", new Vector2(80f, 30f), new Vector2(110f, 30f));
            pages[0].subObjects.Add(backButton);
            backButton.Container.RemoveFromContainer();
            this.cursorContainer.AddChild(backButton.Container);
            backButton.nextSelectable[0] = backButton;
            backButton.nextSelectable[2] = backButton;


            blurSprite = new FSprite("pixel");
            blurSprite.scaleX = 0.1f;
            blurSprite.SetPosition(CardpediaStatics.leftBlurSpritePos);
            blurSprite.shader = manager.rainWorld.Shaders["UIBlur"];
            pages[0].Container.AddChild(blurSprite);

            //大标题
            titleShadow_Cardpedia = new FSprite("buffassets/illustrations/TitleShadow_Cardpedia");
            titleShadow_Cardpedia.alpha = 0.8f;
            titleShadow_Cardpedia.scale = 0.6f;
            titleShadow_Cardpedia.SetPosition(new Vector2(683, 443));
            pages[0].Container.AddChild(titleShadow_Cardpedia);
            titleFlat_Cardpedia = new FSprite("buffassets/illustrations/TitleFlat_Cardpedia");
            titleFlat_Cardpedia.scale = 0.6f;
            titleFlat_Cardpedia.SetPosition(new Vector2(683, 443));
            titleFlat_Cardpedia.shader = manager.rainWorld.Shaders["MenuText"];
            pages[0].Container.AddChild(titleFlat_Cardpedia);

            sheetPage = new CardSheetPage(this, pages[0], Vector2.zero);
            pages[0].subObjects.Add(sheetPage);

            Instance = this;
            inited = true;
        }

        public override void Update()
        {
            base.Update();
            if (!inited)
            {
                InitMenuElements();
            }
            else
            {
                if (!fullyLoaded)
                {
                    LoadManagers();
                }

                if (fullyLoaded && loadingSprite_Dark.alpha > 0f)
                {
                    loadingSprite_Dark.alpha -= 0.025f;
                    loadingSprite_Cards.alpha -= 0.025f;
                    loadingSprite_Main.alpha -= 0.025f;
                }

                if (!fullyLoaded) return;

                //TestButtonPressed();
                Test_UIAnimations();
                lastAlpha = blurSprite.alpha;
                lastScale = new Vector2(blurSprite.scaleX, blurSprite.scaleY);
                lastTitlePos = SetTitlePos;
                if (BrowsingCards)
                {
                    SetAlpha = Mathf.Cos(0.5f * Mathf.PI * switchCount);
                }
                else
                {
                    SetAlpha = -Mathf.Sin(0.5f * Mathf.PI * switchCount) + 1;
                }
                SetScale = Vector2.Lerp(new Vector2(220f, 360f), new Vector2(260, 480), SetAlpha);
                SetTitlePos = Vector2.Lerp(new Vector2(683, 443), new Vector2(683, 563), SetAlpha);

                //textBoxManager.Update();
                //cardSheetManager.Update();
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (fullyLoaded)
            {
                darkSprite_Upper.alpha = 0.85f * Mathf.Lerp(lastAlpha, SetAlpha, timeStacker);
                blurSprite.alpha = Mathf.Lerp(lastAlpha, SetAlpha, timeStacker);
                //Vector2 vec = Vector2.Lerp(lastScale, SetScale, timeStacker);
                blurSprite.scaleX = CardpediaStatics.narrowBlurSpriteScale.x;
                blurSprite.scaleY = CardpediaStatics.narrowBlurSpriteScale.y;
                titleShadow_Cardpedia.SetPosition(Vector2.Lerp(lastTitlePos, SetTitlePos, timeStacker));
                titleFlat_Cardpedia.SetPosition(Vector2.Lerp(lastTitlePos, SetTitlePos, timeStacker));

                //backButton.pos = Vector2.Lerp(backButton.lastPos, Vector2.Lerp(new Vector2(100f, 30f), new Vector2(100f, -50f),SetAlpha),timeStacker);
                leftFlipButton.pos.y = Mathf.Lerp(leftFlipButton.lastPos.y, Mathf.Lerp(-300f, 120f, SetAlpha), timeStacker);
                rightFlipButton.pos.y = Mathf.Lerp(leftFlipButton.lastPos.y, Mathf.Lerp(-300f, 120f, SetAlpha), timeStacker);

                //textBoxManager.Draw(timeStacker);
                //cardSheetManager.GrafUpdate(timeStacker);
                configManager.GrafUpdate(timeStacker);
            }
        }

        public void LoadManagers()
        {
            //textBoxManager = new TextBoxManager(this);
            //pages[0].Container.AddChild(textBoxManager.Container);
            //textBoxLoaded = true;

            configManager = new ConfigManager(this);
            loadingSprite_UI.alpha = 0f;

            loadingSprite_Cards.alpha = 1f;
            //cardSheetManager = new CardSheetManager(this, BuffType.Negative);
            //cardSheetLoaded = true;

            fullyLoaded = true;
        }

        public void TestButtonPressed()
        {
            /*
            if (Input.GetKey(KeyCode.Y))
            {
                if (!BrowsingCards)
                {
                    BrowsingCards = true;
                    currentType = BuffType.Negative;
                }
            }
            else if (Input.GetKey(KeyCode.U))
            {
                if (!BrowsingCards)
                {
                    BrowsingCards = true;
                    currentType = BuffType.Duality;
                }
            }
            else if (Input.GetKey(KeyCode.I))
            {
                if (!BrowsingCards)
                {
                    BrowsingCards = true;
                    currentType = BuffType.Positive;
                }
            }
            
            if (Input.GetKey(KeyCode.O))
            {
                if (BrowsingCards)
                {
                    BrowsingCards = false;
                }
            }
            */
        }

        public void Test_UIAnimations()
        {
            if (!BrowsingCards)
            {
                if (switchCount < 1) switchCount += 0.0625f;
            }
            else
            {
                if (switchCount > 0) switchCount -= 0.0625f;
            }

        }

        public override void Singal(MenuObject sender, string message)
        {
            if (!fullyLoaded) return;
            if (message == "BACK")
            {
                if (!BrowsingCards)
                {
                    //for (int i = 0; i < textBoxManager.textBoxes.Count; i++)
                    //{
                    //    ScruffyPool.RecycleRenderer(textBoxManager.textBoxes[i].pediaTextRenderer);
                    //    textBoxManager.textBoxes[i].pediaTextRenderer = null;
                    //}

                    //ScruffyPool.RecycleRenderer(textBoxManager.titleBox.pediaTextRenderer);
                    //textBoxManager.titleBox.pediaTextRenderer = null;

                    //for (int j = 0; j < 3; j++)
                    //{
                    //    for (int k = 0; k < cardSheetManager.pediaCardSheets[j].cards.Count; k++)
                    //    {
                    //        cardSheetManager.pediaCardSheets[j].cards[k].Destroy();
                    //    }

                    //}

                    OnExit();
                }
                else
                {
                    BrowsingCards = false;
                    sheetPage.Show = false;
                }
            }
            else if (message == "LEFTFLIP")
            {
                sheetPage.SwitchPage(-1);
                //for (int i = 0; i < 3; i++)
                //{
                //    if (cardSheetManager.pediaCardSheets[i].sheetBuffType != textBoxManager.currentType) continue;

                //    if (cardSheetManager.pediaCardSheets[i].flipCounter < 0.05f)
                //    {
                //        if (cardSheetManager.pediaCardSheets[i].sheetPage > 0)
                //        {
                //            cardSheetManager.pediaCardSheets[i].sheetPage--;
                //            cardSheetManager.pediaCardSheets[i].flipCounter = 1f;
                //        }
                //    }
                //}
            }
            else if (message == "RIGHTFLIP")
            {
                //for (int i = 0; i < 3; i++)
                //{
                //    if (cardSheetManager.pediaCardSheets[i].sheetPage + 1 < cardSheetManager.pediaCardSheets[i].maxPage && cardSheetManager.pediaCardSheets[i].flipCounter < 0.05f)
                //    {
                //        cardSheetManager.pediaCardSheets[i].sheetPage++;
                //        cardSheetManager.pediaCardSheets[i].flipCounter = 1f;
                //    }
                //}
                sheetPage.SwitchPage(1);
            }
        }

        public void OnExit()
        {
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            PlaySound(SoundID.MENU_Switch_Page_Out);
        }


        public bool GetChecked(CheckBox checkBox)
        {
            return false;
        }

        public void SetChecked(CheckBox box, bool c)
        {

        }
    }

    internal static class CardpediaStatics
    {
        public static float tinyGap = 4f;
        public static float smallGap = 10f;
        public static float cosmeticRectHeight = 40f;
        public static float infoDisplay_BigRectHeight = 100f;

        public static Vector2 leftBlurSpritePos = new Vector2(200f, 483f);
        public static Vector2 rightBlurSpritePos = new Vector2(1150f, 483f);
        public static Vector2 narrowBlurSpriteScale = new Vector2(260f, 480f);

        public static Vector2 infoDisplayWindowScale = new Vector2(670f, 480f);
        public static Vector2 infoDisplayWindowPos = new Vector2(leftBlurSpritePos.x + narrowBlurSpriteScale.x / 2f + smallGap, leftBlurSpritePos.y - narrowBlurSpriteScale.y / 2f);

        public static Vector2 displayCardTexturePos = (new Vector2(203, 503));

        public static int sheetNumPerPage = 8;

        public static Color negativeColor = new Color(0.6f, 0f, 0.05f);
        public static Color positiveColor = new Color(0f, 0.6f, 0.4f);
        public static Color dualityColor = new Color(0.5f, 0.5f, 0.5f);

        public static Color pediaUILightGrey = new Color(0.6f, 0.6f, 0.6f);
        public static Color pediaUIDarkGrey = new Color(0.15f, 0.15f, 0.15f);


        public static float dropBox_dropButtonHeight = 30f;

        public static float slider_sliderSpan = 60f;
        public static float slider_lineHeight = 4f;
        public static float slider_sliderRectWidth = 8f;
        public static float slider_sliderRectHeight = 20f;

        public static float chainBox_cosmeticRectHeight = 30f;
    }
}
