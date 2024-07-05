using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using AssetBundles;
using JetBrains.Annotations;
using RWCustom;
using UnityEngine;

namespace RandomBuffUtils
{
    public static class BuffSounds
    {
        internal static void OnEnable()
        {
            On.SoundLoader.VariationsForSound += SoundLoader_VariationsForSound;
            On.SoundLoader.CheckIfFileExistsAsExternal += SoundLoader_CheckIfFileExistsAsExternal;
            On.SoundLoader.LoadSounds += SoundLoader_LoadSounds;
        }

        private static void SoundLoader_LoadSounds(On.SoundLoader.orig_LoadSounds orig, SoundLoader self)
        {
            orig(self);
            if (self.assetBundlesLoaded)
            {
                foreach (var item in soundDics.Values)
                    self.LoadSoundInternal(item.id, item.soundPath, item.groupData, item.datas);
                BuffUtils.Log(nameof(BuffSounds), $"Reload {soundDics.Count} sounds");
            }
        }

        private static bool SoundLoader_CheckIfFileExistsAsExternal(On.SoundLoader.orig_CheckIfFileExistsAsExternal orig, SoundLoader self, string name)
        {
            if (name.StartsWith("BUFF-"))
                return true;
            return orig(self, name);
        }

        private static int SoundLoader_VariationsForSound(On.SoundLoader.orig_VariationsForSound orig, SoundLoader self, string name)
        {
            if (name.StartsWith("BUFF-"))
            {
                int count = 1;
                for (int i = 0; i < 100; i++)
                    count += File.Exists(AssetManager.ResolveFilePath(name.Replace("BUFF-", "") + $"_{i}.wav")) ? 1 : 0;
                return count;
            }
            return orig(self, name);
        }

        public static void LoadSound([NotNull] SoundID id, string soundPath, BuffSoundGroupData groupData,
            params BuffSoundData[] datas)
        {
            var loader = Custom.rainWorld.processManager.soundLoader;
            if (soundDics.ContainsKey(id))
            {
                BuffUtils.LogError(nameof(BuffSounds), $"Already contains same Sound ID: {id}");
                return;
            }


            soundDics.Add(id,new BuffSoundPack(id,soundPath,groupData,datas));
        }



        internal class BuffSoundPack
        {
            public BuffSoundPack(SoundID id, string soundPath, BuffSoundGroupData groupData, BuffSoundData[] datas)
            {
                this.id = id;
                this.soundPath = soundPath;
                this.groupData = groupData;
                this.datas = datas;
            }


            public SoundID id;
            public string soundPath;
            public BuffSoundGroupData groupData;
            public BuffSoundData[] datas;
        }
        
