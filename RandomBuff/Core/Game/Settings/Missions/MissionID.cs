using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Core.Game.Settings.Missions
{
    public class MissionID : ExtEnum<MissionID>
    {
        public static readonly MissionID Druid = new MissionID("Druid", true);
        public static readonly MissionID Test = new MissionID("Test", true);

        static MissionID()
        {
           
        }

        public MissionID(string value, bool register = false) : base(value, register)
        { 
        
        }

    }
}
