using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RandomBuffUtils;
using UnityEngine;

namespace HotDogGains.Positive
{
    class SpiderSenseBuff : Buff<SpiderSenseBuff, SpiderSenseBuffData>
    {
        public override BuffID ID => SpiderSenseBuffEntry.SpiderSenseID;

    }
    class SpiderSenseBuffData : BuffData { public override BuffID ID => SpiderSenseBuffEntry.SpiderSenseID; }
    class SpiderSenseBuffEntry : IBuffEntry
    {
        public static BuffID SpiderSenseID = new BuffID("SpiderSenseID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpiderSenseBuff, SpiderSenseBuffData, SpiderSenseBuffEntry>(SpiderSenseID);
        }
        public static void HookOn()
        {
            On.Weapon.Update += Weapon_Update;
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.mode == Weapon.Mode.Thrown && self.thrownBy != null && Mathf.Abs(self.firstChunk.vel.x) > 0.5f)
            {

                foreach (var player in self.room.PlayersInRoom.Where(i => i != null))
                {
                    if (player != self.thrownBy)
                    {
                        player.SpiderSense().FlyingWeapon(self);
                    }
                }

            }
        }
    }
    public static class ExPlayer
    {
        public static ConditionalWeakTable<Player, SpiderSense> modules = new ConditionalWeakTable<Player, SpiderSense>();
        public static SpiderSense SpiderSense(this Player player) => modules.GetValue(player, (Player p) => new SpiderSense(player));
    }

    public class SpiderSense
    {
        Player player;

        public bool BallisticCollision(Vector2 checkPos, Vector2 weaponLast, Vector2 weaponNext, float rad, float gravity)
        {
            float num = this.CrossHeight(checkPos.x, weaponLast, weaponNext, gravity);
            return num > checkPos.y - rad && num < checkPos.y + rad;
        }
        public float CrossHeight(float xPos, Vector2 weaponLast, Vector2 weaponNext, float gravity)
        {
            if (Mathf.Abs(weaponLast.x - weaponNext.x) < 1f)
            {
                return -1000f;
            }
            float num = Mathf.Abs(xPos - weaponNext.x) / Mathf.Abs(weaponLast.x - weaponNext.x);
            return Custom.VerticalCrossPoint(weaponLast, weaponNext, xPos).y - gravity * num * num;
        }


        public bool OutOfDanger(Player player, Vector2 weaponLast, Vector2 weaponNext, Vector2[] tryPositions, float weaponRad, float gravity)
        {
            for (int i = 0; i < tryPositions.Length; i++)
            {
                if (this.BallisticCollision(tryPositions[i], weaponLast, weaponNext, player.bodyChunks[i].rad + weaponRad, gravity))
                {
                    return false;
                }
            }
            return true;
        }


        public void FlyingWeapon(Weapon weapon)
        {
            if (player.room == null || player.inShortcut)
                return;

            //排除不需要躲的情况
            if (!player.Consious || Mathf.Abs(weapon.firstChunk.pos.y - player.mainBodyChunk.pos.y) > 120f || !Custom.DistLess(weapon.firstChunk.pos, player.mainBodyChunk.pos, 400f + 600) || Mathf.Abs(weapon.firstChunk.pos.x - weapon.firstChunk.lastPos.x) < 1f || !weapon.HeavyWeapon || weapon.firstChunk.pos.x < weapon.firstChunk.lastPos.x != player.mainBodyChunk.pos.x < weapon.firstChunk.pos.x)
            {
                return;
            }

            //击中玩家还需要的时间
            float t = Mathf.Abs(player.mainBodyChunk.pos.x - weapon.firstChunk.pos.x) / Mathf.Abs(weapon.firstChunk.pos.x - weapon.firstChunk.lastPos.x);


            //检测玩家身体是不是会被击中
            bool flag = false;
            for (int i = 0; i < player.bodyChunks.Length; i++)
            {
                if (BallisticCollision(player.bodyChunks[i].pos, weapon.firstChunk.lastPos, weapon.firstChunk.pos, player.bodyChunks[i].rad + weapon.firstChunk.rad + 5f +20f, (weapon is Spear) ? 0.45f : 0.9f))
                {
                    flag = true; break;
                }
            }
            if (!flag) return;


            float hitPositionY = CrossHeight(player.mainBodyChunk.pos.x, weapon.firstChunk.lastPos, weapon.firstChunk.pos, (weapon is Spear) ? 0.45f : 0.9f);

            Vector2[] testPos = new Vector2[player.bodyChunks.Length];

            for (int i = 0; i < testPos.Length; i++) testPos[i] = player.bodyChunks[i].pos;


            //检测趴下能不能躲开矛
            if (player.room.aimap.getAItile(player.bodyChunks[1].pos).acc == AItile.Accessibility.Floor)
            {
                float floorY = player.room.MiddleOfTile(player.bodyChunks[1].pos).y - 10f;
                if (hitPositionY > floorY)
                {
                    for (int l = 0; l < player.bodyChunks.Length; l++)
                    {
                        testPos[l].y = floorY + player.bodyChunks[l].rad;
                    }

                    //如果趴下可以躲开矛
                    if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, testPos, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                    {
                        for (int m = 0; m < player.bodyChunks.Length; m++)
                        {
                            //让身体瞬间到地板上一点的位置
                            player.bodyChunks[m].pos.y = testPos[m].y;
                            BodyChunk bodyChunk = player.bodyChunks[m];
                            //给一个继续把全身往下拉的速度创造动画效果
                            bodyChunk.vel.y = bodyChunk.vel.y - 8;
                        }
                        //player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                        player.animation = Player.AnimationIndex.DownOnFours;
                        return;
                    }
                }
            }

            //头上有空间可以跳的时候躲开
            if (hitPositionY < Mathf.Max(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y)
                &&
                (player.room.aimap.TileAccessibleToCreature(player.bodyChunks[0].pos + new Vector2(0f, 20f), player.Template)
                || player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos + new Vector2(0f, 20f), player.Template)
                || (!player.room.GetTile(player.firstChunk.pos + new Vector2(0f, 20f)).Solid && player.room.aimap.TileAccessibleToCreature(player.firstChunk.pos + new Vector2(0f, 40f), player.Template))))
            {
                Vector2 vector = (player.bodyChunks[1].pos + player.bodyChunks[0].pos)/2f;

                if (!player.room.GetTile(vector + new Vector2(0f, 25f)).Solid)
                {
                    vector.y += 15f;
                    for (int n = 0; n < player.bodyChunks.Length; n++)
                    {
                        testPos[n] = player.bodyChunks[n].pos;
                        if (testPos[n].y < vector.y)
                        {
                            testPos[n].y = Mathf.Lerp(testPos[n].y, vector.y, 0.75f);
                        }
                    }

                    if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, testPos, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                    {
                        for (int num5 = 0; num5 < player.bodyChunks.Length; num5++)
                        {
                            if (player.bodyChunks[num5].pos.y < vector.y)
                            {
                                BodyChunk bodyChunk2 = player.bodyChunks[num5];
                                bodyChunk2.vel.y = bodyChunk2.vel.y + 6;
                            }
                            player.bodyChunks[num5].pos.y = testPos[num5].y;
                        }
                        //player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                        return;
                    }
                }
            }

            if (hitPositionY > Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y) && !player.room.GetTile(player.bodyChunks[1].pos + new Vector2(0f, -20f)).Solid)
            {
                for (int num6 = 0; num6 < player.bodyChunks.Length; num6++)
                {
                    testPos[num6] = player.bodyChunks[num6].pos;
                    if (!player.room.GetTile(testPos[num6] + new Vector2(0f, -20f)).Solid)
                    {
                        Vector2[] array2 = testPos;
                        int num7 = num6;
                        array2[num7].y = array2[num7].y - 25f;
                    }
                }
                if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, testPos, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                {
                    for (int num8 = 0; num8 < player.bodyChunks.Length; num8++)
                    {
                        BodyChunk bodyChunk3 = player.bodyChunks[num8];
                        bodyChunk3.pos.y = bodyChunk3.pos.y - 6f;
                        BodyChunk bodyChunk4 = player.bodyChunks[num8];
                        bodyChunk4.vel.y = bodyChunk4.vel.y - 2f;
                    }
                    //player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                    return;
                }
            }

            //平地起跳躲矛
            for (int i = 0; i < player.bodyChunks.Length; i++)
            {
                if (BallisticCollision(player.bodyChunks[i].pos, weapon.firstChunk.lastPos, weapon.firstChunk.pos, player.bodyChunks[i].rad + weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f) && t < 6f && player.room.GetTile(player.bodyChunks[i].pos + new Vector2(0f, -player.bodyChunks[i].rad - 5f)).Solid && !player.room.GetTile(player.mainBodyChunk.pos + new Vector2(0f, 20f)).Solid)
                {
                    for (int num9 = 0; num9 < player.bodyChunks.Length; num9++)
                    {
                        testPos[num9] = player.bodyChunks[num9].pos + new Vector2(0f, 30);
                    }

                    if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, testPos, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                    {

                        for (int num10 = 0; num10 < player.bodyChunks.Length; num10++)
                        {
                            BodyChunk bodyChunk5 = player.bodyChunks[num10];
                            bodyChunk5.pos.y = bodyChunk5.pos.y + 15;
                        }
                        player.bodyChunks[0].vel.y = 2f;
                        player.bodyChunks[1].vel.y = 3f;
                        return;
                    }
                }
            }
        }

        public SpiderSense(Player player)
        {
            this.player = player;
        }
    }

}
