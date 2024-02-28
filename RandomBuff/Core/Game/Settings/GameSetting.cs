using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.SaveData;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings
{
    internal class GameSetting
    {
        private const string SettingSplit = "<GSA>";
        private const string SubSettingSplit = "<GSB>";


        public List<Condition> conditions = new ();

        public GachaTemplate.GachaTemplate gachaTemplate = new NormalGachaTemplate();

        public bool MissingDependence => fallBack != null;

        private string fallBack = null;

        public string TemplateName { get; private set; }


        public GameSetting()
        {
            LoadTemplate("Normal");
      
        }

        public bool Win
        {
            get
            {
                if (conditions.Count == 0) return false;
                return conditions.All(i => i.Finished);
            }
        }


        public float Difficulty { get; private set; } = 0.5f;


        public void NewGame()
        {
            gachaTemplate.NewGame();
        }

        public void EnterGame(RainWorldGame game)
        {
            gachaTemplate.EnterGame(game);
        }

        public void InGameUpdate(RainWorldGame game)
        {
            try
            {
                gachaTemplate.InGameUpdate(game);
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"Exception In {gachaTemplate.ID}:InGameUpdate");
            }

            foreach (var condition in conditions)
            {
                try
                {
                    condition.InGameUpdate(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e, $"Exception In {condition.ID}:InGameUpdate");
                }
            }
        }

        public void SessionEnd(RainWorldGame game)
        {
            try
            {
                gachaTemplate.SessionEnd();
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception In {gachaTemplate.ID}:SessionEnd");
            }

            foreach (var condition in conditions)
            {
                try
                {
                    condition.SessionEnd(game.GetStorySession.saveState);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e, $"Exception In {condition.ID}:SessionEnd");
                }
            }
        }

        public GameSetting Clone()
        {
            if (!TryLoadGameSetting(SaveToString(), out var setting))
            {
                BuffPlugin.LogError("Error In GameSetting:Clone");
                setting = new GameSetting();
            }
            return setting;
        }

        public void LoadTemplate(string name)
        {
            if (!BuffConfigManager.ContainsTemplateName(name))
            {
                BuffPlugin.LogFatal($"Unknown template: {name}, use default");
                gachaTemplate = new NormalGachaTemplate();
            }

            var data = BuffConfigManager.GetTemplateData(name);
            gachaTemplate = (GachaTemplate.GachaTemplate)Activator.CreateInstance(BuffRegister.GetTemplateType(data.Id).Type);
            gachaTemplate.ExpMultiply = data.ExpMultiply;
            foreach (var pair in data.datas)
            {
                try
                {
                    var field = gachaTemplate.GetType().GetField(pair.Key, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                        field.SetValue(gachaTemplate, Convert.ChangeType(pair.Value,field.FieldType));
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e,$"Exception in load template {pair.Key}-{pair.Value}-{data.Id}");
                }
            }

            if (!gachaTemplate.TemplateLoaded())
            {
                BuffPlugin.LogError($"Template:{name} has wrong data, fallback to Normal");
                LoadTemplate("Normal");
                return;
            }
            TemplateName = name;
            if (BuffPlugin.DevEnabled)
            {
                conditions.Clear();
                BuffPlugin.Log($"{GetRandomCondition().canGetMore},{GetRandomCondition().canGetMore},{GetRandomCondition().canGetMore}");
                
            }
        }

        public Condition CreateNewCondition(ConditionID id)
        {
            var re = (Condition)Activator.CreateInstance(BuffRegister.GetConditionType(id).Type);
            var same = conditions.Where(i => i.ID == id);
            re.SetRandomParameter(Difficulty, same.Any() ? same.ToList() : null);
            conditions.Add(re);
            return re;
        }

        /// <summary>
        /// 返回获取的条件以及是否还能获取
        /// canGetMore == false则不能继续获取
        /// 如果非得获取那返回(null,false)
        /// </summary>
        /// <returns></returns>
        public (Condition condition, bool canGetMore) GetRandomCondition()
        {
            var list = BuffRegister.GetAllConditionList();
            list.RemoveAll(i => conditions.Any(j => j.ID == i) ||
                                !BuffRegister.GetConditionType(i).CanUseInCurrentTemplate(gachaTemplate.ID));
            if (list.Count == 0)
                return (null, false);
            return (CreateNewCondition(list[Random.Range(0, list.Count)]),
                    list.Sum(i => BuffRegister.GetConditionType(i).CanUseMore ? 10 : 1) > 1);
        }


        public void SaveGameSettingToPath(string path)
        {
            File.WriteAllText(path, SaveToString());
        }


        public static bool TryLoadGameSetting(string str,out GameSetting setting)
        {
            setting = new GameSetting();
            setting.conditions.Clear();
            try
            {
                var splits = Regex.Split(str, SettingSplit);
                foreach (var split in splits)
                {
                    var subs = Regex.Split(split, SubSettingSplit);
                    if (subs.Length == 0)
                    {
                        BuffPlugin.LogFatal($"Corrupted Game Setting {str}");
                        return false;
                    }

                    switch (subs[0])
                    {
                        case "DIFFICULTY":
                            setting.Difficulty = float.Parse(subs[1]);
                            break;
                        case "TEMPLATE":
                            if (!ExtEnumBase.TryParse(typeof(GachaTemplateID), subs[1], true, out var id))
                            {
                                BuffPlugin.LogWarning($"Missing Dependence, Can't find Template ID: {subs[1]}");
                                setting.fallBack = str;
                                return true;
                            }

                            setting.gachaTemplate = (GachaTemplate.GachaTemplate)JsonConvert.DeserializeObject(subs[2],
                                    BuffRegister.GetTemplateType((GachaTemplateID)id).Type);
                            if (subs.Length == 4)
                                setting.TemplateName = subs[3];
                            else
                                setting.TemplateName = subs[1];
                            break;
                        case "CONDITION":
                            if (!ExtEnumBase.TryParse(typeof(ConditionID), subs[1], true, out var cid))
                            {
                                BuffPlugin.LogWarning($"Missing Dependence, Can't find Condition ID: {subs[1]}");
                                setting.fallBack = str;
                                return true;
                            }
                            setting.conditions.Add((Condition)JsonConvert.DeserializeObject(subs[2],
                                BuffRegister.GetConditionType((ConditionID)cid).Type));
                            break;
                    }
                }

            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e,$"Exception in loading game setting {str}");
                return false;
            }
            return true;
        }

        public string SaveToString()
        {
            if (MissingDependence)
                return fallBack;

            StringBuilder builder = new StringBuilder();
            builder.Append($"DIFFICULTY{SubSettingSplit}{Difficulty}{SettingSplit}");
            builder.Append($"TEMPLATE{SubSettingSplit}{gachaTemplate.ID}{SubSettingSplit}{JsonConvert.SerializeObject(gachaTemplate)}{SubSettingSplit}{TemplateName}{SettingSplit}");
            foreach (var condition in conditions)
            {
                builder.Append($"CONDITION{SubSettingSplit}{condition.ID}{SubSettingSplit}{JsonConvert.SerializeObject(condition)}{SettingSplit}");
            }
            //BuffPlugin.LogDebug(builder.ToString());
            return builder.ToString();
        }


    }
}
