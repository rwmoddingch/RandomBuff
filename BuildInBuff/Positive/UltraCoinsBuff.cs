using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ObjectExtend;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevel;
using static SharedPhysics;
using Random = UnityEngine.Random;
using RandomBuff;
using TemplateGains;
using System.IO;
using MoreSlugcats;
using static BuiltinBuffs.Missions.UltraKill.UltraKillWave;
using static System.Net.Mime.MediaTypeNames;

namespace BuiltinBuffs.Positive
{
    internal class UltraCoinsBuff : Buff<UltraCoinsBuff, UltraCoinsBuffData>
    {
        public override BuffID ID => UltraCoinsBuffEntry.ultraCoinsBuffID;

        FLabel label;
        
        

        public UltraCoinsBuff()
        {
            //label = new FLabel(Custom.GetDisplayFont(), "");
            //Futile.stage.AddChild(label);
            
            label = new FLabel(Custom.GetFont(), "");
        }

        public override (bool, bool) TriggerWithEffect(RainWorldGame game)
        {
            return (Trigger(game), false);
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            //label.MoveToFront();
            
        }

        public override void Destroy()
        {
            base.Destroy();
           
        }

        public override bool Trigger(RainWorldGame game)
        {
            bool playSound = false;
            foreach(var p in game.AlivePlayers)
            {
                if (p.realizedCreature != null && p.realizedCreature.room != null)
                {
                    var player = p.realizedCreature as Player;
                    var abCoin = new AbstractUltraCoin(p.world, null, p.pos, player.room.game.GetNewID());
                    player.room.abstractRoom.AddEntity(abCoin);
                    abCoin.RealizeInRoom();

                    var coin = abCoin.realizedObject as UltraCoin;
                    Vector2 extraVel = player.firstChunk.vel + player.input[0].analogueDir * 9f + Vector2.up * 9f + Custom.RNV();
                    if (player.input[0].IntVec.y == 0)
                    {
                        extraVel += (player.flipDirection == 1 ? Vector2.right : Vector2.left) * 9f;
                    }

                    coin.firstChunk.pos = player.firstChunk.pos + (player.firstChunk.rad + coin.firstChunk.rad) * extraVel.normalized;
                    coin.firstChunk.lastPos = coin.firstChunk.pos;

                    coin.firstChunk.vel = extraVel;

                    if (!playSound)
                    {
                        player.room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, 0f, 1f, 3f + Random.value * 0.2f);
                        player.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 0.7f, 6f + Random.value * 0.2f);
                        playSound = true;
                    }
                }
            }

