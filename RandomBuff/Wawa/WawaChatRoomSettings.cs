using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RandomBuff.Wawa
{
    internal static class WawaChatRoomSettings
    {
        public static Dictionary<string, PlacedObject> placedTokens = new ();
        public static string settingFilePath;

        public static void LoadSetting()
        {
            settingFilePath = AssetManager.ResolveFilePath("buffassets/wawa/wawachatsroomsettings.txt");

            var s = File.ReadAllLines(settingFilePath);
            for (int i = 0; i < s.Length; i++)
            {
                string[] a = s[i].Trim().Split('|');
                string room = a[0];
                string[] array = Regex.Split(a[1], "><");
                string text = array[0];
                if (text == null || !(text == ""))
                {
                    var newPlaced = new PlacedObject(PlacedObject.Type.None, null);
                    newPlaced.FromString(array);
                    placedTokens.Add(room, newPlaced);
                    BuffUtils.Log("WawaChatRoomSettings", $"Load : {room}, {newPlaced.type}, {newPlaced.data.GetType()}, {newPlaced.data.ToString()}");
                }
            }
        }

        public static void Save()
        {
            StringBuilder sb = new StringBuilder();
            foreach(var pair in placedTokens)
            {
                sb.AppendLine($"{pair.Key}|{pair.Value.ToString()}");
            }
            File.WriteAllText(settingFilePath, sb.ToString());
        }

        public static void DevSetTokenPlaced(Room room, PlacedObject placedObject)
        {
            if (placedTokens.ContainsKey(room.abstractRoom.name))
            {
                placedTokens[room.abstractRoom.name] = placedObject;
            }
            else
            {
                placedTokens.Add(room.abstractRoom.name, placedObject);
            }
        }

        public static void DevRemoveToken(Room room)
        {
            if(placedTokens.ContainsKey(room.abstractRoom.name))
                placedTokens.Remove(room.abstractRoom.name);
        }

        public static PlacedObject GetToken(Room room)
        {
            if(placedTokens.ContainsKey(room.abstractRoom.name))
                return placedTokens[room.abstractRoom.name];
            return null;
        }
    }
}
