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

        //private static void Room_Loaded1(MonoMod.Cil.ILContext il)
        //{
        //    ILCursor c1 = new ILCursor(il);
        //    ILCursor c2 = new ILCursor(il);
        //    ILLabel skipLabel = null;

        //    if (c1.TryGotoNext(MoveType.After,
        //        (i) => i.MatchLdsfld<RoomSettings.RoomEffect.Type>("SSMusic"),
        //        (i) => i.Match(OpCodes.Call),
        //        (i) => i.Match(OpCodes.Brfalse_S)))
        //    {
        //        c2.Index = c1.Index;

        //        c2.GotoNext(MoveType.After, (i) => i.MatchCall<Room>("AddObject"));
        //        skipLabel = c2.MarkLabel();

        //        c1.Emit(OpCodes.Ldarg_0);
        //        c1.Emit(OpCodes.Ldloca_S, 16);
        //        c1.EmitDelegate<Action<Room, RoomSettings.RoomEffect>>((room, effect) =>
        //        {
        //            room.AddObject(new FaCaiMusicTrigger(effect));
        //        });
        //    }
        //}
    }

    //internal class FaCaiMusicTrigger : UpdatableAndDeletable
    //{
    //    public FaCaiMusicTrigger(RoomSettings.RoomEffect effect)
    //    {
    //        this.effect = effect;
    //    }

    //    public override void Update(bool eu)
    //    {
    //        base.Update(eu);
    //        if (ModManager.MMF)
    //        {
    //            if (room.game.cameras[0].room != null && room.game.cameras[0].room == room)
    //            {
    //                Trigger();
    //                return;
    //            }
    //        }
    //        else
    //        {
    //            for (int i = 0; i < room.game.Players.Count; i++)
    //            {
    //                if (room.game.Players[i].realizedCreature != null && room.game.Players[i].realizedCreature.room == room)
    //                {
    //                    Trigger();
    //                    return;
    //                }
    //            }
    //        }
    //    }

    //    private void Trigger()
    //    {
    //        if (room.game.manager.musicPlayer == null || room.gravity > 0f)
    //        {
    //            return;
    //        }
    //        if (room.game.manager.musicPlayer.song == null || !(this.room.game.manager.musicPlayer.song is SSSong))
    //        {
    //            room.game.manager.musicPlayer.RequestSSSong();
    //            return;
    //        }
    //        if ((room.game.manager.musicPlayer.song as SSSong).setVolume != null)
    //        {
    //            (room.game.manager.musicPlayer.song as SSSong).setVolume = new float?(Mathf.Max((room.game.manager.musicPlayer.song as SSSong).setVolume.Value, this.effect.amount));
    //            return;
    //        }
    //        (room.game.manager.musicPlayer.song as SSSong).setVolume = new float?(effect.amount);
    //    }

    //    public void RequestFaCaiSong(MusicPlayer player)
    //    {
    //        if (player.song != null && player.song is SSSong)
    //        {
    //            return;
    //        }
    //        if (player.nextSong != null && player.nextSong is SSSong)
    //        {
    //            return;
    //        }
    //        if (!player.manager.rainWorld.setup.playMusic)
    //        {
    //            return;
    //        }
    //        Song song;
    //        if (ModManager.MSC && player.manager.currentMainLoop is RainWorldGame && (player.manager.currentMainLoop as RainWorldGame).IsStorySession && ((player.manager.currentMainLoop as RainWorldGame).world.region.name == "DM" || (player.manager.currentMainLoop as RainWorldGame).world.region.name == "MS" || (player.manager.currentMainLoop as RainWorldGame).world.region.name == "SL"))
    //        {
    //            song = new SSSong(player, "RW_95 - Reflection of the Moon");
    //        }
    //        else if (ModManager.MSC && player.manager.currentMainLoop is RainWorldGame && (player.manager.currentMainLoop as RainWorldGame).IsStorySession && (player.manager.currentMainLoop as RainWorldGame).world.region.name == "HR")
    //        {
    //            song = new SSSong(player, "RW_86 - The Cycle");
    //        }
    //        else
    //        {
    //            song = new SSSong(player);
    //        }
    //        if (player.song == null)
    //        {
    //            player.song = song;
    //            player.song.playWhenReady = true;
    //            return;
    //        }
    //        player.nextSong = song;
    //        player.nextSong.playWhenReady = false;
    //    }

    //    public RoomSettings.RoomEffect effect;
    //}

    //internal class FaCaiSong : Song
    //{
    //    public FaCaiSong(MusicPlayer musicPlayer, string name, MusicPlayer.MusicContext context) : base(musicPlayer, name, context)
    //    {
    //    }
    //}
}
