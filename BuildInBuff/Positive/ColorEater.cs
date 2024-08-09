using BuiltinBuffs.Positive;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using Color = UnityEngine.Color;
using Random = UnityEngine.Random;

namespace BuildInBuff.Positive
{
    class ColorPicker : Weapon
    {
        public static AbstractPhysicalObject.AbstractObjectType Picker = new AbstractPhysicalObject.AbstractObjectType("ColorPicker",true);
        public ColorPicker(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject, world)
        {
            base.bodyChunks = new BodyChunk[1];
            base.bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 5f, 0.07f);
            this.bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            base.airFriction = 0.999f;
            base.gravity = 0.9f;
            this.bounce = 0.4f;
            this.surfaceFriction = 0.4f;
            this.collisionLayer = 2;
            base.waterFriction = 0.98f;
            base.buoyancy = 0.4f;
            base.firstChunk.loudness = 7f;
            this.tailPos = base.firstChunk.pos;
            this.soundLoop = new ChunkDynamicSoundLoop(base.firstChunk);
        }
        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
        }


        public bool spinning;
        public int stillCounter;
        public int stuckBodyPart;
        public int stuckInChunkIndex;
        //public bool pivotAtTip;
        public PhysicalObject stuckInObject;
        public PhysicalObject.Appendage.Pos stuckInAppendage;
        public float spearDamageBonus;
        public float stuckRotation;
        public void PulledOutOfStuckObject()
        {
            for (int i = 0; i < this.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).Spear == this.abstractPhysicalObject)
                {
                    this.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearAppendageStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).Spear == this.abstractPhysicalObject)
                {
                    this.abstractPhysicalObject.stuckObjects[i].Deactivate();
                    break;
                }
            }
            this.stuckInObject = null;
            this.stuckInAppendage = null;
            this.stuckInChunkIndex = 0;
        }
        public BodyChunk stuckInChunk
        {
            get
            {
                return this.stuckInObject.bodyChunks[this.stuckInChunkIndex];
            }
        }
        public void LodgeInCreature(SharedPhysics.CollisionResult result, bool eu)
        {
            this.stuckInObject = result.obj;
            this.ChangeMode(Weapon.Mode.StuckInCreature);
            if (result.chunk != null)
            {
                this.stuckInChunkIndex = result.chunk.index;
                if (this.stuckBodyPart == -1)
                {
                    this.stuckRotation = Custom.Angle(this.throwDir.ToVector2(), this.stuckInChunk.Rotation);
                }
                base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, new Vector2(0f, 0f));
                Debug.Log("Add spear to creature chunk " + this.stuckInChunk.index.ToString());
                new AbstractPhysicalObject.AbstractSpearStick(this.abstractPhysicalObject, (result.obj as Creature).abstractCreature, this.stuckInChunkIndex, this.stuckBodyPart, this.stuckRotation);
            }
            else if (result.onAppendagePos != null)
            {
                this.stuckInChunkIndex = 0;
                this.stuckInAppendage = result.onAppendagePos;
                this.stuckRotation = Custom.VecToDeg(this.rotation) - Custom.VecToDeg(this.stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage));
                Debug.Log("Add spear to creature Appendage");
                new AbstractPhysicalObject.AbstractSpearAppendageStick(this.abstractPhysicalObject, (result.obj as Creature).abstractCreature, result.onAppendagePos.appendage.appIndex, result.onAppendagePos.prevSegment, result.onAppendagePos.distanceToNext, this.stuckRotation);
            }
            if (this.room.BeingViewed)
            {
                for (int i = 0; i < 8; i++)
                {
                    this.room.AddObject(new WaterDrip(result.collisionPoint, -base.firstChunk.vel * Random.value * 0.5f + Custom.DegToVec(360f * Random.value) * base.firstChunk.vel.magnitude * Random.value * 0.5f, false));
                }
            }
        }
        public override bool HitSomething(SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj == null)
            {
                return false;
            }
            bool flag = false;
            if (this.abstractPhysicalObject.world.game.IsArenaSession && this.abstractPhysicalObject.world.game.GetArenaGameSession.GameTypeSetup.spearHitScore != 0 && this.thrownBy != null && this.thrownBy is Player && result.obj is Creature)
            {
                flag = true;
                if ((result.obj as Creature).State is HealthState && ((result.obj as Creature).State as HealthState).health <= 0f)
                {
                    flag = false;
                }
                else if (!((result.obj as Creature).State is HealthState) && (result.obj as Creature).State.dead)
                {
                    flag = false;
                }
            }
            if (result.obj is Creature)
            {
                if (!(result.obj is Player) || (result.obj as Creature).SpearStick(this, Mathf.Lerp(0.55f, 0.62f, Random.value), result.chunk, result.onAppendagePos, base.firstChunk.vel))
                {
                    float num = this.spearDamageBonus;
                    (result.obj as Creature).Violence(base.firstChunk, new Vector2?(base.firstChunk.vel * base.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, num, 10f);
                    if (result.obj is Player)
                    {
                        Player player = result.obj as Player;
                        player.playerState.permanentDamageTracking += (double)(num / player.Template.baseDamageResistance);
                        if (player.playerState.permanentDamageTracking >= 1.0)
                        {
                            player.Die();
                        }
                    }
                }
            }
            else if (result.chunk != null)
            {
                result.chunk.vel += base.firstChunk.vel * base.firstChunk.mass / result.chunk.mass;
            }
            else if (result.onAppendagePos != null)
            {
                (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, base.firstChunk.vel * base.firstChunk.mass);
            }
            if (result.obj is Creature && (result.obj as Creature).SpearStick(this, Mathf.Lerp(0.55f, 0.62f, Random.value), result.chunk, result.onAppendagePos, base.firstChunk.vel))
            {
                this.room.PlaySound(SoundID.Spear_Stick_In_Creature, base.firstChunk);
                this.LodgeInCreature(result, eu);
                if (flag)
                {
                    this.abstractPhysicalObject.world.game.GetArenaGameSession.PlayerLandSpear(this.thrownBy as Player, this.stuckInObject as Creature);
                }
                return true;
            }
            this.room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, base.firstChunk);
            this.vibrate = 20;
            this.ChangeMode(Weapon.Mode.Free);
            base.firstChunk.vel = base.firstChunk.vel * -0.5f + Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, Random.value) * base.firstChunk.vel.magnitude;
            this.SetRandomSpin();
            return false;
        }

        // Token: 0x06003177 RID: 12663 RVA: 0x003889F0 File Offset: 0x00386BF0
        public override void HitSomethingWithoutStopping(PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
        {
            base.HitSomethingWithoutStopping(obj, chunk, appendage);
        }

        // Token: 0x06003178 RID: 12664 RVA: 0x003889FB File Offset: 0x00386BFB
        public override void PickedUp(Creature upPicker)
        {
            this.ChangeMode(Weapon.Mode.Carried);
            this.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, base.firstChunk);
        }

        // Token: 0x06003179 RID: 12665 RVA: 0x00388A20 File Offset: 0x00386C20
        public void ProvideRotationBodyPart(BodyChunk chunk, BodyPart bodyPart)
        {
            this.stuckBodyPart = bodyPart.bodyPartArrayIndex;
            this.stuckRotation = Custom.Angle(base.firstChunk.vel, (bodyPart.pos - chunk.pos).normalized);
            bodyPart.vel += base.firstChunk.vel;
        }
        public override void RecreateSticksFromAbstract()
        {
            for (int i = 0; i < this.abstractPhysicalObject.stuckObjects.Count; i++)
            {
                if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).Spear == this.abstractPhysicalObject && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick).LodgedIn.realizedObject != null)
                {
                    AbstractPhysicalObject.AbstractSpearStick abstractSpearStick = this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearStick;
                    this.stuckInObject = abstractSpearStick.LodgedIn.realizedObject;
                    this.stuckInChunkIndex = abstractSpearStick.chunk;
                    this.stuckBodyPart = abstractSpearStick.bodyPart;
                    this.stuckRotation = abstractSpearStick.angle;
                    this.ChangeMode(Weapon.Mode.StuckInCreature);
                }
                else if (this.abstractPhysicalObject.stuckObjects[i] is AbstractPhysicalObject.AbstractSpearAppendageStick && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).Spear == this.abstractPhysicalObject && (this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick).LodgedIn.realizedObject != null)
                {
                    AbstractPhysicalObject.AbstractSpearAppendageStick abstractSpearAppendageStick = this.abstractPhysicalObject.stuckObjects[i] as AbstractPhysicalObject.AbstractSpearAppendageStick;
                    this.stuckInObject = abstractSpearAppendageStick.LodgedIn.realizedObject;
                    this.stuckInAppendage = new PhysicalObject.Appendage.Pos(this.stuckInObject.appendages[abstractSpearAppendageStick.appendage], abstractSpearAppendageStick.prevSeg, abstractSpearAppendageStick.distanceToNext);
                    this.stuckRotation = abstractSpearAppendageStick.angle;
                    this.ChangeMode(Weapon.Mode.StuckInCreature);
                }
            }
        }
        public override void ChangeMode(Weapon.Mode newMode)
        {
            if (base.mode == Weapon.Mode.StuckInCreature)
            {
                if (this.room != null)
                {
                    this.room.PlaySound(SoundID.Spear_Dislodged_From_Creature, base.firstChunk);
                }
                this.PulledOutOfStuckObject();
                base.ChangeOverlap(true);
            }
            else if (newMode == Weapon.Mode.StuckInCreature)
            {
                base.ChangeOverlap(false);
            }
            if (newMode != Weapon.Mode.Thrown)
            {
                //this.spearDamageBonus = 0.85f;
            }
            if (newMode != Weapon.Mode.Free)
            {
                this.spinning = false;
            }
            base.ChangeMode(newMode);
        }
        public override void Update(bool eu)
        {
            base.Update(eu);
            //this.lastPivotAtTip = this.pivotAtTip;
            //this.pivotAtTip = (base.mode == Weapon.Mode.Thrown || base.mode == Weapon.Mode.StuckInCreature);
            if (base.mode == Weapon.Mode.Free)
            {
                if (this.spinning)
                {
                    if (Custom.DistLess(base.firstChunk.pos, base.firstChunk.lastPos, 4f * this.room.gravity))
                    {
                        this.stillCounter++;
                    }
                    else
                    {
                        this.stillCounter = 0;
                    }
                    if (base.firstChunk.ContactPoint.y < 0 || this.stillCounter > 20)
                    {
                        this.spinning = false;
                        this.rotationSpeed = 0f;
                        //if (this.myStalk == null)
                        //{
                        //    this.rotation = Custom.DegToVec(Mathf.Lerp(-50f, 50f, Random.value) + 180f);
                        //}
                        base.firstChunk.vel *= 0f;
                        this.room.PlaySound(SoundID.Spear_Stick_In_Ground, base.firstChunk);
                    }
                }
                else if (!Custom.DistLess(base.firstChunk.lastPos, base.firstChunk.pos, 6f))
                {
                    this.SetRandomSpin();
                }
                //if (this.myStalk != null)
                //{
                //    this.rotation = Custom.DegToVec(180f + base.firstChunk.vel.x * 5f);
                //}
            }
            else if (base.mode == Weapon.Mode.Thrown)
            {
                BodyChunk firstChunk = base.firstChunk;
                firstChunk.vel.y = firstChunk.vel.y + 0.45f;
            }
            else if (base.mode == Weapon.Mode.StuckInCreature)
            {
                if (this.stuckInAppendage != null)
                {
                    this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.VecToDeg(this.stuckInAppendage.appendage.OnAppendageDirection(this.stuckInAppendage))));
                    base.firstChunk.pos = this.stuckInAppendage.appendage.OnAppendagePosition(this.stuckInAppendage);
                }
                else
                {
                    base.firstChunk.vel = this.stuckInChunk.vel;
                    if (this.stuckBodyPart == -1 || !this.room.BeingViewed || (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart) == null)
                    {
                        this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.VecToDeg(this.stuckInChunk.Rotation)));
                        base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, new Vector2(0f, 0f));
                    }
                    else
                    {
                        this.setRotation = new Vector2?(Custom.DegToVec(this.stuckRotation + Custom.AimFromOneVectorToAnother(this.stuckInChunk.pos, (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart).pos)));
                        base.firstChunk.MoveWithOtherObject(eu, this.stuckInChunk, Vector2.Lerp(this.stuckInChunk.pos, (this.stuckInChunk.owner as Creature).BodyPartByIndex(this.stuckBodyPart).pos, 0.5f) - this.stuckInChunk.pos);
                    }
                }
                if (this.stuckInChunk.owner.slatedForDeletetion)
                {
                    this.ChangeMode(Weapon.Mode.Free);
                }
            }
            for (int k = this.abstractPhysicalObject.stuckObjects.Count - 1; k >= 0; k--)
            {
                if (this.abstractPhysicalObject.stuckObjects[k] is AbstractPhysicalObject.ImpaledOnSpearStick)
                {
                    if (this.abstractPhysicalObject.stuckObjects[k].B.realizedObject != null && (this.abstractPhysicalObject.stuckObjects[k].B.realizedObject.slatedForDeletetion || this.abstractPhysicalObject.stuckObjects[k].B.realizedObject.grabbedBy.Count > 0))
                    {
                        this.abstractPhysicalObject.stuckObjects[k].Deactivate();
                    }
                    else if (this.abstractPhysicalObject.stuckObjects[k].B.realizedObject != null && this.abstractPhysicalObject.stuckObjects[k].B.realizedObject.room == this.room)
                    {
                        this.abstractPhysicalObject.stuckObjects[k].B.realizedObject.firstChunk.MoveFromOutsideMyUpdate(eu, base.firstChunk.pos + this.rotation * Custom.LerpMap((float)(this.abstractPhysicalObject.stuckObjects[k] as AbstractPhysicalObject.ImpaledOnSpearStick).onSpearPosition, 0f, 4f, 15f, -15f));
                        this.abstractPhysicalObject.stuckObjects[k].B.realizedObject.firstChunk.vel *= 0f;
                    }
                }
            }
        }
        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("buffassets/illustrations/ColorPicker", true);
            sLeaser.sprites[1] = new FSprite("buffassets/illustrations/ColorPicker_Pigment", true);
            this.AddToContainer(sLeaser, rCam, null);
        }
        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.color = Color.Lerp( palette.blackColor,Color.white,0.9f);
            
            sLeaser.sprites[0].color = this.color;
        }
        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(base.firstChunk.lastPos, base.firstChunk.pos, timeStacker);
            
            if (this.vibrate > 0)
            {
                vector += Custom.DegToVec(Random.value * 360f) * 2f * Random.value;
            }
            Vector3 v = Vector3.Slerp(this.lastRotation, this.rotation, timeStacker);
            for (int i = 0; i >= 0; i--)
            {
                sLeaser.sprites[i].x = vector.x - camPos.x;
                sLeaser.sprites[i].y = vector.y - camPos.y;

                //sLeaser.sprites[i].anchorY = Mathf.Lerp(this.lastPivotAtTip ? 0.85f : 0.5f, this.pivotAtTip ? 0.85f : 0.5f, timeStacker);
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), v);
            }
            if (this.blink > 0 && Random.value < 0.5f)
            {
                sLeaser.sprites[0].color = base.blinkColor;
            }
            else
            {
                sLeaser.sprites[0].color = this.color;
            }
            if (base.slatedForDeletetion || this.room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }

        }
    }

    class ColorEaterBuff : Buff<ColorEaterBuff, ColorEaterBuffData>
    {
        public override BuffID ID => ColorEaterBuffEntry.ColorEaterID;
      
        //按下按键可以吸取周围的颜色
        public override bool Trigger(RainWorldGame game)
        {
            if (game.AlivePlayers.Count > 0)
            {

                var player = (game.AlivePlayers[0].realizedCreature as Player);

                var absPicker = new AbstractPhysicalObject(player.room.world, ColorPicker.Picker, null, new WorldCoordinate(player.room.abstractRoom.index, -1, -1, 0), player.room.game.GetNewID());

                var picker = new ColorPicker(absPicker, game.world);
                picker.PlaceInRoom(player.room);
                picker.firstChunk.pos= player.firstChunk.pos;

                //absPicker.RealizeInRoom();
                //var picker = absPicker.realizedObject;
                //picker.firstChunk.pos = player.firstChunk.pos;

                //var color = game.cameras[0].PixelColorAtCoordinate(player.bodyChunks[1].pos - new Vector2(0, 10));
                //player.EatPlate().MainColor = Color.Lerp(player.EatPlate().MainColor, color, 0.9f);
                //player.EatPlate().MainColor = Custom.HSL2RGB(Custom.RGB2HSL(player.EatPlate().MainColor).x, 0.4f, 0.5f);
            }

            //return false;
            return base.Trigger(game);
        }

    }
    class ColorEaterBuffData : BuffData
    {
        public override BuffID ID => ColorEaterBuffEntry.ColorEaterID;

    }
    class ColorEaterBuffEntry : IBuffEntry
    {
        public static BuffID ColorEaterID = new BuffID("ColorEaterID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ColorEaterBuff, ColorEaterBuffData, ColorEaterBuffEntry>(ColorEaterID);
        }

        public static void HookOn()
        {
            On.AbstractPhysicalObject.Realize += AbstractPhysicalObject_Realize;

            On.Player.Update += Player_Update;
            On.Player.ShortCutColor += Player_ShortCutColor;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            //On.PlayerGraphics.SlugcatColor += PlayerGraphics_SlugcatColor;//行不通的改色方法
        }
        private static void AbstractPhysicalObject_Realize(On.AbstractPhysicalObject.orig_Realize orig, AbstractPhysicalObject self)
        {
            orig(self);
            if (self.type == ColorPicker.Picker)
            {
                self.realizedObject = new ColorPicker(self,self.world);
            }
        }

        private static Color PlayerGraphics_SlugcatColor(On.PlayerGraphics.orig_SlugcatColor orig, SlugcatStats.Name i)
        {
            Color color = orig.Invoke(i);
            if (BuffCustom.TryGetGame(out var game))
            {
                if(game.Players.Count>0&& game.Players[0].realizedCreature!=null)
                {
                    color=(game.Players[0].realizedCreature as Player).EatPlate().MainColor;
                }
            }
            return color;
        }

        private static void ChangeColor(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser)
        {

            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if (i != 9)
                {
                    sLeaser.sprites[i].color = self.player.EatPlate().MainColor;
                }
            }
            var mesh = sLeaser.sprites[2] as TriangleMesh;
            if (mesh != null && mesh.customColor)
            {
                for (int i = 0; i < mesh.verticeColors.Length; i++)
                {
                    mesh.verticeColors[i] = self.player.EatPlate().MainColor;
                }
            }

        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            ChangeColor(self, sLeaser);
        }

        private static Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
        {
            return (self.EatPlate().MainColor);
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.room == null || !self.Consious)
                return;

            var plateData = self.EatPlate();
            //趴下按下吸色
            if (self.bodyMode == Player.BodyModeIndex.Crawl && self.input[0].y < 0)
            {
                var color = self.room.game.cameras[0].PixelColorAtCoordinate(self.firstChunk.pos - new Vector2(0, -20));

                self.EatPlate().lerpTo(color, 0.1f);
            }

            //可以让圣徒用舌头吸色
            if (self.tongue!=null&&self.tongue.Attached)
            {
                var color = self.room.game.cameras[0].PixelColorAtCoordinate(self.tongue.AttachedPos);

                self.EatPlate().lerpTo(color, 0.1f);
            }

            //让匹配吸色后玩家的饱食度
            self.slugcatStats.foodToHibernate = Convert.ToInt32(Mathf.Lerp(0, self.slugcatStats.maxFood, plateData.InvertLerpColor()));

            //如果饱食度显示不匹配就刷新显示
            if (self.room.game.cameras.Any())
            {
                if (self.room.game.cameras[0].hud.foodMeter.survivalLimit != self.slugcatStats.foodToHibernate)
                {
                    self.room.game.cameras[0].hud.foodMeter.survivalLimit = self.slugcatStats.foodToHibernate;
                    self.room.game.cameras[0].hud.foodMeter.RefuseFood();
                    self.PlayHUDSound(SoundID.HUD_Food_Meter_Fill_Plop_A);
                }
            }


        }
    }
    public class ColorPlateData
    {
        public Color targetColor;
        public Player player;
        public Color MainColor;

        public bool refresh = true;

        public float InvertLerpColor()
        {
            //float targetH = Custom.RGB2HSL(targetColor).x;
            //float mainH = Custom.RGB2HSL(MainColor).x;

            //return Mathf.Min(Mathf.Abs(targetH - mainH), Mathf.Abs(targetH - mainH + 1));

            var a = MainColor;
            var b = targetColor;
            return (Math.Abs(a.r - b.r) + Math.Abs(a.g - b.g) + Math.Abs(a.b - b.b)) / 3f;

        }
        public void lerpTo(Color target, float t)
        {
            MainColor = Color.Lerp(MainColor, target, t);
            //MainColor = Custom.HSL2RGB(Custom.RGB2HSL(MainColor).x, 0.4f, 0.5f);
        }
        public ColorPlateData(Player player)
        {
            this.targetColor = Random.ColorHSV();
            //this.MainColor = Custom.HSL2RGB(Random.value, 0.4f, 0.5f);
            this.MainColor = Random.ColorHSV();

            this.player = player;
        }
        public void Update()
        {


        }

    }
    public static class ColorPlate
    {
        private static readonly ConditionalWeakTable<Player, ColorPlateData> modules = new ConditionalWeakTable<Player, ColorPlateData>();

        public static ColorPlateData EatPlate(this Player player)
        {
            return modules.GetValue(player, (Player p) => new ColorPlateData(p));
        }

    }


}