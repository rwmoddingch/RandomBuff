using RandomBuff;
using UnityEngine;
using RWCustom;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;

namespace BuiltinBuffs.Positive
{
    internal class EjectionRockBuff : Buff<EjectionRockBuff, EjectionRockBuffData>
    {
        public override BuffID ID => EjectionRockIBuffEntry.ejectionRockBuffID;
        public ConditionalWeakTable<Rock, EjectionRockModule> modules = new ConditionalWeakTable<Rock, EjectionRockModule>();
    }

    internal class EjectionRockBuffData : BuffData
    {
        public override BuffID ID => EjectionRockIBuffEntry.ejectionRockBuffID;
    }

    internal class EjectionRockIBuffEntry : IBuffEntry
    {
        public static BuffID ejectionRockBuffID = new BuffID("EjectionRock", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<EjectionRockBuff, EjectionRockBuffData, EjectionRockIBuffEntry>(ejectionRockBuffID);
        }

        public static void HookOn()
        {
            On.Weapon.HitWall += Weapon_HitWall;
            On.Weapon.Update += Weapon_Update;
            On.Weapon.ChangeMode += Weapon_ChangeMode;
            On.Weapon.WeaponDeflect += Weapon_WeaponDeflect;
            On.Rock.DrawSprites += Rock_DrawSprites;
            On.Rock.HitSomething += Rock_HitSomething;
            //On.Player.Update += Player_Update;
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (Input.GetKey(KeyCode.C))
            {
                Vector2 dir = Custom.RNV();
                AbstractPhysicalObject abstractPhysicalObject = new AbstractPhysicalObject(self.room.world, AbstractPhysicalObject.AbstractObjectType.Rock, null, self.room.GetWorldCoordinate(self.firstChunk.pos + dir * 80f), self.room.game.GetNewID());
                abstractPhysicalObject.RealizeInRoom();
                Rock rock = abstractPhysicalObject.realizedObject as Rock;

                rock.firstChunk.pos += dir * 40f;

                rock.thrownPos = rock.firstChunk.pos;
                rock.thrownBy = null;
                IntVector2 throwDir = new IntVector2(0, 0);

                if (dir.x > 0)
                    throwDir.x = 1;
                else if (dir.x < 0)
                    throwDir.x = -1;

                if (dir.y > 0)
                    throwDir.y = 1;
                else if (dir.y < 0)
                    throwDir.y = -1;
                rock.throwDir = throwDir;

                rock.firstFrameTraceFromPos = rock.thrownPos;
                rock.changeDirCounter = 3;
                rock.ChangeOverlap(true);
                rock.firstChunk.MoveFromOutsideMyUpdate(false, rock.thrownPos);

                float vel = 10f;

                rock.firstChunk.vel = vel * dir;
                rock.firstChunk.pos = dir * 40f + self.firstChunk.pos;
                rock.firstChunk.lastPos = rock.firstChunk.pos;
                rock.tailPos = rock.firstChunk.pos;
                rock.setRotation = dir;


                if (BuffCore.TryGetBuff(EjectionRockIBuffEntry.ejectionRockBuffID, out var _))
                {
                    rock.ChangeMode(Weapon.Mode.Thrown); rock.doNotTumbleAtLowSpeed = true;
                    rock.rotationSpeed = 0f;
                    rock.meleeHitChunk = null;
                    rock.overrideExitThrownSpeed = 0f;
                }
            }
        }

        private static bool Rock_HitSomething(On.Rock.orig_HitSomething orig, Rock self, SharedPhysics.CollisionResult result, bool eu)
        {
            if(result.obj != null && EjectionRockBuff.Instance.modules.TryGetValue(self, out var module))
            {
                if(result.obj is Creature creature)
                {
                    if(result.obj is Player)
                    {
                        creature.Stun((int)(10 * module.frc));
                    }
                    else
                    {
                        creature.Stun((int)(45 * module.frc));
                    }
                }
                if(result.chunk != null)
                {
                    float masFac = (result.chunk.owner is Player) ? 0.1f : 2f * result.chunk.mass;
                    result.chunk.vel += self.firstChunk.vel * module.frc * self.firstChunk.mass * masFac / result.chunk.mass;
                }
                if (result.onAppendagePos != null)
                {
                    (result.obj as PhysicalObject.IHaveAppendages).ApplyForceOnAppendage(result.onAppendagePos, self.firstChunk.vel * module.frc);
                }
            }
            return orig.Invoke(self, result, eu);
        }

        private static void Weapon_WeaponDeflect(On.Weapon.orig_WeaponDeflect orig, Weapon self, Vector2 inbetweenPos, Vector2 deflectDir, float bounceSpeed)
        {
            if (self is Rock rock && self.firstChunk.vel.magnitude > 40f)
            {
                RandomBounce(rock, false, -rock.firstChunk.vel.normalized);
                return;
            }
            orig.Invoke(self, inbetweenPos, deflectDir, bounceSpeed);
        }

        private static void Rock_DrawSprites(On.Rock.orig_DrawSprites orig, Rock self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (sLeaser.sprites[1].color != self.color)
                sLeaser.sprites[1].color = self.color;
        }

        private static void Weapon_ChangeMode(On.Weapon.orig_ChangeMode orig, Weapon self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);
            if (!(self is Rock rock))
                return;
            if(newMode == Weapon.Mode.Free && EjectionRockBuff.Instance.modules.TryGetValue(rock, out var module))
            {
                module.StopBounce(rock);
                EjectionRockBuff.Instance.modules.Remove(rock);
            }
        }

