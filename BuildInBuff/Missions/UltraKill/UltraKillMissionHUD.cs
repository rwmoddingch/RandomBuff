using DevInterface;
using HUD;
using Menu.Remix.MixedUI;
using RandomBuff;
using RandomBuff.Render.UI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;

namespace BuiltinBuffs.Missions.UltraKill
{
    internal class UltraKillMissionHUD : HudPart
    {
        static Vector2 panelSize = new Vector2(200f, 300f);
        static float bloodBarWidth = 60f;

        static string[] levelTitles = new string[] { "ESTRUCTIVE", "HAOTIC", "RUTAL", "NARCHIC", "UPREME", "ADISTIC", "HITSTORM", "" };
        static string[] coloredLevelTItles = new string[] { "D", "C","B", "A", "S", "SS", "SSS", "ULTRAKILL" };

        static Color[] color0 = new Color[] { Custom.hexToColor("242FA9"), Custom.hexToColor("128C1C"), Custom.hexToColor("90620C"), Custom.hexToColor("893700"), Custom.hexToColor("870A57"), Custom.hexToColor("870A57"), Custom.hexToColor("870A57"), Custom.hexToColor("FF3500") };

        static Color[] color1 = new Color[] { Custom.hexToColor("004EE8"), Custom.hexToColor("34CB1B"), Custom.hexToColor("C89E06"), Custom.hexToColor("C05D02"),  Custom.hexToColor("B20A4A"), Custom.hexToColor("B20A4A"), Custom.hexToColor("B20A4A"), Custom.hexToColor("FF6E00") };

        //Blue Green Yellow Orange Red Red Red UltraKill
        static Color[] color2 = new Color[] { Custom.hexToColor("1D7FFF"), Custom.hexToColor("6DFF1D"), Custom.hexToColor("FFEB1D"), Custom.hexToColor("FF9000s"), Custom.hexToColor("FF003E"), Custom.hexToColor("FF003E"), Custom.hexToColor("FF003E"), Custom.hexToColor("FFC200") };
        static Color[][] colors = new Color[][] { color0, color1, color2 };

        Vector2 drawPosTopLeft;

        int currentLevel = -1;

        internal RainWorldGame game;

        FSprite panelBackground;

        FLabel[] titleLabel = new FLabel[3];
        FLabel[] coloredTitleLabel = new FLabel[3];

        FSprite scoreBar;

        FLabel[] eventSign = new FLabel[8];
        FLabel[] events = new FLabel[8];

        FSprite[] bloodBar = new FSprite[3];
        FSprite[] bloodFlashSprite = new FSprite[3];

        internal ObjTrackerProj objTrackerProj;

        //draw pos
        Vector2 drawPos;
        Vector2 lastDrawPos;

        Vector2[] lastVels;
        Vector2 momentum;

        //titleMovemenet
        float titlePop;
        float lastTitlePop;
        Vector2 coloredTitleBias;
        Vector2 titleBias;
        Vector2 titleAnchor;

        //scoreBar
        public float percentage;
        float lastPercentage;
        Vector2 scoreBarAnchor;

        //event
        Vector2 eventAnchor;
        int deleteEventLabelTimer;
        List<UltraKillEvent> ultraKillEvents = new List<UltraKillEvent>();

        //bloodEnergy
        Vector2 bloodAnchor;
        public float blood;
        float lastBlood;
        float bloodFlash;
        float blankSpace;


        public UltraKillMissionHUD(RainWorldGame game, HUD.HUD hud) : base(hud)
        {
            this.game = game;
            objTrackerProj = new ObjTrackerProj(this);

            InitSprites();

            drawPosTopLeft = game.rainWorld.options.ScreenSize - new Vector2(panelSize.x + 30f, 100f);

            drawPos = lastDrawPos = drawPosTopLeft;
            titleAnchor = new Vector2(panelSize.x / 2f, -40f);
            scoreBarAnchor = new Vector2(0f, -60f);
            eventAnchor = new Vector2(0f, -80f);
            bloodAnchor = new Vector2(0f, -80f - 7 * 30f);
            blankSpace = (panelSize.x - bloodBarWidth * 3f) / 5f;

            lastVels =  new Vector2[game.Players.Count];

            for(int i = 0; i < game.Players.Count; i++)
            {
                if (game.Players[i].realizedCreature == null)
                    continue;

                lastVels[i] = game.Players[i].realizedCreature.firstChunk.vel;
            }
            UpdateTitle(0, false);
        }

