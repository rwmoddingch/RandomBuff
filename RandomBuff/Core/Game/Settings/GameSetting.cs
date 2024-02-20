using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Condition;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.SaveData;

namespace RandomBuff.Core.Game.Settings
{
    internal class GameSetting
    {
        private const string SettingSplit = "<GSA>";
        private const string SubSettingSplit = "<GSB>";


        public List<BaseCondition> conditions = new ();

        public BaseGachaTemplate gachaTemplate = new NormalGachaTemplate();

        public bool MissingDependence => fallBack != null;

        private string fallBack = null;

        public string TemplateName { get; private set; }


        public GameSetting()
        {
            LoadTemplate("Normal");
            if(BuffPlugin.DevEnabled)
                CreateNewCondition(ConditionID.Cycle);
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

        public void EnterGame()
        {
            gachaTemplate.EnterGame();
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
            gachaTemplate = (BaseGachaTemplate)Activator.CreateInstance(BuffRegister.GetTemplate(data.Id));
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
            TemplateName = name;
        }

        public BaseCondition CreateNewCondition(ConditionID id)
        {
            var re = (BaseCondition)Activator.CreateInstance(BuffRegister.GetCondition(id));
            re.SetRandomParameter(Difficulty);
            conditions.Add(re);
            return re;
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

                            setting.gachaTemplate = (BaseGachaTemplate)JsonConvert.DeserializeObject(subs[2],
                                    BuffRegister.GetTemplate((GachaTemplateID)id));
                            break;
                        case "CONDITION":
                            if (!ExtEnumBase.TryParse(typeof(ConditionID), subs[1], true, out var cid))
                            {
                                BuffPlugin.LogWarning($"Missing Dependence, Can't find Condition ID: {subs[1]}");
                                setting.fallBack = str;
                                return true;
                            }
                            setting.conditions.Add((BaseCondition)JsonConvert.DeserializeObject(subs[2],
                                BuffRegister.GetCondition((ConditionID)cid)));
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
            builder.Append($"TEMPLATE{SubSettingSplit}{gachaTemplate.ID}{SubSettingSplit}{JsonConvert.SerializeObject(gachaTemplate)}{SettingSplit}");
            foreach (var condition in conditions)
            {
                builder.Append($"CONDITION{SubSettingSplit}{condition.ID}{SubSettingSplit}{JsonConvert.SerializeObject(condition)}{SettingSplit}");
            }
            //BuffPlugin.LogDebug(builder.ToString());
            return builder.ToString();
        }


    }
}
