using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public static class MissionRegister
    {
        public static Dictionary<MissionID, Mission> registeredMissions = new ();

        public static void RegisterMission(MissionID ID, Mission mission)
        {
            registeredMissions.Add(ID, mission);
        }

        public static void RegisterAllMissions(Assembly assembly, bool buitIn)
        {
            Assembly _assembly;
            if (!buitIn)
            {
                _assembly = assembly;
            }
            else _assembly = System.Reflection.Assembly.GetExecutingAssembly();

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
                    }
                    
                }
            }
        }
    }
}
