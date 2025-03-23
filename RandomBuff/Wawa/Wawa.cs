using RandomBuff.Core.Option;
using RandomBuff.Wawa.ChatConv;
using RandomBuff.Wawa.WawaDevInterface;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa
{
    internal static class Wawa
    {
        public static PlacedObject.Type WawaTokenPlacedType = new ("WawaToken", true);

        public static bool EnableDevChat => BuffOptionInterface.Instance.EnableDevChat.Value;
        public static void Init()
        {
            //On.Player.Jump += Player_Jump;
            On.Room.Loaded += Room_Loaded;
            WawaChatLoader.LoadExpressions();
            WawaChatLoader.LoadChatFiles();
            WawaChatLoader.LoadDevNames();
            WawaDevHooks.HooksOn();
            WawaChatRoomSettings.LoadSetting();
        }

        private static void Room_Loaded(On.Room.orig_Loaded orig, Room self)
        {
            orig.Invoke(self);
            PlacedObject token;
            if((token = WawaChatRoomSettings.GetToken(self)) != null && EnableDevChat)
            {
                var tokenData = token.data as WawaTokenData;
                self.AddObject(new WawaToken.WawaToken(self, token.pos + tokenData.handlePos,
                    token.pos, tokenData.convFile, WawaSaveData.hasRead.Contains(tokenData.convFile)));
            }
        }
    }
}
