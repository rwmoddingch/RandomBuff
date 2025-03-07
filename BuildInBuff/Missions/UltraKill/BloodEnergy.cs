using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static SharedPhysics;

namespace BuiltinBuffs.Missions.UltraKill
{
    internal class BloodEnergy : CosmeticSprite, IProjectileTracer
    {
        static int tailPosCount = 5;
        float carriedEnergy;
        List<Vector2> tailPosList = new List<Vector2>();

        public BloodEnergy(Room room, Vector2 pos, Vector2 vel, float carriedEnergy) 
        {
            this.room = room;
            this.pos = this.lastPos = pos;
            this.vel = vel;
            this.carriedEnergy = carriedEnergy;
            for (int i = 0; i < tailPosCount; i++)
                tailPosList.Add(pos);
        }

        public bool HitThisChunk(BodyChunk chunk)
        {
            return chunk.owner is Player;
        }

        public bool HitThisObject(PhysicalObject obj)
        {
            return obj is Player;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion)
                return;

            vel += Vector2.down * room.gravity;
            if (room.GetTile(pos).Solid)
                Destroy();

            SharedPhysics.CollisionResult collisionResult = SharedPhysics.TraceProjectileAgainstBodyChunks(this, room, lastPos, ref pos, 10f, 1, null, false);
            if(collisionResult.chunk != null)
            {
                var manager = UltraKillManager.Get(room);
                if(manager != null)
                {
                    manager.ultraKillPlayerRevulver.AddEnergy(carriedEnergy);
                }
                Destroy();
            }

            tailPosList.Insert(0, pos);
            if (tailPosList.Count > tailPosCount)
                tailPosList.RemoveAt(tailPosCount);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(tailPosCount - 1, false, true, "Futile_White");
            sLeaser.sprites[0].shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"];
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);

            var triangleMesh = sLeaser.sprites[0] as TriangleMesh;

            for (int i = 0; i < tailPosCount - 1; i++)
            {
                float width = 1.5f * (1f - i / (float)tailPosCount);

                Vector2 smoothPos = GetSmoothPos(i, timeStacker);
                Vector2 smoothPos2 = GetSmoothPos(i + 1, timeStacker);
                Vector2 v2 = (vector - smoothPos).normalized;
                Vector2 v3 = Custom.PerpendicularVector(v2);
                v2 *= Vector2.Distance(vector, smoothPos2) / 5f;
                triangleMesh.MoveVertice(i * 4, vector - v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 1, vector + v3 * width - v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 2, smoothPos - v3 * width + v2 - camPos);
                triangleMesh.MoveVertice(i * 4 + 3, smoothPos + v3 * width + v2 - camPos);

                for (int j = 0; j < 4; j++)
                    triangleMesh.verticeColors[i * 4 + j] = Color.Lerp(Color.red, Color.black, i/(float)tailPosCount);

                vector = smoothPos;
            }
        }

        Vector2 GetSmoothPos(int i, float timeStacker)
        {
            return Vector2.Lerp(GetPos(i + 1), GetPos(i), timeStacker);
        }

        Vector2 GetPos(int i)
        {
            return tailPosList[Custom.IntClamp(i, 0, tailPosList.Count - 1)];
        }
    }
}
