using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace RandomBuff.Wawa;


public class WawaSaveData
{
    public static void OnModsInit()
    {
        On.PlayerProgression.MiscProgressionData.FromString += MiscProgressionData_FromString;
        On.PlayerProgression.MiscProgressionData.ToString += MiscProgressionData_ToString;
    }

    private static string MiscProgressionData_ToString(On.PlayerProgression.MiscProgressionData.orig_ToString orig, PlayerProgression.MiscProgressionData self)
    {
        var re = orig(self);
        re += "BUFFWAWA<mpdB>" + JsonConvert.SerializeObject(hasRead) + "<mpdA>";
        return re;
    }

    private static void MiscProgressionData_FromString(On.PlayerProgression.MiscProgressionData.orig_FromString orig, PlayerProgression.MiscProgressionData self, string s)
    {
        orig(self, s);
        for (int i = self.unrecognizedSaveStrings.Count - 1; i >= 0; i--)
        {
            var split = Regex.Split(self.unrecognizedSaveStrings[i], "<mpdB>");
            if (split.Length != 2)
                continue;
            if (split[0] == "BUFFWAWA")
            {
                hasRead = JsonConvert.DeserializeObject<HashSet<int>>(split[1]);
                self.unrecognizedSaveStrings.RemoveAt(i);
                break;
            }

        }
    }

    [JsonProperty] 
    public static HashSet<int> hasRead = new();
}