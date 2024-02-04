
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

//namespace BuiltinBuffs.Negative
//{
//    internal class UpgradationBuff : Buff<UpgradationBuff, UpgradationBuffData>, IOwnCardTimer
//    {
//        public override BuffID ID => UpgradationIBuffEntry.UpgradationBuffID;

//        int _timer;
//        public int CurrentCount { get => _timer / 40; }
//        public bool HideBelowZero => true;

//        public List<int> ignoreID = new List<int>();

//        public UpgradationBuff() : base()
//        {
//            _timer = 30 * 40;
//        }

//        public override void Update(RainWorldGame game)
//        {
//            base.Update(game);
//            if (_timer > 0 && !game.paused)
//                _timer--;

//            if (_timer == 0)
//            {
//                CheckRoom(game); 
//            }
//        }

//        public override bool Trigger(RainWorldGame game)
//        {
//            return false;
//        }

//        public void CheckRoom(RainWorldGame game)
//        {
//            if (game.Players[0].realizedCreature == null)
//                return;
//            if (game.Players[0].realizedCreature.room == null)
//                return;

//            var lst = new List<Creature>();
//            foreach(var obj in game.Players[0].realizedCreature.room.updateList)
//            {
//                if (obj is Player)
//                    continue;
//                if (obj is Creature creature)
//                {
//                    if (IgnoreThisType(creature.abstractCreature.creatureTemplate.type))
//                        continue;
//                    if (GetUperAndBetterType(creature.abstractCreature.creatureTemplate.type, false) == creature.abstractCreature.creatureTemplate.type)
//                        continue;
//                    if (ignoreID.Contains(creature.abstractCreature.ID.number))
//                        continue;

//                    lst.Add(creature);
//                }
//            }

//            if(lst.Count > 0)
//            {
//                var target = lst[Random.Range(0, lst.Count)];
//                ignoreID.Add(target.abstractCreature.ID.number);
//                target.room.AddObject(new UpgradationSign(target.DangerPos, target.room));
//                if (SpawnUperCreature(target.abstractCreature))
//                {
//                    target.Destroy();
//                    target.abstractCreature.Destroy();
//                }
//                _timer = 30 * 40;
//                //BuffPool.Singleton.TriggerBuff(ID, true);
//            }
//        }

//        public static CreatureTemplate.Type GetUperAndBetterType(CreatureTemplate.Type origType, bool upgradation)
//        {
//            CreatureTemplate.Type result = origType;

//            if (origType == CreatureTemplate.Type.SmallCentipede)
//            {
//                if (Random.value < 0.9f) result = CreatureTemplate.Type.Centipede;
//                else result = CreatureTemplate.Type.RedCentipede;
//            }
//            else if (origType == CreatureTemplate.Type.Centipede)
//            {
//                if (Random.value < 0.4f) result = CreatureTemplate.Type.Centipede;
//                else result = CreatureTemplate.Type.RedCentipede;
//            }
//            else if (origType == CreatureTemplate.Type.RedCentipede) result = CreatureTemplate.Type.RedCentipede;
//            else if (origType == CreatureTemplate.Type.RedLizard) result = origType;
//            else if (origType == CreatureTemplate.Type.GreenLizard) result = MoreSlugcatsEnums.CreatureTemplateType.SpitLizard;
//            else if (origType == CreatureTemplate.Type.PinkLizard || origType == CreatureTemplate.Type.BlueLizard) result = CreatureTemplate.Type.CyanLizard;
//            else if (origType == CreatureTemplate.Type.CyanLizard)
//            {
//                if (upgradation)
//                    result = CreatureTemplate.Type.CyanLizard;
//                else
//                    result = CreatureTemplate.Type.RedLizard;//随便给一个以通过测试
//            }
//            else if (origType == CreatureTemplate.Type.WhiteLizard) result = CreatureTemplate.Type.WhiteLizard;
//            else if (origType == CreatureTemplate.Type.Salamander)
//            {
//                if (Random.value < 0.5f) result = origType;
//                else result = MoreSlugcatsEnums.CreatureTemplateType.EelLizard;
//            }
//            else if (StaticWorld.GetCreatureTemplate(origType).ancestor != null && StaticWorld.GetCreatureTemplate(origType).ancestor.type == CreatureTemplate.Type.LizardTemplate)
//            {
//                if (Random.value < 0.3f) result = MoreSlugcatsEnums.CreatureTemplateType.TrainLizard;
//                else result = CreatureTemplate.Type.RedLizard;
//            }
//            else if (origType == CreatureTemplate.Type.Scavenger) result = MoreSlugcatsEnums.CreatureTemplateType.ScavengerElite;
//            else if (origType == CreatureTemplate.Type.BigSpider) result = CreatureTemplate.Type.SpitterSpider;
//            else if (origType == CreatureTemplate.Type.Vulture)
//            {
//                if (Random.value < 0.3f) result = MoreSlugcatsEnums.CreatureTemplateType.MirosVulture;
//                else result = CreatureTemplate.Type.KingVulture;
//            }
//            else if (origType == CreatureTemplate.Type.KingVulture) result = origType;
//            else if (origType == MoreSlugcatsEnums.CreatureTemplateType.MirosVulture) result = origType;
//            else if (origType == CreatureTemplate.Type.MirosBird) result = origType;
//            else if (origType == CreatureTemplate.Type.BrotherLongLegs) result = CreatureTemplate.Type.DaddyLongLegs;
//            else if (origType == CreatureTemplate.Type.DaddyLongLegs) result = MoreSlugcatsEnums.CreatureTemplateType.TerrorLongLegs;
//            else if (origType == CreatureTemplate.Type.SmallNeedleWorm) result = CreatureTemplate.Type.BigNeedleWorm;
//            else if (origType == CreatureTemplate.Type.DropBug)
//            {
//                if (Random.value < 0.2f) result = MoreSlugcatsEnums.CreatureTemplateType.StowawayBug;
//                else result = origType;
//            }
//            else if (origType == CreatureTemplate.Type.EggBug) result = MoreSlugcatsEnums.CreatureTemplateType.FireBug;
//            else if (origType == CreatureTemplate.Type.Spider)
//            {
//                if (upgradation)
//                {
//                    if (Random.value < 0.8f)
//                        result = CreatureTemplate.Type.BigSpider;
//                    else
//                        result = CreatureTemplate.Type.SpitterSpider;
//                }
//                else
//                {
//                    if (Random.value < 0.7f)
//                        result = origType;
//                    else if (Random.value < 0.2f)
//                        result = CreatureTemplate.Type.BigSpider;
//                    else
//                        result = CreatureTemplate.Type.SpitterSpider;
//                }
//            }

