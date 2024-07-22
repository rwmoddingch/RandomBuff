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
using RandomBuff.Core.Progression.Record;
using RandomBuff.Core.SaveData;
using Random = UnityEngine.Random;

namespace RandomBuff.Core.Game.Settings
{
    public class GameSetting
    {
        private const string SettingSplit = "<GSA>";
        private const string SubSettingSplit = "<GSB>";

        /// <summary>
        /// 不能直接移除，记得调用RemoveCondition,ClearCondition
        /// </summary>
        public List<Condition> conditions = new ();

        public GachaTemplate.GachaTemplate gachaTemplate = new NormalGachaTemplate();

        public bool MissingDependence => fallBack != null;

        private string fallBack = null;

        public string TemplateName { get; private set; }

        public SlugcatStats.Name name;

        public HashSet<ConditionID> cantAddMore = new();

        public List<BuffID> fallbackPick = null;

        public bool IsValid { get; private set; } = true;


        public string MissionId { get; set; }

        public InGameRecord inGameRecord = new ();

        public GameSetting(SlugcatStats.Name name,string gachaTemplate = "Normal", string startPos = null)
        {
            LoadTemplate(gachaTemplate);
            this.name = name;
            this.gachaTemplate.ForceStartPos = startPos;
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
            if (name != null && ModManager.CoopAvailable)
            {
                game.GetStorySession.characterStatsJollyplayer[0].name = name;
                game.rainWorld.options.jollyPlayerOptionsArray[0].playerClass = name;
            }
            name ??= game.StoryCharacter;
            gachaTemplate.EnterGame(game);

            foreach (var condition in conditions)
            {
                try
                {
                    condition.EnterGame(game);
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e, $"Exception In {condition.ID}:EnterGame");
                }
            }
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
                gachaTemplate.SessionEnd(game);
                BuildFallBackCard(game);
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
            if (!TryLoadGameSetting(name,SaveToString(), out var setting))
            {
                BuffPlugin.LogError("Error In GameSetting:Clone");
                setting = new GameSetting(name);
            }
            return setting;
        }

