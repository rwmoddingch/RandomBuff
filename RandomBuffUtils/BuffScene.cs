using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Music;
using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using RWCustom;
using UnityEngine;

namespace RandomBuffUtils
{
    public static class BuffScene
    {

        public class SlideShowData
        {
            public class SlideShowSceneData
            {
                public string sceneId;
                public float fadeIn;
                public float duringTime;
                public float fadeOut;
            }

            public SlideShowData(string sound, string processId,params SlideShowSceneData[] scenes)
            {
                this.scenes = new (scenes);
                this.sound = sound;
                this.processId = processId;
            }

            public SlideShowData(string sound, string processId = "DeathScreen")
            {
                scenes = new (scenes);
                this.sound = sound;
                this.processId = processId;
            }

            public List<SlideShowSceneData> scenes = new();

            public string sound;

            public string processId;

        }

        internal static void OnModsInit()
        {
            IL.Menu.SlideShow.ctor += SlideShow_ctor;
            On.Menu.MenuScene.BuildScene += MenuScene_BuildScene;
            On.Music.MusicPiece.SubTrack.Update += SubTrack_Update;

        }

        private static readonly string[] FileEnd = new[] { ".ogg", ".wav" };

        private static void SubTrack_Update(On.Music.MusicPiece.SubTrack.orig_Update orig, MusicPiece.SubTrack self)        
        {
            if (!self.readyToPlay)
            {
                foreach (var end in FileEnd)
                {
                    if (self.source.clip == null && self.trackName.StartsWith("BUFF_") && File.Exists(
                            AssetManager.ResolveFilePath(self.trackName.Replace("BUFF_", "") +
                                                         end)))
                    {
                        self.isStreamed = true;
                        self.source.clip = AssetManager.SafeWWWAudioClip(
                            "file://" + AssetManager.ResolveFilePath(self.trackName.Replace("BUFF_", "")+end),
                            false, true, end == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV); 
                        BuffUtils.Log(nameof(BuffScene),$"Load buff music at {self.trackName.Replace("BUFF_", "") + end}");
                    }
                }
            }

            try
            {
                orig(self);

            }          
            catch
            {
                BuffUtils.LogError(nameof(BuffScene),$"Null at {self.trackName}");
            }

        }

        public static void RegisterSlideShow(SlideShow.SlideShowID id, SlideShowData data)
        {
            if (SlideShowReg.TryGetValue(id, out _))
            {
                BuffUtils.LogError(nameof(BuffScene),$"Already contains SlideShow ID: {id}");
            }
            else
            {
                SlideShowReg.Add(id, data);
            }
        }

        public static void RegisterMenuScene(MenuScene.SceneID id, Action<MenuScene> action)
        {
            if (MenuSceneReg.TryGetValue(id, out _))
            {
                BuffUtils.LogError(nameof(BuffScene), $"Already contains Scene ID: {id}");
            }
            else
            {
                MenuSceneReg.Add(id, action);
            }
        }


        private static void MenuScene_BuildScene(On.Menu.MenuScene.orig_BuildScene orig, MenuScene self)
        {
            orig(self);
            if(self.sceneID != null && MenuSceneReg.TryGetValue(self.sceneID,out var func))
                func?.Invoke(self);
        }

        private static void SlideShow_ctor(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchStfld<SlideShow>("playList"));
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate(TryBuildSlideShow);
            }
            catch (Exception e)
            {
               BuffUtils.LogException(nameof(BuffScene),e);
            }
        }

        private static void CreateSlideScene(this List<SlideShow.Scene> self, string id, ref float startTime, float fadeIn, float duringTime, float fadeOut)
        {
            self.Add(new SlideShow.Scene(new MenuScene.SceneID(id), startTime, startTime + fadeIn,
                startTime + fadeIn + duringTime));
            startTime += fadeIn + duringTime + fadeOut;
        }

        private static void TryBuildSlideShow(this SlideShow self, ProcessManager manager, SlideShow.SlideShowID slideShowID)
        {
            if (SlideShowReg.TryGetValue(self.slideShowID, out var data))
            {
                if (manager.musicPlayer != null)
                {
                    self.waitForMusic = data.sound;
                    self.stall = true;
                    manager.musicPlayer.MenuRequestsSong(self.waitForMusic, 1.5f, 40f);
                }
                float time = 0;
                self.playList.CreateSlideScene(MenuScene.SceneID.Empty.value, ref time, 0f, 0f, 20 / 100f);
                foreach (var scene in data.scenes)
                    self.playList.CreateSlideScene(scene.sceneId, ref time, scene.fadeIn, scene.duringTime,
                        scene.fadeOut);
                self.processAfterSlideShow = new ProcessManager.ProcessID(data.processId);
            }
        }

        private static readonly Dictionary<SlideShow.SlideShowID, SlideShowData> SlideShowReg = new();
        private static readonly Dictionary<MenuScene.SceneID, Action<MenuScene>> MenuSceneReg = new();
    }
}