        void InitSprites()
        {
            panelBackground = new FSprite("pixel", true)
            {
                color = Color.black,
                alpha = 0.2f,
                anchorX = 0f,
                anchorY = 1f,
                scaleX = panelSize.x,
                scaleY = panelSize.y,
            };
            hud.fContainers[0].AddChild(panelBackground);

            for(int i = 0;i < 3; i++)
            {
                titleLabel[i] = new FLabel(Custom.GetDisplayFont(), "")
                {
                    color = Color.Lerp(Color.white, Color.black, (2 - i) / 5f),//除以五保证不会真的变成黑色
                    alpha = (i + 1f) / 3f,
                    scaleX = 1.5f,
                    scaleY = 1.5f
                };
                hud.fContainers[0].AddChild(titleLabel[i]);
                coloredTitleLabel[i] = new FLabel(Custom.GetDisplayFont(), "")
                {
                    scaleX = 1.5f,
                    scaleY = 2.2f
                };
                hud.fContainers[0].AddChild(coloredTitleLabel[i]);
            }

            scoreBar = new FSprite("pixel", true)
            {
                color = Color.gray,
                alpha = 0.5f,
                anchorX = 0f,
                anchorY = 1f,
                scaleY = 15f,
                scaleX = 0f
            };
            hud.fContainers[0].AddChild(scoreBar);

            for (int i = 0; i < events.Length; i++)
            {
                eventSign[i] = new FLabel(Custom.GetDisplayFont(), "+")
                {
                    anchorX = 0f,
                    anchorY = 1f,
                    isVisible = false
                };
                hud.fContainers[0].AddChild(eventSign[i]);

                events[i] = new FLabel(Custom.GetDisplayFont(), "")
                {
                    anchorX = 0f,
                    anchorY = 1f,
                    isVisible = false
                };
                hud.fContainers[0].AddChild(events[i]);
            }

            for(int i = 0;i < 3; i++)
            {
                bloodBar[i] = new FSprite("pixel", true)
                {
                    anchorX = 0f,
                    anchorY = 1f,
                    scaleX = 0f,
                    scaleY = 10f,
                    color = color2[6]
                };
                hud.fContainers[0].AddChild(bloodBar[i]);

                bloodFlashSprite[i] = new FSprite(BuffUIAssets.OnePixelGradient20_2, true)
                {
                    anchorX = 0f,
                    scaleX = 0f,
                    color = color2[6],
                    shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"]
                };
                hud.fContainers[0].AddChild(bloodFlashSprite[i]);
            }
        }

        public void UpdateTitle(int newLevel, bool pop)
        {
            if (currentLevel == newLevel)
                return;

            for(int i = 0;i < 3; i++)
            {
                titleLabel[i].text = levelTitles[newLevel];
                
                coloredTitleLabel[i].text = coloredLevelTItles[newLevel];
                coloredTitleLabel[i].color = colors[i][newLevel];
            }

            float totalWidth = LabelTest.GetWidth(levelTitles[newLevel] + coloredLevelTItles[newLevel], true) * titleLabel[0].scaleX;
            float coloredWidth = coloredTitleLabel[0].textRect.width * coloredTitleLabel[0].scaleX;
            float titleWidth = titleLabel[0].textRect.width * titleLabel[0].scale;

            coloredTitleBias = new Vector2(-totalWidth / 2f + coloredWidth / 2f, 7f);
            titleBias = new Vector2(totalWidth / 2f - titleWidth / 2f, 0f);

            currentLevel = newLevel;
            if (pop)
                titlePop = 1f;
        }

        public void AddEvent(UltraKillEvent ultraKillEvent)
        {
            ultraKillEvents.Insert(0, ultraKillEvent);
            UpdateEventLabels();
        }

