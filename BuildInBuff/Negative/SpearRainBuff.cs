
using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;
using System.Runtime.Remoting.Contexts;
using MoreSlugcats;
using RWCustom;
using Random = UnityEngine.Random;
using System.Runtime.CompilerServices;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;

namespace BuiltinBuffs.Negative
{
    internal class SpearRainBuff : Buff<SpearRainBuff, SpearRainBuffData>
    {
        public override BuffID ID => SpearRainIBuffEntry.SpearRainBuffID;
    }

    internal class SpearRainBuffData : BuffData
    {
        public override BuffID ID => SpearRainIBuffEntry.SpearRainBuffID;
    }

    internal class SpearRainIBuffEntry : IBuffEntry
    {
        public static BuffID SpearRainBuffID = new BuffID("SpearRain", true);
        public static RoomRain.DangerType SpearRain = new RoomRain.DangerType("SpearRain", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpearRainBuff, SpearRainBuffData, SpearRainIBuffEntry>(SpearRainBuffID);
        }

        public static void HookOn()
        {
            IL.RoomRain.Update += RoomRain_Update_IL;
            On.RoomRain.Update += RoomRain_Update;

            On.Spear.ChangeMode += Spear_ChangeMode;
            On.Spear.Update += Spear_Update;

            IL.RoomRain.InitiateSprites += RoomRain_InitiateSprites;
            IL.RoomRain.DrawSprites += RoomRain_DrawSprites;
            IL.RoomRain.ThrowAroundObjects += RoomRain_ThrowAroundObjects;
        }

        private static void RoomRain_InitiateSprites(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            c1.Index = 0;
            c1.Emit(OpCodes.Ret);
        }

        private static void RoomRain_ThrowAroundObjects(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            ILCursor c2 = new ILCursor(il);
            ILLabel skipLabel = null;

            if(c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdloc(1),
                (i) => i.MatchLdcI4(1),
                (i) => i.MatchAdd(),
                (i) => i.MatchStloc(1)
            ))
            {
                c1.Index -= 4;
                skipLabel = c1.MarkLabel();
            }
            else
            {
                BuffPlugin.LogException(new Exception("RoomRain_ThrowAroundObjects1 c1 cant match"));
            }

            try
            {
                if (skipLabel != null && c2.TryGotoNext(MoveType.After,
                    (i) => i.MatchLdloc(1),
                    (i) => i.MatchLdarg(0),
                    (i) => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    (i) => i.MatchLdfld<Room>("physicalObjects"),
                    (i) => i.MatchLdloc(0),
                    (i) => i.MatchLdelemRef()))
                {
                    c2.Index -= 5;
                    BuffPlugin.Log($"{c2.Next.OpCode}");
                    c2.Emit(OpCodes.Ldloc_0);
                    c2.Emit(OpCodes.Ldarg_0);
                    c2.EmitDelegate<Func<int,int,RoomRain, bool>>((j,i,self) =>
                    {
                        //EmgTxCustom.Log($"i:{i}, count:{self.room.physicalObjects.Count()}" + (i < self.room.physicalObjects.Count() ? $"j:{j}, count:{self.room.physicalObjects[i].Count}" : ""));
                        return (j < self.room.physicalObjects[i].Count - 1) && (self.room.physicalObjects[i][j] is Spear spear && RoomRainModule.rainSpearModules.TryGetValue(spear, out var _));
                    });
                    c2.Emit(OpCodes.Brtrue_S, skipLabel);
                    c2.Emit(OpCodes.Ldloc_1);
                }
                else
                {
                    BuffPlugin.LogException(new Exception("RoomRain_ThrowAroundObjects1 c2 cant match"));
                }
            }
            catch(Exception e)
            {
                BuffPlugin.LogException(e, "RoomRain_ThrowAroundObjects1 c2 format error");
                Debug.LogException(e);
            }
        }

        private static void RoomRain_DrawSprites(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            c1.Index = 0;
            c1.Emit(OpCodes.Ret);
        }

