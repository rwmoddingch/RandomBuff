
using System;
using Mono.Cecil.Cil;
using RandomBuff;
using MonoMod.Cil;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class SpearMasterIBuffEntry : IBuffEntry
    {
        public static BuffID spearMasterBuffID = new BuffID("SpearMaster", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SpearMasterBuff,SpearMasterBuffData,SpearMasterIBuffEntry>(spearMasterBuffID);
        }

        public static void HookOn()
        {
            IL.Player.GrabUpdate += Player_GrabUpdate_CraftIL;
            On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
            On.SlugcatStats.SpearSpawnModifier += SlugcatStats_SpearSpawnModifier;
            On.Player.Grabability += Player_Grabability;
        }

        private static Player.ObjectGrabability Player_Grabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
        {
            if (obj is Spear) return Player.ObjectGrabability.OneHand;
            return orig(self,obj);
        }

        private static float SlugcatStats_SpearSpawnModifier(On.SlugcatStats.orig_SpearSpawnModifier orig, SlugcatStats.Name index, float originalSpearChance)
        {
            return orig(index, originalSpearChance) * 2;
            
        }

        private static void Player_GrabUpdate_CraftIL(ILContext il)
        {
            try
            {
                ILCursor c = new ILCursor(il);
                c.GotoNext(MoveType.After, i => i.MatchLdarg(0),
                    i => i.MatchCall<Player>("PickupPressed"),
                    i => i.MatchLdarg(0),
                    i => i.MatchCall<Player>("get_input"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchLdelema<Player.InputPackage>(),
                    i => i.MatchLdfld<Player.InputPackage>("pckp"),
                    i => i.OpCode == OpCodes.Brfalse);

                ILLabel l = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<Player, bool>>((player) =>
                {
                    var re = player.craftingObject = player.craftingObject || CanBeCrafted(player) != 0;
                    return re;
                });
                c.Emit(OpCodes.Brfalse, l);

                c.Emit(OpCodes.Ldc_I4_1);
                c.Emit(OpCodes.Stloc_1);
                c.Emit(OpCodes.Ldc_I4_M1);
                c.Emit(OpCodes.Stloc_S, (byte)6);

                c.MarkLabel(l);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

        }
        private static void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
        {
            if (CanBeCrafted(self) != 0)
            {
                self.grasps[0].grabbed.Destroy();
                self.grasps[1].grabbed.Destroy();
                self.ReleaseGrasp(0);
                self.ReleaseGrasp(1);

                var r = Random.value > 0.5f;
                AbstractSpear spear = new AbstractSpear(self.abstractCreature.world, null, self.abstractCreature.pos,
                    self.abstractCreature.world.game.GetNewID(), r, !r);
                spear.RealizeInRoom();
                self.room.AddObject(spear.realizedObject);
                self.SlugcatGrab(spear.realizedObject,0);
            }
            else
                orig(self);
        }
        public static int CanBeCrafted(Player player)
        {
            if ( (player.grasps[0]?.grabbed is Spear && !(player.grasps[0]?.grabbed is ExplosiveSpear) && !(player.grasps[0]?.grabbed is ElectricSpear)) &&
                 (player.grasps[1]?.grabbed is Spear && !(player.grasps[1]?.grabbed is ExplosiveSpear) && !(player.grasps[1]?.grabbed is ElectricSpear)) )
                    return 1;
            return 0;
        }

    }

    class SpearMasterBuffData : BuffData
    {
        public override BuffID ID => SpearMasterIBuffEntry.spearMasterBuffID;
    }

    class SpearMasterBuff : Buff<SpearMasterBuff, SpearMasterBuffData>
    {
        public override BuffID ID => SpearMasterIBuffEntry.spearMasterBuffID;

    }


}
