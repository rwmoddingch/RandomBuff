using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Game.Settings.Conditions
{
#if TESTVERSION
    internal class TestCondition : Condition
    {
        public static ConditionID id = new ConditionID("Test_Condition", true);
        public override ConditionID ID => id;

        public override int Exp => 0;

        public override string DisplayName(InGameTranslator translator)
        {
            return "TEST";
        }

        public override string DisplayProgress(InGameTranslator translator)
        {
            return $"Complete-{Finished} Failed-{Failed} | Ctrl+C => Finish; Ctrl+N => Normal; Ctrl+F => Failed; with F clear Failed";
        }

        public override ConditionState SetRandomParameter(SlugcatStats.Name name, float difficulty, List<Condition> conditions)
        {
            return ConditionState.Fail;
        }

        public override void InGameUpdate(RainWorldGame game)
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if(Input.GetKey(KeyCode.C))
                {
                    if (!Finished)
                    {
                        Finished = true;
                        onLabelRefresh?.Invoke(this);
                    }
                }
                else if(Input.GetKey(KeyCode.N))
                {
                    if (Finished)
                    {
                        Finished = false;
                        onLabelRefresh?.Invoke(this);
                    }
                   
                }
                else if(Input.GetKey(KeyCode.F))
                {
                    if (!Failed)
                    {
                        Failed = true;
                        onLabelRefresh?.Invoke(this);
                    }

                }
            }
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKey(KeyCode.F))
                    if (Failed)
                    {
                        Failed = false;
                        onLabelRefresh?.Invoke(this);
                    }
            }
        }
    }
#endif
}
