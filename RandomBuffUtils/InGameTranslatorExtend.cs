using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RWCustom;

namespace RandomBuffUtils
{
    public static class InGameTranslatorExtend
    {

        public static void AddLoadFolder(string folder)
        {
            if (!Directory.Exists(AssetManager.ResolveDirectory(folder)))
            {
                BuffUtils.LogException("Translator Extend",
                new DirectoryNotFoundException($"can't find folder at {folder}"));
            }

            //if (folder.Last() != '\\' && folder.Last() != '/')
            //    folder += '/';

            ExtendShortStringPaths.Add(folder);
            if (hasLoaded)
            {
                LoadShortStrings(Custom.rainWorld.inGameTranslator, folder);
                BuffUtils.Log("Translator Extend", $"Instantly load at {folder}");
            }
        }
        internal static void OnModsInit()
        {
            On.InGameTranslator.LoadShortStrings += InGameTranslator_LoadShortStrings;
        }

        private static void InGameTranslator_LoadShortStrings(On.InGameTranslator.orig_LoadShortStrings orig, InGameTranslator self)
        {
            orig(self);
            foreach (var path in ExtendShortStringPaths)
                LoadShortStrings(self, path);
            
            hasLoaded = true;
        }

        private static void LoadShortStrings(InGameTranslator self, string path)
        {
            var filePath = $"{path}/{LocalizationTranslator.LangShort(self.currentLanguage)}.txt";
            if (!File.Exists(AssetManager.ResolveFilePath(filePath)))
                filePath = $"{path}/{LocalizationTranslator.LangShort(InGameTranslator.LanguageID.English)}.txt";

            if (!File.Exists(AssetManager.ResolveFilePath(filePath)))
            {
                BuffUtils.LogWarning("Translator Extend", $"can't find language file at {path}, current language:{self.currentLanguage}");
                return;
            }

            string text = File.ReadAllText(AssetManager.ResolveFilePath(filePath), Encoding.UTF8);
            if (text[0] == '1')
            {
                text = Custom.xorEncrypt(text, 12467);
            }
            else if (text[0] == '0')
            {
                text = text.Remove(0, 1);
            }
            string[] array = Regex.Split(text, "\r\n");
            for (int j = 0; j < array.Length; j++)
            {
                if (array[j].Contains("///"))
                    array[j] = array[j].Split('/')[0].TrimEnd();

                string[] array2 = array[j].Split('|');
                if (array2.Length >= 2 && !string.IsNullOrEmpty(array2[1]))
                {
                    self.shortStrings[array2[0]] = array2[1];
                    //BuffUtils.Log("Translator Extend", $"load {array2[0]},{array2[1]}");

                }
            }
            BuffUtils.Log("Translator Extend", $"load at {filePath}");

        }

        private static bool hasLoaded = false;
        private static readonly HashSet<string> ExtendShortStringPaths = new ();
    }
}
