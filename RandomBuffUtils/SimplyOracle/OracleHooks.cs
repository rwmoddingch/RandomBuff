using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Conversation;
using Random = System.Random;

namespace RandomBuffUtils.SimplyOracle
{

    public class CustomSpecialEvent : SpecialEvent
    {
        public CustomSpecialEvent(Conversation owner, int initialWait, string eventName,params string[] arg) : base(owner,
            initialWait, eventName)
        {
            this.args = arg;
        }

        public override void Activate()
        {
            base.Activate();
            OracleHooks.OnEventTriggerInternal(owner.interfaceOwner, this);
        }

        public string[] args;
    }

    public static class OracleHooks
    {
        public delegate void EventTriggerHandler(IOwnAConversation owner, CustomSpecialEvent eventData);

        public delegate bool OracleSeePlayerHandler(OracleBehavior behavior);

        public delegate void OracleConversationHandler(OracleBehavior behavior, Conversation conversation);


        public static event EventTriggerHandler OnEventTrigger;


        public static event OracleSeePlayerHandler OnSeePlayer;
        public static event OracleConversationHandler OnLoadConversation;



        internal static void OnModsInit()
        {
            On.SSOracleBehavior.SeePlayer += SSOracleBehavior_SeePlayer;
            On.SSOracleBehavior.NewAction += SSOracleBehavior_NewAction;
            OnEventTrigger += CustomOracle_OnEventTrigger;

        }

        internal static void OnEventTriggerInternal(IOwnAConversation owner, CustomSpecialEvent eventData)
            => OnEventTrigger?.Invoke(owner, eventData);



        private static void CustomOracle_OnEventTrigger(IOwnAConversation owner, CustomSpecialEvent eventData)
        {
            try
            {
                if (owner is SSOracleBehavior ss)
                {
                    switch (eventData.eventName)
                    {
                        case "gravity":
                            ss.oracle.gravity = int.Parse(eventData.args[0]);
                            break;
                        case "locked":
                            ss.LockShortcuts();
                            break;
                        case "unlocked":
                            ss.UnlockShortcuts();
                            break;
                        case "work":
                            ss.getToWorking = int.Parse(eventData.args[0]);
                            break;
                        case "behavior":
                            if (ExtEnumBase.TryParse(typeof(SSOracleBehavior.MovementBehavior), eventData.args[0], true,
                                    out var re))
                                ss.movementBehavior = (SSOracleBehavior.MovementBehavior)re;
                            else
                                BuffUtils.LogError("BuffOracleExtend",
                                    $"Unknown movement behavior:{eventData.args[0]}");
                            break;
                        case "sound":
                            if (ExtEnumBase.TryParse(typeof(SoundID), eventData.args[0], true, out var soundId))
                            {
                                if (eventData.args.Length == 3)
                                    ss.oracle.room.PlaySound((SoundID)soundId, ss.oracle.firstChunk, false,
                                        float.Parse(eventData.args[1]), float.Parse(eventData.args[2]));
                                else
                                    ss.oracle.room.PlaySound((SoundID)soundId, ss.oracle.firstChunk);

                            }
                            else
                                BuffUtils.LogError("BuffOracleExtend", $"Unknown sound Id:{eventData.args[0]}");

                            break;
                        case "turnOff":
                            ss.TurnOffSSMusic(eventData.args.Length == 0 || bool.Parse(eventData.args[0]));
                            break;
                        case "move":
                            ss.SetNewDestination(new Vector2(float.Parse(eventData.args[0]),
                                float.Parse(eventData.args[1])));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }


        }

        private static void SSOracleBehavior_NewAction(On.SSOracleBehavior.orig_NewAction orig, SSOracleBehavior self,
            SSOracleBehavior.Action nextAction)
        {
            BuffUtils.Log("BuffOracleExtend", $"Action:{nextAction}, {self.action}");
            if (nextAction == CustomOracleBehavior.CustomAction)
            {

                BuffUtils.Log("BuffOracleExtend",
                    $"Use Behavior: {CustomOracleBehavior.CustomBehavior}, action:{nextAction}");
                self.inActionCounter = 0;
                if (self.currSubBehavior.ID == CustomOracleBehavior.CustomBehavior)
                {
                    self.currSubBehavior.Activate(self.action, nextAction);
                    self.action = nextAction;
                    return;
                }

                SSOracleBehavior.SubBehavior subBehavior = null;
                foreach (var behavior in self.allSubBehaviors)
                {
                    if (behavior.ID == CustomOracleBehavior.CustomBehavior)
                    {
                        subBehavior = behavior;
                        break;
                    }
                }

                if (subBehavior == null)
                {
                    subBehavior = new CustomOracleBehavior(self);
                    self.allSubBehaviors.Add(subBehavior);
                }

                subBehavior.Activate(self.action, nextAction);
                self.action = nextAction;

                self.currSubBehavior.Deactivate();
                self.currSubBehavior = subBehavior;

            }
            else
            {
                orig(self, nextAction);
            }
        }

        private static void SSOracleBehavior_SeePlayer(On.SSOracleBehavior.orig_SeePlayer orig, SSOracleBehavior self)
        {
            bool seePeople = false;
            foreach (var player in self.oracle.room.game.Players)
                if (player.realizedCreature is Player)
                    seePeople = true;

            if (seePeople && CustomSeePlayer(self))
            {
                self.NewAction(CustomOracleBehavior.CustomAction);
                BuffUtils.Log("BuffOracleExtend","Custom See Player");
                return;
            }

            orig(self);
        }

        private static bool CustomSeePlayer(OracleBehavior self)
        {
            if (OnSeePlayer == null)
                return false;

            foreach (var deg in OnSeePlayer.GetInvocationList())
            {
                try
                {
                    var re = (deg as OracleSeePlayerHandler).Invoke(self);
                    if (re) return true;
                }
                catch (Exception e)
                {
                    BuffUtils.LogException($"BuffOracleExtend - OracleSeePlayerHandler", e);
                }
            }

            return false;
        }

        internal static void LoadCustomConversationInternal(OracleBehavior behavior, Conversation conversation)
        {
            OnLoadConversation?.Invoke(behavior,conversation);
        }
    }


    internal class CustomOracleBehavior : SSOracleBehavior.ConversationBehavior
    {
        public static readonly SubBehavID CustomBehavior = new($"Buff.{nameof(CustomBehavior)}", true);
        public static readonly Conversation.ID PlaceHolder = new($"Buff.{nameof(PlaceHolder)}", true);

        public static readonly SSOracleBehavior.Action CustomAction = new($"Buff.{nameof(CustomAction)}", true);

        public CustomOracleBehavior(SSOracleBehavior owner) : base(owner, CustomBehavior, PlaceHolder)
        {
        }



        public override void Update()
        {
            base.Update();
            if (owner.oracle.room.game.cameras.All(i => i.room != owner.oracle.room))
            {
                return;
            }

            if (owner.conversation != null && owner.conversation.slatedForDeletion)
            {
                BuffUtils.Log("BuffOracleExtend", "End Conv");
                owner.conversation = null;
            }
        }

        public override void NewAction(SSOracleBehavior.Action oldAction, SSOracleBehavior.Action newAction)
        {
            base.NewAction(oldAction, newAction);
            if (newAction == CustomAction && oldAction != SSOracleBehavior.Action.General_GiveMark)
            {
                owner.InitateConversation(PlaceHolder, this);
                OracleHooks.LoadCustomConversationInternal(owner, owner.conversation);

            }

        }
    }
}
