using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.Game;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class BountyHunterBuff : Buff<BountyHunterBuff, BountyHunterBuffData>, BuffHudPart.IOwnBuffHudPart
    {
        public override BuffID ID => BountyHunterBuffEntry.bountyHUnterBuffID;

        AbstractCreature bountyCreature;
        BountyHunterBuffHUD currentHUD;

        bool init;

        public BountyHunterBuff()
        {
            MyTimer = new DownCountBuffTimer(SelectNextBounty, 120);
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if(!init && bountyCreature == null)
            {
                SelectNextBounty(null, game);
                if(bountyCreature != null)
                    init = true;
            }

            if(currentHUD != null)
            {
                if (bountyCreature != null && bountyCreature.realizedCreature != null)
                {
                    var cam = game.cameras[0];
                    if (bountyCreature.realizedCreature.room == cam.room)
                    {
                        currentHUD.show = true;
                        currentHUD.pos = bountyCreature.realizedCreature.mainBodyChunk.pos + Vector2.up * 60f - game.cameras[0].pos;
                    }
                    else
                        currentHUD.show = false;
                }
                else
                    currentHUD.show = false;
            }

        }

        public BuffHudPart CreateHUDPart()
        {
            return currentHUD = new BountyHunterBuffHUD();
        }

        void SelectNextBounty(BuffTimer timer, RainWorldGame game)
        {
            List<AbstractCreature> lst = new List<AbstractCreature>();
            if (game.world == null || game.world.abstractRooms == null)
                return;

            foreach(var room in game.world.abstractRooms)
            {
                foreach(var creature in room.creatures)
                {
                    if (creature.creatureTemplate.type == CreatureTemplate.Type.Slugcat ||
                        creature.creatureTemplate.type == CreatureTemplate.Type.Overseer ||
                        creature.creatureTemplate.smallCreature || 
                        creature.state.dead)
                        continue;
                    lst.Add(creature);
                }
            }

            if (lst.Count == 0)
                return;

            bountyCreature = lst[Random.Range(0, lst.Count)];

            currentHUD?.ChangeBounty(bountyCreature);
        }

        public void BountyCheck(AbstractCreature abstractCreature, RainWorldGame game)
        {
            if (abstractCreature != bountyCreature)
                return;
            bountyCreature = null;
            var newCard = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 1,
                    BuffType.Positive).First().BuffID;
            GetTemporaryBuffPool().CreateTemporaryBuff(newCard);
            currentHUD?.ChangeBounty(bountyCreature);
            TriggerSelf(true);
            MyTimer.Reset();
        }
    }

    internal class BountyHunterBuffHUD : BuffHudPart
    {
        static Color coinCol = Helper.GetRGBColor(255, 160, 0);
        static Color flashCol = Color.white;

        FSprite coin;
        FSprite flash;
        FLabel testLabel1;

        public bool show;
        public Vector2 pos;
        public Vector2 lastPos;

        int flashCounter;
        int lastFlashConuter;

        int showCounter;

        public override void InitSprites(HUD.HUD hud)
        {
            hud.fContainers[0].AddChild(flash = new FSprite("Futile_White"));
            flash.shader = Custom.rainWorld.Shaders["FlatLight"];
            flash.color = flashCol;
            flash.alpha = 0f;

            hud.fContainers[0].AddChild(coin = new FSprite("pixel"));
            coin.rotation = 45;
            coin.scale = 10f;
            coin.alpha = 0f;

            //hud.fContainers[0].AddChild(testLabel1 = new FLabel(Custom.GetFont(), ""));
            //testLabel1.SetPosition(300f, 400f);
        }

        public override void Update(HUD.HUD hud)
        {
            lastPos = pos;
            lastFlashConuter = flashCounter;

            if(flashCounter < 40)
            {
                flashCounter++;
                if(flashCounter == 40)
                {
                    lastFlashConuter = flashCounter = 0;
                }
            }

            if (show)
            {
                if (showCounter < 40)
                    showCounter++;
            }
            else
            {
                if (showCounter > 0)
                    showCounter--;
            }
        }

        public override void Draw(HUD.HUD hud, float timeStacker)
        {
            if(showCounter >= 0)
            {
                float alpha = showCounter / 40f;
                float smoothflash = Mathf.Lerp(lastFlashConuter / 40f, flashCounter / 40f, timeStacker);
                Vector2 smoothPos = Vector2.Lerp(lastPos, pos ,timeStacker);

                coin.SetPosition(smoothPos);
                coin.alpha = alpha;
                coin.color = Color.Lerp(flashCol, coinCol, 1f - Mathf.Sin(Helper.EaseInOutCubic(Mathf.Pow(smoothflash, 2f)) * Mathf.PI));

                flash.SetPosition(smoothPos);
                flash.alpha = 0.5f * Mathf.Sin(smoothflash * Mathf.PI);
                flash.scaleX = (0.3f + 0.7f * Helper.LerpEase(smoothflash)) * 6f;
                flash.scaleY = 1.5f;

                if (showCounter == 0 && !show)
                    showCounter--;
            }
            else
            {
                coin.alpha = 0f;
                flash.alpha = 0f;
            }
        }

        public override void ClearSprites()
        {
            coin.RemoveFromContainer();
            flash.RemoveFromContainer();
            base.ClearSprites();
        }

        public void ChangeBounty(AbstractCreature abstractCreature)
        {
            //if (abstractCreature != null)
            //    testLabel1.text = $"{abstractCreature.creatureTemplate.type} {abstractCreature.ID.number}";
            //else
            //    testLabel1.text = "empty";
        }
    }



    internal class BountyHunterBuffData : BuffData
    {
        public override BuffID ID => BountyHunterBuffEntry.bountyHUnterBuffID;
    }



    internal class BountyHunterBuffEntry : IBuffEntry
    {
        public static BuffID bountyHUnterBuffID = new BuffID("BountyHunter", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<BountyHunterBuff, BountyHunterBuffData, BountyHunterBuffEntry>(bountyHUnterBuffID);
        }

        public static void HookOn()
        {
            On.PlayerSessionRecord.AddKill += PlayerSessionRecord_AddKill;
        }

        private static void PlayerSessionRecord_AddKill(On.PlayerSessionRecord.orig_AddKill orig, PlayerSessionRecord self, Creature victim)
        {
            orig.Invoke(self, victim);
            BountyHunterBuff.Instance.BountyCheck(victim.abstractCreature, victim.abstractCreature.Room.world.game);
        }
    }

}
