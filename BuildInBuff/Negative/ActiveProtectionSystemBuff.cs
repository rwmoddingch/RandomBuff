using BuiltinBuffs.Duality;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class ActiveProtectionSystemBuffEntry : IBuffEntry
    {
        public static BuffID activeProtectionSysytem = new BuffID("ActiveProtectionSystem", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ActiveProtectionSystemBuffEntry>(activeProtectionSysytem);
        }
    }

    public class ProtectionMissile : Missile, IDrawable
    {
        static int tailPosCount = 10;
        static float deflectWeaponRad = 80f;
        int life;

        List<Vector2> tailPosList = new List<Vector2>();
        Color blackColor;
        static Color flashCol = Color.white;
        static Color flashColAlphaZero = flashCol.CloneWithNewAlpha(0f);

        public ProtectionMissile(Room room, Vector2 launchPos, Vector2 launchDir, Creature killTag) : base(room, launchPos, launchDir, 80f, 720f, 35f, 0, 1200f, 0f, killTag)
        {
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = TriangleMesh.MakeLongMesh(101, false, true, "Futile_White");
            sLeaser.sprites[1] = new FSprite("bee", true);

            AddToContainer(sLeaser, rCam, null);
        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Bloom");

            foreach (var sprite in sLeaser.sprites)
                newContatiner.AddChild(sprite);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[1].color = Color.grey;
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker);
            sLeaser.sprites[1].SetPosition(vector - camPos);
            sLeaser.sprites[1].rotation = Custom.VecToDeg(dir);
            sLeaser.sprites[2].SetPosition(vector - camPos);

            //float a = Mathf.Clamp(Mathf.Sin(Mathf.InverseLerp(0f, 0.5f, smoothBlink) * Mathf.PI), 0f, 1f); ;
            //sLeaser.sprites[1].color = Color.Lerp(blackColor, flashCol, smoothBlink);

            var triangleMesh = sLeaser.sprites[0] as TriangleMesh;
            for (int i = 0; i < tailPosCount - 1; i++)
            {
                float width = 1.3f * (1f - i / (float)tailPosCount);

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
                    triangleMesh.verticeColors[i * 4 + j] = Color.Lerp(flashCol, flashColAlphaZero, 0.3f * (i / (float)tailPosCount) + 0.7f);

                vector = smoothPos;
            }

            if (slatedForDeletetion || room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
        }

        public override IEnumerable<Vector3> GetDetectablePosAndRad()
        {
            foreach(var obj in room.updateList.Where(u => u is Weapon).Select(u => u as Weapon))
            {
                if (obj.mode == Weapon.Mode.Thrown)
                    yield return new Vector3(obj.firstChunk.pos.x, obj.firstChunk.pos.y, deflectWeaponRad);
            }
        }

        public override void ExplodeAt(Vector2 pos)
        {
            base.ExplodeAt(pos);

            Vector2 vector = pos;
            this.room.AddObject(new SootMark(this.room, vector, 80f, true));
            this.room.AddObject(new Explosion(this.room, null, vector, 7, 250f, 6.2f, 0.1f, 280f, 0.25f, killTag, 0.7f, 160f, 1f));
            this.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, Color.white));
            this.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
            this.room.AddObject(new ExplosionSpikes(this.room, vector, 14, 30f, 9f, 7f, 170f, Color.white));
            this.room.AddObject(new ShockWave(vector, 330f, 0.045f, 5, false));
            for (int i = 0; i < 25; i++)
            {
                Vector2 vector2 = Custom.RNV();
                if (this.room.GetTile(vector + vector2 * 20f).Solid)
                {
                    if (!this.room.GetTile(vector - vector2 * 20f).Solid)
                    {
                        vector2 *= -1f;
                    }
                    else
                    {
                        vector2 = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    this.room.AddObject(new Spark(vector + vector2 * Mathf.Lerp(30f, 60f, UnityEngine.Random.value), vector2 * Mathf.Lerp(7f, 38f, UnityEngine.Random.value) + Custom.RNV() * 20f * UnityEngine.Random.value, Color.Lerp(Color.white, new Color(1f, 1f, 1f), UnityEngine.Random.value), null, 11, 28));
                }
                this.room.AddObject(new Explosion.FlashingSmoke(vector + vector2 * 40f * UnityEngine.Random.value, vector2 * Mathf.Lerp(4f, 20f, Mathf.Pow(UnityEngine.Random.value, 2f)), 1f + 0.05f * UnityEngine.Random.value, new Color(1f, 1f, 1f), Color.white, UnityEngine.Random.Range(3, 11)));
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