        public void UpdateEventLabels(int refreshCounter = 160)
        {
            List<UltraKillEvent> notClosedEvents = new List<UltraKillEvent>();
            List<UltraKillEvent> closedEvents = new List<UltraKillEvent>();
            foreach(var e in  ultraKillEvents)
            {
                if(!e.EventClose)
                    notClosedEvents.Add(e);
                else
                    closedEvents.Add(e);
            }
            int p1 = 0, p2 = 0;
            for (int i = 0; i < events.Length; i++)
            {
                if (p1 < notClosedEvents.Count)
                {
                    eventSign[i].isVisible = true;
                    events[i].isVisible = true;
                    events[i].text = notClosedEvents[p1].DisplayText();
                    events[i].color = GetColorOfType(notClosedEvents[p1].ColorType());
                    p1++;
                }
                else if(p2 < closedEvents.Count)
                {
                    eventSign[i].isVisible = true;
                    events[i].isVisible = true;
                    events[i].text = closedEvents[p2].DisplayText();
                    events[i].color = GetColorOfType(closedEvents[p2].ColorType());
                    p2++;
                }
                else
                {
                    eventSign[i].isVisible = false;
                    events[i].isVisible = false;
                }
            }

            for(int i = p2; i < closedEvents.Count; i++)
            {
                closedEvents[i].allowForDeletion = true;
                ultraKillEvents.Remove(closedEvents[i]);
            }
            deleteEventLabelTimer = refreshCounter;
        }

        public void BloodFlash(float flash = 0.5f)
        {
            bloodFlash = Mathf.Max(bloodFlash, flash);
        }

        Color GetColorOfType(int type)
        {
            if (type == -1)
                return Color.gray;
            return color2[type];
        }

        public override void Update()
        {
            for (int i = 0; i < game.Players.Count; i++)
            {
                if (game.Players[i].realizedCreature == null)
                    continue;

                momentum = Vector2.Lerp(momentum, -(game.Players[i].realizedCreature.firstChunk.vel - lastVels[i]) * 10f, 0.1f);
                lastVels[i] = Vector2.Lerp(lastVels[i], game.Players[i].realizedCreature.firstChunk.vel, 0.15f);
            }
            momentum = Vector2.Lerp(momentum, Vector2.zero, 0.2f);
            momentum = Vector2.ClampMagnitude(momentum, 6f);

            lastTitlePop = titlePop;
            titlePop = Mathf.Lerp(titlePop, 0f, 0.15f);


            lastDrawPos = drawPos;
            drawPos = drawPosTopLeft + momentum;

            lastPercentage = percentage;

            if(deleteEventLabelTimer > 0)
            {
                deleteEventLabelTimer--;
                if(deleteEventLabelTimer == 0)
                {
                    for(int i = ultraKillEvents.Count - 1; i >= 0; i--)
                    {
                        if (ultraKillEvents[i].EventClose)
                        {
                            ultraKillEvents[i].allowForDeletion = true;
                            ultraKillEvents.RemoveAt(i);
                            UpdateEventLabels(10);
                            break;
                        }
                    }
                }
            }

            if(bloodFlash > 0f)
            {
                bloodFlash = Mathf.Max(0f, bloodFlash - 1 / 40f);
            }
            lastBlood = blood;

            objTrackerProj.Update();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            Vector2 smoothPos = Vector2.Lerp(lastDrawPos, drawPos, timeStacker);

            panelBackground.SetPosition(Vector2.Lerp(lastDrawPos, drawPos, timeStacker));
            float smoothPop = Mathf.Lerp(lastTitlePop, titlePop, timeStacker);

            //title
            for(int i = 0;i < 3; i++)
            {
                Vector2 pop = new Vector2(-2f * i - 9f * smoothPop * i, 2f * i + 9f * smoothPop * i);
                titleLabel[i].SetPosition(smoothPos + titleBias + pop + titleAnchor);
                coloredTitleLabel[i].SetPosition(smoothPos + coloredTitleBias + pop + titleAnchor);
            }

            //score
            float smoothPercentage = Mathf.Lerp(lastPercentage, percentage, timeStacker);
            scoreBar.SetPosition(smoothPos + scoreBarAnchor);
            scoreBar.scaleX = smoothPercentage * panelSize.x;

            //event
            for (int i = 0;i < events.Length; i++)
            {
                events[i].SetPosition(smoothPos + eventAnchor + new Vector2(30f, -20f * i));
                eventSign[i].SetPosition(smoothPos + eventAnchor + new Vector2(0f, -20f * i));
            }

            //blood
            float smoothBlood = Mathf.Lerp(lastBlood, blood, timeStacker);
            float smoothBloodFlash = Helper.EaseInOutCubic(bloodFlash);
            
            for (int i = 0; i < 3; i++)
            {
                Vector2 pos = smoothPos + bloodAnchor + new Vector2((i + 1) * blankSpace + i * bloodBarWidth, 0f);
                float width = Mathf.Clamp01(smoothBlood - i) * bloodBarWidth;
                bloodBar[i].SetPosition(pos);
                bloodBar[i].scaleX = width;
                bloodBar[i].color = Color.Lerp(Color.Lerp(color0[6], color2[6], Mathf.Clamp01(smoothBlood - i)), Color.white, Mathf.Pow(smoothBloodFlash, 2f)); ;

                bloodFlashSprite[i].SetPosition(pos + new Vector2(0f, -10f));
                bloodFlashSprite[i].scaleX = width;
                bloodFlashSprite[i].scaleY = Mathf.Lerp(1.2f, 2.3f, smoothBloodFlash);
                bloodFlashSprite[i].alpha = Mathf.Lerp(0.2f, 1f, smoothBloodFlash);
            }

            objTrackerProj.GrafUpdate(timeStacker);
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            objTrackerProj.Destroy();
        }
    }