        private static void LoadSoundInternal(this SoundLoader loader, [NotNull] SoundID id,string soundPath, BuffSoundGroupData groupData, params BuffSoundData[] datas)
        {
            var index = id.Index;
            loader.soundTriggers[index] = (SoundLoader.SoundTrigger)FormatterServices.GetSafeUninitializedObject(typeof(SoundLoader.SoundTrigger));
            loader.soundTriggers[index].maxVol = groupData.maxVol;
            loader.soundTriggers[index].minVol = groupData.minVol;
            loader.soundTriggers[index].maxPitch = groupData.maxPitch;
            loader.soundTriggers[index].minPitch = groupData.minPitch;
            loader.soundTriggers[index].GROUPVOL = groupData.groupVol;
            loader.soundTriggers[index].range = groupData.rangeFac;
            loader.soundTriggers[index].dopplerFactor = groupData.dopplerFac;
            loader.soundTriggers[index].silentChance = groupData.silentChance;
            loader.soundTriggers[index].DontLog = groupData.dontLog;
            loader.soundTriggers[index].PlayAll = groupData.playAll;
            loader.soundTriggers[index].soundID = id;
            loader.workingTriggers[index] = true;
           var sounds = loader.soundTriggers[index].sounds =
                new SoundLoader.SoundPlayInstruction[datas.Length];
            for(int i =0;i<datas.Length;i++)
            {
                sounds[i] = (SoundLoader.SoundPlayInstruction)FormatterServices.GetSafeUninitializedObject(
                        typeof(SoundLoader.SoundPlayInstruction));
                sounds[i].audioClip = loader.FindIndex($"BUFF-{soundPath}/{datas[i].soundName}");
                sounds[i].maxPitch = datas[i].maxPitch;
                sounds[i].minPitch = datas[i].minPitch;
                sounds[i].maxVol = datas[i].maxVol;
                sounds[i].minVol = datas[i].minVol;
                if (sounds[i].audioClip >= loader.unityAudio.Length)
                {
                    Array.Resize(ref loader.audioClipNames, loader.audioClipNames.Length + 1);
                    loader.audioClipNames[sounds[i].audioClip] = $"BUFF-{soundPath}/{datas[i].soundName}";

                    Array.Resize(ref loader.soundVariations, loader.soundVariations.Length + 1);
                    loader.soundVariations[sounds[i].audioClip] =
                        loader.VariationsForSound(loader.audioClipNames[sounds[i].audioClip]);

                    Array.Resize(ref loader.unityAudio,  loader.unityAudio.Length + 1);
                    loader.unityAudio[sounds[i].audioClip] = new AudioClip[loader.soundVariations[sounds[i].audioClip]];

                    Array.Resize(ref loader.unityAudioLoaders, loader.unityAudioLoaders.Length + 1);
                    loader.unityAudioLoaders[sounds[i].audioClip] = new AssetBundleLoadAssetOperation[sounds[i].audioClip];

                    Array.Resize(ref loader.unityAudioCached, loader.unityAudioCached.Length + 1);
                    loader.unityAudioCached[sounds[i].audioClip] = false;

                    Array.Resize(ref loader.audioClipsThroughUnity, loader.unityAudioCached.Length + 1);
                    loader.audioClipsThroughUnity[sounds[i].audioClip] = false;

                    Array.Resize(ref loader.externalAudio, loader.externalAudio.Length + 1);
                    loader.externalAudio[sounds[i].audioClip] = new AudioClip[loader.soundVariations[sounds[i].audioClip]];

                    LoadSingleClips(ref loader.externalAudio[sounds[i].audioClip], soundPath, datas[i].soundName);
                    BuffUtils.Log(nameof(BuffSounds), $"Loaded audio clip, Name:{datas[i].soundName}, Count:{loader.externalAudio[sounds[i].audioClip].Length}, ID:{sounds[i].audioClip}");
                }

            }

            BuffUtils.Log(nameof(BuffSounds), $"Loaded sound, ID:{id}, Clips Count:{datas.Length}, folder:{soundPath}");


        }


        internal static void LoadSingleClips(ref AudioClip[] clips, string path,string name)
        {
            for (int i = 0; i < clips.Length; i++)
            {
                var fileName = AssetManager.ResolveFilePath(path + Path.DirectorySeparatorChar + name + (i == 0 ? "" : $"_{i - 1}") + ".wav");
                if (File.Exists(fileName))
                {
                    WWW www = new WWW("file://" + fileName);
                    AudioClip myAudioClip = www.GetAudioClip();
                    while (myAudioClip.loadState != AudioDataLoadState.Loaded &&
                           myAudioClip.loadState != AudioDataLoadState.Failed) /*Empty loop*/ ;
                    AudioClip audioClip = www.GetAudioClip(false);
                    audioClip.name = name;
                    clips[i] = audioClip;
                }
                else
                {
                    BuffUtils.LogError(nameof(BuffSounds),$"can't find file at :{fileName}");
                }
            }
        }


        public static int FindIndex(this SoundLoader loader, string name)
        {
            var index = loader.audioClipNames.IndexOf(name);
            return index == -1 ? loader.audioClipNames.Length : index;
        }

        private static readonly Dictionary<SoundID,BuffSoundPack> soundDics = new();
    }




    public class BuffSoundData
    {
        public BuffSoundData(string soundName, float maxVol = 1, float minVol = 1, float maxPitch = 1,
            float minPitch = 1)
        {
            this.soundName = soundName;
            this.maxVol = maxVol;
            this.minVol = minVol;
            this.maxPitch = maxPitch;
            this.minPitch = minPitch;
        }

        public string soundName;
        public float maxVol;
        public float minVol;
        public float maxPitch;
        public float minPitch;
    }

    public class BuffSoundGroupData
    {
        public float groupVol = 1f;
        public float minVol = 1f;
        public float maxVol = 1f;
        public float minPitch = 1f;
        public float maxPitch = 1f;
        public float rangeFac = 1f;
        public float dopplerFac = 1f;
        public float silentChance = 0f;
        public bool playAll = false;
        public bool dontLog = false;


        public float Vol
        {
            set => minVol = maxVol = value;
        }

        public float Pitch
        {
            set => minPitch = maxPitch = value;
        }
    }

}
