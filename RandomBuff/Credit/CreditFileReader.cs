using RandomBuff.Core.Buff;
using RandomBuff.Core.SaveData;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RandomBuff.Credit
{
    internal class CreditFileReader
    {
        public readonly List<KeyValuePair<CreditStageType, CreditStageData>> creditStagesAndData = new List<KeyValuePair<CreditStageType, CreditStageData>>();

        public CreditFileReader()
        {
            string realPath = AssetManager.ResolveFilePath($"buffassets/credit_{Custom.rainWorld.inGameTranslator.currentLanguage}.txt");
            if(!File.Exists(realPath)) 
                realPath = AssetManager.ResolveFilePath($"buffassets/credit_{InGameTranslator.LanguageID.English}.txt");

            string[] lines = File.ReadAllLines(realPath);
            creditStagesAndData.Add(new KeyValuePair<CreditStageType, CreditStageData>(CreditStageType.Intro, new CreditStageData() { stageType = CreditStageType.Intro }));

            foreach(var line in lines)
            {
                if(line.StartsWith("//") || string.IsNullOrEmpty(line))
                    continue;

                var trimedLine = line.Trim();
                if (MatchAndReplaceMark(trimedLine, "<Stage>", out var result))
                {
                    CreditStageType creditStageType = new CreditStageType(result);
                    CreditStageData data = null;

                    if (creditStageType == CreditStageType.Coding)
                        data = new CodingStageData() { stageType = CreditStageType.Coding };
                    else if (creditStageType == CreditStageType.ArtWorks)
                        data = new ArtWorksStageData() { stageType = CreditStageType.ArtWorks };
                    else if(creditStageType == CreditStageType.PlayTest)
                        data = new PlayTestStageData() { stageType = CreditStageType.PlayTest };

                    creditStagesAndData.Add(new KeyValuePair<CreditStageType, CreditStageData>(creditStageType, data));
                }
                else if(MatchAndReplaceMark(trimedLine, "<Name>", out result))
                {
                    var type = creditStagesAndData.Last().Key;
                    var data = creditStagesAndData.Last().Value;
                    if(type == CreditStageType.Coding)
                    {
                        (data as CodingStageData).names.Add(result);
                        (data as CodingStageData).details.Add(string.Empty);
                    }
                    else if(type == CreditStageType.ArtWorks)
                    {
                        (data as ArtWorksStageData).names.Add(result);
                        (data as ArtWorksStageData).details.Add(string.Empty);
                        (data as ArtWorksStageData).buffIDs.Add(new BuffID[0]);
                    }
                    else if(type == CreditStageType.PlayTest)
                    {
                        (data as PlayTestStageData).names.Add(result);
                        (data as PlayTestStageData).details.Add(string.Empty);
                    }
                }
                else if(MatchAndReplaceMark(trimedLine, "<Detail>", out result))
                {
                    var type = creditStagesAndData.Last().Key;
                    var data = creditStagesAndData.Last().Value;
                    if (type == CreditStageType.Coding)
                    {
                        (data as CodingStageData).details[(data as CodingStageData).details.Count - 1] = result;
                    }
                    else if (type == CreditStageType.ArtWorks)
                    {
                        (data as ArtWorksStageData).details[(data as ArtWorksStageData).details.Count - 1] = result;
                    }
                    else if (type == CreditStageType.PlayTest)
                    {
                        (data as PlayTestStageData).details[(data as PlayTestStageData).details.Count - 1] = result;
                    }
                }
                else if(MatchAndReplaceMark(trimedLine, "<BuffIDs>", out result))
                {
                    string[] ids = result.Split(',');
                    List<BuffID> buffIDs = new List<BuffID>();
                    var data = creditStagesAndData.Last().Value as ArtWorksStageData;

                    foreach (string id in ids)
                    {
                        var newId = new BuffID(id.Trim());
                        if(BuffConfigManager.ContainsId(newId))
                            buffIDs.Add(newId);
                    }
                    data.buffIDs[data.buffIDs.Count - 1] = buffIDs.ToArray();
                }
            }
        }

        public bool MatchAndReplaceMark(string orig, string mark, out string result)
        {
            bool r = false;
            if(orig.Contains(mark))
            {
                r = true;
                result = Regex.Replace(orig, mark, "");
            }
            else
            {
                r = false;
                result = orig;
            }
            return r;
        }

        public class CreditStageData
        {
            public CreditStageType stageType;
        }

        public sealed class CodingStageData : CreditStageData
        {
            public List<string> names = new List<string>();
            public List<string> details = new List<string>();
        }

        public sealed class ArtWorksStageData : CreditStageData
        {
            public List<string> names = new List<string>();
            public List<string> details = new List<string>();
            public List<BuffID[]> buffIDs = new List<BuffID[]>();
        }

        public sealed class PlayTestStageData : CreditStageData
        {
            public List<string> names = new List<string>();
            public List<string> details = new List<string>();
        }
    }

    public class CreditStageType : ExtEnum<CreditStageType>
    {
        public static readonly CreditStageType Coding = new CreditStageType("Coding", true);
        public static readonly CreditStageType ArtWorks = new CreditStageType("ArtWorks", true);
        public static readonly CreditStageType PlayTest = new CreditStageType("PlayTest", true);
        public static readonly CreditStageType Intro = new CreditStageType("Intro", true);

        public CreditStageType(string value, bool register = false) : base(value, register)
        {
        }
    }
}
