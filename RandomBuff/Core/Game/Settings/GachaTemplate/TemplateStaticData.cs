using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.GachaTemplate
{
    internal class TemplateStaticData
    {
        public static bool TryLoadTemplateStaticData(FileInfo jsonFile, out TemplateStaticData data)
        {
            string loadState = "";
            data = new TemplateStaticData();
            try
            {
                BuffPlugin.Log($"try load template data at {jsonFile.FullName}");
                var rawData = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(jsonFile.FullName));
                data.Name = rawData[loadState = "Name"].ToString();
                rawData.Remove("Name");
                data.Id = (GachaTemplateID)ExtEnumBase.Parse((typeof(GachaTemplateID)),rawData[loadState = "ID"].ToString(),true);
                rawData.Remove("ID");
                data.datas = rawData;
                return true;
            }
            catch (Exception e)
            {
                BuffPlugin.LogError($"Load template json file failed! at {jsonFile.Name}-{loadState}");
                BuffPlugin.LogException(e);
                return false;
            }
        }
        private TemplateStaticData()
        {
   
        }

        public string Name { get; private set; } = null;

        public GachaTemplateID Id { get; private set; } = null;

        public Dictionary<string,object> datas = new ();


    }
}
