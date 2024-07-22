using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using RWCustom;
using System.Collections.Generic;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System.Runtime.CompilerServices;
using Smoke;
using HotDogGains;

namespace TemplateGains
{
    class SnailMissileBuff : Buff<SnailMissileBuff, SnailMissileBuffData> { public override BuffID ID => SnailMissileBuffEntry.SnailMissileID; }
    class SnailMissileBuffData : BuffData { public override BuffID ID => SnailMissileBuffEntry.SnailMissileID; }
    class SnailMissileBuffEntry : IBuffEntry
    {
        public static BuffID SnailMissileID = new BuffID("SnailMissileID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SnailMissileBuff, SnailMissileBuffData, SnailMissileBuffEntry>(SnailMissileID);
        }
        public static void HookOn()
        {
            On.Snail.Update += Snail_Update;
            On.Snail.Collide += Snail_Collide;
            On.Snail.TerrainImpact += Snail_TerrainImpact;
        }

        private static void Snail_TerrainImpact(On.Snail.orig_TerrainImpact orig, Snail self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            orig.Invoke(self, chunk, direction, speed, firstContact);
            if (self.Missile().target!=null&&speed>10)
            {
                self.triggered = true;
                self.Stun(10);
                self.Missile().target = null;
            }
        }

        private static void Snail_Collide(On.Snail.orig_Collide orig, Snail self, PhysicalObject otherObject, int myChunk, int otherChunk)
        {
            if (otherObject is Player&&self.Consious)
            {
                self.triggered = true;
                self.Stun(160);

                self.Missile().target = null;
            }

            orig.Invoke(self, otherObject, myChunk, otherChunk);
        }

        private static void Snail_Update(On.Snail.orig_Update orig, Snail self, bool eu)
        {
            orig.Invoke(self, eu);
            if (!self.Consious) return;
            if (self.grabbedBy.Count > 0) { self.Missile().target = null; return; }
            var missile = self.Missile();

            Player niceSlug = null;
            foreach (var slug in self.room.PlayersInRoom)
            {
                if (Custom.Dist(slug.firstChunk.pos, self.firstChunk.pos) < 800f/*&&self.room.VisualContact(self.firstChunk.pos,slug.firstChunk.pos)*/)
                {
                    if (niceSlug == null)
                    {
                        niceSlug = slug;
                    }
                    else if (Custom.Dist(slug.firstChunk.pos, self.firstChunk.pos) < Custom.Dist(niceSlug.firstChunk.pos, self.firstChunk.pos))
                    {
                        niceSlug = slug;
                    }
                }
            }

            if (missile.target==null&&niceSlug!=null&&Random.value>0.9f)
            {
                missile.SetTarget(niceSlug);
            }

            if (missile.target!=null)
            {
                missile.Update();
            }

        }

    }
    public static class MissileSnail
    {
        public static ConditionalWeakTable<Snail,ExSnail> modules = new ConditionalWeakTable<Snail, ExSnail>();
        public static ExSnail Missile(this Snail snail) => modules.GetValue(snail, (Snail s) => new ExSnail(snail));
    }
    public class ExSnail
    {
        Snail self;

        public Player target;

        public Vector2 start;
        public Vector2 end;
        public Vector2 exPos1;
        public Vector2 exPos2;


        public float trackCount = 0;
        public static int maxCount = 40;
        public MissileSmoke smoke;
        //public QuickPathFinder pathFinder;

        public ExSnail(Snail self)
        {
            this.self = self;
            smoke = new MissileSmoke(self.room);
        }


        public void SetTarget(Player player)
        {
            target = player;
            trackCount = maxCount;//追踪的时间

            var room = self.room;
            start = self.DangerPos;
            end = player.firstChunk.pos+Vector2.down*60;

            exPos1 = Vector2.Lerp(start, end, Random.value)+Vector2.up*Random.Range(10,500);
            exPos2 = Vector2.Lerp(start, end, Random.value)+Vector2.up*Random.Range(10,500);
        }

