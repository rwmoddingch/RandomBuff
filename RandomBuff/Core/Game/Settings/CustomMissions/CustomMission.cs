using Newtonsoft.Json;
using RandomBuff.Core.Buff;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.CustomMissions;

internal class CustomMission
{
    [JsonProperty]
    [ElementProperty("Mission ID","")]
    public string ID { get; set; }

    [JsonProperty]
    [ElementProperty("Bind Slugcat","")]
    public SlugcatStats.Name BindSlug { get; set; }

    [JsonProperty]
    [ElementProperty("Text Color","")]
    public Color TextCol{ get; set; }

    [JsonProperty]
    [ElementProperty("Mission Name","")]
    public string MissionName { get; set; }
    
    [JsonProperty]
    [ElementProperty("Initial Cards","")]
    public BuffID[] startBuffSet;
    
    [JsonProperty]
    [ElementProperty("Game Settings","")]
    public GameSetting gameSetting = new(null);
    
    /// <summary>
    /// 验证依赖是否完整
    /// </summary>
    /// <returns></returns>
    public bool VerifyId()
    {
        foreach (var id in startBuffSet)
        {
            if (!BuffID.values.entries.Contains(id.value))
                return false;
        }

        return gameSetting.IsValid;
    }
}