            return false;
        }
    }

 

    internal class UltraCoinsBuffData : BuffData
    {
        public override BuffID ID => UltraCoinsBuffEntry.ultraCoinsBuffID;
    }

    internal class UltraCoinsBuffEntry : IBuffEntry
    {
        public static BuffID ultraCoinsBuffID = new BuffID("UltraCoins", true);
        public static string ultraCoinsVFX0;
        
        public static bool skipGameUpdate;
        public static ConditionalWeakTable<Spear, CoinDeflectCountKeeper> coinDeflectCount = new ConditionalWeakTable<Spear, CoinDeflectCountKeeper>();

        public static Action richshotCallBack;
        public static Action penetrateCallBack;

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<UltraCoinsBuff, UltraCoinsBuffData, UltraCoinsBuffEntry>(ultraCoinsBuffID);
        }

        public static void LoadAssets()
        {
            ultraCoinsVFX0 = Futile.atlasManager.LoadImage(ultraCoinsBuffID.GetStaticData().AssetPath + Path.DirectorySeparatorChar + "ultracoinspark").elements[0].name;
        }
        public static void HookOn()
        {
            On.Spear.Update += Spear_Update;
            On.Spear.Thrown += Spear_Thrown;
            On.RainWorldGame.RawUpdate += RainWorldGame_RawUpdate;
            //On.Room.PlaySound_SoundID_BodyChunk_bool_float_float_bool += Room_PlaySound_SoundID_BodyChunk_bool_float_float_bool;
            //On.Room.PlaySound_SoundID_Vector2_float_float += Room_PlaySound_SoundID_Vector2_float_float;
        }

      

        //private static void Room_PlaySound_SoundID_Vector2_float_float(On.Room.orig_PlaySound_SoundID_Vector2_float_float orig, Room self, SoundID soundId, Vector2 pos, float vol, float pitch)
        //{
        //    UltraCoinsBuff.Instance.blindWaveEffectManager.PositonedSoundPlayed(soundId, pos, vol * 100f, null);
        //    orig.Invoke(self, soundId, pos , vol, pitch);
        //}

        //private static ChunkSoundEmitter Room_PlaySound_SoundID_BodyChunk_bool_float_float_bool(On.Room.orig_PlaySound_SoundID_BodyChunk_bool_float_float_bool orig, Room self, SoundID soundId, BodyChunk chunk, bool loop, float vol, float pitch, bool randomStartPosition)
        //{
        //    UltraCoinsBuff.Instance.blindWaveEffectManager.PositonedSoundPlayed(soundId, chunk.pos, vol * 100f, chunk);
        //    return orig.Invoke(self, soundId, chunk, loop, vol, pitch, randomStartPosition);
        //}

      

        private static void RainWorldGame_RawUpdate(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame self, float dt)
        {
            orig.Invoke(self, skipGameUpdate ? dt / 10f : dt);
        }

        private static void Spear_Thrown(On.Spear.orig_Thrown orig, Spear self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            orig.Invoke(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
            if ((self is Spear) && self.mode == Weapon.Mode.Thrown && self.thrownBy is Player)//重置Y额外速度防止飞偏
            {
                if (throwDir.x != 0)
                {
                    self.firstChunk.vel.y = self.firstChunk.vel.y - 1.5f;
                }
            }
        }

        public static CollisionResult TraceProjectileAgainstBodyChunks(IProjectileTracer projTracer, Room room, Vector2 lastPos, ref Vector2 pos, float rad, int collisionLayer, PhysicalObject exemptObject, bool hitAppendages)
        {
            float num = float.MaxValue;
            CollisionResult result = new CollisionResult(null, null, null, hitSomething: false, pos);
            int num2 = collisionLayer;
            int num3 = collisionLayer;
            if (collisionLayer < 0)
            {
                num2 = 0;
                num3 = room.physicalObjects.Length - 1;
            }

            for (int i = num2; i <= num3; i++)
            {
                foreach (PhysicalObject item in room.physicalObjects[i])
                {
                    if (item == exemptObject || !item.canBeHitByWeapons || (projTracer != null && !projTracer.HitThisObject(item)))
                    {
                        continue;
                    }

                    bool flag = false;
                    for (int j = 0; j < item.grabbedBy.Count; j++)
                    {
                        if (flag)
                        {
                            break;
                        }

                        flag = item.grabbedBy[j].grabber == exemptObject;
                    }

                    if (flag)
                    {
                        continue;
                    }

                    BodyChunk[] bodyChunks = item.bodyChunks;
                    foreach (BodyChunk bodyChunk in bodyChunks)
                    {
                        if (projTracer == null || projTracer.HitThisChunk(bodyChunk))
                        {
                            float num4 = Custom.CirclesCollisionTime(lastPos.x, lastPos.y, bodyChunk.pos.x, bodyChunk.pos.y, pos.x - lastPos.x, pos.y - lastPos.y, rad, bodyChunk.rad);
                            BuffUtils.Log("UltraCoins", $"try {item}, {num4}");
                            if (num4 > 0f && num4 < 1f && num4 < num)
                            {
                                num = num4;
                                result = new CollisionResult(item, bodyChunk, null, hitSomething: true, Vector2.Lerp(lastPos, pos, num4));
                            }
                        }
                    }

                    if (!hitAppendages || result.chunk != null || item.appendages == null)
                    {
                        continue;
                    }

                    foreach (PhysicalObject.Appendage appendage in item.appendages)
                    {
                        if (!appendage.canBeHit)
                        {
                            continue;
                        }

                        for (int l = 1; l < appendage.segments.Length; l++)
                        {
                            Vector2 vector = Custom.LineIntersection(lastPos, pos, appendage.segments[l - 1], appendage.segments[l]);
                            if (Mathf.InverseLerp(0f, Vector2.Distance(lastPos, pos), Vector2.Distance(lastPos, vector)) < num && Custom.DistLess(vector, lastPos, Vector2.Distance(lastPos, pos)) && Custom.DistLess(vector, pos, Vector2.Distance(lastPos, pos)) && Custom.DistLess(vector, appendage.segments[l - 1], Vector2.Distance(appendage.segments[l - 1], appendage.segments[l])) && Custom.DistLess(vector, appendage.segments[l], Vector2.Distance(appendage.segments[l - 1], appendage.segments[l])))
                            {
                                result = new CollisionResult(item, null, new PhysicalObject.Appendage.Pos(appendage, l - 1, Mathf.InverseLerp(0f, Vector2.Distance(appendage.segments[l - 1], appendage.segments[l]), Vector2.Distance(appendage.segments[l - 1], vector))), hitSomething: true, vector);
                            }
                        }
                    }
                }
            }

            return result;
        }

        private static void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig.Invoke(self, eu);

            if(!(self.thrownBy is Player))
            {
                return;
            }

            bool haveKeeper = coinDeflectCount.TryGetValue(self, out var keeper);
            if (self.mode != Weapon.Mode.Thrown)
            {
                if (haveKeeper)
                    coinDeflectCount.Remove(self);
                return;
            }

            Vector2 rayCastStartPos = self.firstChunk.lastPos;
            Vector2 rayCastEndPos = self.firstChunk.lastPos;
            Vector2 rayCastDir = self.firstChunk.vel.normalized;

            RayCastResult res = new RayCastResult();
            bool coinOnlyRayCastSuccess = false;

            if (haveKeeper && keeper.deflectCount > 0)
            {
                var returns = CoinOnlyRayCastScan(self, rayCastStartPos, rayCastDir);
                coinOnlyRayCastSuccess = returns.Item1;
                res = returns.Item2;
            }
            if (!coinOnlyRayCastSuccess)
            {
                res = RayCastScan(self, rayCastStartPos, rayCastDir, eu);
            }

            rayCastEndPos = res.endPos;

            self.room.AddObject(new SpeedTailEffect(self.room, rayCastStartPos, rayCastEndPos, ((haveKeeper && keeper.deflectCount > 0) ? UltraCoin.gold : Color.gray), 120, 5f));
            self.firstChunk.lastPos = self.firstChunk.pos = rayCastEndPos;

            if (res.deflect)
            {
                Vector2 dir;
                if (keeper != null && keeper.nextDeflectTarget != null && keeper.nextDeflectTarget.TryGetTarget(out var target))
                    dir = (target.pos - self.firstChunk.pos).normalized;
                else
                    dir = res.deflectDir;

                if (dir == Vector2.zero)
                    dir = Custom.RNV();

                self.firstChunk.vel = dir * self.firstChunk.vel.magnitude;
                self.doNotTumbleAtLowSpeed = true;
                self.spearDamageBonus = (self.spearDamageBonus + 1);
                //BuffUtils.Log("UltraCoins", $"{dir.x}, {dir.y}");
            }

            //rayCastStartPos = rayCastEndPos;
            //rayCastDir = res.deflectDir;
        }

        static RayCastResult RayCastScan(Spear self, Vector2 rayCastStart, Vector2 rayCastDir, bool eu)
        {
            RayCastResult rayCastResult = new RayCastResult();

            float num = self.exitThrownModeSpeed;
            if (self.overrideExitThrownSpeed > 0f)
            {
                num = self.overrideExitThrownSpeed;
            }

            if (self.thrownBy != null && Mathf.Abs(self.firstChunk.vel.x) > 0.5f)
            {
                for (int k = 0; k < self.room.abstractRoom.creatures.Count; k++)//Notice flying weapon
                {
                    if (self.room.abstractRoom.creatures[k].realizedCreature != null && self.room.abstractRoom.creatures[k].realizedCreature != self.thrownBy && self.room.abstractRoom.creatures[k].realizedCreature.room == self.room)
                    {
                        for (int l = 0; l < self.room.abstractRoom.creatures[k].realizedCreature.bodyChunks.Length; l++)
                        {
                            if (Custom.InRange(self.room.abstractRoom.creatures[k].realizedCreature.bodyChunks[l].pos.x, self.thrownPos.x - Mathf.Sign(self.firstChunk.vel.x) * (20f + self.room.abstractRoom.creatures[k].realizedCreature.bodyChunks[l].rad), self.thrownPos.x + Mathf.Sign(self.firstChunk.vel.x) * 2000f) && Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[k].realizedCreature.bodyChunks[l].pos, self.closestCritDist * (self.room.abstractRoom.creatures[k].creatureTemplate.quantified ? 2f : 1f)))
                            {
                                self.thrownClosestToCreature = self.room.abstractRoom.creatures[k].realizedCreature;
                                self.closestCritDist = Vector2.Distance(self.firstChunk.pos, self.room.abstractRoom.creatures[k].realizedCreature.bodyChunks[l].pos) * (self.room.abstractRoom.creatures[k].creatureTemplate.quantified ? 2f : 1f);
                            }
                        }
                        if (self.room.abstractRoom.creatures[k].realizedCreature is Weapon.INotifyOfFlyingWeapons)
                        {
                            (self.room.abstractRoom.creatures[k].realizedCreature as Weapon.INotifyOfFlyingWeapons).FlyingWeapon(self);
                        }
                    }
                }
            }
            self.changeDirCounter = -1;

            float maxReachDistance = float.MaxValue;
            Vector2 pos = Custom.RectCollision(rayCastStart, rayCastStart + rayCastDir * 10000f, self.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            maxReachDistance = Mathf.Min(maxReachDistance, Vector2.Distance(pos, rayCastStart));

            var collided = RayTraceTilesForTerrainReturnFirstSolid(self.room, self.room.GetTilePosition(rayCastStart), self.room.GetTilePosition(pos));

            if (collided != null)
            {
                pos = rayCastStart + rayCastDir * Vector2.Distance(rayCastStart, self.room.MiddleOfTile(collided.Value));
                maxReachDistance = Mathf.Min(maxReachDistance, Vector2.Distance(pos, rayCastStart));
            }

            SharedPhysics.CollisionResult result = UltraCoinsBuffEntry.TraceProjectileAgainstBodyChunks(self, self.room, rayCastStart, ref pos, self.firstChunk.rad + ((self.thrownBy != null && self.thrownBy is Player) ? 12f : 0f), 1, self.thrownBy, true);

            if (self.meleeHitChunk != null)
            {
                result.obj = self.meleeHitChunk.owner;
                result.chunk = self.meleeHitChunk;
                result.hitSomething = true;
                result.collisionPoint = self.meleeHitChunk.pos + Custom.DirVec(self.meleeHitChunk.pos, rayCastStart) * self.meleeHitChunk.rad;
            }
            if (result.hitSomething)
            {
                maxReachDistance = Mathf.Min(maxReachDistance, Vector2.Distance(result.collisionPoint, rayCastStart));
            }

            float accumulateMass = 0f;
            bool accumulateToFreeMode = false;

            List<BodyChunk> hitableChunks = new List<BodyChunk>();

            for (int i = 0; i < self.room.physicalObjects[0].Count; i++)
            {
                if (self.room.physicalObjects[0][i] != self && self.room.physicalObjects[0][i].canBeHitByWeapons)
                {
                    bool isFromThrower = false;
                    int j = 0;
                    while (j < self.room.physicalObjects[0][i].grabbedBy.Count && !isFromThrower)
                    {
                        isFromThrower = self.room.physicalObjects[0][i].grabbedBy[j].grabber == self.thrownBy;
                        j++;
                    }

                    if (!isFromThrower)
                    {
                        for (int k = 0; k < self.room.physicalObjects[0][i].bodyChunks.Length; k++)
                        {
                            BodyChunk bodyChunk = self.room.physicalObjects[0][i].bodyChunks[k];
                            float collideF = Custom.CirclesCollisionTime(rayCastStart.x, rayCastStart.y, bodyChunk.pos.x, bodyChunk.pos.y, pos.x - rayCastStart.x, pos.y - self.firstChunk.pos.y, self.firstChunk.rad + ((self.thrownBy != null && self.thrownBy is Player) ? 12f : 0f), bodyChunk.rad + (self.room.physicalObjects[0][i] is UltraCoin ? 15f : 0f));
                            if (collideF > 0f && collideF < 1f && Vector2.Distance(rayCastStart, bodyChunk.pos) < maxReachDistance)
                            {
                                hitableChunks.Add(bodyChunk);
                            }
                        }
                    }
                }
            }
            hitableChunks.Sort((a, b) => (Vector2.Distance(a.pos, rayCastStart).CompareTo(Vector2.Distance(b.pos, rayCastStart))));

            foreach(var chunk in hitableChunks)
            {
                if (chunk.owner is Weapon weapon && weapon.mode == Weapon.Mode.Thrown && weapon.HeavyWeapon)
                {
                    result.hitSomething = false;
                    result.obj = null;
                    result.chunk = null;

                    pos = chunk.pos + Custom.DirVec(chunk.pos, rayCastStart) * chunk.rad;

                    self.HitAnotherThrownWeapon(chunk.owner as Weapon);
                    accumulateMass = float.MaxValue;
                    break;
                }
                else
                {
                    chunk.vel += self.firstChunk.vel;
                    self.HitSomethingWithoutStopping(chunk.owner, chunk, null);
                    accumulateMass += chunk.mass;

                    if (chunk.owner is UltraCoin coin && !coin.Deflected)//金币反射
                    {
                        rayCastResult.deflect = true;
                        rayCastResult.endPos = coin.firstChunk.pos;

                        if (!coinDeflectCount.TryGetValue(self, out var deflectCountKeeper))
                        {
                            deflectCountKeeper = new CoinDeflectCountKeeper();
                            coinDeflectCount.Add(self, deflectCountKeeper);
                        }

                        coin.SpearDeflectOnThis(deflectCountKeeper.deflectCount);
                        deflectCountKeeper.deflectCount++;

                        BodyChunk nextTarget = CoinDeflectSelectTarget(self, rayCastResult.endPos, coin);
                        //BuffUtils.Log("UltraCoins", $"Hit coin next target : {nextTarget?.owner}");

                        if (nextTarget != null)
                        {
                            rayCastResult.deflectDir = (nextTarget.pos - rayCastResult.endPos).normalized;
                            deflectCountKeeper.nextDeflectTarget = new WeakReference<BodyChunk>(nextTarget);
                        }
                        else
                        {
                            rayCastResult.deflectDir = Custom.RNV();
                            deflectCountKeeper.nextDeflectTarget = null;
                        }

                        if (DivisibleSpearBuff.Instance != null)//分裂矛兼容
                        {
                            float damage = self.spearDamageBonus;
                            if (self.bugSpear)
                            {
                                damage *= 3f;
                            }
                            SplitShot(self, rayCastResult.endPos, damage, nextTarget);
                        }
                        richshotCallBack?.Invoke();

                        return rayCastResult;
                    }        
                }

                if (accumulateMass <= 0.6f && chunk.owner.appendages != null)
                {
                    for (int num11 = 0; num11 < chunk.owner.appendages.Count; num11++)
                    {
                        if (chunk.owner.appendages[num11].canBeHit && chunk.owner.appendages[num11].LineCross(rayCastStart, pos))
                        {
                            (chunk.owner as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(new PhysicalObject.Appendage.Pos(chunk.owner.appendages[num11], 0, 0.5f), self.firstChunk.vel * self.firstChunk.mass);
                            self.HitSomethingWithoutStopping(chunk.owner, null, chunk.owner.appendages[num11]);
                        }
                    }
                }
                else if (accumulateMass > 0.6f)
                {
                    accumulateToFreeMode = true;
                    pos = chunk.pos;
                    self.room.PlaySound(SoundID.Spear_Hit_Small_Creature, chunk);
                    break;
                }
            }


            Vector2 startPos = rayCastStart;
            if (result.hitSomething && !accumulateToFreeMode)
            {
                bool hitSomething = self.HitSomething(result, eu);

                rayCastResult.hitSth = true;
                rayCastResult.endPos = result.collisionPoint;

                if (result.obj != null)
                {
                    if (self.thrownBy != null && result.obj is Creature)
                    {
                        self.room.socialEventRecognizer.WeaponAttack(self, self.thrownBy, result.obj as Creature, hitSomething);
                    }
                    result.obj.HitByWeapon(self);
                    if (!hitSomething && self is ExplosiveSpear)
                    {
                        (self as ExplosiveSpear).Explode();
                    }
                    self.thrownBy = null;
                }
            }
            else
            {
                rayCastResult.endPos = pos;
                if (collided != null && !accumulateToFreeMode)
                {
                    rayCastResult.hitWall = true;
                    self.HitWall();
                }
                else if (accumulateToFreeMode)
                {
                    self.ChangeMode(Weapon.Mode.Free);
                    rayCastResult.hitAir = true;
                    self.firstChunk.vel *= 0.5f;

                }
                else
                {
                    rayCastResult.hitAir = true;
                    self.ChangeMode(Weapon.Mode.Free);
                    self.SetRandomSpin();
                }
            }
            return rayCastResult;
        }

        static (bool, RayCastResult) CoinOnlyRayCastScan(Spear self, Vector2 rayCastStart, Vector2 rayCastDir)
        {
            RayCastResult rayCastResult = new RayCastResult();
            float damage = self.spearDamageBonus;
            if (self.bugSpear)
            {
                damage *= 3f;
            }

            if (!coinDeflectCount.TryGetValue(self, out var deflectCountKeeper))
            {
                deflectCountKeeper = new CoinDeflectCountKeeper();
                coinDeflectCount.Add(self, deflectCountKeeper);
            }

            float maxReachDistance = float.MaxValue;
            Vector2 pos = Custom.RectCollision(rayCastStart, rayCastStart + rayCastDir * 10000f, self.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            maxReachDistance = Mathf.Min(maxReachDistance, Vector2.Distance(pos, rayCastStart));

            var collided = RayTraceTilesForTerrainReturnFirstSolid(self.room, self.room.GetTilePosition(rayCastStart), self.room.GetTilePosition(pos));

            if (collided != null)
            {
                pos = rayCastStart + rayCastDir * Vector2.Distance(rayCastStart, self.room.MiddleOfTile(collided.Value));
                maxReachDistance = Mathf.Min(maxReachDistance, Vector2.Distance(pos, rayCastStart));
                rayCastResult.hitWall = true;
            }
            else
                rayCastResult.hitAir = true;

            //BuffUtils.Log("UltraCoins", $"max reach distance {maxReachDistance}");

            UltraCoin hittedCoin = null;
            float dist = float.MaxValue;
            for (int i = 0; i < self.room.physicalObjects.Length; i++)//检测是否能打中金币
            {
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                {
                    if (!(self.room.physicalObjects[i][j] is UltraCoin coin) || coin.Deflected || coin.notDeflectCounter > 0)
                        continue;

                    float collideF = Custom.CirclesCollisionTime(rayCastStart.x - rayCastDir.x * 20f, rayCastStart.y - rayCastDir.y * 20f, coin.firstChunk.pos.x, coin.firstChunk.pos.y, pos.x - rayCastStart.x, pos.y - self.firstChunk.pos.y, self.firstChunk.rad + ((self.thrownBy != null && self.thrownBy is Player) ? 12f : 0f), coin.firstChunk.rad * 5f);

                    BuffUtils.Log("UltraCoins", $"detect coin hit: {coin} {coin.abstractPhysicalObject.ID}, {collideF}");

                    if (collideF > 0f && collideF < 1f)
                    {
                        float coinDist = Vector2.Distance(rayCastStart, coin.firstChunk.pos);
                        BuffUtils.Log("UltraCoins", $"coinDist {coinDist}, dist {dist}");
                        if (coinDist >= maxReachDistance)
                            continue;

                        if (coinDist < dist)
                        {
                            hittedCoin = coin;
                            dist = coinDist;
                        }
                    }
                }
            }

            if (hittedCoin == null)
            {
                if (deflectCountKeeper.deflectCount > 0)//反射过一次后即使未能命中硬币也继续飞行直到撞墙
                {
                    if (rayCastResult.hitWall)
                        self.HitWall();
                    else
                    {
                        self.ChangeMode(Weapon.Mode.Free);
                        self.SetRandomSpin();
                    }
                    rayCastResult.endPos = pos;
                    rayCastResult.deflectDir = rayCastDir;
                }
                ApplyDamage(self, rayCastStart, pos, damage);
                return (deflectCountKeeper.deflectCount > 0, rayCastResult);
            }
            
            BuffUtils.Log("UltraCoins", $"hit coin {hittedCoin} {hittedCoin.abstractPhysicalObject.ID}");

            richshotCallBack?.Invoke();

            pos = hittedCoin.firstChunk.pos;
            BodyChunk nextTarget = CoinDeflectSelectTarget(self, pos, hittedCoin);
            BuffUtils.Log("UltraCoins", $"Hit coin next target : {nextTarget?.owner}");

            if (DivisibleSpearBuff.Instance != null)
            {
                SplitShot(self, pos, damage, nextTarget);
            }

            rayCastResult.endPos = pos;
            rayCastResult.deflect = true;

            if (nextTarget != null)
            {
                rayCastResult.hitWall = false;
                rayCastResult.hitAir = false;
                rayCastResult.deflectDir = (nextTarget.pos - pos).normalized;
                deflectCountKeeper.nextDeflectTarget = new WeakReference<BodyChunk>(nextTarget);
            }
            else
            {
                rayCastResult.deflectDir = Custom.RNV();
            }


            hittedCoin.SpearDeflectOnThis(deflectCountKeeper.deflectCount);
            deflectCountKeeper.deflectCount++;


            ApplyDamage(self, rayCastStart, pos, damage);

            return (true, rayCastResult);
        }

        static BodyChunk CoinDeflectSelectTarget(Spear self, Vector2 rayCastStart, UltraCoin deflectingCoin, bool ignoreCoins = false, params PhysicalObject[] ignoreTargets)
        {
            float dist = float.MinValue;
            BodyChunk nextTarget = null;
            var room = self.room;

            if (!ignoreCoins)
            {
                foreach (var obj in room.physicalObjects[0])//选择反射到的目标
                {
                    if (obj.room == null)
                        continue;
                    if (!(obj is UltraCoin nextCoin) || nextCoin == deflectingCoin || nextCoin.Deflected)
                        continue;
                    if (!room.VisualContact(rayCastStart, obj.firstChunk.pos))
                    {
                        continue;
                    }
                    if (nextCoin.notDeflectCounter > 0)
                        continue;

                    var thisDist = Vector2.Distance(rayCastStart, obj.firstChunk.pos);
                    //BuffUtils.Log("UltraCoins", $"test coin {obj} {obj.abstractPhysicalObject.ID.number}, thisDist {thisDist}, dist {dist}");
                    if (thisDist > dist)
                    {
                        nextTarget = obj.firstChunk;
                        dist = thisDist;
                    }
                }
            }

            if (nextTarget == null)
            {
                dist = float.MaxValue;
                foreach (var obj in room.physicalObjects[0])
                {
                    if (obj.room == null || ignoreTargets.Contains(obj))
                        continue;
                    if (!(obj is Weapon weapon) || obj == self)
                        continue;
                    if (weapon.mode != Weapon.Mode.Thrown || weapon.thrownBy == self.thrownBy)
                        continue;
                    if (!self.room.VisualContact(rayCastStart, obj.firstChunk.pos))
                        continue;

                    var thisDist = Vector2.Distance(rayCastStart, obj.firstChunk.pos);
                    if (thisDist < dist)
                    {
                        nextTarget = obj.firstChunk;
                        dist = thisDist;
                    }
                }
            }
            //else
            //    BuffUtils.Log("UltraCoins", $"next coin {nextTarget.owner} {nextTarget.owner.abstractPhysicalObject.ID.number}");

            if (nextTarget == null)
            {
                for (int m = 0; m < self.room.physicalObjects.Length; m++)
                {
                    foreach (var obj in self.room.physicalObjects[m])
                    {
                        if (obj.room == null || ignoreTargets.Contains(obj))
                            continue;
                        if (!(obj is Creature creature))
                            continue;
                        if (creature == self.thrownBy || creature is Player || creature.dead)
                            continue;
                        if (creature.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.GarbageWorm)
                            continue;


                        foreach (var tryChunk in creature.bodyChunks)
                        {
                            if (creature.abstractCreature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.LizardTemplate)
                            {
                                if (tryChunk == creature.firstChunk)
                                    continue;
                            }
                            if (!self.room.VisualContact(rayCastStart, tryChunk.pos))
                                continue;

                            var thisDist = Vector2.Distance(rayCastStart, tryChunk.pos);
                            if (thisDist < dist)
                            {
                                nextTarget = tryChunk;
                                dist = thisDist;
                            }
                        }
                    }
                }
            }

            return nextTarget;
        }

        static void ApplyDamage(Spear self, Vector2 start, Vector2 end, float damage)
        {
            for (int i = 0; i < self.room.physicalObjects.Length; i++)//检测可能造成伤害的生物
            {
                for (int j = 0; j < self.room.physicalObjects[i].Count; j++)
                {
                    var obj = self.room.physicalObjects[i][j];

                    if (!(obj is Creature) && i != 0)//非生物仅检测第一层碰撞
                        continue;

                    if (!obj.canBeHitByWeapons)
                        continue;

                    for (int k = 0; k < obj.bodyChunks.Length; k++)
                    {
                        var bodyChunk = obj.bodyChunks[k];

                        float collideF = Custom.CirclesCollisionTime(start.x, start.y, bodyChunk.pos.x, bodyChunk.pos.y, end.x - start.x, end.y - start.y, self.firstChunk.rad + ((self.thrownBy != null && self.thrownBy is Player) ? 12f : 0f), bodyChunk.rad);
                        if (collideF > 0f && collideF < 1f)
                        {
                            if(obj is Weapon weapon && weapon != self && weapon.thrownBy != self.thrownBy && weapon.mode == Weapon.Mode.Thrown && weapon.HeavyWeapon)
                            {
                                weapon.WeaponDeflect(weapon.firstChunk.pos, (end - start).normalized, weapon.firstChunk.vel.magnitude);
                            }
                            else if(obj is ScavengerBomb bomb)
                            {
                                bomb.Explode(null);
                            }
                            else if((self.room.physicalObjects[i][j] is Creature creature) && creature != self.thrownBy && !(creature is Player))
                            {
                                creature.SetKillTag(self.thrownBy?.abstractCreature);
                                if (creature.abstractCreature.creatureTemplate.smallCreature)
                                {
                                    creature.Die();
                                    if(self is ElectricSpear eSpear)
                                    {
                                        eSpear.sparkPoint = bodyChunk.pos;
                                        eSpear.Spark();
                                    }
                                }
                                else
                                {
                                    creature.Violence(self.firstChunk, self.firstChunk.vel, bodyChunk, null, Creature.DamageType.Stab, damage, 20f);
                                    if(self is ElectricSpear eSpear)
                                    {
                                        eSpear.sparkPoint = bodyChunk.pos;
                                        eSpear.Zap();
                                    }
                                    else if (self is ExplosiveSpear eExplosive)
                                    {
                                        if (!eExplosive.exploded)
                                        {
                                            Vector2 posKeep = eExplosive.firstChunk.pos;
                                            eExplosive.firstChunk.pos = bodyChunk.pos;

                                            eExplosive.stuckInObject = obj;
                                            eExplosive.stuckInChunkIndex = k;
                                            eExplosive.Explode();
                                            eExplosive.exploded = false;
                                            eExplosive.slatedForDeletetion = false;

                                            eExplosive.firstChunk.pos = posKeep;
                                        }

                                    }
                                }

                                penetrateCallBack?.Invoke();
                            }
                            else if (!(obj is UltraCoin))
                            {
                                obj.HitByWeapon(self);
                            }


                            CreateSparkleEmitter(self.room, bodyChunk.pos, (end - start).normalized * Mathf.Clamp(damage * 30f, 40f, 400f));
                        }
                    }

                    if ((obj is Creature creature1) && creature1 != self.thrownBy && !(creature1 is Player) && creature1.appendages != null)
                    {
                        for (int appI = 0; appI < creature1.appendages.Count; appI++)
                        {
                            if (creature1.appendages[appI].canBeHit && creature1.appendages[appI].LineCross(start, end))
                            {
                                (creature1.appendages[appI].owner as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(new PhysicalObject.Appendage.Pos(creature1.appendages[appI], 0, 0.5f), self.firstChunk.vel * self.firstChunk.mass);
                                //self.HitSomethingWithoutStopping(creature1, null, creature1.appendages[appI]);
                            }
                        }
                    }
                }
            }
        }

        static void SplitShot(Spear self, Vector2 pos, float damage, BodyChunk selectedTarget)
        {
            var splitTarget = CoinDeflectSelectTarget(self, pos, null, true, selectedTarget?.owner);
            var splitShotDir = splitTarget != null ? (splitTarget.pos - pos).normalized : Custom.RNV();

            float splitShotMaxReachDistance = float.MaxValue;
            Vector2 splitShotEndPos = Custom.RectCollision(pos, pos + splitShotDir * 10000f, self.room.RoomRect.Grow(200f)).GetCorner(FloatRect.CornerLabel.D);
            splitShotMaxReachDistance = Mathf.Min(splitShotMaxReachDistance, Vector2.Distance(pos, splitShotEndPos));

            var splitShotCollided = RayTraceTilesForTerrainReturnFirstSolid(self.room, self.room.GetTilePosition(pos), self.room.GetTilePosition(splitShotEndPos));

            if (splitShotCollided != null)
            {
                splitShotEndPos = pos + splitShotDir * Vector2.Distance(pos, self.room.MiddleOfTile(splitShotCollided.Value));
            }

            ApplyDamage(self, pos, splitShotEndPos, damage);
            self.room.AddObject(new SpeedTailEffect(self.room, pos, splitShotEndPos, UltraCoin.gold, 120, 5f));
        }

        static void CreateSparkleEmitter(Room room, Vector2 pos, Vector2 movement)
        {
            var emitter = new ParticleEmitter(room);
            emitter.pos = emitter.lastPos = pos;

            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 5, false));
            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 5));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam(ultraCoinsVFX0, "StormIsApproaching.AdditiveDefault", alpha: 0.5f, scale : 0.05f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "StormIsApproaching.AdditiveDefault", constCol: UltraCoin.gold, scale: 0.5f)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, 40, 50));
            emitter.ApplyParticleModule(new SetConstColor(emitter, UltraCoin.gold));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 2f, 2.5f));
            emitter.ApplyParticleModule(new SetAlpha(emitter, 1f));
            emitter.ApplyParticleModule(new SetRandomPos(emitter, 0f));

            emitter.ApplyParticleModule(new PositionOverLife(emitter,
                (p, l) =>
                {
                    Vector2 dir = Custom.DegToVec(p.randomParam1 * 360f);
                    float radParam = p.randomParam2;
                    return (dir * StagnantForcefieldBuff.rad * radParam + movement) * p.randomParam3 * Mathf.Min(1f, Helper.LerpEase(l)) * 0.5f + p.emitter.pos;
                }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter,
                (p, l) =>
                {
                    return 1f - Helper.LerpEase(l);
                }));
            //emitter.ApplyParticleModule(new StagnantForceFieldBlink(emitter));

            ParticleSystem.ApplyEmitterAndInit(emitter);
        }

        public struct RayCastResult
        {
            public bool hitWall, hitAir, hitSth, deflect;
            public Vector2 endPos;
            public Vector2 deflectDir;
        }

        public class CoinDeflectCountKeeper
        {
            public int deflectCount;
            public WeakReference<BodyChunk> nextDeflectTarget;
        }
    }

    public class SpeedTailEffect : CosmeticSprite
    {
        Vector2 start, end, setEnd;
        Color color;
        int initLife, life, lastLife;
        float width;

        public SpeedTailEffect(Room room, Vector2 start, Vector2 end, Color effectCol, int life, float width)
        {
            this.room = room;
            this.start = start;
            setEnd = this.end = end;
            this.width = width / 2f;
            color = effectCol;
            initLife = this.life = lastLife = life;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new CustomFSprite("pixel")
            {
                shader = Custom.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
            };
            AddToContainer(sLeaser, rCam, rCam.ReturnFContainer("Water"));
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (slatedForDeletetion)
                return;

            lastLife = life;
            if (life > 0)
                life--;
            if (lastLife == 0)
                Destroy();
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            if (slatedForDeletetion)
                return;

            Vector2 dir = (end - start).normalized;
            Vector2 perp = Custom.PerpendicularVector(dir);

            end = Vector2.Lerp(end, setEnd, 0.5f);

            float f = Mathf.Lerp(lastLife, life, timeStacker) / initLife;
            var customSprite = sLeaser.sprites[0] as CustomFSprite;

            customSprite.MoveVertice(0, -perp * f * width + end - camPos);
            customSprite.MoveVertice(1, perp * f * width + end - camPos);
            customSprite.MoveVertice(2, perp * f * width * 0.5f + start - camPos);
            customSprite.MoveVertice(3, -perp * f * width * 0.5f + start - camPos);

            Color a = Color.Lerp(Color.black, color, f);
            Color b = Color.Lerp(Color.black, color, f * 0.5f);

            customSprite.verticeColors[0] = customSprite.verticeColors[1] = a;
            customSprite.verticeColors[2] = customSprite.verticeColors[3] = b;
        }
    }

    [BuffAbstractPhysicalObject]
    public class AbstractUltraCoin : AbstractPhysicalObject
    {
        public static AbstractObjectType flameThrowerType = new AbstractObjectType("UltraCoin", true);

        public AbstractUltraCoin(World world, AbstractObjectType type, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : base(world, type, realizedObject, pos, ID)
        {
        }

        public AbstractUltraCoin(World world, PhysicalObject realizedObject, WorldCoordinate pos, EntityID ID) : this(world, flameThrowerType, realizedObject, pos, ID)
        {
        }

        public override void Realize()
        {
            base.Realize();
            realizedObject = new UltraCoin(this, world);
        }
    }
    public class UltraCoin : PhysicalObject, IDrawable
    {
        public static Color gold = Custom.hexToColor("FFAE00");
        static Color darkGold = Custom.hexToColor("5F3317");

        float rotation;

        public int notDeflectCounter = 10;
        int life = 240;
        float flash, lastFlash, extraFlash;
        int lastContact;

        public bool Deflected { get; private set; }

        public UltraCoin(AbstractPhysicalObject abstractPhysicalObject, World world) : base(abstractPhysicalObject)
        {
            bodyChunks = new BodyChunk[1];
            bodyChunks[0] = new BodyChunk(this, 0, new Vector2(0f, 0f), 3.5f, 0.1f);
            bodyChunkConnections = new PhysicalObject.BodyChunkConnection[0];
            canBeHitByWeapons = true;
            airFriction = 0.99f;
            gravity = 0.45f;
            bounce = 0.95f;
            surfaceFriction = 0.4f;
            collisionLayer = 0;
            waterFriction = 0.98f;
            buoyancy = 0.4f;
            BuffUtils.Log("UltraCoins", $"Init Coin {abstractPhysicalObject.ID.number}");
        }

        public void SpearDeflectOnThis(int deflectCount)
        {
            Deflected = true;
            extraFlash = 3f;
            room.PlaySound(SoundID.Spear_Bounce_Off_Creauture_Shell, firstChunk.pos, 1f, 2f + deflectCount * 0.2f);
            room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, firstChunk.pos, 3f, 1.5f + deflectCount * 0.2f);
            room.AddObject(new ShockWave(firstChunk.pos, deflectCount * 40f + 80f, 0.02f, 3, false));
            BuffUtils.Instance.StartCoroutine(PauseGameCoroutine(deflectCount));
        }

        IEnumerator PauseGameCoroutine(int deflectCount)
        {
            UltraCoinsBuffEntry.skipGameUpdate = true;
            yield return new WaitForSeconds(0.05f + 0.05f * deflectCount);
            UltraCoinsBuffEntry.skipGameUpdate = false;
            yield break;
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            //if (firstChunk.ContactPoint.y == -1)
            //    Destroy();
            if (life > 0)
            {
                life--;
                if (firstChunk.contactPoint.y == -1)
                {
                    life--;
                    if (lastContact != -1 && !Deflected)
                    {
                        flash = 0.5f;
                    }
                }
                lastContact = firstChunk.contactPoint.y;

                if (Deflected && life > 40)
                {
                    life = 40;
                }

                if (life <= 0)
                    Destroy();
            }

            if (notDeflectCounter > 0)
                notDeflectCounter--;
            if (notDeflectCounter == 0)
            {
                notDeflectCounter--;
                flash = lastFlash = 1f;
            }

            lastFlash = flash;
            flash = Mathf.Lerp(flash, 0f, 0.075f);
            extraFlash = Mathf.Lerp(extraFlash, 0f, 0.5f);
            rotation += 10f;
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[2];
            sLeaser.sprites[0] = new FSprite("Circle20", true)
            {
                color = gold,
                scaleX = 0.2f,
                scaleY = 0.4f
            };
            sLeaser.sprites[1] = new FSprite(UltraCoinsBuffEntry.ultraCoinsVFX0, true)
            {
                color = gold,
                shader = rCam.game.rainWorld.Shaders["StormIsApproaching.AdditiveDefault"],
                scale = 0f
            };
            AddToContainer(sLeaser, rCam, null);
        }
        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            rCam.ReturnFContainer("Midground").AddChild(sLeaser.sprites[0]);
            rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[1]);
        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (!sLeaser.deleteMeNextFrame && (base.slatedForDeletetion || room != rCam.room))
            {
                sLeaser.CleanSpritesAndRemove();
            }

            Vector2 pos = Vector2.Lerp(firstChunk.lastPos, firstChunk.pos, timeStacker) - camPos;
            float f = Mathf.Sin(rotation % 360f / 360f * Mathf.PI);
            sLeaser.sprites[0].rotation = rotation;
            sLeaser.sprites[0].color = Deflected ? darkGold : Color.Lerp(gold, Color.white, Mathf.Pow(f, 3f));
            sLeaser.sprites[0].SetPosition(pos);
            sLeaser.sprites[1].SetPosition(pos + new Vector2(-2f, 2f));

            sLeaser.sprites[1].scale = Mathf.Lerp(0f, 0.5f + extraFlash * 0.3f, Mathf.Lerp(lastFlash, flash, timeStacker) + extraFlash) * 2f;
        }

        public override string ToString()
        {
            return base.ToString() + $"_{abstractPhysicalObject.ID.number}";
        }
    }
}
