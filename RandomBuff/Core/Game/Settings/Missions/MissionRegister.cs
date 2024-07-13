using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using RandomBuff.Core.Progression;
using RandomBuff.Core.Progression.Quest;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.Quest;
using RandomBuff.Render.UI.Component;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public static class MissionRegister
    {
        private static readonly Dictionary<MissionID, Mission> registeredMissions = new ();


        internal static bool TryGetMission(MissionID id, out Mission mission)
        {
            if (!BuffConfigManager.IsItemLocked(QuestUnlockedType.Mission, id.value) && registeredMissions.TryGetValue(id,out mission))
                return true;
            
            mission = null;
            return false;
        }

        internal static List<MissionID> GetAllUnlockedMissions()
        {
            return registeredMissions.Keys.Where(i => !BuffConfigManager.IsItemLocked(QuestUnlockedType.Mission, i.value)).ToList();
        }

        public static void RegisterMission(MissionID ID, Mission mission)
        {
            if(mission.VerifyId())
                registeredMissions.Add(ID, mission);
            else
                BuffPlugin.LogError($"Missing some dependence for Mission ID:{ID}");
        }

        internal static void RegisterAllMissions()
        {
            foreach (var _assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in _assembly.GetTypes())
                {
                    if (type.GetInterfaces().Contains(typeof(IMissionEntry)))
                    {
                        var obj = type.GetConstructor(Type.EmptyTypes).Invoke(Array.Empty<object>());
                        try
                        {
                            type.GetMethod("RegisterMission").Invoke(obj, Array.Empty<object>());
                            BuffPlugin.Log("Registered mission: " + type.Name);
                        }
                        catch (Exception ex)
                        {
                            BuffPlugin.LogException(ex);
                            BuffPlugin.LogError("Failed in registering mission: " + type.Name);
                            ExceptionTracker.TrackException(ex, "Failed in registering mission: " + type.Name);
                        }

                    }
                }
            }

            QuestRendererManager.AddProvider(new MissionQuestRendererProvider());
        }
    }
}
