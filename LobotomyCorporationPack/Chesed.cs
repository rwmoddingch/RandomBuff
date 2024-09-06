using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HUD;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative.SephirahMeltdown
{
    internal class ChesedBuffData : SephirahMeltdownBuffData
    {
        public static readonly BuffID Chesed = new BuffID(nameof(Chesed), true);
        public override BuffID ID => Chesed;

        public float DeathMulti => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, 5f);
        public float SpeedMulti => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, (SephirahMeltdownEntry.Hell ? 2 : 1.5f));
        public float SpeedMulti2 => Custom.LerpMap(CycleUse, 0, MaxCycleCount - 1, 1, 1.5f);



    }



    internal class ChesedBuff : Buff<ChesedBuff,ChesedBuffData>
    {
        public override BuffID ID => ChesedBuffData.Chesed;

        public readonly HashSet<ChesedEnum> activeEnums = new HashSet<ChesedEnum>();

        private readonly int enumCount;

        public static ChesedHudPart hudPart;


        public ChesedBuff()
        {
            enumCount = Data.CycleUse == 0 ? 1 : Data.CycleUse == 3 ? 3 : 2;
            if (hudPart == null && BuffCustom.TryGetGame(out var game) && game.cameras[0].hud is HUD.HUD hud)
                hud.AddPart(hudPart = new ChesedHudPart(hud));
            MyTimer = new DownCountBuffTimer((timer, worldGame) => GetRandomEnum(enumCount), 60);
            GetRandomEnum(enumCount);
        }

        public override void Destroy()
        {
            base.Destroy();
            hudPart?.WaitForClean();
            hudPart = null;
        }

        private void GetRandomEnum(int count)
        {
            activeEnums.Clear();
            for(int i = 0; i < count;i++)
                while (!activeEnums.Add((ChesedEnum)Random.Range(0, 3))) ;

            foreach (var en in activeEnums)
                BuffUtils.Log(ChesedBuffData.Chesed,$"Now active: {en.ToString()}");
            

        }


        private const int WaitCounter = 60 * 40;

    }

    enum ChesedEnum
    {
        Damage,
        Resistance,
        Speed,
    }

    internal class ChesedBuffHook
    {
        public static void HookOn()
        {
            On.Player.DeathByBiteMultiplier += Player_DeathByBiteMultiplier;
            On.VultureAI.OnlyHurtDontGrab += VultureAI_OnlyHurtDontGrab;


            On.Lizard.GetFrameSpeed += Lizard_GetFrameSpeed;
            IL.BigSpider.MoveTowards += BigSpider_MoveTowards;
            On.Centipede.Crawl += Centipede_Crawl;
            On.Centipede.Fly += Centipede_Fly;
            On.DropBug.MoveTowards += DropBug_MoveTowards;


            On.Creature.Violence += Creature_Violence;
            On.Lizard.Violence += Lizard_Violence;

            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;


        }

        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if(ChesedBuff.hudPart == null)
                self.AddPart(ChesedBuff.hudPart = new ChesedHudPart(self));
        }

        private static void DropBug_MoveTowards(On.DropBug.orig_MoveTowards orig, DropBug self, Vector2 moveTo)
        {
            orig(self, moveTo);
            if (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Speed))
                orig(self, moveTo);
        }

        private static void Centipede_Fly(On.Centipede.orig_Fly orig, Centipede self)
        {
            orig(self);
            if (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Speed))
                orig(self);
        }

        private static void Centipede_Crawl(On.Centipede.orig_Crawl orig, Centipede self)
        {
            orig(self);
            if (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Speed))
                orig(self);
        }

        private static void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Resistance))
            {
                damage /= 3;
                stunBonus /= 3;
            }
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (ChesedBuff.Instance != null)
            {
                if (!(self is Player) && ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Resistance))
                {
                    damage /= 3;

                    if(SephirahMeltdownEntry.Hell)
                        stunBonus /= 3;
                }
                else if (self is Player && ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Damage) &&
                         !(source?.owner is Lizard))
                {
                    damage *= 3;
                    stunBonus *= 2;

                }
            }
        
            orig(self,source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus + (SephirahMeltdownEntry.Hell ?0: (damage * 30f) / self.Template.baseStunResistance));
        }

        private static void BigSpider_MoveTowards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After, i => i.MatchLdcR4(4.1f));
            c.EmitDelegate<Func<float, float>>(f => f * (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Speed) ? 2f : 1f));
        }

        private static float Lizard_GetFrameSpeed(On.Lizard.orig_GetFrameSpeed orig, Lizard self, float runSpeed)
        {
            return orig(self, runSpeed) * (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Speed) ? (SephirahMeltdownEntry.Hell ? 2.5f : 1.75f) : 1f);
        }

        private static bool VultureAI_OnlyHurtDontGrab(On.VultureAI.orig_OnlyHurtDontGrab orig, VultureAI self, PhysicalObject testObj)
        {
            if (testObj is Player)
                return ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Damage);
            return orig(self, testObj);
        }

        private static float Player_DeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            var re = orig(self);
            return re + (ChesedBuff.Instance.activeEnums.Contains(ChesedEnum.Damage) ? 4f : 0f);
        }
    }


    internal class ChesedHudPart : HudPart
    {
        public const float Scale = 0.4f;
        public const float SafeBorder = 15;
        public ChesedHudPart(HUD.HUD hud) : base(hud)
        {
            var shader = hud.rainWorld.Shaders["SephirahMeltdownEntry.GrayCast"];
            sprites = new FSprite[3];
            sprites[0] = new FSprite("Chesed-D") { scale = Scale, anchorY = 1, shader = shader,color = new Color(0,1,1), alpha = 0};
            sprites[1] = new FSprite("Chesed-R") { scale = Scale, anchorY = 1,anchorX = 1,shader = shader, color = new Color(0, 1, 1), alpha = 0 };
            sprites[2] = new FSprite("Chesed-S") { scale = Scale, anchorY = 1,anchorX = 0, shader = shader, color = new Color(0, 1, 1), alpha = 0 };
            Container.AddChild(sprites[0]);
            Container.AddChild(sprites[1]);
            Container.AddChild(sprites[2]);
            sprites[1].SetPosition(hud.rainWorld.screenSize.x / 2f, hud.rainWorld.screenSize.y - SafeBorder);
            sprites[2].SetPosition(hud.rainWorld.screenSize.x / 2f, hud.rainWorld.screenSize.y - SafeBorder);
            sprites[0].SetPosition(hud.rainWorld.screenSize.x / 2f, hud.rainWorld.screenSize.y - SafeBorder - sprites[1].height);

            lastGrays = new float[3];
            grays = new float[3];

        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            for (int i = 0; i < 3; i++)
            {
                sprites[i].alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                sprites[i].color = new Color(Mathf.Lerp(lastGrays[i], grays[i], timeStacker),1,1);
            }
        }

        public override void Update()
        {
            base.Update();
            lastAlpha = alpha;
            alpha = Mathf.Lerp(alpha, waitForClean ? 0 : 1, 0.05f);
            for (int i = 0; i < 3; i++)
            {
                lastGrays[i] = grays[i];
                grays[i] = Mathf.Lerp(grays[i], (!waitForClean && ChesedBuff.Instance.activeEnums.Contains((ChesedEnum)i)) ? 0 : 1, 0.1f);
            }

            if(alpha < 0.01f && waitForClean)
                ClearSprites();
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            foreach (var sprite in sprites)
                sprite.RemoveFromContainer();
            
            hud.parts.Remove(this);
        }


        public void WaitForClean()
        {
            waitForClean = true;
        }

        private bool waitForClean = false;
        private float alpha = 0;
        private float lastAlpha = 0;

        private float[] lastGrays;
        private float[] grays;

        private readonly FSprite[] sprites;

        private FContainer Container => hud.fContainers[1];
    }

}