//            return result;
//        }

//        public static bool IgnoreThisType(CreatureTemplate.Type type)
//        {
//            return (StaticWorld.GetCreatureTemplate(type).TopAncestor().type == CreatureTemplate.Type.Leech) ||
//                (type == CreatureTemplate.Type.Overseer) ||
//                (type == CreatureTemplate.Type.Fly);
//        }

//        public static bool SpawnUperCreature(AbstractCreature origCreature)
//        {
//            AbstractRoom abRoom = origCreature.Room;
//            World world = abRoom.world;
//            CreatureTemplate.Type type = GetUperAndBetterType(origCreature.creatureTemplate.type, true);

//            WorldCoordinate pos = origCreature.pos;
//            AbstractCreature abstractCreature = new AbstractCreature(world, StaticWorld.GetCreatureTemplate(type), null, pos, world.game.GetNewID());
//            origCreature.Room.AddEntity(abstractCreature);
//            abstractCreature.RealizeInRoom();
//            return type != origCreature.creatureTemplate.type;
//        }
//    }

//    public class UpgradationSign : CosmeticSprite
//    {
//        public static Color color;
//        public float life = 80;

//        static UpgradationSign()
//        {
//            ColorUtility.TryParseHtmlString("#FF019A", out color);
//        }

//        public UpgradationSign(Vector2 pos, Room room)
//        {
//            this.room = room;
//            this.pos = pos;
//        }

//        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
//        {
//            sLeaser.sprites = new FSprite[3];
//            sLeaser.sprites[0] = new CustomFSprite("pixel");
//            sLeaser.sprites[1] = new FSprite("pixel", true) { anchorX = 0.5f, anchorY = 1f, color = color * 0.5f + Color.white * 0.5f };
//            sLeaser.sprites[2] = new FSprite("Futile_White", true) { shader = rCam.game.rainWorld.Shaders["FlatLight"], color = color };
//            for (int i = 0; i < 4; i++)
//            {
//                (sLeaser.sprites[0] as CustomFSprite).verticeColors[i] = color * 0.5f + Color.white * 0.5f;
//            }
//            AddToContainer(sLeaser, rCam, null);
//        }

//        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
//        {
//            if (newContatiner == null)
//                newContatiner = rCam.ReturnFContainer("HUD");
//            foreach(var sprite in sLeaser.sprites)
//                newContatiner.AddChild(sprite);
//        }

//        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
//        {
//            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

//            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(0, pos + Vector2.left * 15f - camPos);
//            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(1, pos + Vector2.up * 20f - camPos);
//            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(2, pos + Vector2.right * 15f - camPos);
//            (sLeaser.sprites[0] as CustomFSprite).MoveVertice(3, pos - camPos);

//            for (int i = 0; i < 4; i++)
//            {
//                (sLeaser.sprites[0] as CustomFSprite).verticeColors[i].a = life / 80f;
//            }

//            sLeaser.sprites[1].SetPosition(pos - camPos);
//            sLeaser.sprites[1].width = 10f;
//            sLeaser.sprites[1].height = 15f;
//            sLeaser.sprites[1].alpha = life / 80f;

//            sLeaser.sprites[2].SetPosition(pos - camPos);
//            sLeaser.sprites[2].scale = 10f * life / 80f;
//            sLeaser.sprites[2].alpha = life / 80f;
//        }

//        public override void Update(bool eu)
//        {
//            base.Update(eu);
//            if (life > 0)
//                life--;
//            else
//                Destroy();
//        }
//    }

//    internal class UpgradationBuffData : BuffData
//    {
//        public override BuffID ID => UpgradationIBuffEntry.UpgradationBuffID;
//    }

//    internal class UpgradationIBuffEntry : IBuffEntry
//    {
//        public static BuffID UpgradationID = new BuffID("Upgradation", true);

//        public override void OnEnable()
//        {
//            BuffRegister.RegisterBuff<UpgradationBuff, UpgradationBuffData, UpgradationIBuffEntry>(UpgradationBuffID);
//        }

//        public static void HookOn()
//        {
//        }
//    }
//}
