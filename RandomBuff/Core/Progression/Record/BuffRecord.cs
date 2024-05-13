using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using RandomBuff.Core.Buff;

namespace RandomBuff.Core.Progression.Record
{
    public abstract class BuffRecord
    {
        public Dictionary<string, string> GetValueDictionary()
        {
            var re = new Dictionary<string, string>();
            foreach (var field in GetType()
                         .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (field.GetCustomAttribute<JsonPropertyAttribute>() != null)
                    re.Add(field.Name,field.GetValue(this).ToString());
            }
            foreach (var property in GetType()
                         .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.GetCustomAttribute<JsonPropertyAttribute>() != null)
                    re.Add(property.Name, property.GetValue(this).ToString());
            }
            return re;
        }
    }

    public class InGameRecord : BuffRecord
    {
        [JsonProperty] 
        public int totCard;

        [JsonProperty]
        public int totPositiveCard;

        [JsonProperty]
        public int totNegativeCard;

        [JsonProperty]
        public int totDualityCard;

        [JsonProperty]
        public int totTriggerCount;

        public static InGameRecord operator+(InGameRecord self, InGameRecord record)
        {
            return new InGameRecord()
            {
                totCard = self.totCard + record.totCard,
                totDualityCard = self.totDualityCard + record.totDualityCard,
                totNegativeCard = self.totNegativeCard + record.totNegativeCard,
                totPositiveCard = self.totPositiveCard + record.totPositiveCard,
                totTriggerCount = self.totTriggerCount + record.totTriggerCount
            };
        }

        public void AddCard(BuffType type)
        {
            switch (type)
            {
                case BuffType.Duality:
                    totDualityCard++;
                    break;
                case BuffType.Negative:
                    totNegativeCard++;
                    break;
                case BuffType.Positive:
                    totPositiveCard++;
                    break;
            }
            totCard++;
        }

        public void ActiveCard()
        {
            totTriggerCount++;
        }
    }

    public class SlotRecord : InGameRecord
    {
    }
}
