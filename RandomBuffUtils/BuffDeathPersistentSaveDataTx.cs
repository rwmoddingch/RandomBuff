using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomSaveTx
{
    /// <summary>
    /// DeathPersistentSaveData拓展
    /// </summary>
    public static class DeathPersistentSaveDataRx
    {
        public static string TotalHeader => "BUFFUTILSDEATHPERSISTENTSAVETX";

        public static bool hooked = false;
        public static List<DeathPersistentSaveDataTx> treatments = new List<DeathPersistentSaveDataTx>();

        public static void AppplyTreatment(DeathPersistentSaveDataTx treatment)
        {
            treatments.Add(treatment);
            HookOn();
        }

        public static void HookOn()//call Patch() in OnModInit
        {
            if (hooked) return;
            try
            {
                On.SaveState.ctor += SaveState_ctor;

                On.DeathPersistentSaveData.FromString += DeathPersistentSaveData_FromString;
                On.DeathPersistentSaveData.SaveToString += DeathPersistentSaveData_SaveToString;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            hooked = true;
        }

        private static void SaveState_ctor(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig.Invoke(self, saveStateNumber, progression);
            foreach (var unit in treatments)
            {
                unit.ClearDataForNewSaveState(saveStateNumber);
            }
        }

        private static string DeathPersistentSaveData_SaveToString(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            string result = orig.Invoke(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);

            foreach (var unit in treatments)
            {
                string header = unit.header;
                string data = unit.SaveToString(saveAsIfPlayerDied, saveAsIfPlayerQuit);
                if (header != string.Empty && data != string.Empty)
                {
                    result += TotalHeader + header + "<dpB>" + data + "<dpA>";
                }
                BuffUtils.Log(TotalHeader, header + " Save to string : " + data);
            }
            return result;
        }

        static private void DeathPersistentSaveData_FromString(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
        {
            orig.Invoke(self, s);

            string[] array = Regex.Split(s, "<dpA>");
            for (int i = 0; i < array.Length; i++)
            {
                string[] array2 = Regex.Split(array[i], "<dpB>");
                string header = array2[0];

                foreach (var unit in treatments)
                {
                    if (TotalHeader + unit.header == header)
                    {
                        for (int k = self.unrecognizedSaveStrings.Count - 1; k >= 0; k--)
                        {
                            if (self.unrecognizedSaveStrings[k].Contains(header)) self.unrecognizedSaveStrings.RemoveAt(k);
                        }
                        unit.LoadDatas(array2[1]);
                        BuffUtils.Log(TotalHeader, unit.header + " load from string : " + array2[1]);
                    }
                }
            }
        }

        public static DeathPersistentSaveDataTx GetTreatmentOfHeader(string header)
        {
            foreach (var unit in treatments)
            {
                if (unit.header == header) return unit;
            }
            return null;
        }

        public static bool TryGetValue(string header, out DeathPersistentSaveDataTx unit)
        {
            unit = GetTreatmentOfHeader(header);
            return unit != null;
        }

        /// <summary>
        /// 通过类型来获取Treatment，这也是最建议使用的方法。
        /// 但最好不要在循环中使用该方法。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetTreatmentOfType<T>() where T : DeathPersistentSaveDataTx
        {
            foreach (var treatment in treatments)
            {
                if (treatment is T) return (T)treatment;
            }
            return null;
        }
    }

    /// <summary>
    /// 如果你需要保存自己的数据，请继承该类并重写部分方法。
    /// </summary>
    public class DeathPersistentSaveDataTx
    {
        /// <summary>
        /// 当前对应的存档名
        /// </summary>
        public SlugcatStats.Name slugName;

        public string origSaveData;

        /// <summary>
        /// 该存档单元对应的标题头，需要为唯一命名，所以请尽量起一个独特的名字
        /// </summary>
        public virtual string header => "";

        public DeathPersistentSaveDataTx(SlugcatStats.Name name)
        {
            slugName = name;
        }

        /// <summary>
        /// 保存方法
        /// </summary>
        /// <param name="saveAsIfPlayerDied">玩家是否死亡</param>
        /// <param name="saveAsIfPlayerQuit">玩家是否主动退出</param>
        /// <returns></returns>
        public virtual string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            return "";
        }

        /// <summary>
        /// 加载数据的方法，提供的数据为 SaveToString 格式化处理后的字符串
        /// </summary>
        /// <param name="data"></param>
        public virtual void LoadDatas(string data)
        {
            origSaveData = data;
        }

        /// <summary>
        /// 为新存档清除当前保存的数据
        /// </summary>
        /// <param name="newSlugName"></param>
        public virtual void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            origSaveData = "";
            slugName = newSlugName;
        }

        public override string ToString()
        {
            return base.ToString() + " SlugStateName:" + slugName.ToString() + " header:" + header;
        }
    }

    /// <summary>
    /// 一个简易的示范类，请不要直接使用该类
    /// </summary>
    public sealed class TestSaveUnit : DeathPersistentSaveDataTx
    {
        public int loadThisForHowManyTimes = 0;
        public TestSaveUnit(SlugcatStats.Name name) : base(name)
        {
        }

        public override string header => "THISISJUSTATESTLOL";

        public override void LoadDatas(string data)
        {
            base.LoadDatas(data);

            loadThisForHowManyTimes = int.Parse(data);
        }

        public override string SaveToString(bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied || saveAsIfPlayerQuit) return origSaveData;
            else
            {
                loadThisForHowManyTimes++;
                return loadThisForHowManyTimes.ToString();
            }
        }

        public override void ClearDataForNewSaveState(SlugcatStats.Name newSlugName)
        {
            base.ClearDataForNewSaveState(newSlugName);
            loadThisForHowManyTimes = 0;
        }

        public override string ToString()
        {
            return base.ToString() + " loadThisForHowManyTimes:" + loadThisForHowManyTimes.ToString();
        }
    }
}