        public void LoadTemplate(string name)
        {
            TemplateName = "Normal";
            if (!BuffConfigManager.ContainsTemplateName(name))
            {
                BuffPlugin.LogFatal($"Unknown template: {name}, use default");
                gachaTemplate = new NormalGachaTemplate();
                IsValid = false;
                return;
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
        }

        public Condition CreateNewCondition(ConditionID id)
        {
            var re = (Condition)Activator.CreateInstance(BuffRegister.GetConditionType(id).Type);
            var result = re.SetRandomParameter(name, Difficulty, conditions.ToList());
            if (result == Condition.ConditionState.Ok_NoMore || result == Condition.ConditionState.Fail)
                cantAddMore.Add(re.ID);
            conditions.Add(re);

            if (result == Condition.ConditionState.Fail)
                return null;
            return re;
        }

        public void RemoveCondition(Condition condition)
        {
            if (cantAddMore.Contains(condition.ID))
                cantAddMore.Remove(condition.ID);
            conditions.Remove(condition);
        }

        public void ClearCondition()
        {
            cantAddMore.Clear();
            conditions.Clear();
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
            list.RemoveAll(i =>cantAddMore.Contains(i) ||
                                 !BuffRegister.GetConditionType(i).CanUseInCurrentTemplate(gachaTemplate.ID));

            if (list.Count == 0)
                return (null, false);

            Condition newCondition = null;
            while(list.Count > 0 && (newCondition = CreateNewCondition(list[Random.Range(0, list.Count)])) == null) {}

            if (newCondition == null)
                return (null, false);

            return (newCondition, cantAddMore.Count < BuffRegister.GetAllConditionList().Count);
        }


        public void SaveGameSettingToPath(string path)
        {
            File.WriteAllText(path, SaveToString());
        }


        public static bool TryLoadGameSetting(SlugcatStats.Name name,string str,out GameSetting setting)
        {
            setting = new GameSetting(name);
            setting.ClearCondition();
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
                            if (!ExtEnumBase.TryParse(typeof(ConditionID), subs[1], true, out var cid) ||
                                BuffRegister.GetConditionType((ConditionID)cid) == null)
                            {
                                BuffPlugin.LogWarning($"Missing Dependence, Can't find Condition ID: {subs[1]}");
                                setting.fallBack = str;
                                return true;
                            }
                            setting.conditions.Add((Condition)JsonConvert.DeserializeObject(subs[2],
                                BuffRegister.GetConditionType((ConditionID)cid).Type));
                            break;
                        case "FALLBACK":
                            setting.fallbackPick = JsonConvert.DeserializeObject<List<BuffID>>(subs[1]);
                            foreach (var fallback in setting.fallbackPick)
                                if(BuffRegister.GetBuffType(fallback) != null)
                                    BuffDataManager.Instance.GetOrCreateBuffData(name, fallback, true);
                            BuffPlugin.Log($"Load Fallback List, Count: {setting.fallbackPick.Count}");
                            break;
                        case "MISSION":
                            setting.MissionId = subs[1];
                            break;
                        case "TOTCARDS":
                            try
                            {
                                setting.inGameRecord = JsonConvert.DeserializeObject<InGameRecord>(subs[1]);
                            }
                            catch (Exception e)
                            {
                                setting.inGameRecord = new InGameRecord();
                            }
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

        public void BuildFallBackCard(RainWorldGame game)
        {
            if(!gachaTemplate.CurrentPacket.NeedMenu)
                return;
            fallbackPick = new List<BuffID>();
            var negative = gachaTemplate.CurrentPacket.negative.pickTimes *
                           gachaTemplate.CurrentPacket.negative.selectCount;
            if (negative > 0)
            {
                fallbackPick.AddRange(
                    BuffPicker.GetNewBuffsOfType(game.StoryCharacter, negative, 
                        BuffType.Duality, BuffType.Negative).Select(i => i.BuffID));
            }
            var positive = gachaTemplate.CurrentPacket.negative.pickTimes *
                           gachaTemplate.CurrentPacket.negative.selectCount;

            if (positive > 0)
            {
                var pos = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, positive,
                    BuffType.Positive);
                var negCount = pos.Count(i => i.BuffProperty == BuffProperty.Special);
                fallbackPick.AddRange(pos.Select(i => i.BuffID));
                if (negCount > 0)
                {
                    fallbackPick.AddRange(
                        BuffPicker.GetNewBuffsOfType(game.StoryCharacter, negCount,
                            BuffType.Duality, BuffType.Negative).Select(i => i.BuffID));
                }
            }
        }

        public string SaveToString()
        {
            if (MissingDependence)
                return fallBack;

            StringBuilder builder = new StringBuilder();
            builder.Append($"DIFFICULTY{SubSettingSplit}{Difficulty}{SettingSplit}");
            builder.Append($"TEMPLATE{SubSettingSplit}{gachaTemplate.ID}{SubSettingSplit}{JsonConvert.SerializeObject(gachaTemplate)}{SubSettingSplit}{TemplateName}{SettingSplit}");
            builder.Append($"TOTCARDS{SubSettingSplit}{JsonConvert.SerializeObject(inGameRecord)}{SettingSplit}");

            foreach (var condition in conditions)
                builder.Append($"CONDITION{SubSettingSplit}{condition.ID}{SubSettingSplit}{JsonConvert.SerializeObject(condition)}{SettingSplit}");
            

            if (fallbackPick != null)
                builder.Append($"FALLBACK{SubSettingSplit}{JsonConvert.SerializeObject(fallbackPick)}{SettingSplit}");
            if(MissionId != null)
                builder.Append($"MISSION{SubSettingSplit}{MissionId}");

            return builder.ToString();
        }



    }
}
