using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Wawa.ChatConv
{
    internal class WawaChatConv
    {
        public Dictionary<InGameTranslator.LanguageID, List<string>> convs = new Dictionary<InGameTranslator.LanguageID, List<string>>();
        public Dictionary<InGameTranslator.LanguageID, List<int>> expression = new Dictionary<InGameTranslator.LanguageID, List<int>>();
        public Dictionary<InGameTranslator.LanguageID, List<string>> devs = new Dictionary<InGameTranslator.LanguageID, List<string>>();

        public string GetConv(InGameTranslator.LanguageID languageID, int index)
        {
            if (convs.ContainsKey(languageID))
                return convs[languageID][index];
            else
                return convs[InGameTranslator.LanguageID.English][index];
        }

        public string GetDev(InGameTranslator.LanguageID languageID, int index)
        {
            if (!WawaChatLoader.devNames.ContainsKey(languageID))
                languageID = InGameTranslator.LanguageID.English;
            return WawaChatLoader.devNames[languageID][devs[languageID][index]];
        }

        public string GetOrigDev(InGameTranslator.LanguageID languageID, int index)
        {
            if (!WawaChatLoader.devNames.ContainsKey(languageID))
                languageID = InGameTranslator.LanguageID.English;
            return devs[languageID][index];
        }

        public int GetExpression(InGameTranslator.LanguageID languageID, int index)
        {
            if (!expression.ContainsKey(languageID))
                languageID = InGameTranslator.LanguageID.English;
            return expression[languageID][index];
        }

        public int GetConvCount(InGameTranslator.LanguageID languageID)
        {
            if (!convs.ContainsKey(languageID))
                languageID = InGameTranslator.LanguageID.English;
            return convs[languageID].Count;
        }
    } 
}
