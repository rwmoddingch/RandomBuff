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
        public static PlacedObject.Type WawaTokenPlacedType = new PlacedObject.Type("WawaToken", true);

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
                self.AddObject(new WawaToken.WawaToken(self, token.pos + tokenData.handlePos, token.pos, tokenData.convFile));
            }
        }

        private static void Player_Jump(On.Player.orig_Jump orig, Player self)
        {
            orig.Invoke(self);
            foreach(var u in self.room.updateList)
            {
                if (u is WawaToken.WawaToken)
                    return;
            }

            
            self.room.AddObject(new WawaToken.WawaToken(self.room, self.firstChunk.pos + Vector2.right * 120f + Vector2.up * 40f, self.firstChunk.pos + Vector2.right * 120f, 0));
        }
    }
}