    internal class ObjTrackerProj
    {
        UltraKillMissionHUD hud;
        List<ObjProj> objProjs = new List<ObjProj>();


        public ObjTrackerProj(UltraKillMissionHUD hud)
        {
            this.hud = hud;
        }

        public void AddObject(PhysicalObject physicalObject)
        {
            objProjs.Add(new ObjProj(this, physicalObject));
        }

        public void Update()
        {
            for(int i = objProjs.Count - 1; i >= 0 ; i--)
            {
                if(objProjs[i].slateForDeletion)
                {
                    objProjs.RemoveAt(i);
                    continue;
                }
                objProjs[i].Update();
            }
        }

        public void GrafUpdate(float timeStacker)
        {
            foreach(var  objProj in objProjs)
            {
                objProj.GrafUpdate(timeStacker);
            }
        }

        public void Destroy()
        {
            foreach (var objProj in objProjs)
            {
                objProj.Destroy();
            }
            objProjs.Clear();
        }

        class ObjProj
        {
            ObjTrackerProj proj;
            PhysicalObject trackedObj;
            Vector2 pos, lastPos;
            public bool slateForDeletion;

            FSprite arrow;

            public ObjProj(ObjTrackerProj proj, PhysicalObject trackedObj)
            {
                this.trackedObj = trackedObj;
                this.proj = proj;
                pos = lastPos = trackedObj.firstChunk.pos - proj.hud.game.cameras[0].pos + Vector2.up * 30f;

                proj.hud.game.cameras[0].ReturnFContainer("Foreground").AddChild(arrow = new FSprite("Big_Menu_Arrow", true)
                {
                    color = Color.red,
                    shader = proj.hud.game.rainWorld.Shaders["Hologram"],
                    rotation = 180f,
                    scale = 0.5f,
                    isVisible = false
                });
            }

            public void Update()
            {
                if (slateForDeletion)
                    return;
                if (trackedObj.slatedForDeletetion)
                {
                    Destroy();
                    return;
                }

                lastPos = pos;
                pos = trackedObj.firstChunk.pos - proj.hud.game.cameras[0].pos + Vector2.up * 30f;
            }

            public void GrafUpdate(float timeStacker)
            {
                if (slateForDeletion)
                    return;
                arrow.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker));
                arrow.isVisible = trackedObj.grabbedBy.Count == 0;
            }

            public void Destroy()
            {
                slateForDeletion = true;
                arrow.RemoveFromContainer();
                trackedObj = null;
            }
        }
    }
}
