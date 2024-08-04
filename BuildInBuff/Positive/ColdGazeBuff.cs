using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System.Diagnostics.Eventing.Reader;
using MoreSlugcats;
using Expedition;
using System.Net.NetworkInformation;
using System.Reflection;
using BuiltinBuffs.Duality;
using static ExtraExtentions;

namespace BuiltinBuffs.Positive
{
    internal class ColdGazeBuff : Buff<ColdGazeBuff, ColdGazeBuffData>
    {
        public override BuffID ID => ColdGazeBuffEntry.ColdGaze;
        
        public ColdGazeBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var coldGaze = new ColdGaze(player, player.room);
                    ColdGazeBuffEntry.ColdGazeFeatures.Add(player, coldGaze); 

                    if (ColdGazeBuffEntry.StackLayer >= 3)
                    {
                        Medusa medusaCat = new Medusa(player, coldGaze);
                        ColdGazeBuffEntry.MedusaFeatures.Add(player, medusaCat);
                        medusaCat.ctor(player.graphicsModule as PlayerGraphics);
                        medusaCat.InitiateSprites(game.cameras[0].spriteLeasers.
                            First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                    }
                }
            }
        }
    }

    internal class ColdGazeBuffData : CountableBuffData
    {
        public override BuffID ID => ColdGazeBuffEntry.ColdGaze;
        public override int MaxCycleCount => 5;

        public override void Stack()
        {
            base.Stack();
            if (BuffCustom.TryGetGame(out var game) &&
                ColdGazeBuffEntry.ColdGaze.GetBuffData() != null &&
                ColdGazeBuffEntry.StackLayer >= 3)
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    if (ColdGazeBuffEntry.ColdGazeFeatures.TryGetValue(player, out var coldGaze) &&
                       !ColdGazeBuffEntry.MedusaFeatures.TryGetValue(player, out _))
                    {
                        Medusa medusaCat = new Medusa(player, coldGaze);
                        ColdGazeBuffEntry.MedusaFeatures.Add(player, medusaCat);
                        medusaCat.ctor(player.graphicsModule as PlayerGraphics);
                        medusaCat.InitiateSprites(game.cameras[0].spriteLeasers.
                            First(i => i.drawableObject == player.graphicsModule), game.cameras[0]);
                    }
                }
            }
        }
    }

    internal class ColdGazeBuffEntry : IBuffEntry
    {
        public static BuffID ColdGaze = new BuffID("ColdGaze", true);

        public static ConditionalWeakTable<Player, ColdGaze> ColdGazeFeatures = new ConditionalWeakTable<Player, ColdGaze>();
        public static ConditionalWeakTable<Player, Medusa> MedusaFeatures = new ConditionalWeakTable<Player, Medusa>();
        public static ConditionalWeakTable<AbstractCreature, Freeze> FreezeFeatures = new ConditionalWeakTable<AbstractCreature, Freeze>();

        public static int StackLayer => ColdGaze.GetBuffData()?.StackLayer ?? 0;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ColdGazeBuff, ColdGazeBuffData, ColdGazeBuffEntry>(ColdGaze);
        }
        
        public static void HookOn()
        {
            IL.Room.Update += Room_UpdateIL;
            On.AbstractCreature.ctor += AbstractCreature_ctor;
            On.AbstractCreature.Update += AbstractCreature_Update;
            On.RoomCamera.SpriteLeaser.Update += RoomCamera_SpriteLeaser_Update;

            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;

            On.PlayerGraphics.ctor += PlayerGraphics_ctor;
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
        }

        #region 玩家
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!ColdGazeFeatures.TryGetValue(self, out _))
            {
                ColdGaze newColdGaze = new ColdGaze(self, self.room);
                self.room.AddObject(newColdGaze);
                ColdGazeFeatures.Add(self, newColdGaze);
            }
            if (StackLayer >= 3 &&
                ColdGazeFeatures.TryGetValue(self, out var coldGaze) &&
                !MedusaFeatures.TryGetValue(self, out _))
            {
                Medusa medusaCat = new Medusa(self, coldGaze);
                MedusaFeatures.Add(self, medusaCat);
            }
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (ColdGazeFeatures.TryGetValue(self, out var coldGaze))
            {
                ColdGazeFeatures.Remove(self);
                coldGaze.Destroy();
                coldGaze = new ColdGaze(self, self.room);
                self.room.AddObject(coldGaze);
                ColdGazeFeatures.Add(self, coldGaze);
            }
            else
            {
                ColdGaze newColdGaze = new ColdGaze(self, self.room);
                self.room.AddObject(newColdGaze);
                ColdGazeFeatures.Add(self, newColdGaze);
            }

            if (MedusaFeatures.TryGetValue(self, out var medusaCat))
                medusaCat.NewRoom(self.room);
        }
        #endregion

        #region 蛇发
        private static void PlayerGraphics_ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.ApplyPalette(sLeaser, rCam, palette);
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
            {
                medusaCat.InitiateSprites(sLeaser, rCam);
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PlayerGraphics_ctor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.ctor(self);
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);/*
            if (Input.GetKeyDown(KeyCode.T) && BuffPlugin.DevEnabled)
                ColdGaze.GetBuffData().Stack();*/
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.Update();
        }

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (MedusaFeatures.TryGetValue(self.player, out var medusaCat))
                medusaCat.Reset();
        }
        #endregion

        private static void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
        {
            orig(self, world, creatureTemplate, realizedCreature, pos, ID);
            if (!FreezeFeatures.TryGetValue(self, out _))//  && !(self is Player)
            {
                Freeze freeze = new Freeze(self);
                FreezeFeatures.Add(self, freeze);
            }
        }

        private static void AbstractCreature_Update(On.AbstractCreature.orig_Update orig, AbstractCreature self, int time)
        {
            orig(self, time);
            if (!FreezeFeatures.TryGetValue(self, out _))
            {
                Freeze freeze = new Freeze(self);
                FreezeFeatures.Add(self, freeze);
            }
        }

        private static void Room_UpdateIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                //找到ShouldBeDeferred结束的地方
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchLdloc(10),
                    (i) => i.MatchCall<Room>("ShouldBeDeferred"),
                    (i) => i.MatchStloc(11)))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc_S, (byte)10);
                    c.Emit(OpCodes.Ldloc_S, (byte)11);
                    c.EmitDelegate<Func<Room, UpdatableAndDeletable, bool, bool>>((self, updatableAndDeletable, flag) =>
                    {
                        bool newFlag = false;

                        if ((updatableAndDeletable is Creature creature) &&
                             FreezeFeatures.TryGetValue(creature.abstractCreature, out var freeze))
                        {
                            freeze.Update();
                            newFlag = freeze.ShouldSkipUpdate();
                            if (newFlag && creature is Deer)
                            {
                                bool eu = true;
                                Deer deer = (Deer) creature;
                                if(deer.graphicsModule != null)
                                {
                                    for (int n = 0; n < deer.room.game.Players.Count; n++)
                                    {
                                        if (deer.room.game.Players[n].pos.room == deer.room.abstractRoom.index && 
                                            deer.room.game.Players[n].realizedCreature != null && 
                                            (deer.room.game.Players[n].realizedCreature as Player).wantToGrab > 0 && 
                                            Custom.DistLess(deer.room.game.Players[n].realizedCreature.mainBodyChunk.pos, deer.antlers.pos, deer.antlers.rad))
                                        {
                                            (deer.room.game.Players[n].realizedCreature as Player).wantToGrab = 0;
                                            bool flag2 = true;
                                            int num5 = 0;
                                            while (num5 < deer.playersInAntlers.Count && flag2)
                                            {
                                                flag2 = (deer.playersInAntlers[num5].player != deer.room.game.Players[n].realizedCreature as Player);
                                                num5++;
                                            }
                                            if (flag2)
                                            {
                                                if ((deer.room.game.Players[n].realizedCreature as Player).playerInAntlers != null)
                                                {
                                                    (deer.room.game.Players[n].realizedCreature as Player).playerInAntlers.playerDisconnected = true;
                                                }
                                                deer.playersInAntlers.Add(new Deer.PlayerInAntlers(deer.room.game.Players[n].realizedCreature as Player, deer));
                                            }
                                        }
                                    }
                                    for (int num6 = deer.playersInAntlers.Count - 1; num6 >= 0; num6--)
                                    {
                                        if (deer.playersInAntlers[num6].playerDisconnected)
                                        {
                                            deer.playersInAntlers.RemoveAt(num6);
                                        }
                                        else
                                        {
                                            deer.playersInAntlers[num6].Update(eu);
                                        }
                                    }
                                }
                                else
                                {
                                    deer.playersInAntlers.Clear();
                                }
                            }
                        }
                        
                        return flag || newFlag;
                    });
                    c.Emit(OpCodes.Stloc_S, (byte)11);
                }

                //找到即将绘图的地方
                if (c.TryGotoNext(MoveType.After,
                    (i) => i.MatchLdloc(10),
                    (i) => i.MatchIsinst<PhysicalObject>(),
                    (i) => i.Match(OpCodes.Brfalse_S),
                    (i) => i.MatchLdloc(11)))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloc_S, (byte)10);
                    c.EmitDelegate<Func<bool, Room, UpdatableAndDeletable, bool>>((flag, self, updatableAndDeletable) =>
                    {
                        flag = self.ShouldBeDeferred(updatableAndDeletable);
                        return flag;
                    });
                    c.Emit(OpCodes.Stloc_S, (byte)11);
                    c.Emit(OpCodes.Ldloc_S, (byte)11);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void RoomCamera_SpriteLeaser_Update(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            Freeze freeze = null;
            bool canFind = (self.drawableObject is GraphicsModule graphicsModule && graphicsModule.owner is Creature creature1 &&
                 FreezeFeatures.TryGetValue(creature1.abstractCreature, out freeze)) ||
                (self.drawableObject is Creature creature2 &&
                 FreezeFeatures.TryGetValue(creature2.abstractCreature, out freeze));
            if (canFind)
            {
                if (freeze.ShouldSkipUpdate())
                {
                    timeStacker = 0;
                }
                else
                {
                    self.drawableObject.ApplyPalette(self, rCam, rCam.currentPalette);
                }
            }

            orig.Invoke(self, timeStacker, rCam, camPos);

            if (canFind)
            {
                freeze.DrawSprites(self, rCam);
            }
        }
    }

    internal class ColdGaze : CosmeticSprite
    {
        private Player owner;
        private LightBeam.LightBeamData.BlinkType blinkType;
        private Vector2[] quad;
        private Vector2[] verts;
        private bool meshDirty;
        private float lastAlpha;
        private int gridDiv = 1;
        private int lastCamPos = -1;
        private float rangeAngle;
        private Color c;
        private float colorAlpha;
        private int blinkTicker;
        private float blinkRate;
        private bool nightLight;
        private float nightFade;
        private float alpha;

        public int Level
        {
            get
            {
                return ColdGazeBuffEntry.StackLayer;
            }
        }

        public float RangeAngle
        {
            get
            {
                return this.rangeAngle;
            }
            set
            {
                this.rangeAngle = value;
            }
        }

        public Color Color
        {
            get
            {
                return this.c;
            }
            set
            {
                this.c = value;
                this.colorAlpha = 0f;
                if (this.c.r > this.colorAlpha)
                {
                    this.colorAlpha = this.c.r;
                }
                if (this.c.g > this.colorAlpha)
                {
                    this.colorAlpha = this.c.g;
                }
                if (this.c.b > this.colorAlpha)
                {
                    this.colorAlpha = this.c.b;
                }
                this.c /= this.colorAlpha;
            }
        }

        public Vector2 LookDirection
        {
            get
            {
                if (owner.room != null && owner.room == owner.room.game.cameras[0].room && owner.graphicsModule != null)
                {
                    RoomCamera.SpriteLeaser spriteLeaser = owner.room.game.cameras[0].spriteLeasers.FirstOrDefault(i => i.drawableObject == owner.graphicsModule);
                    if (spriteLeaser != null)
                    {
                        for (int i = 3; i <= 7; i++)
                        {
                            if (spriteLeaser.sprites[3].element.name.Contains(i.ToString()))
                            {
                                if (owner.input[0].x != 0 || !(owner.graphicsModule is PlayerGraphics graphic))
                                    return new Vector2(owner.input[0].x, 0);
                                else
                                    return (graphic.head.pos - owner.bodyChunks[0].pos).normalized;
                            }
                        }
                    }
                }

                if (owner.graphicsModule is PlayerGraphics graphics)
                {
                    if (graphics.lookDirection != Vector2.zero)
                        return graphics.lookDirection;
                    return (graphics.head.pos - owner.bodyChunks[0].pos).normalized;
                }
                return new Vector2(owner.input[0].x, 0);

            }
        }

        public ColdGaze(Player owner, Room room)
        {
            this.owner = owner;
            this.room = room;

            this.RangeAngle = 30f;
            this.Color = new Color(227f / 255f, 171f / 255f, 78f / 255f);

            this.quad = new Vector2[4];
            this.quad[0] = Vector2.zero;
            this.quad[1] = Vector2.zero;
            this.quad[2] = Vector2.zero;
            this.quad[3] = Vector2.zero;
            this.gridDiv = this.GetIdealGridDiv();
            this.meshDirty = true;
            this.blinkType = LightBeam.LightBeamData.BlinkType.None;
            this.nightFade = 1f;
            this.alpha = 0.5f;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (this.blinkType != LightBeam.LightBeamData.BlinkType.None)
            {
                this.blinkTicker = this.room.syncTicker;
            }
            if (this.nightLight && this.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.DayNight) > 0f && (float)this.room.world.rainCycle.dayNightCounter >= 6000f * this.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.DayNight) * 1.75f)
            {
                this.nightFade = Mathf.Lerp(this.nightFade, 1f, 0.005f);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            TriangleMesh triangleMesh = TriangleMesh.MakeGridMesh("Futile_White", this.gridDiv);
            this.meshDirty = true;
            sLeaser.sprites[0] = triangleMesh;
            sLeaser.sprites[0].isVisible = false;
            //sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["FlatLight"];
            //sLeaser.sprites[0].shader = rCam.room.game.rainWorld.Shaders["LightBeam"];
            this.verts = new Vector2[(sLeaser.sprites[0] as TriangleMesh).vertices.Length];
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            this.UpdateVerts(sLeaser, rCam, timeStacker);
            for (int i = 0; i < this.verts.Length; i++)
            {
                (sLeaser.sprites[0] as TriangleMesh).MoveVertice(i, this.verts[i] - camPos);
            }
            if (!owner.Consious || owner.dead || owner.sleepCurlUp != 0)
            {
                sLeaser.sprites[0].isVisible = false;
            }
            else
            {
                sLeaser.sprites[0].isVisible = true;
            }
            AlphaChange();
            this.alpha *= (owner.graphicsModule as PlayerGraphics).blink <= 0 ? 1f : 0f;
            float num = (float)Mathf.FloorToInt(this.alpha * 3f);//float num = (float)Mathf.FloorToInt(this.alpha * 3f);
            float num2 = Mathf.InverseLerp(0.33333334f * num, 0.33333334f * (num + 1f), this.alpha);
            num2 *= Mathf.Pow(Mathf.InverseLerp(-0.2f, 0f, -1f), 1.2f);//num2 *= Mathf.Pow(Mathf.InverseLerp(-0.2f, 0f, rCam.room.world.rainCycle.ShaderLight), 1.2f);
            num2 = Mathf.Lerp(0.33333334f * num, 0.33333334f * (num + 1f), num2);
            num2 = (num2 - 0.33333334f * num) * this.nightFade * this.BlinkFade() + 0.33333334f * num;
            num2 *= 0.5f * this.alpha * (1f - rCam.room.darkenLightsFactor);
            if (num2 != this.lastAlpha)
            {
                this.UpdateColor(sLeaser, rCam, num2);
                this.lastAlpha = num2;
            }
            if (rCam.currentCameraPosition != this.lastCamPos)
            {
                this.lastCamPos = rCam.currentCameraPosition;
                this.UpdateColor(sLeaser, rCam, num2);
            }
            if (base.slatedForDeletetion || this.room != rCam.room || owner.room == null)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            this.UpdateColor(sLeaser, rCam, this.lastAlpha);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            sLeaser.sprites[0].RemoveFromContainer();
            rCam.ReturnFContainer((rCam.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.WaterLights) > 0f) ? "Water" : "ForegroundLights").AddChild(sLeaser.sprites[0]);
        }

        private int GetIdealGridDiv()
        {
            float num = 0f;
            for (int i = 0; i < 3; i++)
            {
                if (Vector2.Distance(this.quad[i], this.quad[i + 1]) > num)
                {
                    num = Vector2.Distance(this.quad[i], this.quad[i + 1]);
                }
            }
            if (Vector2.Distance(this.quad[0], this.quad[3]) > num)
            {
                num = Vector2.Distance(this.quad[0], this.quad[3]);
            }
            return Mathf.Clamp(Mathf.RoundToInt(num / 250f), 1, 20);
        }

        private float BlinkFade()
        {
            float result = 1f;
            float num = (1.01f - this.blinkRate) * 1000f;
            if (this.blinkType == LightBeam.LightBeamData.BlinkType.Flash)
            {
                num /= 4f;
            }
            if (this.blinkType == LightBeam.LightBeamData.BlinkType.Flash && (float)this.blinkTicker % (num * 2f) <= num)
            {
                result = 0f;
            }
            else if (this.blinkType == LightBeam.LightBeamData.BlinkType.Fade)
            {
                result = (Mathf.Sin((float)this.blinkTicker % num / num * 3.1415927f * 2f) + 1f) / 2f;
            }
            return result;
        }

        private void AlphaChange()
        {
            bool shouldHide = true;
            if (owner.room == null)
                return;
            foreach (List<PhysicalObject> physicalObjectsList in owner.room.physicalObjects)
            {
                foreach (PhysicalObject physicalObject in physicalObjectsList)
                {
                    if (physicalObject is Creature)
                    {
                        if (ColdGazeBuffEntry.FreezeFeatures.TryGetValue((physicalObject as Creature).abstractCreature, out var freeze) &&
                            freeze.ShouldBeFired())
                            shouldHide = false;
                    }
                }
            }
            if(shouldHide)
                this.alpha = Mathf.Max(0f, this.alpha - 0.005f);
            else 
                this.alpha = Mathf.Min(1f, this.alpha + 0.05f);
        }

        private void UpdateColor(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float alpha)
        {
            Color color = Custom.RGB2RGBA(this.Color, alpha);
            for (int i = 0; i <= this.gridDiv; i++)
            {
                for (int j = 0; j <= this.gridDiv; j++)
                {
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[j * (this.gridDiv + 1) + i] = color;
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[j * (this.gridDiv + 1) + i].a *= Mathf.Pow((float)j / (float)this.gridDiv, 0.3f);
                    (sLeaser.sprites[0] as TriangleMesh).verticeColors[j * (this.gridDiv + 1) + i].a *= Mathf.Pow(1 - (float)j / (float)this.gridDiv, 0.3f);
                }
            }
        }

        private void UpdateVerts(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam , float timeStacker)
        {
            Vector2 headPos = Vector2.Lerp((owner.graphicsModule as PlayerGraphics).head.lastPos, (owner.graphicsModule as PlayerGraphics).head.pos, timeStacker);
            this.quad[0] = headPos;
            this.quad[1] = headPos + Radius(Level, 0f) * Custom.DegToVec(Custom.VecToDeg(LookDirection) + RangeAngle);
            this.quad[2] = headPos + Radius(Level, 0f) * Custom.DegToVec(Custom.VecToDeg(LookDirection));
            this.quad[3] = headPos + Radius(Level, 0f) * Custom.DegToVec(Custom.VecToDeg(LookDirection) - RangeAngle);
            int idealGridDiv = this.GetIdealGridDiv();
            if (idealGridDiv != this.gridDiv)
            {
                this.gridDiv = idealGridDiv;
                sLeaser.sprites[0].RemoveFromContainer();
                this.InitiateSprites(sLeaser, rCam);
            }
            for (int i = 0; i <= this.gridDiv; i++)//目光周向
            {
                for (int j = 0; j <= this.gridDiv; j++)//目光径向
                {
                    Vector2 a = Vector2.Lerp(this.quad[0], this.quad[2], (float)j / (float)this.gridDiv);
                    Vector2 b = Vector2.Lerp(this.quad[1], this.quad[3], (float)i / (float)this.gridDiv);
                    float r = (a - this.quad[0]).magnitude;
                    float ang = Custom.VecToDeg(b - this.quad[0]);
                    this.verts[j * (this.gridDiv + 1) + i] = this.quad[0] +  r * Custom.DegToVec(ang);
                }
            }
        }

        public float Radius(float ring, float timeStacker)
        {
            Vector2 corner = new Vector2(-100000f, -100000f) ;
            //Vector2 corner = Custom.RectCollision((owner.graphicsModule as PlayerGraphics).head.pos, 100000f * this.LookDirection, owner.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            return Mathf.Min((5f + 2f * ring) * 80f, corner.magnitude);
        }
    }

    internal class Freeze
    {
        WeakReference<AbstractCreature> ownerRef; 
        private int freezeCount;
        private int cycleCount;
        private bool isRecorded;
        private Dictionary<FSprite, Color> oldColor;
        private Dictionary<FSprite, Color> newColor;
        private Dictionary<FSprite, Color[]> oldMeshColor;
        private Dictionary<FSprite, Color[]> newMeshColor;

        private float FreezeRatio
        {
            get
            {
                switch (ColdGazeBuffEntry.StackLayer)
                {
                    case 0:
                    case 1:
                        return 0.75f;
                    case 2:
                        return 0.9f;
                    case 3:
                    default:
                        return 1f;
                }
            }
        }

        private bool IsPetrified
        {
            get
            {
                return this.freezeCount > Mathf.Max(0f, (1 - this.FreezeRatio) * this.cycleCount);
            }
        }

        private bool IsPermanentPetrified
        {
            get
            {
                return IsPetrified && FreezeRatio >= 1;
            }
        }

        public Freeze(AbstractCreature c)
        {
            ownerRef = new WeakReference<AbstractCreature>(c);
            freezeCount = 0; 
            cycleCount = 40;
            isRecorded = false;
            oldColor = new Dictionary<FSprite, Color>();
            newColor = new Dictionary<FSprite, Color>();
            oldMeshColor = new Dictionary<FSprite, Color[]>();
            newMeshColor = new Dictionary<FSprite, Color[]>();
        }

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return;
            var creature = abstractCreature.realizedCreature;

            if (isRecorded && creature.room != creature.room.game.cameras[0].room)
            {
                oldColor.Clear();
                newColor.Clear();
                oldMeshColor.Clear();
                newMeshColor.Clear();
                isRecorded = false;
            }

            //石化直接让生物即死
            if (IsPermanentPetrified)
                creature.Die();
            //没有石化则逐渐解除冻结
            else if (freezeCount > 0)
                freezeCount--;

            if (IsPetrified)
            {
                foreach (var bodyChunk in creature.bodyChunks)
                {
                    bodyChunk.vel *= 0f;
                    bodyChunk.HardSetPosition(bodyChunk.pos);
                }
                if (TemperatrueModule.TryGetTemperatureModule(creature, out var heatModule))
                {
                    heatModule.temperature = Mathf.Max(0f, heatModule.temperature - 0.025f);
                }
            }

            if (freezeCount == 0 && ShouldBeFired())
            {
                freezeCount = cycleCount;
                //音效
                creature.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, creature.mainBodyChunk);
                //图像
                EmitterUpdate();
            }
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam = null)
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) || 
                abstractCreature.realizedCreature == null || 
                abstractCreature.realizedCreature.room == null)
                return;
            Color backgroundColor = Color.gray;
            if (rCam == null)
                backgroundColor = rCam.currentPalette.blackColor;
            if (IsPetrified && !isRecorded)
            {
                isRecorded = true;
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null)
                        {
                            oldColor.Add(sLeaser.sprites[i], sLeaser.sprites[i].color == null ? backgroundColor : sLeaser.sprites[i].color);
                            Color.RGBToHSV(oldColor[sLeaser.sprites[i]], out var hue, out _, out var light);
                            newColor.Add(sLeaser.sprites[i], Color.HSVToRGB(hue, 0f, light));
                        }
                        else
                        {
                            oldMeshColor.Add(sLeaser.sprites[i], mesh.verticeColors);
                            Color[] hsvMeshColor = new Color[mesh.vertices.Length];
                            for (int j = 0; j < mesh.vertices.Length; j++)
                            {
                                Color.RGBToHSV(mesh.verticeColors[j], out var hue, out _, out var light);
                                hsvMeshColor[j] = Color.HSVToRGB(hue, 0f, light);
                            }
                            newMeshColor.Add(sLeaser.sprites[i], hsvMeshColor);
                        }
                    }
                    else
                    {
                        oldColor.Add(sLeaser.sprites[i], sLeaser.sprites[i].color == null ? backgroundColor : sLeaser.sprites[i].color);
                        Color.RGBToHSV(oldColor[sLeaser.sprites[i]], out var hue, out _, out var light);
                        newColor.Add(sLeaser.sprites[i], Color.HSVToRGB(hue, 0f, light));
                    }
                }
            }
            //石化
            if (IsPetrified)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null && newColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            sLeaser.sprites[i].color = newColor[sLeaser.sprites[i]];
                        }
                        else if (newMeshColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            for (int j = 0; j < mesh.vertices.Length; j++)
                                mesh.verticeColors[j] = newMeshColor[sLeaser.sprites[i]][j];
                        }
                    }
                    else if (newColor.ContainsKey(sLeaser.sprites[i]))
                    {
                        sLeaser.sprites[i].color = newColor[sLeaser.sprites[i]];
                    }
                }
            }
            //还原
            else if (isRecorded)
            {
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (sLeaser.sprites[i] is TriangleMesh)
                    {
                        var mesh = sLeaser.sprites[i] as TriangleMesh;
                        if (mesh.verticeColors == null && oldColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            sLeaser.sprites[i].color = oldColor[sLeaser.sprites[i]];
                        }
                        else if (oldMeshColor.ContainsKey(sLeaser.sprites[i]))
                        {
                            for (int j = 0; j < mesh.vertices.Length; j++)
                                mesh.verticeColors[j] = oldMeshColor[sLeaser.sprites[i]][j];
                        }
                    }
                    else if (oldColor.ContainsKey(sLeaser.sprites[i]))
                    {
                        sLeaser.sprites[i].color = oldColor[sLeaser.sprites[i]];
                    }
                }
                oldColor.Clear();
                newColor.Clear();
                oldMeshColor.Clear();
                newMeshColor.Clear();
                isRecorded = false;
            }
        }
        
        public bool ShouldSkipUpdate()
        {
            if (!ownerRef.TryGetTarget(out var creature))
                return false;

            if (IsPetrified)
            {
                return true;
            }

            return false;
        }

        public bool ShouldBeFired()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return false;

            bool shouldFire = true;
            bool inRange = false;
            var self = abstractCreature.realizedCreature;

            if (self is Player)
                shouldFire = false;

            foreach (var player in self.room.game.AlivePlayers.Select(i => i.realizedCreature as Player)
                                     .Where(i => i != null && i.graphicsModule != null))
            {
                if (ColdGazeBuffEntry.ColdGazeFeatures.TryGetValue(player, out var coldGaze))
                {
                    Vector2 headPos = (player.graphicsModule as PlayerGraphics).head.pos;
                    Vector2 LookDirection = coldGaze.LookDirection;
                    if (player != null && player.room != null &&
                        Mathf.Abs(Custom.VecToDeg(self.mainBodyChunk.pos - headPos) - Custom.VecToDeg(LookDirection)) <= coldGaze.RangeAngle && //角度
                        Vector2.Dot(self.mainBodyChunk.pos - headPos, LookDirection) > 0 && //方向
                        Custom.DistLess(self.mainBodyChunk.pos, headPos, coldGaze.Radius(coldGaze.Level, 0f)) && //距离
                        (player.graphicsModule as PlayerGraphics).blink <= 0) //猫猫是否有睁眼
                    {
                        inRange = true;
                        if (this.FreezeRatio >= 1)
                        {
                            self.SetKillTag(player.abstractCreature);
                        }
                    }

                    if (self is Overseer && (self as Overseer).AI.LikeOfPlayer(player.abstractCreature) > 0.5f)
                    {
                        shouldFire = false;
                    }
                    if (self is Lizard)
                    {
                        foreach (RelationshipTracker.DynamicRelationship relationship in (self as Lizard).AI.relationshipTracker.relationships.
                            Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == player.abstractCreature))
                        {
                            if ((self as Lizard).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                shouldFire = false;
                        }
                    }
                    if (self is Scavenger &&
                        (double)(self as Scavenger).abstractCreature.world.game.session.creatureCommunities.
                        LikeOfPlayer(CreatureCommunities.CommunityID.Scavengers,
                                    (self as Scavenger).abstractCreature.world.game.world.RegionNumber,
                                    player.playerState.playerNumber) > 0.5)
                    {
                        shouldFire = false;
                    }
                    if (self is Cicada)
                    {
                        foreach (RelationshipTracker.DynamicRelationship relationship in (self as Cicada).AI.relationshipTracker.relationships.
                            Where((RelationshipTracker.DynamicRelationship m) => m.trackerRep.representedCreature == player.abstractCreature))
                        {
                            if ((self as Cicada).AI.LikeOfPlayer(relationship.trackerRep) > 0.5f)
                                shouldFire = false;
                        }
                    }
                }
                else
                    return false;
            }

            return shouldFire && inRange;
        }

        private void EmitterUpdate()
        {
            if (!ownerRef.TryGetTarget(out var abstractCreature) ||
                abstractCreature.realizedCreature == null ||
                abstractCreature.realizedCreature.room == null)
                return;
            var creature = abstractCreature.realizedCreature;
            var emitter = new ParticleEmitter(creature.room);
            int randomBody = Random.Range(0, creature.bodyChunks.Length);
            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));
            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, creature));

            emitter.ApplyParticleSpawn(new RateSpawnerModule(emitter, 150, 200));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "")));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", 8, 0.3f, 0.15f, Color.white)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 5, 10));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, new Vector2(8f, 3f), new Vector2(6f, 4f)));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 1.2f * creature.bodyChunks[randomBody].rad + 10f));
            emitter.ApplyParticleModule(new SetRingRotation(emitter, creature.bodyChunks[randomBody].pos, 90f));
            emitter.ApplyParticleModule(new SetRingVelocity(emitter, creature.bodyChunks[randomBody].pos, 0));


            emitter.ApplyParticleModule(new ScaleOverLife(emitter, (p, a) =>
            {
                return p.setScaleXY * (1f - a);
            }));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }
    }

    #region 蛇发外观
    internal class Medusa
    {
        WeakReference<Player> ownerRef;
        public MedusaHair[] hairs;

        public void ctor(PlayerGraphics self)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].ctor(self);
        }

        public Medusa(Player player, ColdGaze coldGaze)
        {
            ownerRef = new WeakReference<Player>(player);
            this.hairs = new MedusaHair[6];
            for (int i = 0; i < hairs.Length; i++)
                hairs[i] = new MedusaHair(player, coldGaze, this, i, 30f + Random.value * 5f);
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].InitiateSprites(sLeaser, rCam);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].ApplyPalette(sLeaser, rCam, palette);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].AddToContainer(sLeaser, rCam, newContatiner);
        }

        public void Reset()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            for (int i = 0; i < hairs.Length; i++)
                hairs[i].Reset();
        }
        #endregion

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            for (int i = 0; i < hairs.Length; i++)
                this.hairs[i].Update();
        }

        public void NewRoom(Room room)
        {
            for (int i = 0; i < this.hairs.Length; i++)
                this.hairs[i].NewRoom(room);
        }
    }

    internal class MedusaHair
    {
        #region 属性
        WeakReference<Player> ownerRef;
        ColdGaze coldGaze;
        Medusa medusa;
        private PlayerGraphics.PlayerObjectLooker objectLooker;
        private Tentacle tentacle;
        private Vector2 wantPos;
        private Vector2 randomMovePos;
        private Vector2 movePos;
        private Vector2 rootPos;
        private Vector2 lastRootPos;
        private Vector2 headToFace;
        private Vector2 lastHeadToFace;

        private Vector2 lookPoint;
        private Vector2 wantLookPoint;
        private Vector2 lookDirection;
        private Vector2 lastLookDir;

        private Color eyesColor;
        private float numberOfWavesOnBody;
        private float sinWave;
        private float sinSpeed;
        private float sinPhase;
        private float[] swallowArray;
        private float lastExtended;
        private float extended;
        private float s;
        private float retractSpeed;
        private float length;
        private float indexDeg;
        private int attackCounter;
        private int index;
        private int moveCount;
        private bool showAsAngry;

        public float bodySize
        {
            get
            {
                return 1f;
            }
        }

        public float stress
        {
            get
            {
                return this.s;
            }
            set
            {
                this.s = Mathf.Clamp(value, 0f, 1f);
            }
        }

        public Vector2 WantLookPoint
        {
            get
            {
                if (!ownerRef.TryGetTarget(out var player))
                    return Vector2.zero;
                Vector2 lookDir = Vector2.zero;

                if (player.grasps[0] != null && player.grasps[0].grabbed is JokeRifle)
                {
                    //this.lookDirection = (player.grasps[0].grabbed as JokeRifle).aimDir;
                    lookDir = (player.grasps[0].grabbed as JokeRifle).aimDir;
                }
                else if (player.room != null && 
                         player.room == player.room.game.cameras[0].room && 
                         player.graphicsModule != null)
                {
                    RoomCamera.SpriteLeaser spriteLeaser = player.room.game.cameras[0].spriteLeasers.First(i => i.drawableObject == player.graphicsModule);
                    for (int i = 3; i <= 7; i++)
                    {
                        if (spriteLeaser.sprites[3].element.name.Contains(i.ToString()))
                        {
                            if (player.input[0].x != 0)
                                lookDir = new Vector2(player.input[0].x, 0);
                            else
                                lookDir = ((player.graphicsModule as PlayerGraphics).head.pos - player.bodyChunks[0].pos).normalized;
                        }
                    }
                }
                if (lookDir == Vector2.zero && this.wantLookPoint != Vector2.zero)
                    return this.wantLookPoint;
                else
                    lookDir = ((player.graphicsModule as PlayerGraphics).head.pos - player.bodyChunks[0].pos).normalized;
                return this.tentacle.Tip.pos + 0.1f * length * lookDir;
            }
        }


        private int firstSprite;

        private int TotalSprites => 4;
        #endregion
        public void ctor(PlayerGraphics self)
        {
            this.numberOfWavesOnBody = 1.8f;
            this.sinSpeed = 0.016666668f;
            this.swallowArray = new float[this.tentacle.tChunks.Length];
            this.extended = 1f;
            this.objectLooker = new PlayerGraphics.PlayerObjectLooker(self);
        }

        public MedusaHair(Player player, ColdGaze coldGaze, Medusa medusa, int index, float length)
        {
            ownerRef = new WeakReference<Player>(player);
            this.coldGaze = coldGaze;
            this.medusa = medusa;
            this.length = length;
            this.index = index;
            this.indexDeg = ((float)this.index + 0.5f) * 360f / (float)medusa.hairs.Length;
            this.moveCount = 80;
            this.randomMovePos = Vector2.zero;
            this.headToFace = Vector2.zero;
            this.wantLookPoint = Vector2.zero;
            this.lookDirection = Vector2.zero;
            this.sinPhase = Random.value * 0.5f + 0.75f;
            this.tentacle = new Tentacle(player, player.bodyChunks[0], this.length * this.bodySize);
            this.tentacle.room = player.room;
            this.tentacle.segments = new List<IntVector2>();
            for (int i = 0; i < (int)(this.tentacle.idealLength / 20f); i++)
            {
                this.tentacle.segments.Add(player.abstractCreature.pos.Tile);//room.GetTilePosition(this.owner.firstChunk.pos)
            }
            this.tentacle.tProps = new Tentacle.TentacleProps(false, false, true, 0.5f, 0f, 1.4f, 0f, 0f, 1.2f, 10f, 0.25f, 5f, 15, 60, 12, 0);
            this.tentacle.tChunks = new Tentacle.TentacleChunk[(int)(15f * Mathf.Lerp(this.bodySize, 1f, 0.5f))];
            for (int i = 0; i < this.tentacle.tChunks.Length; i++)
            {
                this.tentacle.tChunks[i] = new Tentacle.TentacleChunk(this.tentacle, i, (float)(i + 1) / (float)this.tentacle.tChunks.Length, 2f * Mathf.Lerp(this.bodySize, 1f, 0.5f));
                this.tentacle.tChunks[i].PhaseToSegment();
                this.tentacle.tChunks[i].Reset();
            }
            this.tentacle.stretchAndSqueeze = 0.1f;
            this.Extend();
        }

        #region 外观
        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            firstSprite = sLeaser.sprites.Length;
            Array.Resize(ref sLeaser.sprites, firstSprite + TotalSprites);
            sLeaser.sprites[firstSprite + 0] = new FSprite("WormEye", true);
            sLeaser.sprites[firstSprite + 1] = TriangleMesh.MakeLongMesh(this.tentacle.tChunks.Length, false, false);
            sLeaser.sprites[firstSprite + 2] = new FSprite("WormHead", true);
            sLeaser.sprites[firstSprite + 2].scale = Mathf.Lerp(this.bodySize, 1f, 0.5f);
            sLeaser.sprites[firstSprite + 2].scaleX = 0.75f;
            sLeaser.sprites[firstSprite + 2].anchorY = 0.12f;
            sLeaser.sprites[firstSprite + 3] = new FSprite("WormEye", true);
            self.AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            this.eyesColor = sLeaser.sprites[9].color;
            float extend = Mathf.Lerp(this.lastExtended, this.extended, timeStacker);
            Vector2 lasttChunkPos = Vector2.Lerp(lastRootPos, rootPos, timeStacker);
            float lastRad = 4f;
            for (int i = 0; i < this.tentacle.tChunks.Length; i++)
            {
                Vector2 tChunkPos = Vector2.Lerp(this.tentacle.tChunks[i].lastPos, this.tentacle.tChunks[i].pos, timeStacker);
                float ratio = (float)i / (float)(this.tentacle.tChunks.Length - 1);
                float tendToRetract = Mathf.Pow(Mathf.Max(1f - ratio - extend, 0f), 1.5f);
                if (extend < 0.2f)
                {
                    tendToRetract = Mathf.Min(1f, tendToRetract + Mathf.InverseLerp(0.2f, 0f, extend));
                }
                tChunkPos = Vector2.Lerp(tChunkPos, rootPos, tendToRetract) + new Vector2(0f, -this.length / 4f * Mathf.Pow(tendToRetract, 0.5f));
                float d = Mathf.Sin((Mathf.Lerp(this.sinWave - this.sinSpeed, this.sinWave, timeStacker) + ratio * this.numberOfWavesOnBody) * 3.1415927f * 2f);

                Vector2 dir = (tChunkPos - lasttChunkPos).normalized;
                Vector2 perp = Custom.PerpendicularVector(dir);
                Vector2 wave = perp * d * 11f * Mathf.Sqrt(this.length / 400f) * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(ratio * 3.1415927f)), 0.75f) * extend;

                //头两节不受波动影响
                if (i != this.tentacle.tChunks.Length - 1 && i != this.tentacle.tChunks.Length - 2)
                {
                    tChunkPos += wave;
                    dir = (tChunkPos - lasttChunkPos).normalized;
                }
                else
                    dir = (lookPoint - wantPos).normalized;
                perp = Custom.PerpendicularVector(dir); 
                //头部
                if (i == this.tentacle.tChunks.Length - 1)
                {
                    sLeaser.sprites[firstSprite + 2].x = tChunkPos.x - camPos.x;
                    sLeaser.sprites[firstSprite + 2].y = tChunkPos.y - camPos.y;
                    sLeaser.sprites[firstSprite + 2].rotation = Mathf.Lerp(sLeaser.sprites[firstSprite + 2].rotation, Custom.AimFromOneVectorToAnother(-dir, dir), 0.05f);
                    float num5 = Mathf.Cos(Custom.AimFromOneVectorToAnother(-dir, dir) / 360f * 2f * 3.1415927f);
                    num5 = Mathf.Pow(Mathf.Abs(num5), 0.25f) * Mathf.Sign(num5);
                    int num6 = (num5 * Mathf.Sign(dir.x) > 0f) ? 3 : 0;
                    dir = Custom.DegToVec(sLeaser.sprites[firstSprite + 2].rotation);
                    perp = Custom.PerpendicularVector(dir);
                    sLeaser.sprites[firstSprite + 3 - num6].x = tChunkPos.x - camPos.x + dir.x * 10f * this.bodySize + perp.x * 3f * Mathf.Lerp(this.bodySize, 1f, 0.75f) * num5;
                    sLeaser.sprites[firstSprite + 3 - num6].y = tChunkPos.y - camPos.y + dir.y * 10f * this.bodySize + perp.y * 3f * Mathf.Lerp(this.bodySize, 1f, 0.75f) * num5;
                    sLeaser.sprites[firstSprite + 0 + num6].x = tChunkPos.x - camPos.x + dir.x * 10f * this.bodySize - perp.x * 3f * Mathf.Lerp(this.bodySize, 1f, 0.75f) * num5;
                    sLeaser.sprites[firstSprite + 0 + num6].y = tChunkPos.y - camPos.y + dir.y * 10f * this.bodySize - perp.y * 3f * Mathf.Lerp(this.bodySize, 1f, 0.75f) * num5;
                    /*
                    float modifyH = -Mathf.Clamp(tChunkPos.y - lasttChunkPos.y, -2f, 2f);
                    sLeaser.sprites[firstSprite + 2].y += modifyH;
                    sLeaser.sprites[firstSprite + 3 - num6].y += modifyH;
                    sLeaser.sprites[firstSprite + 0 + num6].y += modifyH;*/
                }
                //蛇身
                float tChunkLength = Vector2.Distance(tChunkPos, lasttChunkPos) / 7f;
                float rad = this.tentacle.tChunks[i].stretchedRad + this.swallowArray[i] * 5f;
                (sLeaser.sprites[firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 0, lasttChunkPos - perp * (rad + lastRad) * 0.5f + dir * tChunkLength - camPos);
                (sLeaser.sprites[firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 1, lasttChunkPos + perp * (rad + lastRad) * 0.5f + dir * tChunkLength - camPos);
                (sLeaser.sprites[firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 2, tChunkPos - perp * rad - dir * tChunkLength - camPos);
                (sLeaser.sprites[firstSprite + 1] as TriangleMesh).MoveVertice(i * 4 + 3, tChunkPos + perp * rad - dir * tChunkLength - camPos);
                lastRad = rad;
                lasttChunkPos = tChunkPos;
            }
            sLeaser.sprites[firstSprite + 0].color = (this.showAsAngry ? new Color(1f, 0f, 0f) : eyesColor);
            sLeaser.sprites[firstSprite + 3].color = (this.showAsAngry ? new Color(1f, 0f, 0f) : eyesColor);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[firstSprite + 1].color = sLeaser.sprites[0].color;//palette.blackColor;
            sLeaser.sprites[firstSprite + 2].color = sLeaser.sprites[0].color;//palette.blackColor;
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            PlayerGraphics self = player.graphicsModule as PlayerGraphics;
            if (firstSprite >= 1 && sLeaser.sprites.Length >= firstSprite + TotalSprites)
            {
                var foregroundContainer = rCam.ReturnFContainer("Foreground");
                var midgroundContainer = rCam.ReturnFContainer("Midground");
                if (newContatiner == null)
                {
                    newContatiner = rCam.ReturnFContainer("Midground");
                }
                for (int k = 0; k < TotalSprites; k++)
                {
                    sLeaser.sprites[firstSprite + k].RemoveFromContainer();
                    newContatiner.AddChild(sLeaser.sprites[firstSprite + k]);
                    sLeaser.sprites[firstSprite + k].MoveBehindOtherNode(sLeaser.sprites[0]);
                }
            }
        }

        public void Reset()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            this.tentacle.Reset(player.bodyChunks[0].pos);
        }

        public void NewRoom(Room room)
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            this.tentacle.NewRoom(room);
        }
        #endregion

        public void Update()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            this.RootPosUpdate();
            this.LookUpdate();
            this.tentacle.Update();
            this.tentacle.limp = !player.Consious;
            this.tentacle.retractFac = 1f - this.extended;

            this.showAsAngry = false;
            if (this.attackCounter > 0)
            {
                this.attackCounter++;
                if (this.attackCounter > 220)
                {
                    this.attackCounter = 0;
                }
                return;
            }

            this.moveCount--;
            if (moveCount == 0)
            {
                randomMovePos = Custom.DegToVec(indexDeg + 2f * (Random.value - 0.5f) * 360f / (float)medusa.hairs.Length) * length * 0.8f + 
                                Mathf.Clamp(Random.value, 0.7f, 1f) * Custom.RNV() * length * 0.3f;
                moveCount = Random.Range(40, 240);
                sinPhase = Random.value * 0.5f + 0.75f;
            }

            if (player.Consious)
            {/*
                if (this.chargePos != null)
                {
                    this.lookPoint = this.chargePos.Value + Custom.DirVec(base.mainBodyChunk.pos, this.chargePos.Value) * 100f;
                    base.mainBodyChunk.vel += Vector2.ClampMagnitude(this.chargePos.Value - base.mainBodyChunk.pos, 20f) / 5f;
                    this.chargePos = null;
                }*/
                this.movePos = Vector2.Lerp(this.movePos, 
                                            new Vector2(-3f * headToFace.x, -2.5f * headToFace.y) * length * 0.03f +//随猫猫的头而透视
                                            -(this.lookPoint - this.wantPos).normalized * length * 0.1f +//随自己的视线而移动
                                            randomMovePos,
                                            0.1f);
                this.wantPos = this.rootPos + this.movePos;
                this.lookPoint = Vector2.Lerp(this.lookPoint,
                                              this.WantLookPoint,
                                              0.05f);
                this.extended += this.retractSpeed;
                this.extended = Mathf.Clamp(this.extended, 0f, 1f);
                List<IntVector2> list = null;
                this.tentacle.MoveGrabDest(this.wantPos, ref list);
                float value = Vector2.Distance(this.tentacle.Tip.pos, this.lookPoint);
                if (this.attackCounter == 0)
                {
                    this.tentacle.tProps.goalAttractionSpeedTip = Mathf.Lerp(0.15f, 1.9f, Mathf.InverseLerp(40f, 90f, value));
                    //this.tentacle.tProps.goalAttractionSpeedTip = Mathf.Lerp(0.15f, 1.9f, Mathf.InverseLerp(40f, this.AI.searchingGarbage ? 90f : 290f, value));
                    
                    if (this.tentacle.backtrackFrom == -1 && player.room.aimap.getTerrainProximity(player.mainBodyChunk.pos) < 2)
                    {
                        for (int num6 = 0; num6 < 8; num6++)
                        {
                            if (player.room.aimap.getTerrainProximity(player.room.GetTilePosition(player.mainBodyChunk.pos) + Custom.eightDirections[num6]) > 1)
                            {
                                this.tentacle.Tip.vel += Custom.eightDirections[num6].ToVector2() * 2f;
                                break;
                            }
                        }
                    }
                }
                else if (this.attackCounter < 20)
                {
                    this.tentacle.tProps.goalAttractionSpeedTip = 0.1f;
                }
                else if (this.attackCounter < 40)
                {
                    this.tentacle.tProps.goalAttractionSpeedTip = 40f;
                    this.tentacle.Tip.vel += Vector2.ClampMagnitude(this.lookPoint - player.mainBodyChunk.pos, 30f) / 1f;
                }
                else if (this.attackCounter < 190)
                {
                    this.tentacle.Tip.pos = this.lookPoint;
                }
                else
                {
                    this.lookPoint.y = this.lookPoint.y + 20f;
                    this.tentacle.tProps.goalAttractionSpeedTip = 0.01f;
                }

                for (int num7 = 0; num7 < this.tentacle.tChunks.Length; num7++)
                {
                    if (this.tentacle.backtrackFrom == -1 || num7 < this.tentacle.backtrackFrom)
                    {
                        float num8 = ((float)num7 + 0.5f) / (float)this.tentacle.tChunks.Length;
                        this.tentacle.tChunks[num7].vel *= Mathf.Lerp(0.9f, 0.99f, num8);
                        if (this.attackCounter > 20 || this.extended < 1f)
                        {
                            Tentacle.TentacleChunk tentacleChunk = this.tentacle.tChunks[num7];
                            tentacleChunk.vel.y += 0.5f;
                        }
                        else
                        {
                            Tentacle.TentacleChunk tentacleChunk2 = this.tentacle.tChunks[num7];
                            //tentacleChunk2.vel.y = tentacleChunk2.vel.y + (1f - num8) * 0.5f;
                            tentacleChunk2.vel += (1f - num8) * 0.5f * Custom.DegToVec(this.index * 360f / (float)medusa.hairs.Length);
                            //tentacleChunk2.vel.y -= (1f - num8) * 0.5f;
                        }
                        float num9 = Mathf.Sin(3.1415927f * Mathf.Pow(num8, 2f)) * 0.1f;/*
                        if (this.CurrentlyLookingAtScaryCreature())
                        {
                            num9 *= 3f * Mathf.InverseLerp(220f, 20f, value);
                        }
                        else */
                        if (this.attackCounter > 0 && this.attackCounter < 20)
                        {
                            num9 *= 3f;
                        }
                        this.tentacle.tChunks[num7].vel += Custom.DirVec(this.wantPos, this.tentacle.tChunks[num7].pos) * num9;
                        if (num7 > 1 && num7 < this.tentacle.tChunks.Length - 1)
                        {
                            this.tentacle.tChunks[num7].vel += Custom.DirVec(this.tentacle.tChunks[num7 - 2].pos, this.tentacle.tChunks[num7].pos) * 0.2f;
                            this.tentacle.tChunks[num7 - 2].vel -= Custom.DirVec(this.tentacle.tChunks[num7 - 2].pos, this.tentacle.tChunks[num7].pos) * 0.2f;
                        }
                    }
                }
            }
            else
            {
                for (int num10 = 0; num10 < this.tentacle.tChunks.Length; num10++)
                {
                    if (this.tentacle.backtrackFrom == -1 || num10 < this.tentacle.backtrackFrom)
                    {
                        this.tentacle.tChunks[num10].vel *= 0.95f;
                        Tentacle.TentacleChunk tentacleChunk3 = this.tentacle.tChunks[num10];
                        tentacleChunk3.vel.y = tentacleChunk3.vel.y - 0.5f;
                    }
                }
            }

            if (this.extended != 0f)
            {
                float num14 = 0.5f;
                if (this.tentacle.backtrackFrom == -1)
                {
                    num14 = 0.5f;
                }
                else if (!player.Consious)
                {
                    num14 = 0.7f;
                }
                Vector2 a = Custom.DirVec(this.tentacle.Tip.pos, wantPos);
                float distA = Vector2.Distance(this.tentacle.Tip.pos, wantPos);
                this.tentacle.Tip.pos += Mathf.Min(5f, distA) * a * num14;
                distA = Vector2.Distance(this.tentacle.Tip.pos, wantPos);
                this.tentacle.Tip.vel *= 0f;
                this.tentacle.Tip.vel += Mathf.Min(5f, distA) * a * num14;
                
                Vector2 lookDir = (lookPoint - wantPos).normalized;
                Vector2 neckPos = this.tentacle.Tip.pos - lookDir * this.tentacle.idealLength / (float)this.tentacle.tChunks.Length * 0.9f;

                Vector2 b = Custom.DirVec(this.tentacle.tChunks[tentacle.tChunks.Length - 2].pos, neckPos);
                float distB = Vector2.Distance(this.tentacle.tChunks[tentacle.tChunks.Length - 2].pos, neckPos);
                this.tentacle.tChunks[tentacle.tChunks.Length - 2].pos += Mathf.Min(5f, distB) * b * (1f - num14);
                distB = Vector2.Distance(this.tentacle.tChunks[tentacle.tChunks.Length - 2].pos, neckPos); 
                this.tentacle.tChunks[tentacle.tChunks.Length - 2].vel *= 0f;
                this.tentacle.tChunks[tentacle.tChunks.Length - 2].vel += Mathf.Min(5f, distB) * b * (1f - num14);
            }

            if (player.Consious)
            {
                if (this.attackCounter < 20)
                {
                    this.numberOfWavesOnBody = Mathf.Lerp(this.numberOfWavesOnBody, Mathf.Lerp(1.8f, 3.4f, this.stress), 0.1f);
                    this.sinSpeed = Mathf.Lerp(this.sinSpeed, Mathf.Lerp(0.016666668f, 0.05f, this.stress) * sinPhase, 0.05f);
                }
                else
                {
                    this.numberOfWavesOnBody = Mathf.Lerp(this.numberOfWavesOnBody, 5f, 0.01f);
                    this.sinSpeed = Mathf.Lerp(this.sinSpeed, 0.05f, 0.1f);
                }
                this.sinWave += this.sinSpeed;
                if (this.sinWave > 1f)
                {
                    this.sinWave -= 1f;
                }
                if (this.attackCounter > 40 && this.attackCounter < 190 && UnityEngine.Random.value < 0.033333335f)
                {
                    this.swallowArray[this.swallowArray.Length - 1] = Mathf.Pow(UnityEngine.Random.value, 0.5f);
                }
                if (UnityEngine.Random.value < 0.33333334f)
                {
                    for (int i = 0; i < this.swallowArray.Length - 1; i++)
                    {
                        this.swallowArray[i] = Mathf.Lerp(this.swallowArray[i], this.swallowArray[i + 1], 0.7f);
                    }
                }
                this.swallowArray[this.swallowArray.Length - 1] = Mathf.Lerp(this.swallowArray[this.swallowArray.Length - 1], 0f, 0.7f);
            }
            this.lastExtended = this.extended;
        }

        public void RootPosUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;

            lastHeadToFace = headToFace;
            lastRootPos = rootPos;

            headToFace = Vector2.zero;
            foreach (var sLeaser in player.room.game.cameras[0].spriteLeasers)
            {
                if (sLeaser.drawableObject == player.graphicsModule)
                {
                    //头部至脸部方向的向量(转动修正)
                    headToFace = new Vector2(sLeaser.sprites[9].x - sLeaser.sprites[3].x, sLeaser.sprites[9].y - sLeaser.sprites[3].y);
                }
            }

            rootPos = (player.graphicsModule as PlayerGraphics).head.pos + new Vector2(-3f * headToFace.x, -2.5f * headToFace.y);
        }

        public void LookUpdate()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            if (player.room.world.rainCycle.RainApproaching < 1f && 
                Random.value > player.room.world.rainCycle.RainApproaching && 
                Random.value < 0.009803922f && 
                (player.room.roomSettings.DangerType == RoomRain.DangerType.Rain || player.room.roomSettings.DangerType == RoomRain.DangerType.FloodAndRain))
            {
                this.objectLooker.LookAtPoint(new Vector2(player.room.PixelWidth * Random.value, player.room.PixelHeight + 100f), (1f - player.room.world.rainCycle.RainApproaching) * 0.6f);
            }
            if (!player.Consious)
            {
                this.objectLooker.LookAtNothing();
            }
            if (!player.dead)
            {
                if (player.lungsExhausted || player.exhausted)
                {
                    this.objectLooker.LookAtNothing();
                }
            }
            if (Random.value < 0.1f)
            {
                this.objectLooker.Update();
            }
            if (Random.value < 0.0025f)
            {
                this.objectLooker.LookAtNothing();
            }
            this.lastLookDir = this.lookDirection;
            if (player.Consious && this.objectLooker.looking)
            {
                //this.lookDirection = Custom.DirVec(this.tentacle.Tip.pos, this.objectLooker.mostInterestingLookPoint);
                this.wantLookPoint = this.objectLooker.mostInterestingLookPoint;
            }
            else
            {
                //this.lookDirection *= 0f;
                wantLookPoint = Vector2.zero;
            }
        }

        public void Extend()
        {
            if (!ownerRef.TryGetTarget(out var player))
                return;
            IntVector2 tile = player.abstractCreature.pos.Tile;
            this.tentacle.segments = new List<IntVector2>
            {
                player.abstractCreature.pos.Tile
            };
            int num = player.abstractCreature.pos.Tile.y + 1;
            while ((float)num < (float)player.abstractCreature.pos.Tile.y + this.tentacle.idealLength / 20f && !player.room.GetTile(tile).Solid)
            {
                this.tentacle.segments.Add(tile);
                tile.y = num;
                num++;
            }
            for (int k = 0; k < this.tentacle.tChunks.Length; k++)
            {
                this.tentacle.tChunks[k].pos = player.room.MiddleOfTile(this.tentacle.segments[this.tentacle.tChunks[k].currentSegment]);
                this.tentacle.tChunks[k].lastPos = this.tentacle.tChunks[k].pos;
            }
            this.tentacle.retractFac = 0f;
            this.extended = 1f;
            this.retractSpeed = 0.005f;
        }
    }
    #endregion
}
