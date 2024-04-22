using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RWCustom;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using static System.Net.Mime.MediaTypeNames;
using Kittehface.Framework20;
using RandomBuff.Core.Entry;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI.Component;

namespace RandomBuff.Core.Buff
{
    /// <summary>
    /// Buff静态数据
    ///
    /// 对外接口
    /// </summary>
    public partial class BuffStaticData
    {
        //必须存在的项
        public BuffID BuffID { get; private set; } = null;
        public bool Triggerable { get; private set; } = false;
        public string FaceName { get; private set; } = null;
        public Color Color { get; private set; } = Color.white;
        public BuffType BuffType { get; private set; } = BuffType.Positive;
        public BuffProperty BuffProperty { get; private set; } = BuffProperty.Normal;
        public bool Stackable { get; private set; } = false;
        public bool NeedUnlocked { get; private set; } = false;

        public int MaxCycleCount { get; private set; } = -1;
        public bool Countable => MaxCycleCount != -1;

        public Dictionary<string, object> ExtProperty { get; private set; } = new ();


        //帮助方法
        internal Texture GetFaceTexture()
        {
            return Futile.atlasManager.GetAtlasWithName(FaceName).texture;
        }

        internal Texture GetBackTexture()
        {
            if (BuffType == BuffType.Positive)
                return CardBasicAssets.MoonBack;
            else if (BuffType == BuffType.Duality)
                return CardBasicAssets.SlugBack;
            else
                return CardBasicAssets.FPBack;
        }

        internal (InGameTranslator.LanguageID id, CardInfo info) GetCardInfo(InGameTranslator.LanguageID languageID)
        {
            if(CardInfos.TryGetValue(languageID, out CardInfo cardInfo))
                return (languageID, cardInfo);
            return (CardInfos.First().Key, CardInfos.First().Value);
        }
    }


    /// <summary>
    /// Buff静态数据
    /// </summary>
    public partial class BuffStaticData
    {
        private BuffStaticData()
        {
        }

        internal class CardInfo
        {
            public string BuffName { get; internal set; } = null;
            public string Description { get; internal set; } = null;
        }

