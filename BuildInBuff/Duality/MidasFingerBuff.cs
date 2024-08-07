using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Duality
{
    internal class MidasFingerBuff : Buff<MidasFingerBuff, MidasFingerBuffData>
    {
        public override BuffID ID => MidasFingerBuffEntry.MidasFinger;
    }

    class MidasFingerBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 3;
        public override BuffID ID => MidasFingerBuffEntry.MidasFinger;
    }

    class MidasFingerBuffEntry : IBuffEntry
    {
        public static BuffID MidasFinger = new BuffID("MidasFinger", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<MidasFingerBuff, MidasFingerBuffData, MidasFingerBuffEntry>(MidasFinger);
        }

        public static void HookOn()
        {
            On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            try
            {
                if (self.room != null)
                {
                    if (self.grasps != null && self.grasps.Length > 0)
                    {
                        for (int i = 0; i < self.grasps.Length; i++)
                        {
                            if (self.grasps[i] != null)
                            {
                                if (self.grasps[i].grabbed != null && self.grasps[i].grabbed is Rock && !(self.grasps[i].grabbed is WaterNut))
                                {
                                    Vector2 pos = self.grasps[i].grabbed.firstChunk.pos;
                                    self.room.AddObject(new SmallShiny(self.room, pos));

                                    var pearl = new DataPearl.AbstractDataPearl(self.room.world, AbstractPhysicalObject.AbstractObjectType.DataPearl, null,
                                        new WorldCoordinate(self.room.abstractRoom.index, -1, -1, 0), self.room.game.GetNewID(), -1, -1, null, DataPearl.AbstractDataPearl.DataPearlType.Misc);
                                    pearl.RealizeInRoom();
                                    self.grasps[i].grabbed.Destroy();
                                    self.ReleaseGrasp(i);
                                    self.SlugcatGrab(pearl.realizedObject, i);                                    
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
                         
        }
    }

    public class SmallShiny : CosmeticSprite
    {
        public float lastLife;
        public float life;
        public int lastRotation;
        public int rotation;

        public SmallShiny(Room room, Vector2 pos) 
        { 
            this.room = room;
            this.pos = pos;
            life = 1f;
            lastLife = 1f;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("buffassets/illustrations/MStar");
            sLeaser.sprites[0].alpha = 0.8f;
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[0]);
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = Color.Lerp(Color.yellow, Color.white, 0.2f);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].scale = 0.8f * Mathf.Sin(Mathf.PI * Mathf.Lerp(lastLife, life, timeStacker));
            sLeaser.sprites[0].rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);
            sLeaser.sprites[0].SetPosition(pos - camPos);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion) return;

            lastLife = life;
            life -= 0.05f;

            if (life <= 0)
            {
                Destroy();
            }

            lastRotation = rotation;
            rotation += 12;
            if(rotation == 360) rotation = 0;
        }
    }
}
