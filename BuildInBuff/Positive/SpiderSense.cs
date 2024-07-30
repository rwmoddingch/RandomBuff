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

        public Weapon weapon;

        public void TriggerWithWeapon(Weapon weapon)
        {
            if(this.weapon == weapon) return;
            this.weapon = weapon;
            TriggerSelf(true);
        }

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
            On.Weapon.ChangeMode += Weapon_ChangeMode;
            On.Player.Update += Player_Update;
        }

        private static void Weapon_ChangeMode(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
           orig(self, newMode);
           if (newMode != Weapon.Mode.Thrown)
           {
               if (SpiderSenseBuff.Instance.weapon == self)
                   SpiderSenseBuff.Instance.weapon = null;
           }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.SpiderSense().dodgeDelay > 0) self.SpiderSense().dodgeDelay--;
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            orig.Invoke(self, eu);
            if (self.mode==Weapon.Mode.Thrown&&self.thrownBy != null && Mathf.Abs(self.firstChunk.vel.x) > 0.5f)
            {
                foreach (var player in self.room.PlayersInRoom)
                {
                    if (player!=self.thrownBy)
                    {
                        player.SpiderSense().FlyingWeapon(self);
                    }

                }
            }
        }
    }
    public static class ExPlayer
    {
        public static ConditionalWeakTable<Player,SpiderSense> modules=new ConditionalWeakTable<Player, SpiderSense>();
        public static SpiderSense SpiderSense(this Player player) => modules.GetValue(player, (Player p) => new SpiderSense(player));
    }
    public class SpiderSense
    {
        WeakReference<Player> playerRef;
        float dodgeSkill = 1;
        public float dodgeDelay = 0;

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

        public bool OutOfDanger(Player player,Vector2 weaponLast, Vector2 weaponNext, Vector2[] tryPositions, float weaponRad, float gravity)
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
            if(!playerRef.TryGetTarget(out var player) || player.room == null || player.inShortcut)
                return;
            

            float num = Mathf.Max(0, dodgeSkill) * (0.3f + 0.7f * Mathf.Pow(1, 0.3f));

            if (dodgeDelay > 0 || !player.Consious || Mathf.Abs(weapon.firstChunk.pos.y - player.mainBodyChunk.pos.y) > 120f || !Custom.DistLess(weapon.firstChunk.pos, player.mainBodyChunk.pos, 400f + 400f * num) || Mathf.Abs(weapon.firstChunk.pos.x - weapon.firstChunk.lastPos.x) < 1f || !weapon.HeavyWeapon || weapon.firstChunk.pos.x < weapon.firstChunk.lastPos.x != player.mainBodyChunk.pos.x < weapon.firstChunk.pos.x)
            {
                return;
            }
            float num2 = Mathf.Abs(player.mainBodyChunk.pos.x - weapon.firstChunk.pos.x) / Mathf.Abs(weapon.firstChunk.pos.x - weapon.firstChunk.lastPos.x);

            bool flag = false;
            for (int i = 0; i < player.bodyChunks.Length; i++)
            {
                if (BallisticCollision(player.bodyChunks[i].pos, weapon.firstChunk.lastPos, weapon.firstChunk.pos, player.bodyChunks[i].rad + weapon.firstChunk.rad + 5f + 40f * player.abstractCreature.personality.nervous, (weapon is Spear) ? 0.45f : 0.9f))
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)return;

            float num3 = CrossHeight(player.mainBodyChunk.pos.x, weapon.firstChunk.lastPos, weapon.firstChunk.pos, (weapon is Spear) ? 0.45f : 0.9f);

            Vector2[] array = new Vector2[]
            {
            player.bodyChunks[0].pos,
            player.bodyChunks[1].pos,
            };

            if (player.room.aimap.getAItile(player.bodyChunks[1].pos).acc == AItile.Accessibility.Floor)
            {
                float num4 = player.room.MiddleOfTile(player.bodyChunks[1].pos).y - 10f;
                if (num3 > num4)
                {
                    for (int l = 0; l < player.bodyChunks.Length; l++)
                    {
                        array[l].y = num4 + player.bodyChunks[l].rad;
                    }
                    if (OutOfDanger(player,weapon.firstChunk.lastPos, weapon.firstChunk.pos, array, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                    {
                        SpiderSenseBuff.Instance.TriggerWithWeapon(weapon);
                        //BuffUtils.Log(SpiderSenseBuffEntry.SpiderSenseID,"DUCK!");
                        dodgeDelay = (int)Mathf.Lerp(25f, 1f, dodgeSkill);
                        for (int m = 0; m < player.bodyChunks.Length; m++)
                        {
                            player.bodyChunks[m].pos.y = array[m].y;
                            BodyChunk bodyChunk = player.bodyChunks[m];
                            bodyChunk.vel.y = bodyChunk.vel.y - 4f * Mathf.Max(1f, 0.5f + dodgeSkill);
                        }
                        player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                        return;
                    }
                }
            }

            if (num3 < Mathf.Max(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y) && (player.room.aimap.TileAccessibleToCreature(player.bodyChunks[0].pos + new Vector2(0f, 20f), player.Template) || player.room.aimap.TileAccessibleToCreature(player.bodyChunks[1].pos + new Vector2(0f, 20f), player.Template) || (!player.room.GetTile(player.firstChunk.pos + new Vector2(0f, 20f)).Solid && player.room.aimap.TileAccessibleToCreature(player.firstChunk.pos + new Vector2(0f, 40f), player.Template))))
            {
                Vector2 vector = (player.bodyChunks[0].pos * 2f + player.bodyChunks[1].pos * 2f) / 4f;
                if (!player.room.GetTile(vector + new Vector2(0f, 25f)).Solid)
                {
                    vector.y += 15f;
                    for (int n = 0; n < player.bodyChunks.Length; n++)
                    {
                        array[n] = player.bodyChunks[n].pos;
                        if (array[n].y < vector.y)
                        {
                            array[n].y = Mathf.Lerp(array[n].y, vector.y, 0.75f);
                        }
                    }
                    if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, array, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f) )
                    {
                        SpiderSenseBuff.Instance.TriggerWithWeapon(weapon);
                        //BuffUtils.Log(SpiderSenseBuffEntry.SpiderSenseID,"UP DODGE!");
                        dodgeDelay = (int)Mathf.Lerp(25f, 1f, dodgeSkill);
                        for (int num5 = 0; num5 < player.bodyChunks.Length; num5++)
                        {
                            if (player.bodyChunks[num5].pos.y < vector.y)
                            {
                                BodyChunk bodyChunk2 = player.bodyChunks[num5];
                                bodyChunk2.vel.y = bodyChunk2.vel.y + 4f * Mathf.Max(1f, 0.5f + dodgeSkill);
                                bodyChunk2.vel.y = bodyChunk2.vel.y + 4f * Mathf.Max(1f, 0.5f + dodgeSkill);
                            }
                            player.bodyChunks[num5].pos.y = array[num5].y;
                        }
                        player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                        return;
                    }
                }
            }
            if (num3 > Mathf.Min(player.bodyChunks[0].pos.y, player.bodyChunks[1].pos.y) && !player.room.GetTile(player.bodyChunks[1].pos + new Vector2(0f, -20f)).Solid)
            {
                for (int num6 = 0; num6 < player.bodyChunks.Length; num6++)
                {
                    array[num6] = player.bodyChunks[num6].pos;
                    if (!player.room.GetTile(array[num6] + new Vector2(0f, -20f)).Solid)
                    {
                        Vector2[] array2 = array;
                        int num7 = num6;
                        array2[num7].y = array2[num7].y - 25f;
                    }
                }
                if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, array, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                {
                    SpiderSenseBuff.Instance.TriggerWithWeapon(weapon);
                    //BuffUtils.Log(SpiderSenseBuffEntry.SpiderSenseID,"DROP DODGE!");
                    dodgeDelay = (int)Mathf.Lerp(25f, 1f, dodgeSkill);
                    for (int num8 = 0; num8 < player.bodyChunks.Length; num8++)
                    {
                        BodyChunk bodyChunk3 = player.bodyChunks[num8];
                        bodyChunk3.pos.y = bodyChunk3.pos.y - 8f * Mathf.Max(1f, 0.5f + dodgeSkill);
                        BodyChunk bodyChunk4 = player.bodyChunks[num8];
                        bodyChunk4.vel.y = bodyChunk4.vel.y - 4f * Mathf.Max(1f, 0.5f + dodgeSkill);
                    }
                    player.stun = Mathf.Max(player.stun, (int)(24f * (1f - dodgeSkill)));
                    return;
                }
            }
            if (BallisticCollision(player.bodyChunks[1].pos, weapon.firstChunk.lastPos, weapon.firstChunk.pos, player.bodyChunks[1].rad + weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f) && num2 < 6f && player.room.GetTile(player.bodyChunks[1].pos + new Vector2(0f, -player.bodyChunks[1].rad - 10f)).Solid && !player.room.GetTile(player.mainBodyChunk.pos + new Vector2(0f, 20f)).Solid)
            {
                for (int num9 = 0; num9 < player.bodyChunks.Length; num9++)
                {
                    array[num9] = player.bodyChunks[num9].pos + new Vector2(0f, 13f + dodgeSkill * 3f);
                }
                if (OutOfDanger(player, weapon.firstChunk.lastPos, weapon.firstChunk.pos, array, weapon.firstChunk.rad + 5f, (weapon is Spear) ? 0.45f : 0.9f))
                {
                    SpiderSenseBuff.Instance.TriggerWithWeapon(weapon);
                    //BuffUtils.Log(SpiderSenseBuffEntry.SpiderSenseID,"HOP!");
                    dodgeDelay = (int)Mathf.Lerp(35f, 3f, dodgeSkill);
                    //player.footingCounter = 0;
                    for (int num10 = 0; num10 < player.bodyChunks.Length; num10++)
                    {
                        BodyChunk bodyChunk5 = player.bodyChunks[num10];
                        bodyChunk5.pos.y = bodyChunk5.pos.y + (15f + dodgeSkill * 3f);
                    }
                    player.bodyChunks[0].vel.y = 2f * Mathf.Max(1f, 0.5f + dodgeSkill);
                    player.bodyChunks[1].vel.y = 7f * Mathf.Max(1f, 0.5f + dodgeSkill);
                    player.stun = Mathf.Max(player.stun, (int)(44f * (1f - dodgeSkill)));
                    return;
                }
            }

        }

        public SpiderSense(Player player)
        {
            this.playerRef = new WeakReference<Player>(player);
        }
    }

}
