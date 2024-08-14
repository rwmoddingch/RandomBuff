using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RWCustom;
using UnityEngine;

namespace BuiltinBuffs.Negative
{
    internal class RocketBoostTusksEntry : IBuffEntry
    {
        public static readonly BuffID RocketBoostTusks = new BuffID(nameof(RocketBoostTusks), true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<RocketBoostTusksEntry>(RocketBoostTusks);
        }
        public static void HookOn()
        {
            On.KingTusks.Tusk.ShootUpdate += Tusk_ShootUpdate;
            On.KingTusks.Tusk.ctor += Tusk_ctor;
            On.KingTusks.Tusk.SwitchMode += Tusk_SwitchMode;
            On.Vulture.Update += Vulture_Update;
            IL.KingTusks.Tusk.ShootUpdate += Tusk_ShootUpdate1;
        }

        private static void Tusk_ShootUpdate1(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(i => i.MatchStloc(0));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, KingTusks.Tusk, float>>((f, tusk) =>
            {
                if (Modules.TryGetValue(tusk, out var module))
                    return module.InitSpeed;
                return f;
            });
        }

        private static void Vulture_Update(On.Vulture.orig_Update orig, Vulture self, bool eu)
        {
            var origRange = KingTusks.Tusk.shootRange;
            var origLength = KingTusks.Tusk.maxWireLength;

            KingTusks.Tusk.shootRange = 700;
            KingTusks.Tusk.maxWireLength = 650;
            orig(self,eu);
            KingTusks.Tusk.shootRange = origRange;
            KingTusks.Tusk.maxWireLength = origLength;
        }

        private static void Tusk_SwitchMode(On.KingTusks.Tusk.orig_SwitchMode orig, KingTusks.Tusk self, KingTusks.Tusk.Mode newMode)
        {
            if (Modules.TryGetValue(self, out var module))
                module.ChangeMode(self,ref newMode);
            if (newMode == KingTusks.Tusk.Mode.StuckInWall)
            {
                self.room.AddObject(new Explosion(self.room, null, self.stuckInWallPos.Value,
                    7, 150f, 6.2f, 0f, 10f, 0.25f, self.vulture, 0.7f, 0f, 1f));
                var pos = self.stuckInWallPos.Value;
                self.room.AddObject(new Explosion.ExplosionLight(pos, 100f, 1f, 7, Color.white));
                self.room.AddObject(new Explosion.ExplosionLight(pos, 90f, 1f, 3, new Color(1f, 1f, 1f)));
                self.room.AddObject(new ExplosionSpikes(self.room, pos, 10, 30f, 9f, 7f, 100f, Color.white));
                self.room.AddObject(new ShockWave(pos, 130f, 0.025f, 5, false));
                self.modeCounter = 30;
                newMode = KingTusks.Tusk.Mode.Dangling;
            }
            orig(self, newMode);

            if (self.mode == KingTusks.Tusk.Mode.StuckInCreature)
            {
                var pos = self.impaleChunk?.pos ?? TuskPos(self);
                self.room.AddObject(new Explosion(self.room, null, pos,
                    7, 150f, 6.2f, 0f, 10f, 0.25f, self.vulture, 0.3f, 0f, 1f));

                self.room.AddObject(new Explosion.ExplosionLight(pos, 100f, 1f, 7, Color.white));
                self.room.AddObject(new Explosion.ExplosionLight(pos, 90f, 1f, 3, new Color(1f, 1f, 1f)));
                self.room.AddObject(new ExplosionSpikes(self.room, pos, 10, 30f, 9f, 7f, 100f, Color.white));
                self.room.AddObject(new ShockWave(pos, 130f, 0.025f, 5, false));
            }
        }

        private static void Tusk_ctor(On.KingTusks.Tusk.orig_ctor orig, KingTusks.Tusk self, KingTusks owner, int side)
        {
            orig(self, owner, side);
            if(!Modules.TryGetValue(self,out _))
                Modules.Add(self,new TuskModule());
        }

        private static void Tusk_ShootUpdate(On.KingTusks.Tusk.orig_ShootUpdate orig, KingTusks.Tusk self, float speed)
        {
            var origRange = KingTusks.Tusk.shootRange;
            var origLength = KingTusks.Tusk.maxWireLength;
            KingTusks.Tusk.shootRange = 1000;
            KingTusks.Tusk.maxWireLength = 900;
            if(Modules.TryGetValue(self,out var module))
                module.ShootUpdate(self,ref speed);
            orig(self, speed);
            KingTusks.Tusk.shootRange = origRange;
            KingTusks.Tusk.maxWireLength = origLength;

        }

        private static readonly ConditionalWeakTable<KingTusks.Tusk, TuskModule> Modules = new ConditionalWeakTable<KingTusks.Tusk, TuskModule>();

        internal class TuskModule
        {
            private int shootCounter;
            private Creature focusCreature;

            public void ShootUpdate(KingTusks.Tusk self, ref float speed)
            {
                self.modeCounter = Mathf.Max(0, shootCounter - 25);
                shootCounter++;
                if (shootCounter < 20)
                {
                    if (focusCreature != null && (focusCreature.room == null || focusCreature.inShortcut))
                        focusCreature = null;
                    speed = 5;
                    if (focusCreature != null)
                        self.shootDir = Vector3.Slerp(self.shootDir,
                            Custom.DirVec(TuskPos(self), focusCreature.DangerPos), 0.15f);
                }
                else
                    speed = (shootCounter - 20) * 20;

            }

            public void ChangeMode(KingTusks.Tusk self,ref KingTusks.Tusk.Mode mode)
            {
                if (mode != KingTusks.Tusk.Mode.ShootingOut)
                {
                    shootCounter = 0;
                    focusCreature = null;
                }

                if (mode == KingTusks.Tusk.Mode.ShootingOut)
                {
                    self.shootDir = self.AimDir(1);
                    var max = 0f;

                    foreach (var crit in self.room.updateList.OfType<Creature>())
                    {
                        if(crit == self.vulture)
                            continue;
                        var dir = Custom.DirVec(TuskPos(self), crit.DangerPos);
                        if (Vector2.Dot(self.shootDir, dir) > max)
                        {
                            max = Vector2.Dot(self.shootDir, dir);
                            focusCreature = crit;
                        }
                    }
                }
                if (mode == KingTusks.Tusk.Mode.Dangling && self.mode == KingTusks.Tusk.Mode.ShootingOut)
                    mode = KingTusks.Tusk.Mode.Retracting;
            }

            public float InitSpeed => Custom.LerpMap(shootCounter, 20, 30, 0, 20);
        }

        public static Vector2 TuskPos(KingTusks.Tusk tusk)
        {
            return (tusk.chunkPoints[0, 0] + tusk.chunkPoints[1, 0]) / 2f;
        }

    
    }
}
