﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RandomBuff.Render.UI.ExceptionTracker;
using static RandomBuff.Core.Buff.BuffStaticData;

namespace RandomBuff.Core.Buff
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class BuffPluginInfo
    {
        protected BuffPluginInfo() { }

        public BuffPluginInfo(string assemblyName)
        {
            AssemblyName = assemblyName;
            infos.Add(InGameTranslator.LanguageID.English, new PluginInfo()
            {
                Description = "No Description",
                Name = assemblyName
            });
        }

        public string AssemblyName { get; private set; }

        public string Thumbnail { get; private set; } = "buffassets/illustrations/default_thumbnail";



        public PluginInfo GetInfo(InGameTranslator.LanguageID id)
        {
            if(infos.TryGetValue(id,out var info)) return info;
            if(infos.TryGetValue(InGameTranslator.LanguageID.English,out info)) return info;
            return infos.First().Value;
        }

        private Dictionary<InGameTranslator.LanguageID,PluginInfo> infos = new ();

        public static bool LoadPluginInfo(string filePath,out BuffPluginInfo newData)
        {
            newData = null;
            string loadState = "";
            try
            {
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(filePath));
                newData = new BuffPluginInfo { AssemblyName = (string)rawData[loadState = "Assembly"] };
                bool hasMutli = false;

                //或许未来改成延迟加载
                if (rawData.TryGetValue(loadState = "Thumbnail", out var thumbnail))
                {
                    try
                    {
                        Futile.atlasManager.LoadImage("buffplugins" + Path.AltDirectorySeparatorChar + (string)thumbnail);
                        newData.Thumbnail = "buffplugins" + Path.AltDirectorySeparatorChar + (string)thumbnail;

                    }
                    catch (FutileException e)
                    {
                        BuffPlugin.LogError($"Card image not found at :{"buffplugins" + Path.AltDirectorySeparatorChar + (string)thumbnail}");
                    }

                }

                foreach (var language in ExtEnumBase.GetNames(typeof(InGameTranslator.LanguageID)))
                {
                    if (rawData.ContainsKey(language))
                    {
                        JObject lanObj = (JObject)rawData[language];
                        PluginInfo info = new PluginInfo();

                        if (lanObj.TryGetValue(loadState = "Name", out var obj))
                            info.Name = (string)obj;
                        else
                        {
                            info.Name = newData.AssemblyName;
                            BuffPlugin.LogWarning($"Can't find Name At {filePath}:{language}");
                        }


                        if (lanObj.TryGetValue(loadState = "Description", out var obj1))
                            info.Description = (string)obj1;

                        newData.infos.Add(new InGameTranslator.LanguageID(language), info);
                        hasMutli = true;
                    }
                }

                if (!hasMutli)
                {
                    PluginInfo info = new PluginInfo();
                    if (rawData.TryGetValue(loadState = "Name", out var obj))
                        info.Name = (string)obj;
                    else
                    {
                        info.Name = newData.AssemblyName;
                        BuffPlugin.LogWarning($"Can't find Name At {filePath}:Default");
                    }
                    if (rawData.TryGetValue(loadState = "Description", out var obj1))
                        info.Description = (string)obj1;
                    newData.infos.Add(InGameTranslator.LanguageID.English, info);
                }
                return true;
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e);
                BuffPlugin.LogError(
                    $"Load buff plugin info data failed at: {filePath}, {(string.IsNullOrEmpty(loadState) ? "" : $"state:{loadState}")}");
            }
            return false;
        }

        internal string ToDebugString()
        {
            StringBuilder builder = new();
            builder.AppendLine();
            builder.AppendLine("----------------------");
            builder.AppendLine($"Assembly : {AssemblyName}");
            builder.AppendLine($"Thumbnail : {Thumbnail}");



            builder.AppendLine("Infos :");
            foreach (var info in infos)
            {
                builder.AppendLine($"---{info.Key}:");
                builder.AppendLine($"---Name: {info.Value.Name}");
                builder.AppendLine($"---Description: {info.Value.Description}");
            }
            return builder.ToString();
        }
        public class PluginInfo
        {

            public string Name { get; internal set; } = null;
            public string Description { get; internal set; } = null;
        }

    }
}