        private static void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig.Invoke(self, eu);
            if(RoomRainModule.rainSpearModules.TryGetValue(self, out var module))
            {
                module.Update(self);
            }
        }

        private static void RoomRain_Update(On.RoomRain.orig_Update orig, RoomRain self, bool eu)
        {
            orig.Invoke(self, eu);
            if(RoomRainModule.roomRainModules.TryGetValue(self, out var module))
            {
                module.Update(self);
            }
            else
            {
                if (self.room == self.room.game.cameras[0].room)
                {
                    RoomRainModule.roomRainModules.Add(self, new RoomRainModule(self));
                }
            }
        }

        private static void Spear_ChangeMode(On.Spear.orig_ChangeMode orig, Spear self, Weapon.Mode newMode)
        {
            orig.Invoke(self, newMode);
            if (RoomRainModule.rainSpearModules.TryGetValue(self, out var module))
            {
                module.OnSpearChangeMode(self, newMode); 
            }
        }

        private static void RoomRain_Update_IL(ILContext il)
        {
            ILCursor c1 = new ILCursor(il);

            //标记
            if(c1.TryGotoNext(MoveType.Before,
                (i) => i.Match(OpCodes.Bge_S)
            ))
            {
                c1.EmitDelegate<Func<int, int>>((orig) =>
                {
                    return 0;
                });
            }
            else
            {
                BuffPlugin.LogException(new Exception("RoomRain_Update c1 cant mark label"));
            }
        }
    }

    public class RainSpearModule
    {
        public WeakReference<Spear> bindSpearRef;
        public WeakReference<RoomRainModule> roomRainModuleRef;

        Mode mode = Mode.SearchTile;

        public RainSpearModule(Spear bindSpear, RoomRainModule roomRainModule) 
        { 
            bindSpearRef = new WeakReference<Spear>(bindSpear);
            roomRainModuleRef = new WeakReference<RoomRainModule>(roomRainModule);

            ReinitRainThrow(bindSpear);
            roomRainModule.totalCount++;
        }

        public void Update(Spear spear)
        {
            if (spear.room != spear.room.game.cameras[0].room)
                Destroy(true, spear);
            if(spear.mode == Weapon.Mode.StuckInWall)
            {
                spear.ChangeMode(Weapon.Mode.Free);
            }

            if(mode == Mode.SearchTile)
            {
                IntVector2 coord = new IntVector2(Random.Range(0, spear.room.Width), spear.room.Height - 1);
                if (spear.room.GetTile(coord).Solid)
                    return;

                spear.firstChunk.HardSetPosition(spear.room.MiddleOfTile(coord) + Custom.RNV() * 20f + Vector2.up * 60f);
                InitThrow(spear);
            }
        }

        public void OnSpearChangeMode(Spear spear, Weapon.Mode newMode)
        {
            if(newMode == Weapon.Mode.Free)
            {
                ReinitRainThrow(spear);
            }
            else if(newMode != Weapon.Mode.Thrown)
            {
                if (roomRainModuleRef.TryGetTarget(out var module))
                {
                    Destroy(true, spear);
                }
                else
                    Destroy(true, spear);
            }
        }

        public void ReinitRainThrow(Spear spear)
        {
            mode = Mode.SearchTile;
        }

        public void InitThrow(Spear spear)
        {
            mode = Mode.Rain;
            Vector2 dir = Vector2.down;

            spear.firstChunk.pos += dir * 10f;

            spear.thrownPos = spear.firstChunk.pos;
            spear.thrownBy = null;
            IntVector2 throwDir = new IntVector2(0, -1);
            spear.throwDir = throwDir;

            spear.firstFrameTraceFromPos = spear.thrownPos;
            spear.changeDirCounter = 3;
            spear.ChangeOverlap(true);
            spear.firstChunk.MoveFromOutsideMyUpdate(false, spear.thrownPos);

            spear.ChangeMode(Weapon.Mode.Thrown);

            float vel = Mathf.Lerp(40f, 240f, spear.room.roomRain.intensity);
            spear.overrideExitThrownSpeed = 0f;

            spear.firstChunk.vel = vel * dir;
            spear.firstChunk.pos += dir;
            spear.setRotation = dir;
            spear.rotationSpeed = 0f;
            spear.meleeHitChunk = null;
        }

        public void Destroy(bool destroySpear, Spear spear = null)
        {
            if(roomRainModuleRef.TryGetTarget(out var module))
                module.totalCount--;

            
            if (spear == null && bindSpearRef.TryGetTarget(out var target))
                spear = target;

            if (spear != null && RoomRainModule.rainSpearModules.TryGetValue(spear, out var _))
            {
                RoomRainModule.rainSpearModules.Remove(spear);
            }

            if (destroySpear && spear != null)
                spear.Destroy();
        }

        public enum Mode
        {
            Rain,
            SearchTile
        }
    }

    public class RoomRainModule
    {
        public static ConditionalWeakTable<RoomRain, RoomRainModule> roomRainModules = new ConditionalWeakTable<RoomRain, RoomRainModule>();
        public static ConditionalWeakTable<Spear, RainSpearModule> rainSpearModules = new ConditionalWeakTable<Spear, RainSpearModule>();

        public WeakReference<RoomRain> roomRainRef;
        public List<RainSpearModule> mySpearModules = new List<RainSpearModule>();

        public int totalCount;
        public int totalUntrackSpearCount;

        public virtual float Intensity
        {
            get
            {
                if(roomRainRef.TryGetTarget(out var roomRain))
                {
                    return roomRain.intensity;
                }
                return 0f;
            }
        }

        public RoomRainModule(RoomRain bindRoomRain)
        {
            roomRainRef = new WeakReference<RoomRain>(bindRoomRain);
        }

        public void Update(RoomRain self)
        {
            if (self.room != self.room.game.cameras[0].room)
                Destroy();

            if (self.intensity > 0 && Random.value < self.intensity)
            {
                for (int i = 0; i < Mathf.CeilToInt(self.intensity * 10) && totalCount < Mathf.CeilToInt(self.intensity * 40); i++)
                {
                    AbstractPhysicalObject abstractPhysical = new AbstractSpear(self.room.world, null, new WorldCoordinate(self.room.abstractRoom.index, 0, 0, -1), self.room.game.GetNewID(), false);
                    self.room.abstractRoom.entities.Add(abstractPhysical);
                    abstractPhysical.RealizeInRoom();

                    var module = new RainSpearModule(abstractPhysical.realizedObject as Spear, this);
                    rainSpearModules.Add(abstractPhysical.realizedObject as Spear, module);
                    mySpearModules.Add(module);
                }
            }
        }

        public void Destroy()
        {
            foreach(var module in mySpearModules)
            {
                module.Destroy(true);
            }
            mySpearModules.Clear();

            if(roomRainRef.TryGetTarget(out var roomRain))
            {
                roomRainModules.Remove(roomRain);
            }
        }
    }
}