        public void Update()
        {
            if (trackCount--<=0)
            {
                target = null;

                return;
            }
            //trackCount = Custom.LerpAndTick(trackCount,0,0.1f,0.024f);

            if (target.room!=self.room) { target = null;return; }
            self.suckPoint = null;

            Missile2();
            return;


        }
        public void Missile2()
        {
            var t = Mathf.InverseLerp(0,maxCount,trackCount);

            Vector2 dir=Vector2.zero;
            if (t<0.4)
            {
                dir = (Vector2.up + Custom.RNV() * 0.2f).normalized;

                
            }

            if (t>0.45&&t<=0.5)
            {
                end = target.firstChunk.pos;

            }
            if (t >0.5)
            {
                dir = Custom.DirVec(self.firstChunk.pos, end);
                //if (self.firstChunk.vel.normalized!=dir)
                //{

                //}
            }

            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                self.bodyChunks[i].vel.y += self.room.gravity;
            }

            if (dir!=Vector2.zero)
            {
                self.shellDirection = -dir;
                
                //让龟龟按着曲线往玩家那里飞
                self.bodyChunks[0].vel += 1.8f * dir;

                //拉伸龟龟让他更像在飞
                self.bodyChunks[1].vel -= dir;
                self.bodyChunks[0].vel += dir;

                if (smoke.room != self.room)
                {
                    smoke.Destroy();
                    smoke = new MissileSmoke(self.room);
                }
                //冒烟
                smoke.EmitSmoke(self.firstChunk.pos, -self.firstChunk.vel + Custom.RNV(), true, Random.Range(20, 40));

            }
            

        }


    }


    public class MissileSmoke : SmokeSystem
    {
        public MissileSmoke(Room room) : base(SmokeSystem.SmokeType.CyanLizardSmoke, room, 2, 0f)
        {
        }
        public override SmokeSystem.SmokeSystemParticle CreateParticle()
        {
            return new MissileSmoke.SnailMissileParticle();
        }

        // Token: 0x06003814 RID: 14356 RVA: 0x003E49FC File Offset: 0x003E2BFC
        public void EmitSmoke(Vector2 pos, Vector2 vel, bool big, float maxlifeTime)
        {
            MissileSmoke.SnailMissileParticle snailMissileParticle = this.AddParticle(pos, vel, maxlifeTime * Mathf.Lerp(0.3f, 1f, Random.value)) as MissileSmoke.SnailMissileParticle;
            if (snailMissileParticle != null)
            {
                snailMissileParticle.big = big;
            }
        }

        // Token: 0x020007BA RID: 1978
        public class SnailMissileParticle : SpriteSmoke
        {
            public override void Reset(SmokeSystem newOwner, Vector2 pos, Vector2 vel, float newLifeTime)
            {
                base.Reset(newOwner, pos, vel, newLifeTime);
                this.rad = Mathf.Lerp(28f, 46f, Random.value);
                this.moveDir = Random.value * 360f;
                this.counter = 0;
            }
            public override void Update(bool eu)
            {
                base.Update(eu);
                if (this.resting)
                {
                    return;
                }
                this.vel *= 0.7f + 0.3f / Mathf.Pow(this.vel.magnitude, 0.5f);
                this.moveDir += Mathf.Lerp(-1f, 1f, Random.value) * 50f;
                if (this.room.PointSubmerged(this.pos))
                {
                    this.pos.y = this.room.FloatWaterLevel(this.pos.x);
                }
                this.counter++;
                if (this.room.GetTile(this.pos).Solid && !this.room.GetTile(this.lastPos).Solid)
                {
                    IntVector2? intVector = SharedPhysics.RayTraceTilesForTerrainReturnFirstSolid(this.room, this.room.GetTilePosition(this.lastPos), this.room.GetTilePosition(this.pos));
                    FloatRect floatRect = Custom.RectCollision(this.pos, this.lastPos, this.room.TileRect(intVector.Value).Grow(2f));
                    this.pos = floatRect.GetCorner(FloatRect.CornerLabel.D);
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).x < 0f)
                    {
                        this.vel.x = Mathf.Abs(this.vel.x);
                        return;
                    }
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).x > 0f)
                    {
                        this.vel.x = -Mathf.Abs(this.vel.x);
                        return;
                    }
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).y < 0f)
                    {
                        this.vel.y = Mathf.Abs(this.vel.y);
                        return;
                    }
                    if (floatRect.GetCorner(FloatRect.CornerLabel.B).y > 0f)
                    {
                        this.vel.y = -Mathf.Abs(this.vel.y);
                    }
                }
            }

            // Token: 0x06003818 RID: 14360 RVA: 0x003E4CA4 File Offset: 0x003E2EA4
            public override float Rad(int type, float useLife, float useStretched, float timeStacker)
            {
                float num;
                if (type != 0)
                {
                    if (type != 1)
                    {
                        num = Mathf.Lerp(4f, this.rad, Mathf.Pow(1f - useLife, 0.2f));
                    }
                    else
                    {
                        num = 1.5f * Mathf.Lerp(2f, this.rad, Mathf.Pow(1f - useLife, 0.2f));
                    }
                }
                else
                {
                    num = Mathf.Lerp(4f, this.rad, Mathf.Pow(1f - useLife, 0.2f) + useStretched);
                }
                if (this.big)
                {
                    num *= 1f + Mathf.InverseLerp(0f, 10f, (float)this.counter + timeStacker);
                }
                else
                {
                    num *= 0.2f;
                }
                return num;
            }

            // Token: 0x06003819 RID: 14361 RVA: 0x003E4D64 File Offset: 0x003E2F64
            public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
                base.InitiateSprites(sLeaser, rCam);
                for (int i = 0; i < 2; i++)
                {
                    sLeaser.sprites[i].shader = this.room.game.rainWorld.Shaders["FireSmoke"];
                }
            }

            // Token: 0x0600381A RID: 14362 RVA: 0x003E4DB4 File Offset: 0x003E2FB4
            public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                if (this.resting)
                {
                    return;
                }
                float num = Mathf.Lerp(this.lastLife, this.life, timeStacker);
                Color color;
                if (this.big)
                {
                    color = Color.Lerp(SmokeColor, this.fadeColor, Mathf.InverseLerp(1f, 0.25f, num));
                }
                else
                {
                    color = Color.Lerp(SmokeColor, this.fadeColor, Mathf.InverseLerp(1f, 0.25f, num) * 0.5f);
                }
                sLeaser.sprites[0].color = color;
                sLeaser.sprites[1].color = color;
                sLeaser.sprites[0].alpha = Mathf.Pow(num, 0.25f) * (1f - this.stretched) * (this.big ? (1f - 0.2f * Mathf.InverseLerp(0f, 10f, (float)this.counter + timeStacker)) : 1f);
                sLeaser.sprites[1].alpha = (0.3f + Mathf.Pow(Mathf.Sin(num * 3.1415927f), 0.7f) * 0.65f * (1f - this.stretched)) * (this.big ? (1f - 0.2f * Mathf.InverseLerp(0f, 10f, (float)this.counter + timeStacker)) : 1f);
            }

            // Token: 0x0600381B RID: 14363 RVA: 0x003E4F2C File Offset: 0x003E312C
            public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
            {
                this.fadeColor = Color.Lerp(palette.blackColor, palette.fogColor, 0.6f);
            }


            public Color SmokeColor = Custom.hexToColor("ffaf00");
            // Token: 0x04003DB5 RID: 15797
            public Color fadeColor;

            // Token: 0x04003DB7 RID: 15799
            public float moveDir;

            // Token: 0x04003DB8 RID: 15800
            public int counter;

            // Token: 0x04003DB9 RID: 15801
            public bool big;
        }
    }
}
