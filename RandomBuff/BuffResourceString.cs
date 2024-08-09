using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RandomBuff.Core.SaveData;

namespace RandomBuff
{
    public static class BuffResourceString
    {
        public static Dictionary<string, string> engMapper = new();
        public static Dictionary<string, string> currentLangMapper = new();

        static bool engLoaded;

        internal static void Init()
        {
            On.InGameTranslator.LoadShortStrings += InGameTranslator_LoadShortStrings;
        }

        private static void InGameTranslator_LoadShortStrings(On.InGameTranslator.orig_LoadShortStrings orig, InGameTranslator self)
        {
            orig.Invoke(self);
            LoadResourceStrings(self.currentLanguage);
         
        }

        internal static void LoadResourceStrings(InGameTranslator.LanguageID languageID)
        {
            if (!engLoaded)
            {
                if (languageID != InGameTranslator.LanguageID.English)
                    LoadResourceStrings(InGameTranslator.LanguageID.English);
            }
            //BuffPlugin.Log($"Load resource string of : {languageID}");
            currentLangMapper.Clear();
            string suffix = languageID == InGameTranslator.LanguageID.English ? "" : $"_{languageID.value}";

            foreach(var mod in ModManager.ActiveMods)
            {
                string path = string.Concat(mod.path, Path.DirectorySeparatorChar, "buffassets", Path.DirectorySeparatorChar, "resourceStrings", suffix, ".txt");
                if(File.Exists(path))
                {
                    string[] lines = File.ReadAllLines(path);
                    for(int i = 0;i < lines.Length; i++)
                    {
                        if (string.IsNullOrEmpty(lines[i]) || lines[i].StartsWith("//"))
                            continue;

                        var splited = lines[i].Split('|');
                        if (splited.Length != 2)
                        {
                            BuffPlugin.LogError($"Resource string format error at line:{i}, {lines[i]}");
                        }
                        else
                        {


                            if (!currentLangMapper.ContainsKey(splited[0].Trim()))
                            {
                                currentLangMapper.Add(splited[0].Trim(), ReplaceLine(splited[1].Trim()));
                                if (!engLoaded && languageID == InGameTranslator.LanguageID.English)
                                    engMapper.Add(splited[0].Trim(), ReplaceLine(splited[1].Trim()));
                                //BuffPlugin.Log($"Load resource string : {splited[0].Trim()} | {splited[1].Trim()}, {languageID}");

                            }
                            else
                                BuffPlugin.LogError($"buff resource string already contains key:{splited[0].Trim()}");

             
                        }
                    }
                }
            }

            if(!engLoaded && languageID == InGameTranslator.LanguageID.English)
                engLoaded = true;
        }

        static string ReplaceLine(string orig)
        {
            return Regex.Replace(orig, "<LINE>", "\n");
        }

        public static bool TryGet(string orig, out string ret)
        {
            if (currentLangMapper.TryGetValue(orig,out ret))
                return true;

            if (engMapper.TryGetValue(orig, out ret))
                return true;
            ret = orig;
            return false;
        }

        public static string Get(string key, bool returnOrig = false)
        {
            if(currentLangMapper.ContainsKey(key))
                return currentLangMapper[key];

            if(engMapper.ContainsKey(key))
                return engMapper[key];

            if (returnOrig)
                return key;
            return $"ERROR!MISSING KEY {key}";
        }
    }
}
