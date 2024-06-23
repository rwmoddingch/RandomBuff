using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuiltinBuffs.Duality
{
    internal class DreamTravellerBuff : Buff<DreamTravellerBuff, DreamTravellerBuffData>
    {
        public override BuffID ID => DreamTravellerBuffEntry.DreamTraveller;
    }

    class DreamTravellerBuffData : CountableBuffData
    {
        public override BuffID ID => DreamTravellerBuffEntry.DreamTraveller;
        public override int MaxCycleCount => 5;
    }

    class DreamTravellerBuffEntry : IBuffEntry
    {
        public static BuffID DreamTraveller = new BuffID("DreamTraveller", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DreamTravellerBuff, DreamTravellerBuffData, DreamTravellerBuffEntry>(DreamTraveller);
        }

        public static void LongLifeCycleHookOn()
        {
            IL.SaveState.BringUpToDate += SaveState_BringUpToDate;
            On.RegionState.AdaptRegionStateToWorld += RegionState_AdaptRegionStateToWorld;
        }

        private static void RegionState_AdaptRegionStateToWorld(On.RegionState.orig_AdaptRegionStateToWorld orig, RegionState self, int playerShelter, int activeGate)
        {
            orig(self, playerShelter, activeGate);
            try
            {
                if (self.savedObjects.Count > 0)
                {
                    for (int i = 0; i < self.savedObjects.Count; i++)
                    {
                        UnityEngine.Debug.Log("Saving for Travel: " + self.savedObjects[i]);

                        string newPos = string.Empty;
                        string newSavedObj = string.Empty;

                        string[] array = Regex.Split(self.savedObjects[i], "<oA>");
                        if (array[1] == "Spear" && int.TryParse(array[3], out int num) && num >= 1)
                        {
                            UnityEngine.Debug.Log("Skip Spear on Wall");
                            continue;
                        }

                        for (int j = 0; j < array.Length; j++)
                        {
                            if (j != 2)
                            {
                                newSavedObj += array[j];
                            }
                            else
                            {
                                string[] array2 = array[2].Split(new char[]
                                {
                                   '.'
                                });

                                newPos += self.saveState.GetSaveStateDenToUse();
                                for (int k = 0; k < array2.Length; k++)
                                {
                                    if (k > 0)
                                        newPos += array2[k];
                                    if (k < array2.Length - 1)
                                    {
                                        newPos += ".";
                                    }
                                }
                                newSavedObj += newPos;
                            }
                            if (j < array.Length - 1) newSavedObj += "<oA>"; 
                        }                       
                        self.savedObjects[i] = newSavedObj;
                        UnityEngine.Debug.Log("Dream travel object: " + newSavedObj);
                    }
                }

                if (self.saveState.pendingObjects.Count > 0)
                {
                    for (int i = 0; i < self.saveState.pendingObjects.Count; i++)
                    {
                        UnityEngine.Debug.Log("Saving for Travel: " + self.saveState.pendingObjects[i]);

                        string newPos = string.Empty;
                        string newSavedObj = string.Empty;

                        string[] array = Regex.Split(self.saveState.pendingObjects[i], "<oA>");                        
                        for (int j = 0; j < array.Length; j++)
                        {
                            if (j != 2)
                            {
                                newSavedObj += array[j];
                            }
                            else
                            {
                                string[] array2 = array[2].Split(new char[]
                                {
                                   '.'
                                });

                                newPos += self.saveState.GetSaveStateDenToUse();
                                for (int k = 0; k < array2.Length; k++)
                                {
                                    if (k > 0)
                                        newPos += array2[k];
                                    if (k < array2.Length - 1)
                                    {
                                        newPos += ".";
                                    }
                                }
                                newSavedObj += newPos;
                            }
                            if (j < array.Length - 1) newSavedObj += "<oA>";
                        }
                        self.saveState.pendingObjects[i] = newSavedObj;
                        UnityEngine.Debug.Log("Dream travel object: " + newSavedObj);
                    }
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }

        }

        private static void SaveState_BringUpToDate(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdloc(2),
                i => i.Match(OpCodes.Ldfld),
                i => i.Match(OpCodes.Stfld)))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate<Action<SaveState, RainWorldGame>>(delegate (SaveState self, RainWorldGame game)
                {
                    if (game.world.shelters.Length > 1)
                    {
                        UnityEngine.Debug.Log("Dream Travel!");
                        int num = UnityEngine.Random.Range(0, game.world.shelters.Length);
                        self.denPosition = game.world.GetAbstractRoom(game.world.shelters[num]).name;
                    }
                });
            }
        }

    }
}
