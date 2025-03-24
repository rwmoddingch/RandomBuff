using BuiltinBuffs.Positive;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Missions.UltraKill
{
    internal class UltraKillEventManager
    {
        //                                          D   C     B    A    S    SS     SSS   ULTRAKILL
        static float[] scoreStage = new float[] { 0f, 10f, 25f, 45f, 70f, 100f, 130f, 200f, 300f };
        static float[] scoreDropRate = new float[] { 0.1f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f, 1.2f, 1.5f };

        public UltraKillMissionHUD hud;
        public RainWorldGame game;

        internal float score;
        int currentStage;

        List<UltraKillEventTrackerBase> eventTrackers = new List<UltraKillEventTrackerBase>();
        public List<UltraKillEvent> trackedEvents = new List<UltraKillEvent>();

        public UltraKillEventManager(UltraKillMissionHUD hud, RainWorldGame game)
        {
            this.hud = hud;
            this.game = game;
            InitTrackers();
        }

        void InitTrackers()
        {
            eventTrackers.Add(new KillTracker(this));
            eventTrackers.Add(new AirShotTracker(this));
            eventTrackers.Add(new UltraCoinsTracker(this));
            eventTrackers.Add(new KnockBackTracker(this));
            eventTrackers.Add(new BodyAnimTracker(this));
            eventTrackers.Add(new EscapeTracker(this));
            eventTrackers.Add(new ExplosionTracker(this));
            eventTrackers.Add(new InDangerTracker(this));
            eventTrackers.Add(new ViolenceTracker(this));
            eventTrackers.Add(new ParryTracker(this));
            eventTrackers.Add(new TheftTracker(this));

            foreach (var eventTracker in eventTrackers)
                eventTracker.HooksOn();
        }

        public void Destroy()
        {
            foreach (var eventTracker in eventTrackers)
                eventTracker.HooksOff();
        }

        public void Update()
        {
            if (score > 0)
            {
                score = Mathf.Max(0f, score - (1 / 40f) * scoreDropRate[currentStage]);
            }

            foreach (var tracker in eventTrackers)
            {
                tracker.Update();
            }
            foreach (var e in trackedEvents)
                e.Update();

            if (currentStage < 7)
            {
                while (score >= scoreStage[currentStage + 1])
                {
                    currentStage++;
                    hud.UpdateTitle(currentStage, true);
                }
            }
            if (currentStage > 0)
            {
                while (score < scoreStage[currentStage])
                {
                    currentStage--;
                    hud.UpdateTitle(currentStage, false);
                }
            }
            hud.percentage = (score - scoreStage[currentStage]) / (scoreStage[currentStage + 1] - scoreStage[currentStage]);
        }

        public void TrackEvent(UltraKillEvent e)
        {
            trackedEvents.Add(e);
            hud.AddEvent(e);
        }

        public void EventUpdate()
        {
            hud.UpdateEventLabels();
        }
    }

    internal class UltraKillEventType : ExtEnum<UltraKillEventType>
    {
        public static readonly UltraKillEventType Kill = new UltraKillEventType("KILL", true);
        public static readonly UltraKillEventType Ricoshot = new UltraKillEventType("RICOSHOT", true);
        public static readonly UltraKillEventType KnockBack = new UltraKillEventType("KNOC BACK", true);
        public static readonly UltraKillEventType AirShot = new UltraKillEventType("AIRSHOT", true);
        public static readonly UltraKillEventType Parry = new UltraKillEventType("PARRY!", true);
        public static readonly UltraKillEventType Explode = new UltraKillEventType("EXPLODE", true);
        public static readonly UltraKillEventType Electric = new UltraKillEventType("ELECTRIC", true);
        public static readonly UltraKillEventType Glide = new UltraKillEventType("GLIDE", true);
        public static readonly UltraKillEventType Flip = new UltraKillEventType("FLIP", true);
        public static readonly UltraKillEventType Escape = new UltraKillEventType("ESCAPE", true);
        public static readonly UltraKillEventType Penetrate = new UltraKillEventType("PENETRATE", true);
        public static readonly UltraKillEventType AirBurst = new UltraKillEventType("AIR BURST", true);
        public static readonly UltraKillEventType InDanger = new UltraKillEventType("IN DANGER", true);
        public static readonly UltraKillEventType MultiHit = new UltraKillEventType("MACHINE GUN", true);
        public static readonly UltraKillEventType ThroatHit = new UltraKillEventType("THROAT", true);
        public static readonly UltraKillEventType Theft = new UltraKillEventType("THEFT", true);

        public UltraKillEventType(string value, bool register) : base(value, register) { }
    }

    #region tracker

    internal class UltraKillEventTrackerBase
    {
        public UltraKillEventManager eventManager;
        public UltraKillEventTrackerBase(UltraKillEventManager eventManager)
        {
            this.eventManager = eventManager;
        }
        public virtual void Update()
        {
        }

        public virtual void HooksOn()
        {
        }

        public virtual void HooksOff()
        {
        }
    }
    internal class UltraKillEventTracker<T> : UltraKillEventTrackerBase where T : UltraKillEventTracker<T>
    {
        public static T Instance { get; private set; }

        public UltraKillEventTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            Instance = (T)this;
        }

        public override void HooksOff()
        {
            Instance = null;
        }

    }

    internal class KillTracker : UltraKillEventTracker<KillTracker>
    {
        public KillTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Creature.Die += Creature_Die;
        }

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            bool origState = self.dead;
            orig.Invoke(self);
            if (self.dead && !origState)
                TrackKill();
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Creature.Die -= Creature_Die;
        }

        public void TrackKill()
        {
            bool newEvent = true;
            foreach (var tEvent in eventManager.trackedEvents)
            {
                if (tEvent is KillEvent killEvent && !killEvent.EventClose)
                {
                    newEvent = false;
                    killEvent.Stack();
                    eventManager.score += killEvent.stackCount * 10;
                    eventManager.EventUpdate();
                }
            }
            if (newEvent)
            {
                eventManager.TrackEvent(new KillEvent());
            }
        }
    }

    internal class AirShotTracker : UltraKillEventTracker<AirShotTracker>
    {
        public AirShotTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Player.ThrownSpear += Player_ThrownSpear;
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Player.ThrownSpear -= Player_ThrownSpear;
        }

        private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
        {
            orig.Invoke(self, spear);
            if (self.firstChunk.contactPoint.y != -1 && self.bodyChunks[1].contactPoint.y != -1)
            {
                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.AirShot, 0));
                eventManager.score += 1f;
            }

        }
    }

    internal class UltraCoinsTracker : UltraKillEventTracker<UltraCoinsTracker>
    {
        public UltraCoinsTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            UltraCoinsBuffEntry.richshotCallBack += OnCoinRicoshot;
            UltraCoinsBuffEntry.penetrateCallBack += OnCoinPenetrate;
        }

        public void OnCoinPenetrate()
        {
            bool newEvent = true;
            foreach (var tEvent in eventManager.trackedEvents)
            {
                if (tEvent is PenetrateEvent penetrateEvent && !penetrateEvent.EventClose)
                {
                    newEvent = false;
                    penetrateEvent.Stack();
                    eventManager.score += 0.5f;
                    eventManager.EventUpdate();
                }
            }
            if (newEvent)
            {
                eventManager.TrackEvent(new PenetrateEvent());
            }
        }

        public void OnCoinRicoshot()
        {
            bool newEvent = true;
            foreach (var tEvent in eventManager.trackedEvents)
            {
                if (tEvent is RicoshotEvent ricoshotEvent && !ricoshotEvent.EventClose)
                {
                    newEvent = false;
                    ricoshotEvent.Stack();
                    eventManager.score += ricoshotEvent.stackCount * 0.5f;
                    eventManager.EventUpdate();
                }
            }
            if (newEvent)
            {
                eventManager.TrackEvent(new RicoshotEvent());
            }
        }

        public override void HooksOff()
        {
            base.HooksOff();
            UltraCoinsBuffEntry.richshotCallBack -= OnCoinRicoshot;
            UltraCoinsBuffEntry.penetrateCallBack -= OnCoinPenetrate;
        }
    }

    internal class KnockBackTracker : UltraKillEventTracker<KnockBackTracker>
    {
        public KnockBackTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Lizard.Violence += Lizard_Violence;
        }

        private void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if ((source != null && source.owner is Rock) && directionAndMomentum != null && self.HitHeadShield(directionAndMomentum.Value))
            {
                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.KnockBack, -1));
                eventManager.score += 0.5f;
            }
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Lizard.Violence -= Lizard_Violence;
        }
    }

    internal class BodyAnimTracker : UltraKillEventTracker<BodyAnimTracker>
    {
        int glidecCoolDown, flipCoolDown;
        public BodyAnimTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void Update()
        {
            base.Update();
            if (glidecCoolDown > 0)
                glidecCoolDown--;
            if(flipCoolDown > 0) 
                flipCoolDown--;

            foreach (var player in eventManager.game.Players)
            {
                if(player.realizedCreature != null)
                {
                    if (glidecCoolDown == 0 && (player.realizedCreature as Player).animation == Player.AnimationIndex.BellySlide)
                    {
                        glidecCoolDown = 120;
                        eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Glide, -1));
                        eventManager.score += 0.5f;
                        break;
                    }
                    if(flipCoolDown == 0 && (player.realizedCreature as Player).animation == Player.AnimationIndex.Flip)
                    {
                        flipCoolDown = 120;
                        eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Flip, -1));
                        eventManager.score += 0.7f;
                        break;
                    }
                }
            }
        }
    }

    internal class EscapeTracker : UltraKillEventTracker<EscapeTracker>
    {
        public EscapeTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Player.ThrowToGetFree += Player_ThrowToGetFree;
        }

        private void Player_ThrowToGetFree(On.Player.orig_ThrowToGetFree orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Escape, 6));
            eventManager.score += 15f;
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Player.ThrowToGetFree -= Player_ThrowToGetFree;
        }
    }

    internal class ExplosionTracker : UltraKillEventTracker<ExplosionTracker>
    {
        public ExplosionTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Explosion.ctor += Explosion_ctor;
            On.ScavengerBomb.HitByWeapon += ScavengerBomb_HitByWeapon;
        }

        private void ScavengerBomb_HitByWeapon(On.ScavengerBomb.orig_HitByWeapon orig, ScavengerBomb self, Weapon weapon)
        {
            orig.Invoke(self, weapon);
            if(self.mode == Weapon.Mode.Thrown)
            {
                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.AirBurst, 6));
                eventManager.score += 10f;
            }
        }

        private void Explosion_ctor(On.Explosion.orig_ctor orig, Explosion self, Room room, PhysicalObject sourceObject, Vector2 pos, int lifeTime, float rad, float force, float damage, float stun, float deafen, Creature killTagHolder, float killTagHolderDmgFactor, float minStun, float backgroundNoise)
        {
            orig.Invoke(self, room, sourceObject, pos, lifeTime, rad, force, damage, stun, deafen, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise);
            if(killTagHolder is Player)
            {
                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Explode, -1));
                eventManager.score += 1f;
            }
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Explosion.ctor -= Explosion_ctor;
            On.ScavengerBomb.HitByWeapon -= ScavengerBomb_HitByWeapon;
        }
    }

    internal class InDangerTracker : UltraKillEventTracker<InDangerTracker>
    {
        float dangerValue;
        int cd;
        public InDangerTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void Update()
        {
            base.Update();
            if (cd > 0)
                cd--;
            if (dangerValue > 0f)
                dangerValue -= 1 / 80f;
            if(dangerValue > 3f)
            {
                cd = 120;
                dangerValue = 0f;

                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.InDanger, 4));
                eventManager.score += 10f;
            }
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Player.Update += Player_Update;
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if(cd == 0)
                dangerValue += PlayerDangerBonus(self) / 40f;
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Player.Update -= Player_Update;
        }

        private static float PlayerDangerBonus(Player player)
        {
            if (player.room == null || player.inShortcut)
                return 0;
            float re = 0;
            foreach (var crit in player.room.updateList.OfType<Creature>())
            {
                if (crit is Player) continue;
                if (crit.abstractCreature.abstractAI?.RealAI?.friendTracker is FriendTracker tracker &&
                    tracker.friend == player)
                    continue;

                re = Mathf.Max(re, Custom.LerpMap(crit.bodyChunks.Min(i => Custom.Dist(i.pos, player.DangerPos)), 80,
                    200, 1,
                    0) * crit.Template.dangerousToPlayer * 2);

            }

            return re;
        }
    }

    internal class ViolenceTracker : UltraKillEventTracker<ViolenceTracker>
    {
        public static int detectTime = 80;
        int hit;
        int life;
        public ViolenceTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void Update()
        {
            base.Update();
            if(life > 0)
            {
                life--;
                if (life == 0)
                    hit = 0;
            }
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Creature.Violence += Creature_Violence;
            On.Lizard.Violence += Lizard_Violence;
            On.Lizard.HitInMouth += Lizard_HitInMouth;
        }

        private bool Lizard_HitInMouth(On.Lizard.orig_HitInMouth orig, Lizard self, Vector2 direction)
        {
            bool res = orig.Invoke(self, direction);
            if (res)
            {
                eventManager.score += 2.5f;
                eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.ThroatHit, 2));
            }
            return res;
        }

        private void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if (!self.dead)
                CountViolence();
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (!self.dead)
                CountViolence();
            orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Creature.Violence -= Creature_Violence;
            On.Lizard.Violence -= Lizard_Violence;
            On.Lizard.HitInMouth -= Lizard_HitInMouth;
        }

        void CountViolence()
        {
            hit++;
            life = detectTime;

            if(hit > 5)
            {
                bool newEvent = true;
                foreach (var tEvent in eventManager.trackedEvents)
                {
                    if (tEvent is MultiHitEvent multiHitEvent && !multiHitEvent.EventClose)
                    {
                        newEvent = false;
                        multiHitEvent.Stack();
                        eventManager.score += 1;
                        eventManager.EventUpdate();
                    }
                }
                if (newEvent)
                {
                    eventManager.score += 5;
                    eventManager.TrackEvent(new MultiHitEvent(hit));
                }
            }
        }
    }

    internal class ParryTracker : UltraKillEventTracker<ParryTracker>
    {
        public ParryTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Weapon.WeaponDeflect += Weapon_WeaponDeflect;
        }

        private void Weapon_WeaponDeflect(On.Weapon.orig_WeaponDeflect orig, Weapon self, Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            orig.Invoke(self, inbetweenPos, deflectDir, bounceSpeed);
            if (self.thrownBy is Player)
                return;

            eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Parry, 2));
            eventManager.score += 1.5f;
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Weapon.WeaponDeflect -= Weapon_WeaponDeflect;
        }
    }

    internal class TheftTracker : UltraKillEventTracker<TheftTracker>
    {
        public TheftTracker(UltraKillEventManager eventManager) : base(eventManager)
        {
        }

        public override void HooksOn()
        {
            base.HooksOn();
            On.Scavenger.GrabbedObjectSnatched += Scavenger_GrabbedObjectSnatched;
        }

        private void Scavenger_GrabbedObjectSnatched(On.Scavenger.orig_GrabbedObjectSnatched orig, Scavenger self, PhysicalObject grabbedObject, Creature thief)
        {
            orig.Invoke(self, grabbedObject, thief);
            if(grabbedObject == null || !(thief is Player))
            {
                return;
            }
            eventManager.TrackEvent(new SingleStackInstantEvent(UltraKillEventType.Theft, 6));
            eventManager.score += 3.5f;
        }

        public override void HooksOff()
        {
            base.HooksOff();
            On.Scavenger.GrabbedObjectSnatched -= Scavenger_GrabbedObjectSnatched;
        }
    }
    #endregion

    #region event
    internal class UltraKillEvent
    {
        public int eventLife;
        public int stackCount;
        public bool allowForDeletion;

        public UltraKillEventType eventType;

        public virtual bool EventClose
        {
            get;
        }

        public virtual bool AllowEventStack
        {
            get;
        }

        public UltraKillEvent(UltraKillEventType eventName)
        {
            stackCount++;
            this.eventType = eventName;
        }

        public virtual void Update()
        {
            eventLife++;
        }

        public virtual void Stack()
        {
            if (AllowEventStack)
            {
                stackCount++;
                eventLife = 0;
            }
        }
        
        public virtual string DisplayText()
        {
            return stackCount > 1 ? $"{eventType.value} x {stackCount}" : $"{eventType.value}";
        }

        public virtual int ColorType()
        {
            return -1;
        }
    }

    internal class KillEvent : UltraKillEvent
    {
        public override bool EventClose => eventLife > 80;
        public override bool AllowEventStack => true;
        public KillEvent() : base(UltraKillEventType.Kill)
        {
        }

        public override int ColorType()
        {
            if (stackCount > 1)
                return 7;
            return -1;
        }

        public override string DisplayText()
        {
            if (stackCount == 1)
                return "KILL";
            else if (stackCount == 2)
                return "DOUBLE KILL";
            else if (stackCount == 3)
                return "TRIPLE KILL";
            else
                return $"MULTI KILL x {stackCount}";
        }
    }

    internal class RicoshotEvent : UltraKillEvent
    {
        public override bool EventClose => eventLife > 10;
        public override bool AllowEventStack => true;
        public RicoshotEvent() : base(UltraKillEventType.Ricoshot)
        {
        }

        public override int ColorType()
        {
            return 7;
        }
    }

    internal class PenetrateEvent : UltraKillEvent
    {
        public PenetrateEvent() : base(UltraKillEventType.Penetrate)
        {
        }

        public override int ColorType()
        {
            return Mathf.Clamp(stackCount - 2, -1, 7);
        }

        public override string DisplayText()
        {
            return $"PENETRATE x {stackCount}";
        }

        public override bool AllowEventStack => true;
        public override bool EventClose => eventLife > 10;

    }

    internal class KnockBackEvent : UltraKillEvent
    {
        public override bool EventClose => true;
        public override bool AllowEventStack => false;
        public KnockBackEvent() : base(UltraKillEventType.KnockBack)
        {
        }
    }

    internal class MultiHitEvent : UltraKillEvent
    {
        public override bool EventClose => eventLife > ViolenceTracker.detectTime;
        public override bool AllowEventStack => true;

        public MultiHitEvent(int hit) : base(UltraKillEventType.MultiHit)
        {
            this.stackCount = hit;
        }

        public override int ColorType()
        {
            return 6;
        }

        public override void Stack()
        {
            base.Stack();
        }
    }

    internal class SingleStackInstantEvent : UltraKillEvent
    {
        public override bool AllowEventStack => false;
        public override bool EventClose => true;

        int colorType;

        public SingleStackInstantEvent(UltraKillEventType eventName, int colorType = -1) : base(eventName)
        {
            this.colorType = colorType;
        }

        public override int ColorType()
        {
            return colorType;
        }
    }
    #endregion
}
