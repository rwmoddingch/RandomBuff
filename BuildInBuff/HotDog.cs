using BepInEx;
using HotDogGains.Duality;
using Microsoft.CodeAnalysis;
using RandomBuff.Core.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TemplateGains;
using UnityEngine;

namespace HotDogGains
{
    public static class HotDog
    {
        public static string ModId = "HotDogBuff";
        
    }

    internal class Enums
    {
        public class Sounds
        {
            public static void RegisterValues()
            {
                Enums.Sounds.sisyphus = new SoundID("sisyphus",true);
                Enums.Sounds.Snail_Missile = new SoundID("snail_missile", true);
            }
            public static void UnregisterValues()
            {
                SoundID sisyphus = Enums.Sounds.sisyphus;
                if (sisyphus!=null)
                {
                    sisyphus.Unregister();
                }
            }
            public static SoundID sisyphus = new SoundID("sisyphus", true);
            public static SoundID Snail_Missile = new SoundID("snail_missile", true);
        }
    }
}
