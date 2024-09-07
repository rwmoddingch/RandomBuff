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
using RandomBuff.Core.SaveData;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI.ExceptionTracker;

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
        public bool AsPositive { get; private set; } = false;

        public BuffProperty BuffProperty { get; private set; } = BuffProperty.Normal;
        public bool Stackable { get; private set; } = false;
        public bool Hidden { get; private set; } = false;

        public int MaxCycleCount { get; private set; } = -1;
        public bool Countable { get; private set; } = false;
        public string AssetPath { get; private set; }
        public bool MultiLayerFace { get; private set;} = false;
        public int FaceLayer { get; private set; } = 1;
        public float MaxFaceDepth { get; private set; } = 1f;
        public Color FaceBackgroundColor { get; private set; } = Color.black;
        public HashSet<string> Conflict { get; private set; } = new();
        public HashSet<string> Tag { get; private set; } = new();

        public int MaxStackLayers { get; private set; } = int.MaxValue;

        public string AssemblyName => BuffConfigManager.GetAssemblyName(BuffID);

        internal BuffPluginInfo PluginInfo => BuffConfigManager.GetPluginInfo(BuffID);

        private bool tryLoad = false;


        internal void UnloadTexture()
        {
            if (FaceName != CardBasicAssets.MissingFace && Futile.atlasManager.DoesContainAtlas(FaceName))
                Futile.atlasManager.UnloadImage(FaceName);
            tryLoad = false;
        }

        //帮助方法
        internal Texture GetFaceTexture()
        {
            if (!tryLoad)
            {
                try
                {
                    if (!Futile.atlasManager.DoesContainAtlas(FaceName))
                        Futile.atlasManager.LoadImage(FaceName);
                }
                catch (FutileException _)
                {
                    BuffPlugin.LogError($"Card image not found at :{FaceName}");
                }
            }

            tryLoad = true;
            if (!Futile.atlasManager.DoesContainAtlas(FaceName))
            {
                BuffPlugin.LogWarning($"Can;t Get face for {BuffID} {FaceName}");
                return Futile.atlasManager.GetAtlasWithName(CardBasicAssets.MissingFace).texture;
            }
            var atlas = Futile.atlasManager.GetAtlasWithName(FaceName);
            //BuffPlugin.LogError($"Get face for {BuffID} {FaceName}");
            if (!atlas.isSingleImage)
            {
                BuffPlugin.LogError($"Get face for {BuffID} {FaceName}, but get nonsingleimage : {atlas.name}");
                return Futile.atlasManager.GetAtlasWithName(CardBasicAssets.MissingFace).texture;
            }
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

        /// <summary>
        /// 卡面信息
        /// </summary>
        public class CardInfo
        {

            internal CardInfo(string buffName, string description)
            {
                BuffName = buffName;
                Description = description;
            }
            internal CardInfo() { }

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

            Dictionary<string, object> rawData = null;
            try
            {
                rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(jsonFile.FullName));
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
                }
                else
                {
                    newData.FaceName = CardBasicAssets.MissingFace;
                    BuffPlugin.LogWarning($"Can't find faceName At {jsonFile.Name}");
                }

                if (rawData.ContainsKey(loadState = "Triggerable"))
                {
                    if (rawData[loadState] is bool)
                        newData.Triggerable = (bool)rawData[loadState];
                    else
                        newData.Triggerable = bool.Parse(rawData[loadState] as string);
                }

                if (rawData.ContainsKey(loadState = "MaxStackLayers"))
                    newData.MaxStackLayers = LoadAsInt(rawData[loadState]);

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

                if (rawData.ContainsKey(loadState = "AsPositive"))
                {
                    if (rawData[loadState] is bool)
                        newData.AsPositive = (bool)rawData[loadState];
                    else
                        newData.AsPositive = bool.Parse(rawData[loadState] as string);
                }

                if (rawData.ContainsKey(loadState = "Color"))
                {
                    if (((string)rawData[loadState])[0] == '#')
                        newData.Color = Custom.hexToColor(((string)rawData[loadState]).Substring(1));
                    else
                        newData.Color = Custom.hexToColor((string)rawData[loadState]);
                }

                if (rawData.ContainsKey(loadState = "Hidden"))
                {
                    newData.Hidden = (bool)rawData[loadState];
                }

                if (rawData.ContainsKey(loadState = "MultiLayerFace"))
                    if (rawData[loadState] is bool)
                        newData.MultiLayerFace = (bool)rawData[loadState];
                    else
                        newData.MultiLayerFace = bool.Parse(rawData[loadState] as string);

                if (rawData.ContainsKey(loadState = "FaceLayer"))
                    newData.FaceLayer = LoadAsInt(rawData[loadState]);

                if (rawData.ContainsKey(loadState = "MaxFaceDepth"))
                    newData.MaxFaceDepth = LoadAsFloat(rawData[loadState]);

                if (rawData.ContainsKey(loadState = "FaceBackgroundColor"))
                    newData.FaceBackgroundColor = Custom.hexToColor((string)rawData[loadState]);

                if (rawData.ContainsKey(loadState = "Tag"))
                {
                    JArray tagObj = (JArray)rawData[loadState];
                    foreach (var obj in tagObj)
                    {
                        newData.Tag.Add((string)obj);
                        BuffPlugin.LogDebug($"Tag:{obj}");

                    }
                }

                if (rawData.ContainsKey(loadState = "Conflict"))
                {
                    JArray conflictObj = (JArray)rawData[loadState];
                    foreach (var obj in conflictObj)
                    {
                        newData.Conflict.Add((string)obj);
                        BuffPlugin.LogDebug($"Conflict:{obj}");
                    }
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

                //if (rawData.ContainsKey("Custom"))
                //{
                //    var customObject = (JArray)rawData["Custom"];
                //    foreach (var data in customObject)
                //    {
                //        if (data is not JObject param)
                //        {
                //            BuffPlugin.LogWarning($"Error JToken at Custom:{data} At {jsonFile.Name}");
                //            continue;
                //        }
                //        newData.customParameterNames.Add((string)param.GetValue(loadState = "PropertyName"),
                //            (string)param.GetValue(loadState = "DisplayName"));

                //        newData.customParameterDefaultValues.Add((string)param.GetValue(loadState = "PropertyName"),string.Empty);
                //        //BuffPlugin.LogDebug($"Load {newData.BuffID} CustomParameter :{(string)param.GetValue(loadState = "PropertyName")}");
                //    }
                //}

                if (newData.CardInfos.Count == 0)
                {
                    BuffPlugin.LogError($"BuffName not found at {jsonFile.Name}!");
                    newData.CardInfos.Add(InGameTranslator.LanguageID.English,new CardInfo(){BuffName = newData.BuffID.value});
                }

                newData.AssetPath = dirPath.Remove(0,1);
                var rdata = (BuffData)Activator.CreateInstance(BuffRegister.GetDataType(newData.BuffID));
                GetCustomStaticBuffData(rdata, newData);
                //BuffPlugin.LogDebug(newData.ToDebugString());
                return true;

                int LoadAsInt(object input)
                {
                    if (input is string str)
                        return int.Parse(str);
                    else
                        return Convert.ToInt32(input);
                }

                float LoadAsFloat(object input)
                {
                    if (input is string str)
                        return float.Parse(str);
                    else
                        return Convert.ToSingle(input);
                }
            }
            catch (Exception e)
            {
                if(loadState != "")
                    BuffPlugin.LogError($"Load property failed: {loadState}! at {jsonFile.Name}. current data : {rawData[loadState]}, {rawData[loadState].GetType()}");
                else
                    BuffPlugin.LogError($"Load json file failed! at {jsonFile.Name}");
                BuffPlugin.LogException(e);
                ExceptionTracker.TrackException(e, loadState == "" ? $"Load json file failed! at {jsonFile.Name}" : $"Load property failed: {loadState}! at {jsonFile.Name}");

                newData = null;
                return false;
            }

        }

        private static void GetCustomStaticBuffData(BuffData data, BuffStaticData staticData)
        {
            if (data is CountableBuffData countable)
            {
                staticData.MaxCycleCount = countable.MaxCycleCount;
                staticData.Countable = true;
                BuffPlugin.LogDebug($"{staticData.BuffID},{staticData.MaxCycleCount}");
            }
            
            BuffCore.OnCustomStaticDataLoadedInternal(data,staticData);
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

            if (Conflict.Count > 0)
            {
                builder.AppendLine($"Conflict :");
                foreach (var conflict in Conflict)
                    builder.AppendLine($"---{conflict}");
            }

            if (Tag.Count > 0)
            {
                builder.AppendLine($"Tag :");
                foreach (var tag in Tag)
                    builder.AppendLine($"---{tag}");
            }

            builder.AppendLine("Infos :");
            foreach (var info in CardInfos)
            {
                builder.AppendLine($"---{info.Key}:");
                builder.AppendLine($"---BuffName: {info.Value.BuffName}");
                builder.AppendLine($"---Description: {info.Value.Description}");
            }
            return builder.ToString();
        }


        internal Dictionary<InGameTranslator.LanguageID, CardInfo> CardInfos { get; private set; } = new();


    }


}
