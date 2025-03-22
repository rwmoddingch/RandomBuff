using DevInterface;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa.ChatConv
{
    internal class WawaChatLoader
    {
        internal static readonly string[] devs = new string[]
        {
            "harvie", "qed", "pkuyo", "seron", "cutefatalis","shanka"
        };

        internal static readonly Dictionary<string, string> devExpressionName = new Dictionary<string, string>();
        internal static readonly List<WawaChatConv> convs = new List<WawaChatConv>();
        internal static readonly Dictionary<InGameTranslator.LanguageID, Dictionary<string, string>> devNames = new Dictionary<InGameTranslator.LanguageID, Dictionary<string, string>>();

        public static void LoadExpressions()
        {
            foreach(var dev in devs)
            {
                devExpressionName.Add(dev, $"expression_{dev}_");

                int i = 0;
                while(true)
                {
                    string expPath = AssetManager.ResolveFilePath($"buffassets/illustrations/expressions/{dev}_{i}.png");
                    //BuffUtils.Log("WawaChatLoader", $"Try load expression : {expPath} - {File.Exists(expPath)}");
                    if (!File.Exists(expPath))
                        expPath = AssetManager.ResolveFilePath($"buffassets/illustrations/expressions/{dev}_{i}.jpg");
                    if (!File.Exists(expPath))
                        break;

                    Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                    AssetManager.SafeWWWLoadTexture(ref texture, "file:///" + expPath, false, true);
                    Futile.atlasManager.LoadAtlasFromTexture(devExpressionName[dev] + i.ToString(), texture, false);
                    BuffUtils.Log("WawaChatLoader", $"Load expression : {devExpressionName[dev] + i.ToString()} - {expPath}");
                    i++;
                }
            }
        }

        public static void LoadDevNames()
        {
            string filePath = AssetManager.ResolveFilePath($"buffassets/wawa/devnames.txt");
            var lines = File.ReadAllLines(filePath);

            InGameTranslator.LanguageID lastLangID = InGameTranslator.LanguageID.English;
            foreach (var line in lines)
            {
                if(string.IsNullOrEmpty(line)) 
                    continue;

                if (line.StartsWith("LANG"))
                {
                    lastLangID = new InGameTranslator.LanguageID(line.Split('_').Last());
                    devNames.Add(lastLangID, new Dictionary<string, string>());
                }
                else
                {
                    var splited = line.Split(':');
                    devNames[lastLangID].Add(splited[0].Trim(), splited[1].Trim());
                }
            }
        }

        public static void LoadChatFiles()
        {
            string chatDir = AssetManager.ResolveDirectory("buffassets/wawa/chats");
            int fileIndex = 0;
            while(File.Exists(chatDir + $"/{fileIndex}.txt"))
            {
                WawaChatConv wawaChatConv = new WawaChatConv();
                string lastDev = string.Empty;
                InGameTranslator.LanguageID lastLangID = InGameTranslator.LanguageID.English;
                string[] lines = File.ReadAllLines(chatDir + $"/{fileIndex}.txt");

                foreach(var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    string[] splited = line.Split(':');
                    if(splited.Length == 1)
                    {
                        string[] splited2 = line.Split('_');

                        if (splited2[0] == "DEV")
                            lastDev = splited2[1];
                        else if(splited2[0] == "LANG")
                        {
                            lastLangID = new InGameTranslator.LanguageID(splited2[1]);
                            wawaChatConv.convs.Add(lastLangID, new List<string>());
                            wawaChatConv.devs.Add(lastLangID, new List<string>());
                            wawaChatConv.expression.Add(lastLangID, new List<int>());
                        }
                        continue;
                    }
                    else if(splited.Length == 2)
                    {
                        wawaChatConv.devs[lastLangID].Add(lastDev);
                        wawaChatConv.expression[lastLangID].Add(int.Parse(splited[0].Trim()));
                        wawaChatConv.convs[lastLangID].Add(splited[1].Trim());
                    }
                    else
                    {
                        wawaChatConv.devs[lastLangID].Add(splited[0].Trim());
                        wawaChatConv.expression[lastLangID].Add(int.Parse(splited[1].Trim()));
                        wawaChatConv.convs[lastLangID].Add(splited[2].Trim());
                    }
     

                    BuffUtils.Log("WawaChatLoader", $"Load Conv {lastLangID} {wawaChatConv.devs.Last()}, {wawaChatConv.expression.Last()}, {wawaChatConv.convs[lastLangID].Last()}");
                }
                convs.Add(wawaChatConv);
                fileIndex++;
            }
        }

        public static string GetElement(string dev, int index)
        {
            return devExpressionName[dev] + index.ToString();
        }
    }
}
