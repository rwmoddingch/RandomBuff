using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using Menu;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

namespace RandomBuff.Core.BuffMenu
{
    //未完成 不太清楚做成什么样比较好
    internal class BuffGamePage : Page
    {
        public BuffGamePage(Menu.Menu menu, MenuObject owner, string name, int index) : base(menu, owner, name, index)
        {
        }
    }

    internal sealed class BuffGamePageContinue : BuffGamePage, IOwnAHUD
    {
        public BuffGamePageContinue(BuffGameMenu menu, MenuObject owner, BuffGameMenu.WawaSaveData data,SlugcatStats.Name name, int index) : 
            base(menu, owner, $"{name}_CONTINUE", index)
        {
            for (int i = 0; i < hudContainers.Length; i++)
            {
                hudContainers[i] = new FContainer();
                Container.AddChild(hudContainers[i]);
            }
            hud = new HUD.HUD(hudContainers, menu.manager.rainWorld, this);
            saveGameData = data;
            //hud.AddPart(new KarmaMeter(hud, hudContainers[1], 
            //    new IntVector2(saveGameData.karma, saveGameData.karmaCap), saveGameData.karmaReinforced));
            hud.AddPart(new FoodMeter(hud, SlugcatStats.SlugcatFoodMeter(name).x,
                SlugcatStats.SlugcatFoodMeter(name).y, null, 0));
            string text = string.Empty;
            if (saveGameData.shelterName is { Length: > 2 })
            {
                text = Region.GetRegionFullName(saveGameData.shelterName.Substring(0, 2), name);
                if (text.Length > 0)
                {
                    text = string.Concat(new [] 
                        { menu.Translate(text), " - ", menu.Translate("Cycle"), " ", saveGameData.cycle.ToString() });
                }

            }
            regionLabel = new MenuLabel(menu, this, text, new Vector2(-1000f, 484 - 249f), new Vector2(200f, 30f), true, null);
            regionLabel.label.alignment = FLabelAlignment.Center;
            subObjects.Add(regionLabel);
        }

       
        public HUD.HUD.OwnerType GetOwnerType() => HUD.HUD.OwnerType.CharacterSelect;
        

        public void PlayHUDSound(SoundID soundID) => menu.PlaySound(soundID);
        

        public void FoodCountDownDone()
        {
        }

        public int CurrentFood => saveGameData.food;


        public Player.InputPackage MapInput => default;
        public bool RevealMap => false;
        public Vector2 MapOwnerInRoomPosition => default;
        public bool MapDiscoveryActive => false;
        public int MapOwnerRoom => -1;


       
        private BuffGameMenu GameMenu => menu as BuffGameMenu;

        private readonly FContainer[] hudContainers = new FContainer[2];
        private HUD.HUD hud;
        private BuffGameMenu.WawaSaveData saveGameData;
        private MenuLabel regionLabel;
    }
}