        private static void Weapon_Update(On.Weapon.orig_Update orig, Weapon self, bool eu)
        {
            if (self is Rock && self.mode == Weapon.Mode.Thrown)
            {
                IntVector2 pos = self.room.GetTilePosition(self.firstChunk.pos);
                if (pos.x < 0 || pos.x > self.room.Width || pos.y < 0 || pos.y > self.room.Height)
                {
                    Vector2 normOverride = Vector2.zero;
                    if (pos.x < 0)
                        normOverride.x = 1f;
                    if (pos.x > self.room.Width)
                        normOverride.x = -1f;
                    if (pos.y < 0)
                        normOverride.y = 1f;
                    if (pos.y > self.room.Height)
                        normOverride.y = -1f;
                    RandomBounce(self as Rock, eu, normOverride.normalized);
                }

                if (self.firstChunk.ContactPoint.x != 0 || self.firstChunk.contactPoint.y != 0)
                {
                    RandomBounce(self as Rock, eu);
                }
            }
            orig.Invoke(self, eu);
        }

        private static void Weapon_HitWall(On.Weapon.orig_HitWall orig, Weapon self)
        {
            if(!(self is Rock))
            {
                orig.Invoke(self);
                return;
            }
            RandomBounce(self as Rock, false);
            BuffUtils.Log(ejectionRockBuffID,"Rock hit wall");
        }

        static bool RandomBounce(Rock self, bool eu, Vector2? normOverride = null)
        {
            //EmgTxCustom.Log(self.firstChunk.contactPoint.ToVector2());
            Vector2 norm = normOverride ?? -(self.firstChunk.contactPoint.ToVector2().normalized);

            if (self.room == null)
                return false;
            if (!self.room.BeingViewed)
                return false;


            if(!EjectionRockBuff.Instance.modules.TryGetValue(self, out var module))
            {
                module = new EjectionRockModule(self);
                EjectionRockBuff.Instance.modules.Add(self, module);
            }
            module.BounceOnce(self);

            
            Vector2 dir = (norm + Custom.RNV() * 0.5f).normalized;

            self.firstChunk.pos += dir * 10f;

            self.thrownPos = self.firstChunk.pos;
            self.thrownBy = null;
            IntVector2 throwDir = new IntVector2(0, 0);

            if (dir.x > 0)
                throwDir.x = 1;
            else if(dir.x < 0)
                throwDir.x = -1;

            if (dir.y > 0)
                throwDir.y = 1;
            else if(dir.y < 0)
                throwDir.y = -1;
            self.throwDir = throwDir;

            self.firstFrameTraceFromPos = self.thrownPos;
            self.changeDirCounter = 3;
            self.ChangeOverlap(true);
            self.firstChunk.MoveFromOutsideMyUpdate(eu, self.thrownPos);

            self.ChangeMode(Weapon.Mode.Thrown);

            float vel = 40f * module.frc;
            self.overrideExitThrownSpeed = 0f;
            
            //屏幕移动会导致黑屏，有点莫名其妙的问题
            //if(Random.value < 0.1f)
            //{
            //    Vector2 screenMovementPos = self.firstChunk.pos;
            //    screenMovementPos.x = Mathf.Clamp(screenMovementPos.x, 0f, self.room.PixelWidth);
            //    screenMovementPos.y = Mathf.Clamp(screenMovementPos.y, 0f, self.room.PixelHeight);

            //    self.room.ScreenMovement(new Vector2?(screenMovementPos), -dir * 1.5f, 0f);
            //}
  
            self.room.PlaySound(SoundID.Rock_Hit_Wall, self.firstChunk.pos);

            self.firstChunk.vel = vel * dir;
            self.firstChunk.pos += dir;
            self.setRotation = dir;
            self.doNotTumbleAtLowSpeed = true;
            self.rotationSpeed = 0f;
            self.meleeHitChunk = null;

            if (self.room.BeingViewed)
            {
                for (int i = 0; i < 7; i++)
                {
                    self.room.AddObject(new Spark(self.firstChunk.pos + dir * (self.firstChunk.rad - 1f), (Custom.DegToVec(Random.value * 360f) * 10f * Random.value + -dir * 10f) * module.frc, new Color(1f, 1f, 1f), null, 2, 4));
                }
            }
            return true;
        }
    }

    internal class EjectionRockModule
    {
        static Color TrailColor = new Color(0.43f, 0.8f, 1f);

        public float frc;
        public Color origColor;
        public Color finalColor;

        public EjectionRockModule(Rock rock)
        {
            origColor = rock.color;
            frc = 1f;
        }

        public void BounceOnce(Rock rock)
        {
            frc = Mathf.Clamp(frc + 0.05f, 1f, 3f);
            finalColor = Color.Lerp(origColor, TrailColor, Mathf.InverseLerp(1f, 3f, frc));
            rock.color = finalColor;
        }

        public void StopBounce(Rock rock)
        {
            rock.color = origColor;
            rock.exitThrownModeSpeed = 30f;
            if (rock.room != null)
            {
                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 280f, 1f, 7, finalColor));
                rock.room.AddObject(new Explosion.ExplosionLight(rock.firstChunk.pos, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                rock.room.AddObject(new ExplosionSpikes(rock.room, rock.firstChunk.pos, 14, 30f, 9f, 7f, 170f, finalColor));
                rock.room.AddObject(new ShockWave(rock.firstChunk.pos, 330f, 0.045f, 5, false));
            }
        }
    }
}
