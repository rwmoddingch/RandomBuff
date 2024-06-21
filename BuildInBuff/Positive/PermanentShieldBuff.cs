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
using BuiltinBuffs.Duality;

namespace BuiltinBuffs.Positive
{
    internal class PermanentShieldBuff : Buff<PermanentShieldBuff, PermanentShieldBuffData>
    {
        public override BuffID ID => PermanentShieldBuffEntry.PermanentShield;
        
        public PermanentShieldBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                if (PermanentShieldBuffEntry.permanentShieldList == null)
                    PermanentShieldBuffEntry.permanentShieldList = new List<PermanentShield> { };
                else if (PermanentShieldBuffEntry.permanentShieldList.Count > 0)
                {
                    foreach (var permanentShield in PermanentShieldBuffEntry.permanentShieldList)
                    {
                        permanentShield.Destroy();
                    }
                    PermanentShieldBuffEntry.permanentShieldList.Clear();
                }
            }
        }
    }

    internal class PermanentShieldBuffData : BuffData
    {
        public override BuffID ID => PermanentShieldBuffEntry.PermanentShield;
    }

    internal class PermanentShieldBuffEntry : IBuffEntry
    {
        public static BuffID PermanentShield = new BuffID("PermanentShield", true);

        public static List<PermanentShield> permanentShieldList;

        public static int StackLayer
        {
            get
            {
                return PermanentShieldBuffEntry.PermanentShield.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<PermanentShieldBuff, PermanentShieldBuffData, PermanentShieldBuffEntry>(PermanentShield);
        }
        
        public static void HookOn()
        {
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Update += Player_Update;
        }

        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (permanentShieldList == null)
                permanentShieldList = new List<PermanentShield>();
            else if (permanentShieldList.Count > 0)
            {
                foreach (var permanentShield in permanentShieldList)
                {
                    permanentShield.Destroy();
                }
                permanentShieldList.Clear();
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (permanentShieldList.Count != StackLayer)
            {
                if (self != null && self.room != null)
                {
                    for (int j = permanentShieldList.Count; j < StackLayer; j++)
                    {
                        var permanentShield = new PermanentShield(self, j, self.room);
                        permanentShieldList.Add(permanentShield);
                        self.room.AddObject(permanentShield);
                    }
                }
            }
        }
    }

    internal class PermanentShield : CosmeticSprite
    {
        public Player owner;
        public int stackIndex;
        public int disappearCount;
        public float averageVoice;
        public Color color;

        int firstSprite;
        int totalSprites;
        float expand;
        float lastExpand;
        float getToExpand;
        float push;
        float lastPush;
        float getToPush;

        public bool IsExisting
        {
            get
            {
                return (disappearCount == 0);
            }
        }

        public PermanentShield(Player player, int stackIndex, Room room)
        {
            this.owner = player;
            this.room = room;
            this.stackIndex = stackIndex;
            this.disappearCount = 0;
            this.averageVoice = 0f;
            this.color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
            this.firstSprite = 0;
            this.totalSprites = 1;
            this.getToExpand = 1f;
            this.getToPush = 1f;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            this.room = rCam.room;
            sLeaser.sprites = new FSprite[totalSprites];
            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[this.firstSprite + i] = new FSprite("Futile_White", true);
                sLeaser.sprites[this.firstSprite + i].shader = rCam.game.rainWorld.Shaders["VectorCircle"];
                sLeaser.sprites[this.firstSprite + i].color = this.color;
            }
            this.AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (sLeaser.sprites[this.firstSprite].isVisible != this.IsExisting)
            {
                for (int i = 0; i < totalSprites; i++)
                {
                    sLeaser.sprites[this.firstSprite + i].isVisible = this.IsExisting;
                }
            }

            Vector2 vector = this.Center(timeStacker);
            for (int k = 0; k < totalSprites; k++)
            {
                sLeaser.sprites[this.firstSprite + k].x = vector.x - camPos.x;
                sLeaser.sprites[this.firstSprite + k].y = vector.y - camPos.y;
                sLeaser.sprites[this.firstSprite + k].scale = this.Radius(stackIndex, timeStacker) / 8f;
                sLeaser.sprites[this.firstSprite + k].alpha = 3f / this.Radius(stackIndex, timeStacker);
            }

            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
        } 

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(sLeaser, rCam, palette);

            for (int i = 0; i < totalSprites; i++)
            {
                sLeaser.sprites[firstSprite + i].color = this.color;
            }
        }
        /*
        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            var foregroundContainer = rCam.ReturnFContainer("Foreground");
            var midgroundContainer = rCam.ReturnFContainer("Midground");
            for (int i = 0; i < totalSprites; i++)
            {
                var sprite = sLeaser.sprites[firstSprite + i];
                foregroundContainer.RemoveChild(sprite);
                midgroundContainer.AddChild(sprite);
                sprite.MoveBehindOtherNode((owner.room.game.cameras[0].spriteLeasers.
                    First(k => k.drawableObject == owner.graphicsModule)).sprites[0]);
            }
        }*/

        public override void Update(bool eu)
        {
            if(owner.room == null || this.room == null || owner.room != this.room)
            {
                this.Destroy();
                return;
            }

            base.Update(eu);

            this.lastExpand = this.expand;
            this.lastPush = this.push;
            this.expand = Custom.LerpAndTick(this.expand, this.getToExpand, 0.05f, 0.0125f);
            this.push = Custom.LerpAndTick(this.push, this.getToPush, 0.02f, 0.025f);
            bool flag = true;
            if (UnityEngine.Random.value < 0.00625f)
            {
                this.getToExpand = ((UnityEngine.Random.value < 0.5f) ? 1f : Mathf.Lerp(0.95f, 1.05f, Mathf.Pow(UnityEngine.Random.value, 1.5f)));
            }
            if (UnityEngine.Random.value < 0.00625f || flag)
            {
                this.getToPush = 1f;//;((UnityEngine.Random.value < 0.5f && !flag) ? 0f : ((float)(-1 + UnityEngine.Random.Range(0, UnityEngine.Random.Range(0, 1)))));
            }

            if (disappearCount > 0)
                disappearCount--;
            
            if (IsExisting && owner.room != null)
            {
                List<PhysicalObject>[] physicalObjects = owner.room.physicalObjects;
                for (int i = 0; i < physicalObjects.Length; i++)
                {
                    for (int j = 0; j < physicalObjects[i].Count; j++)
                    {
                        PhysicalObject physicalObject = physicalObjects[i][j];
                        if (physicalObject is Weapon)
                        {
                            Weapon weapon = physicalObject as Weapon;
                            if (weapon.mode == Weapon.Mode.Thrown && Custom.Dist(weapon.firstChunk.pos, owner.firstChunk.pos) < this.Radius(stackIndex, 0f))
                            {
                                weapon.ChangeMode(Weapon.Mode.Free);
                                weapon.SetRandomSpin();
                                weapon.firstChunk.vel *= -0.2f;
                                for (int num8 = 0; num8 < 5; num8++)
                                {
                                    owner.room.AddObject(new Spark(weapon.firstChunk.pos, Custom.RNV(), Color.white, null, 16, 24));
                                }
                                owner.room.AddObject(new Explosion.ExplosionLight(weapon.firstChunk.pos, 150f, 1f, 8, Color.white));
                                owner.room.AddObject(new ShockWave(weapon.firstChunk.pos, 60f, 0.1f, 8, false));
                                owner.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, weapon.firstChunk, false, 1f, 1.5f + Random.value * 0.5f);
                                disappearCount = 1200;
                            }
                        }
                    }
                }
            }
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public Vector2 Center(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.owner.bodyChunks[0].lastPos, this.owner.bodyChunks[0].pos, timeStacker);
            return vector + Custom.DirVec(vector, Vector2.Lerp(this.owner.bodyChunks[1].lastPos, this.owner.bodyChunks[1].pos, timeStacker)) * 5f;
        }

        private float Radius(float ring, float timeStacker)
        {
            return (3f + ring + Mathf.Lerp(this.lastPush, this.push, timeStacker) - 0.5f * this.averageVoice) * Mathf.Lerp(this.lastExpand, this.expand, timeStacker) * 10f;
        }
    }
}
