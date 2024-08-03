using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Steamworks;
using UnityEngine;

namespace RandomBuffUtils
{
    public static class SteamHelper
    {
        public delegate void ReceiveAvatarCallBack(Texture2D avatarTex);

        internal static void InitCallBack()
        {
            imageLoadedCallBack = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
            personaStateChangeCallBack = Callback<PersonaStateChange_t>.Create(OnPersonaStateChanged);
        }


        public static void RequestUserAvatar(ulong steamID, [NotNull] ReceiveAvatarCallBack callBack)
        {
            if (CallBackMaps.ContainsKey(steamID))
                return;
            if (!SteamFriends.RequestUserInformation(new CSteamID(steamID), false))
            {
                var index = SteamFriends.GetLargeFriendAvatar(new CSteamID(steamID));
                if (index != -1)
                {
                    callBack.Invoke(GetSteamImageAsTexture2D(index));
                    return;
                }
            }
            CallBackMaps.Add(steamID, callBack);
        }

        private static void OnPersonaStateChanged(PersonaStateChange_t param)
        {
            if (CallBackMaps.TryGetValue(param.m_ulSteamID, out var callBack))
            {
                var index = SteamFriends.GetLargeFriendAvatar(new CSteamID(param.m_ulSteamID));
                if (index != -1)
                {
                    callBack.Invoke(GetSteamImageAsTexture2D(index));
                    CallBackMaps.Remove(param.m_ulSteamID);
                }

            }
        }

        private static void OnImageLoaded(AvatarImageLoaded_t param)
        {
            if (CallBackMaps.TryGetValue(param.m_steamID.m_SteamID,out var callBack))
            {
                callBack.Invoke(GetSteamImageAsTexture2D(SteamFriends.GetLargeFriendAvatar(param.m_steamID)));
                CallBackMaps.Remove(param.m_steamID.m_SteamID);
            }
        }




    
        private static Texture2D GetSteamImageAsTexture2D(int iImage)
        {
            Texture2D ret = null;
        
            bool bIsValid = SteamUtils.GetImageSize(iImage, out var imageWidth, out var imageHeight);

            if (bIsValid)
            {
                byte[] Image = new byte[imageWidth * imageHeight * 4];
                bIsValid = SteamUtils.GetImageRGBA(iImage, Image, (int)(imageWidth * imageHeight * 4));
                if (bIsValid)
                {
                    ret = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false, true);
                    ret.LoadRawTextureData(Image);
                    ret.Apply();
                }
            }

            return ret;
        }

        private static Callback<AvatarImageLoaded_t> imageLoadedCallBack;

        private static Callback<PersonaStateChange_t> personaStateChangeCallBack;

        private static readonly Dictionary<ulong, ReceiveAvatarCallBack> CallBackMaps = new();

    }
}
