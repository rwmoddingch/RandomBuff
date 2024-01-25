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
            BuffPlugin.Log(dirPath);
            string loadState = "";
            try
            {
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(jsonFile.FullName));
                newData = new BuffStaticData();

                newData.BuffID = (BuffID)ExtEnumBase.Parse(typeof(BuffID),(string)rawData[loadState = "BuffID"],true);

                if (rawData.ContainsKey(loadState = "FaceName"))
                {
                    newData.FaceName = $"{dirPath}/{rawData[loadState]}";
                    try
                    {
                        Futile.atlasManager.LoadImage(newData.FaceName);
                    }
                    catch (FutileException e)
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
                    newData.Triggerable = (bool)rawData[loadState];

                if (rawData.ContainsKey(loadState = "BuffType"))
                    newData.BuffType = (BuffType)Enum.Parse(typeof(BuffType),(string)rawData[loadState]);

                if (rawData.ContainsKey(loadState = "BuffProperty"))
                    newData.BuffProperty = (BuffProperty)Enum.Parse(typeof(BuffProperty), (string)rawData[loadState]);


                if (rawData.ContainsKey(loadState = "Color"))
                    newData.Color = Custom.hexToColor((string)rawData[loadState]);


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
                    }
                }

                if (rawData.ContainsKey("Custom"))
                {
                    var customObject = (JObject)rawData["Custom"];
                    foreach (var data in customObject)
                    {
                        if (data.Value == null)
                        {
                            BuffPlugin.LogWarning($"Null JToken at Custom:{data.Key} At {jsonFile.Name}");
                            continue;
                        }
                        newData.customParameters.Add(data.Key,data.Value.ToString());
                    }
                }

                if (newData.CardInfos.Count == 0)
                {
                    BuffPlugin.LogError($"BuffName not found at {jsonFile.Name}!");
                    newData.CardInfos.Add(InGameTranslator.LanguageID.English,new CardInfo(){BuffName = newData.BuffID.value});
                }

                if (BuffPlugin.DevEnabled)
                    BuffPlugin.Log(newData.ToDebugString());
                return true;
            }
            catch (Exception e)
            {
                if(loadState != "")
                    BuffPlugin.LogError($"Load property failed: {loadState}! at {jsonFile.Name}");
                else
                    BuffPlugin.LogError($"Load json file failed! at {jsonFile.Name}");
                Debug.LogException(e);

                newData = null;
                return false;
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
            builder.AppendLine($"BuffID : {BuffID}");
            builder.AppendLine($"Triggerable : {Triggerable}");
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
            return builder.ToString();
        }


        internal Dictionary<InGameTranslator.LanguageID, CardInfo> CardInfos { get; private set; } = new();

        internal Dictionary<string,string> customParameters = new();

    }
}
