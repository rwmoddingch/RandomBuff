using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System.Linq;
using UnityEngine;

namespace BuiltinBuffs.Positive
{
    internal class EmergencyPlatformBuffData : BuffData
    {


        public override BuffID ID => EmergencyPlatformBuffHooks.EmergencyPlatformBuffID;
    }

    internal class EmergencyPlatformBuff : Buff<EmergencyPlatformBuff,EmergencyPlatformBuffData>
    {
        public override BuffID ID => EmergencyPlatformBuffHooks.EmergencyPlatformBuffID;


        public override bool Trigger(RainWorldGame game)
        {
            foreach (var player in game.Players.Select(i => i.realizedCreature as Player))
            {
            
                if(player?.room == null || player.bodyMode == Player.BodyModeIndex.CorridorClimb)
                    continue;
                var pos = player.room.GetTilePosition(player.DangerPos) + new IntVector2(0, -1);
                if(pos.x >= player.room.TileWidth || pos.x < 0 || pos.y >= player.room.TileHeight || pos.y < 0 || player.room.GetTile(pos).Solid)
                    continue;
                player.room.AddObject(new FloatPlat(player.room,pos));
                
            }
            return true;
        }
    }

    internal class FloatPlat : CosmeticSprite
    {
        private int counter = 10 * 40;
        private Room.Tile.TerrainType lastType;
        public FloatPlat(Room room, IntVector2 pos)
        {
            this.room = room;
            lastPos = base.pos = room.MiddleOfTile(pos);
            lastType = room.GetTile(pos).Terrain;
            room.GetTile(pos).Terrain = Room.Tile.TerrainType.Solid;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            counter--;
            if(counter == 0)
                Destroy();
        }

        public override void Destroy()
        {
            base.Destroy();
            room.GetTile(pos).Terrain = lastType;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(sLeaser, rCam);
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            AddToContainer(sLeaser, rCam, null);
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            sLeaser.sprites[0].alpha = counter > 3.5f * 40 ? 1 : 1-((Mathf.Sin((counter-timeStacker) / 20f * Mathf.PI) + 1f) / 2f);
            sLeaser.sprites[0].SetPosition(pos-camPos);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            newContatiner = rCam.ReturnFContainer("Foreground");
            newContatiner.AddChild(sLeaser.sprites[0]);
        }
    }

    internal class EmergencyPlatformBuffHooks : IBuffEntry
    {
        public static readonly BuffID EmergencyPlatformBuffID = new BuffID("EmergencyPlatform", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<EmergencyPlatformBuff, EmergencyPlatformBuffData>(EmergencyPlatformBuffID);
        }
    }
}