        /// <summary>
        /// 尝试读取staticData
        /// 失败则返回false newData = null
        /// </summary>
        /// <param name="jsonFile"></param>
        /// <param name="dirPath">当前目录相对地址</param>
        /// <param name="newData"></param>
        /// <returns></returns>
        public static bool TryLoadStaticData(FileInfo jsonFile, string dirPath,out BuffStaticData newData)
        {
            string loadState = "";
            if (string.IsNullOrEmpty(dirPath))
            {
                BuffPlugin.Log($"load failed at path {dirPath}");
                newData = null;
                return false;
            }
            BuffPlugin.Log($"try load static data at {dirPath}");

            try
            {
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(jsonFile.FullName));
                newData = new BuffStaticData();

                if (rawData.ContainsKey("BuffID"))
                {

                    if (!ExtEnumBase.TryParse(typeof(BuffID), (string)rawData[loadState = "BuffID"], true, out var re))
                    {
                        BuffPlugin.LogFatal("Can't find registered BuffID, Maybe forgot to add codes?");
                        return false;
                    }
                    newData.BuffID = (BuffID)re;


                }
                else
                {
                    BuffPlugin.LogWarning("OutDate Format At ID, Now BuffID");
                    if (!ExtEnumBase.TryParse(typeof(BuffID), (string)rawData[loadState = "ID"], true, out var re))
                    {
                        BuffPlugin.LogFatal("Can't find registered BuffID, Maybe forgot to add codes?");
                        return false;
                    }
                    newData.BuffID = (BuffID)re;

                }



                if (BuffRegister.GetBuffType(newData.BuffID) == null)
                {
                    BuffPlugin.LogFatal("Can't find BuffDataType, Maybe forgot to call BuffRegister.RegisterBuff() ?");
                    return false;
                }

                if (rawData.ContainsKey(loadState = "FaceName"))
                {
                    newData.FaceName = $"{dirPath.Substring(1)}/{rawData[loadState]}";
                    try
                    {
                        Futile.atlasManager.LoadImage(newData.FaceName);
                    }
                    catch (FutileException _)
                    {
                        BuffPlugin.LogError($"Card image not found at :{newData.FaceName}");
                    }
                }
                else
                {
                    newData.FaceName = "Futile_White";
                    BuffPlugin.LogWarning($"Can't find faceName At {jsonFile.Name}");
                }

                if (rawData.ContainsKey(loadState = "Triggerable"))
                {
                    if (rawData[loadState] is bool)
                        newData.Triggerable = (bool)rawData[loadState];
                    else
                        newData.Triggerable = bool.Parse(rawData[loadState] as string);
                }


                if (rawData.ContainsKey(loadState = "Stackable"))
                {
                    if (rawData[loadState] is bool)
                        newData.Stackable = (bool)rawData[loadState];
                    else
                        newData.Stackable = bool.Parse(rawData[loadState] as string);
                }

                if (rawData.ContainsKey(loadState = "BuffType"))
                    newData.BuffType = (BuffType)Enum.Parse(typeof(BuffType),(string)rawData[loadState]);

                if (rawData.ContainsKey(loadState = "BuffProperty"))
                    newData.BuffProperty = (BuffProperty)Enum.Parse(typeof(BuffProperty), (string)rawData[loadState]);


                if (rawData.ContainsKey(loadState = "Color"))
                {
                    if (((string)rawData[loadState])[0] == '#')
                        newData.Color = Custom.hexToColor(((string)rawData[loadState]).Substring(1));
                    else
                        newData.Color = Custom.hexToColor((string)rawData[loadState]);
                }

                if (rawData.ContainsKey(loadState = "NeedUnlocked"))
                {
                    newData.NeedUnlocked = (bool)rawData[loadState];
                }

                bool hasMutli = false;
                foreach (var language in ExtEnumBase.GetNames(typeof(InGameTranslator.LanguageID)))
                {
                    if (rawData.ContainsKey(language))
                    {
                        JObject lanObj = (JObject)rawData[language];
                        CardInfo info = new CardInfo();

                        if (lanObj.TryGetValue(loadState = "BuffName", out var obj))
                            info.BuffName = (string)obj;
                        else
                        {
                            info.BuffName = newData.BuffID.value;
                            BuffPlugin.LogWarning($"Can't find BuffName At {jsonFile.Name}:{language}");
                        }


                        if (lanObj.TryGetValue(loadState = "Description", out var obj1))
                            info.Description = (string)obj1;

                        newData.CardInfos.Add(new InGameTranslator.LanguageID(language),info);
                        hasMutli = true;
                    }
                }

                if (!hasMutli)
                {
                    CardInfo info = new CardInfo();
                    if (rawData.TryGetValue(loadState = "BuffName", out var obj))
                        info.BuffName = (string)obj;
                    else
                    {
                        info.BuffName = newData.BuffID.value;
                        BuffPlugin.LogWarning($"Can't find BuffName At {jsonFile.Name}:Default");
                    }
                    if (rawData.TryGetValue(loadState = "Description", out var obj1))
                        info.Description = (string)obj1;
                    newData.CardInfos.Add(InGameTranslator.LanguageID.English, info);
                }

                if (rawData.ContainsKey("Custom"))
                {
                    var customObject = (JArray)rawData["Custom"];
                    foreach (var data in customObject)
                    {
                        if (data is not JObject param)
                        {
                            BuffPlugin.LogWarning($"Error JToken at Custom:{data} At {jsonFile.Name}");
                            continue;
                        }
                        newData.customParameterNames.Add((string)param.GetValue(loadState = "PropertyName"),
                            (string)param.GetValue(loadState = "DisplayName"));

                        newData.customParameterDefaultValues.Add((string)param.GetValue(loadState = "PropertyName"),string.Empty);
                        //BuffPlugin.LogDebug($"Load {newData.BuffID} CustomParameter :{(string)param.GetValue(loadState = "PropertyName")}");
                    }
                }

                if (newData.CardInfos.Count == 0)
                {
                    BuffPlugin.LogError($"BuffName not found at {jsonFile.Name}!");
                    newData.CardInfos.Add(InGameTranslator.LanguageID.English,new CardInfo(){BuffName = newData.BuffID.value});
                }

                var rdata = (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(newData.BuffID));
                GetCustomStaticBuffData(rdata, newData, rawData);
                //BuffPlugin.LogDebug(newData.ToDebugString());
                return true;
            }
            catch (Exception e)
            {
                if(loadState != "")
                    BuffPlugin.LogError($"Load property failed: {loadState}! at {jsonFile.Name}");
                else
                    BuffPlugin.LogError($"Load json file failed! at {jsonFile.Name}");
                BuffPlugin.LogException(e);
                ExceptionTracker.TrackException(e, loadState == "" ? $"Load json file failed! at {jsonFile.Name}" : $"Load property failed: {loadState}! at {jsonFile.Name}");

                newData = null;
                return false;
            }

        }

        internal static void GetCustomStaticBuffData(BuffData data, BuffStaticData staticData, Dictionary<string,object> customArgs)
        {
            if (data is CountableBuffData countable)
            {
                staticData.MaxCycleCount = countable.MaxCycleCount;
                BuffPlugin.LogDebug($"{staticData.BuffID},{staticData.MaxCycleCount}");
            }
        }

        /// <summary>
        /// 测试用输出
        /// </summary>
        /// <returns></returns>
        internal string ToDebugString()
        {
            StringBuilder builder = new();
            builder.AppendLine();
            builder.AppendLine("----------------------");
            builder.AppendLine($"ID : {BuffID}");
            builder.AppendLine($"Triggerable : {Triggerable}");
            builder.AppendLine($"Stackable : {Stackable}");
            builder.AppendLine($"FaceName : {FaceName}");
            builder.AppendLine($"Color : {Color}");
            builder.AppendLine($"BuffType : {BuffType}");
            builder.AppendLine($"BuffProperty : {BuffProperty}");
            builder.AppendLine("Infos :");
            foreach (var info in CardInfos)
            {
                builder.AppendLine($"---{info.Key}:");
                builder.AppendLine($"---BuffName: {info.Value.BuffName}");
                builder.AppendLine($"---Description: {info.Value.Description}");
            }
            builder.AppendLine("Custom Parameters : ");
            foreach (var info in customParameterNames)
            {
                builder.AppendLine($"------");
                builder.AppendLine($"---PropertyName: {info.Key}");
                builder.AppendLine($"---DisplayName: {info.Value}");
            }
            return builder.ToString();
        }


        internal Dictionary<InGameTranslator.LanguageID, CardInfo> CardInfos { get; } = new();

        internal Dictionary<string, string> customParameterNames = new();
        internal Dictionary<string, string> customParameterDefaultValues = new();


    }


}
