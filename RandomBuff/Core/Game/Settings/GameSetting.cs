using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.Game.Settings.GachaTemplate;
using RandomBuff.Core.Progression.Record;
using RandomBuff.Core.SaveData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using RWCustom;
using UnityEngine;
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

        private HashSet<ConditionID> cantAddMoreTmp = new();

        public List<FallbackPickSlot> fallbackPick;

        public bool IsValid { get; private set; } = true;

        public string MissionId { get; set; }

        public bool CanStackByPassage => MissionId is null && gachaTemplate.CanStackByPassage;

        public InGameRecord inGameRecord = new ();

        public bool Win
        {
            get
            {
                if (conditions.Count == 0) return false;
                return conditions.All(i => i.Finished);
            }
        }

        public class FallbackPickSlot
        {
            public BuffID[] major;
            public BuffID[] additive;
            public int selectCount;

        }

        public float Difficulty { get; private set; } = 0.5f;


        public string Description
        {
            get
            {
                if (MissionId is not null)
                    gachaTemplate.CanStackByPassage = false;

                return gachaTemplate.TemplateDetail + "<ENTRY>" +
                       Custom.rainWorld.inGameTranslator.Translate(BuffResourceString.Get(gachaTemplate.TemplateDescription ?? "",true));
            }
        }


        public GameSetting(SlugcatStats.Name name, string gachaTemplate = "Normal", string startPos = null)
        {
            LoadTemplate(gachaTemplate);
            this.name = name;
            this.gachaTemplate.ForceStartPos = startPos;
        }

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

            //BuffPlugin.LogDebug($"GameSetting Desc: {Description}");
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

        public void OnDestroy()
        {
            try
            {
                gachaTemplate.OnDestroy();
            }
            catch (Exception e)
            {
                BuffPlugin.LogException(e, $"Exception In {gachaTemplate.ID}:OnDestroy");
            }

            foreach (var condition in conditions)
            {
                try
                {
                    condition.OnDestroy();
                }
                catch (Exception e)
                {
                    BuffPlugin.LogException(e, $"Exception In {condition.ID}:OnDestroy");
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
            if(Mathf.Abs(data.ExpMultiply - (-1)) > 0.02f)
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
            if (result is Condition.ConditionState.Ok_NoMore or Condition.ConditionState.Fail)
                cantAddMore.Add(re.ID);
            else if(result == Condition.ConditionState.Fail_Tmp)
                cantAddMoreTmp.Add(re.ID);

            if (result is Condition.ConditionState.Fail or Condition.ConditionState.Fail_Tmp)
                return null;

            conditions.Add(re);
            return re;
        }

        public void RemoveCondition(Condition condition)
        {
            if (cantAddMore.Contains(condition.ID))
                cantAddMore.Remove(condition.ID);
            if (cantAddMoreTmp.Contains(condition.ID))
                cantAddMoreTmp.Remove(condition.ID);
            conditions.Remove(condition);
        }

        public void ClearCondition()
        {
            cantAddMore.Clear();
            conditions.Clear();
            cantAddMoreTmp.Clear();
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
            list.RemoveAll(i => cantAddMore.Contains(i) || cantAddMoreTmp.Contains(i) ||
                                 !BuffRegister.GetConditionType(i).CanUseInCurrentTemplate(gachaTemplate.ID));

            if (list.Count == 0)
                return (null, false);

            Condition newCondition = null;
            while(list.Count > 0 && (newCondition = CreateNewCondition(list[Random.Range(0, list.Count)])) == null) {}

            if (newCondition == null)
                return (null, false);


            cantAddMoreTmp.Clear();
            return (newCondition, (cantAddMore.Count) < BuffRegister.GetAllConditionList().Count);
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
                        case "FALLBACK-NEW":
                            setting.fallbackPick = JsonConvert.DeserializeObject<List<FallbackPickSlot>>(subs[1]);
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
            fallbackPick = new List<FallbackPickSlot>();
       
       
            var positive = gachaTemplate.CurrentPacket.positive;
            if (positive.selectCount > 0)
            {
                for (int i = 0; i < positive.pickTimes; i++)
                {

                    var majorList = BuffPicker
                        .GetNewBuffsOfType(game.StoryCharacter, positive.showCount, BuffType.Positive)
                        .Select(i => i.BuffID).ToArray();

                    var additiveList = BuffPicker
                        .GetNewBuffsOfType(game.StoryCharacter, positive.showCount, BuffType.Negative)
                        .Select(i => i.BuffID).ToArray();


                    for (int j = 0; j < majorList.Length; j++)
                        if (majorList[j].GetStaticData().BuffProperty != BuffProperty.Special)
                            additiveList[j] = null;

                    fallbackPick.Add(new FallbackPickSlot()
                    {
                        additive = additiveList,
                        major = majorList,
                        selectCount = positive.selectCount
                    });
                }
            }

            var negative = gachaTemplate.CurrentPacket.negative;
            if (negative.selectCount > 0)
            {
                for (int i = 0; i < negative.pickTimes; i++)
                {

                    var majorList = BuffPicker
                        .GetNewBuffsOfType(game.StoryCharacter, negative.showCount, BuffType.Negative)
                        .Select(i => i.BuffID).ToArray();

                    var additiveList = BuffPicker
                        .GetNewBuffsOfType(game.StoryCharacter, negative.showCount, BuffType.Positive)
                        .Select(i => i.BuffID).ToArray();


                    for (int j = 0; j < majorList.Length; j++)
                        if (majorList[j].GetStaticData().BuffProperty != BuffProperty.Special)
                            additiveList[j] = null;

                    fallbackPick.Add(new FallbackPickSlot()
                    {
                        additive = additiveList,
                        major = majorList,
                        selectCount = negative.selectCount
                    });
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
                builder.Append($"FALLBACK-NEW{SubSettingSplit}{JsonConvert.SerializeObject(fallbackPick)}{SettingSplit}");
            if(MissionId != null)
                builder.Append($"MISSION{SubSettingSplit}{MissionId}{SettingSplit}");

            return builder.ToString();
        }



    }
}
