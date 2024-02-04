using RandomBuff;

using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Expedition;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Negative
{
    internal class ExplosiveJawBuffData : BuffData
    {
        public override BuffID ID => ExplosiveJawIBuffEntry.explosiveJawBuffID;
    }

    internal class ExplosiveJawBuff : Buff<ExplosiveJawBuff, ExplosiveJawBuffData>
    {
        public override BuffID ID => ExplosiveJawIBuffEntry.explosiveJawBuffID;
    }

    internal class ExplosiveJawIBuffEntry : IBuffEntry
    {
        public static BuffID explosiveJawBuffID = new BuffID("ExplosiveJaw", true);

        public static void HookOn()
        {
            On.Lizard.Violence += Lizard_Violence;
        }

        private static void Lizard_Violence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            if (hitChunk != null && hitChunk.index == 0 && directionAndMomentum != null && self.HitHeadShield(directionAndMomentum.Value))
            {
                Vector2 vector = hitChunk.pos;
                SpawnDamageOnlyExplosion(self, vector, self.room, self.effectColor, 1f);
            }
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ExplosiveJawBuff, ExplosiveJawBuffData, ExplosiveJawIBuffEntry>(explosiveJawBuffID);
        }

        public static void SpawnDamageOnlyExplosion(Creature source, Vector2 pos, Room room, Color color, float radMulti, float damage = 2f)
        {
            room.AddObject(new SootMark(room, pos, 80f, true));
            room.AddObject(new DamageOnlyExplosion(room, source, pos, 2, 250f * radMulti, 6.2f * radMulti, damage, 280f, 0.25f, source, 0.7f, 160f, 1f, source.abstractCreature));
            room.AddObject(new Explosion.ExplosionLight(pos, 280f * radMulti, 1f, 7, color));
            room.AddObject(new Explosion.ExplosionLight(pos, 230f * radMulti, 1f, 3, new Color(1f, 1f, 1f)));
            room.AddObject(new ExplosionSpikes(room, pos, 14, 30f * radMulti, 9f, 7f * radMulti, 170f * radMulti, color));
            room.AddObject(new ShockWave(pos, 330f * radMulti, 0.045f * radMulti, 5, false));

            for (int i = 0; i < 25; i++)
            {
                Vector2 a = Custom.RNV();
                if (room.GetTile(pos + a * 20f).Solid)
                {
                    if (!room.GetTile(pos - a * 20f).Solid)
                    {
                        a *= -1f;
                    }
                    else
                    {
                        a = Custom.RNV();
                    }
                }
                for (int j = 0; j < 3; j++)
                {
                    room.AddObject(new Spark(pos + a * Mathf.Lerp(30f, 60f, Random.value), a * Mathf.Lerp(7f, 38f, Random.value) + Custom.RNV() * 20f * Random.value, Color.Lerp(color, new Color(1f, 1f, 1f), Random.value), null, 11, 28));
                }
                room.AddObject(new Explosion.FlashingSmoke(pos + a * 40f * Random.value, a * Mathf.Lerp(4f, 20f, Mathf.Pow(Random.value, 2f)), 1f + 0.05f * Random.value, new Color(1f, 1f, 1f), color, Random.Range(3, 11)));
            }
            room.ScreenMovement(new Vector2?(pos), default(Vector2), 1.3f * radMulti);
            room.PlaySound(SoundID.Bomb_Explode, pos);
        }
    }

    public class DamageOnlyExplosion : Explosion
    {
        public List<AbstractCreature> immunityCreatures;
        public List<CreatureTemplate.Type> immunityTypes;
        public DamageOnlyExplosion(Room room,
                                    PhysicalObject sourceObject,
                                    Vector2 pos,
                                    int lifeTime,
                                    float rad,
                                    float force,
                                    float damage,
                                    float stun,
                                    float deafen,
                                    Creature killTagHolder,
                                    float killTagHolderDmgFactor,
                                    float minStun,
                                    float backgroundNoise, params AbstractCreature[] immunityCreatures) : base(room, sourceObject, pos, lifeTime, rad, force, damage, stun, deafen, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise)
        {
            this.immunityCreatures = new List<AbstractCreature>();
            this.immunityCreatures.AddRange(immunityCreatures);
        }

        public DamageOnlyExplosion(Room room,
                                    PhysicalObject sourceObject,
                                    Vector2 pos,
                                    int lifeTime,
                                    float rad,
                                    float force,
                                    float damage,
                                    float stun,
                                    float deafen,
                                    Creature killTagHolder,
                                    float killTagHolderDmgFactor,
                                    float minStun,
                                    float backgroundNoise, params CreatureTemplate.Type[] immunityTypes) : base(room, sourceObject, pos, lifeTime, rad, force, damage, stun, deafen, killTagHolder, killTagHolderDmgFactor, minStun, backgroundNoise)
        {
            this.immunityTypes = new List<CreatureTemplate.Type>();
            this.immunityTypes.AddRange(immunityTypes);
        }

        public override void Update(bool eu)
        {
            evenUpdate = eu;

            if (!this.explosionReactorsNotified)
            {
                this.explosionReactorsNotified = true;
                for (int i = 0; i < this.room.updateList.Count; i++)
                {
                    if (this.room.updateList[i] is Explosion.IReactToExplosions)
                    {
                        (this.room.updateList[i] as Explosion.IReactToExplosions).Explosion(this);
                    }
                }
                if (this.room.waterObject != null)
                {
                    this.room.waterObject.Explosion(this);
                }
            }
            this.room.MakeBackgroundNoise(this.backgroundNoise);
            float radFrac = this.rad * (0.25f + 0.75f * Mathf.Sin(Mathf.InverseLerp(0f, (float)this.lifeTime, (float)this.frame) * 3.1415927f));
            for (int j = 0; j < this.room.physicalObjects.Length; j++)
            {
                for (int k = 0; k < this.room.physicalObjects[j].Count; k++)
                {
                    if (this.sourceObject != this.room.physicalObjects[j][k] && !this.room.physicalObjects[j][k].slatedForDeletetion)
                    {
                        float num2 = 0f;
                        float dist = float.MaxValue;
                        int num4 = -1;
                        for (int l = 0; l < this.room.physicalObjects[j][k].bodyChunks.Length; l++)
                        {
                            float num5 = Vector2.Distance(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos);
                            dist = Mathf.Min(dist, num5);
                            if (num5 < radFrac)
                            {
                                float num6 = Mathf.InverseLerp(radFrac, radFrac * 0.25f, num5);
                                if (!this.room.VisualContact(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos))
                                {
                                    num6 -= 0.5f;
                                }
                                if (num6 > 0f)
                                {
                                    float num7 = this.force;
                                    if (ModManager.MSC && this.room.physicalObjects[j][k] is Player && (this.room.physicalObjects[j][k] as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                                    {
                                        num7 *= 0.25f;
                                    }
                                    this.room.physicalObjects[j][k].bodyChunks[l].vel += this.PushAngle(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos) * (num7 / this.room.physicalObjects[j][k].bodyChunks[l].mass) * num6;
                                    this.room.physicalObjects[j][k].bodyChunks[l].pos += this.PushAngle(this.pos, this.room.physicalObjects[j][k].bodyChunks[l].pos) * (num7 / this.room.physicalObjects[j][k].bodyChunks[l].mass) * num6 * 0.1f;
                                    if (num6 > num2)
                                    {
                                        num2 = num6;
                                        num4 = l;
                                    }
                                }
                            }
                        }
                        if (this.room.physicalObjects[j][k] == this.killTagHolder)
                        {
                            num2 *= this.killTagHolderDmgFactor;
                        }
                        if (this.deafen > 0f && this.room.physicalObjects[j][k] is Creature)
                        {
                            (this.room.physicalObjects[j][k] as Creature).Deafen((int)Custom.LerpMap(dist, radFrac * 1.5f * this.deafen, radFrac * Mathf.Lerp(1f, 4f, this.deafen), 150f * this.deafen, 0f));
                        }
                        if (num4 > -1)
                        {
                            if (this.room.physicalObjects[j][k] is Creature creature)
                            {
                                if (immunityCreatures != null && immunityCreatures.Contains(creature.abstractCreature))
                                    continue;
                                if (immunityTypes != null && immunityTypes.Contains(creature.abstractCreature.creatureTemplate.type))
                                    continue;

                                int num8 = 0;
                                while ((float)num8 < Math.Min(Mathf.Round(num2 * this.damage * 2f), 8f))
                                {
                                    Vector2 p = this.room.physicalObjects[j][k].bodyChunks[num4].pos + Custom.RNV() * this.room.physicalObjects[j][k].bodyChunks[num4].rad * Random.value;
                                    this.room.AddObject(new WaterDrip(p, Custom.DirVec(this.pos, p) * this.force * Random.value * num2, false));
                                    num8++;
                                }
                                if (this.killTagHolder != null && this.room.physicalObjects[j][k] != this.killTagHolder)
                                {
                                    (this.room.physicalObjects[j][k] as Creature).SetKillTag(this.killTagHolder.abstractCreature);
                                }
                                float num9 = this.damage;
                                if ((this.room.physicalObjects[j][k] is Player && (this.room.physicalObjects[j][k] as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) || (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity")))
                                {
                                    num9 *= 0.2f;
                                }
                                if (this.room.physicalObjects[j][k] is Player && (this.room.physicalObjects[j][k] as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Saint)
                                {
                                    num9 *= 4f;
                                    if (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity"))
                                    {
                                        num9 /= 4f;
                                    }
                                }
                                (this.room.physicalObjects[j][k] as Creature).Violence(null, null, this.room.physicalObjects[j][k].bodyChunks[num4], null, Creature.DamageType.Explosion, num2 * num9 / (((this.room.physicalObjects[j][k] as Creature).State is HealthState) ? ((float)this.lifeTime) : 1f), num2 * this.stun);
                                if (this.minStun > 0f && (!ModManager.MSC || !(this.room.physicalObjects[j][k] is Player) || (this.room.physicalObjects[j][k] as Player).SlugCatClass != MoreSlugcatsEnums.SlugcatStatsName.Artificer || (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity"))))
                                {
                                    (this.room.physicalObjects[j][k] as Creature).Stun((int)(this.minStun * Mathf.InverseLerp(0f, 0.5f, num2)));
                                }
                                if ((this.room.physicalObjects[j][k] as Creature).graphicsModule != null && (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts != null)
                                {
                                    for (int m = 0; m < (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts.Length; m++)
                                    {
                                        float num10 = this.force;
                                        if ((ModManager.MSC && this.room.physicalObjects[j][k] is Player && (this.room.physicalObjects[j][k] as Player).SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer) || (ModManager.Expedition && this.room.game.rainWorld.ExpeditionMode && ExpeditionGame.activeUnlocks.Contains("unl-explosionimmunity")))
                                        {
                                            num10 *= 0.25f;
                                        }
                                        (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos += this.PushAngle(this.pos, (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * num10 * 5f;
                                        (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].vel += this.PushAngle(this.pos, (this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m].pos) * num2 * num10 * 5f;
                                        if ((this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] is Limb)
                                        {
                                            ((this.room.physicalObjects[j][k] as Creature).graphicsModule.bodyParts[m] as Limb).mode = Limb.Mode.Dangle;
                                        }
                                    }
                                }
                            }
                            this.room.physicalObjects[j][k].HitByExplosion(num2, this, num4);
                        }
                    }
                }
            }
            this.frame++;
            if (this.frame > this.lifeTime)
            {
                this.Destroy();
                BuffPlugin.Log("Destroy explosion");
            }
        }
    }
}
