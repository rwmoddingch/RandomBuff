using Music;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Duality
{
    internal class FaCaiBuff : Buff<FaCaiBuff, FaCaiBuffData>
    {
        public override BuffID ID => FaCaiBuffEntry.facaiID;
    }

    internal class FaCaiBuffData : BuffData
    {
        public override BuffID ID => FaCaiBuffEntry.facaiID;
    }

    internal class FaCaiBuffEntry : IBuffEntry
    {
        public static BuffID facaiID = new BuffID("FaCai", true);

        public static string FaCaiBGM => $"BUFF_{facaiID.GetStaticData().AssetPath}/music/NY_02 - FaCai";

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<FaCaiBuff, FaCaiBuffData, FaCaiBuffEntry>(facaiID);
        }

        public static void HookOn()
        {
            //IL.Room.Loaded += Room_Loaded1;
            On.Music.SSSong.ctor_MusicPlayer += SSSong_ctor_MusicPlayer;
        }

        private static void SSSong_ctor_MusicPlayer(On.Music.SSSong.orig_ctor_MusicPlayer orig, SSSong self, MusicPlayer musicPlayer)
        {
            orig.Invoke(self, musicPlayer);
            self.subTracks.Clear();
            self.subTracks.Add(new MusicPiece.SubTrack(self, 0, FaCaiBGM));
        }
    }
}
