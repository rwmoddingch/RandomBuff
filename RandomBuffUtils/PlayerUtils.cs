using JetBrains.Annotations;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RandomBuffUtils
{
    public static partial class PlayerUtils
    {
        static ConditionalWeakTable<Player, PlayerModule> weakTable = new ConditionalWeakTable<Player, PlayerModule>();
        public static List<IOWnPlayerUtilsPart> owners = new List<IOWnPlayerUtilsPart>();

        public static void OnEnable()
        {
            On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;

            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainer;
            On.PlayerGraphics.Update += PlayerGraphics_Update;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;
            On.PlayerGraphics.Reset += PlayerGraphics_Reset;
            On.RoomCamera.SpriteLeaser.CleanSpritesAndRemove += SpriteLeaser_CleanSpritesAndRemove;

            InitOperations();
        }

        

        #region PlayerHooks
        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);
            if (weakTable.TryGetValue(self, out var module))
            {
                foreach (var owner in owners)
                {
                    if (module.moduleParts.TryGetValue(owner, out var part))
                        part.Update(self, eu);
                }
            }
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig.Invoke(self, abstractCreature, world);
            if (!weakTable.TryGetValue(self, out var _))
            {
                weakTable.Add(self, new PlayerModule(self));
            }
        }
        #endregion

        #region PlayerGraphicHooks

        private static void PlayerGraphics_Reset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (weakTable.TryGetValue(self.player, out var module))
                foreach (var playerModuleGraphicPart in module.graphicParts)
                    playerModuleGraphicPart.Value.Reset(self);
        }
        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            if (weakTable.TryGetValue(self.player, out var module))
            {
                module.graphicsInited = false;
                foreach (var owner in owners)
                {
                    if (module.graphicParts.TryGetValue(owner, out var part))
                    {
                        ProcessSprite(self, sLeaser, rCam, part);
                    }
                }
                module.graphicsInited = true;

                self.AddToContainer(sLeaser, rCam, null);
            }
        }

        static void ProcessSprite(PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, PlayerModuleGraphicPart part)
        {
            part.InitSprites(part.sleaserInstance, self, sLeaser, rCam);
            part.sleaserInstance.realIndex = new int[part.sleaserInstance.sprites.Length];

            int origSize = sLeaser.sprites.Length;//将 sleaserInstance 中的 sprite 合并进入 sLeaser
            Array.Resize(ref sLeaser.sprites, origSize + part.sleaserInstance.sprites.Length);

            for (int i = 0; i < part.sleaserInstance.sprites.Length; i++)
            {
                part.sleaserInstance.realIndex[i] = i + origSize;
                sLeaser.sprites[part.sleaserInstance.realIndex[i]] = part.sleaserInstance.sprites[i];
            }
        }

        private static void PlayerGraphics_AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig.Invoke(self, sLeaser, rCam, newContatiner);
            if (weakTable.TryGetValue(self.player, out var module) && module.graphicsInited)
            {
                foreach (var owner in owners)
                {
                    if (module.graphicParts.TryGetValue(owner, out var part))
                        part.AddToContainer(part.sleaserInstance, self, sLeaser, rCam, newContatiner);
                }
            }
        }

        private static void SpriteLeaser_CleanSpritesAndRemove(On.RoomCamera.SpriteLeaser.orig_CleanSpritesAndRemove orig, RoomCamera.SpriteLeaser self)
        {
            orig.Invoke(self);
            if (self.drawableObject is PlayerGraphics graphics && graphics.player != null && weakTable.TryGetValue(graphics.player, out var module))
            {
                module.graphicsInited = false;
                foreach (var owner in owners)
                {
                    if (module.graphicParts.TryGetValue(owner, out var part))
                        part.sleaserInstance.ClearOutSprites();
                }
            }
        }

        private static void PlayerGraphics_DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (weakTable.TryGetValue(self.player, out var module) && module.graphicsInited)
            {
                if (!module.graphicsInited)
                    return;

                foreach (var owner in owners)
                {
                    if (module.graphicParts.TryGetValue(owner, out var part))
                        part.DrawSprites(part.sleaserInstance, self, sLeaser, rCam, timeStacker, camPos);
                }
            }
        }

        private static void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (weakTable.TryGetValue(self.player, out var module) && module.graphicsInited)
            {
                if (!module.graphicsInited)
                    return;

                foreach (var owner in owners)
                {
                    if (module.graphicParts.TryGetValue(owner, out var part))
                        part.Update(self);
                }
            }
        }



        #endregion

        public static void AddPart(IOWnPlayerUtilsPart owner)
        {           
            if (owners.Contains(owner))
                return;
            owners.Add(owner);    
            
            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                foreach (var abPlayer in game.Players)
                {
                    if (abPlayer.realizedCreature == null)
                        continue;
                    Player player = abPlayer.realizedCreature as Player;
                    PlayerGraphics pGraphic = player.graphicsModule as PlayerGraphics;

                    if (weakTable.TryGetValue(player, out var module))
                    {
                        var part = owner.InitPart(module);
                        if (part != null)
                        {
                            module.moduleParts.Add(owner, part);
                            //BuffUtils.Log("PlayerUtils", $"");
                        }

                        if (pGraphic != null)
                        {
                            var graphicPart = owner.InitGraphicPart(module);
                            if (graphicPart != null)
                            {
                                module.graphicParts.Add(owner, graphicPart);

                                if (module.graphicsInited)
                                {
                                    var rCam = game.cameras[0];
                                    var sLearser = rCam.spriteLeasers.Find((s) => s.drawableObject == pGraphic);

                                    ProcessSprite(pGraphic, sLearser, rCam, graphicPart);
                                    pGraphic.AddToContainer(sLearser, rCam, null);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void RemovePart(IOWnPlayerUtilsPart owner)
        {
            if (!owners.Contains(owner))
                return;
            owners.Remove(owner);

            if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
            {
                foreach (var abPlayer in game.Players)
                {
                    if (abPlayer.realizedCreature == null)
                        continue;
                    Player player = abPlayer.realizedCreature as Player;
                    PlayerGraphics pGraphic = player.graphicsModule as PlayerGraphics;

                    if (weakTable.TryGetValue(player, out var module))
                    {
                        if (module.moduleParts.TryGetValue(owner, out var part))
                        {
                            part.Destroy();
                            module.moduleParts.Remove(owner);
                        }

                        if (module.graphicParts.TryGetValue(owner, out var graphicPart))
                        {
                            if (pGraphic != null)
                            {
                                var rCam = game.cameras[0];
                                var sLearser = rCam.spriteLeasers.Find((s) => s.drawableObject == pGraphic);

                                int decrease = graphicPart.sleaserInstance.realIndex.Length;
                                int lastIndex = graphicPart.sleaserInstance.realIndex.Last() + 1;

                                for (int i = lastIndex; i < sLearser.sprites.Length; i++)
                                {
                                    sLearser.sprites[i - decrease] = sLearser.sprites[i];
                                }
                                Array.Resize(ref sLearser.sprites, sLearser.sprites.Length - decrease);

                                graphicPart.sleaserInstance.ClearOutSprites();
                            }
                        }
                    }
                }
            }
        }

        public static bool TryGetModulePart<T>(Player player, IOWnPlayerUtilsPart owner, out T modulePart) where T : PlayerModulePart
        {
            if (weakTable.TryGetValue(player, out var module) && module.moduleParts.TryGetValue(owner, out var part))
            {
                modulePart = part as T;
                return true;
            }
            modulePart = null;
            return false;
        }

        public static bool TryGetModulePart<T, OwnerT>(Player player, out T modulePart) where T : PlayerModulePart where OwnerT : IOWnPlayerUtilsPart
        {
            if (weakTable.TryGetValue(player, out var module))
            {
                foreach (var pair in module.moduleParts)
                {
                    if (pair.Key.GetType() == typeof(OwnerT))
                    {
                        modulePart = pair.Value as T;
                        return true;
                    }
                }
            }
            modulePart = null;
            return false;
        }

        public static bool TryGetGraphicPart<T>(Player player, IOWnPlayerUtilsPart owner, out T graphicPart) where T : PlayerModuleGraphicPart
        {
            if (weakTable.TryGetValue(player, out var module) && module.graphicParts.TryGetValue(owner, out var part))
            {
                graphicPart = part as T;
                return true;
            }
            graphicPart = null;
            return false;
        }

        public static bool TryGetGraphicPart<T, OwnerT>(Player player, out T graphicPart) where T : PlayerModuleGraphicPart where OwnerT : IOWnPlayerUtilsPart
        {
            if (weakTable.TryGetValue(player, out var module))
            {
                foreach (var pair in module.graphicParts)
                {
                    if (pair.Key.GetType() == typeof(OwnerT))
                    {
                        graphicPart = pair.Value as T;
                        return true;
                    }
                }
            }
            graphicPart = null;
            return false;
        }


        public class PlayerModule
        {
            public WeakReference<Player> PlayerRef { get; private set; }
            public SlugcatStats.Name Name { get; private set; }


            public readonly Dictionary<IOWnPlayerUtilsPart, PlayerModulePart> moduleParts = new Dictionary<IOWnPlayerUtilsPart, PlayerModulePart>();

            internal bool graphicsInited = false;
            public readonly Dictionary<IOWnPlayerUtilsPart, PlayerModuleGraphicPart> graphicParts = new Dictionary<IOWnPlayerUtilsPart, PlayerModuleGraphicPart>();

            public PlayerModule(Player player)
            {
                PlayerRef = new WeakReference<Player>(player);
                Name = player.slugcatStats.name;

                foreach (var owner in owners)
                {
                    var part = owner.InitPart(this);
                    var graphicPart = owner.InitGraphicPart(this);
                    if (part != null)
                        moduleParts.Add(owner, part);
                    if (graphicPart != null)
                        graphicParts.Add(owner, graphicPart);
                }
            }
        }

        public abstract class PlayerModulePart
        {
            public virtual void Update(Player player, bool eu)
            {
            }

            public virtual void Destroy()
            {
            }
        }

        public abstract class PlayerModuleGraphicPart
        {
            internal SLeaserInstance sleaserInstance;

            public PlayerModuleGraphicPart()
            {
                sleaserInstance = new SLeaserInstance();
            }

            public virtual void InitSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
            {
            }

            public virtual void AddToContainer(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
            {
            }

            public virtual void Update(PlayerGraphics playerGraphics)
            {
            }

            public virtual void DrawSprites(SLeaserInstance sLeaserInstance, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
            }


            public virtual void Reset(PlayerGraphics playerGraphics)
            {

            }

            public class SLeaserInstance
            {
                public FSprite[] sprites;
                public int[] realIndex;

                public void ClearOutSprites()
                {
                    if (sprites == null)
                        return;

                    foreach (var sprite in sprites)
                        sprite.RemoveFromContainer();
                    sprites = null;
                    realIndex = null;
                }
            }
        }

        public interface IOWnPlayerUtilsPart
        {
            [CanBeNull] PlayerModulePart InitPart(PlayerModule module);
            [CanBeNull] PlayerModuleGraphicPart InitGraphicPart(PlayerModule module);
        }
    }

    public static partial class PlayerUtils
    {
        public delegate float OperatorDelegate(float origValue, float execValue);

        public static readonly List<OperatorDelegate> OperatorDelegates = new ();

        public static readonly ConditionalWeakTable<SlugcatStats, SlugcatStatStack> SlugcatStatsModifiedStack = new();
        public static readonly ConditionalWeakTable<object, HashSet<SlugcatStatModifer>> SignalKeyMap = new();

        public static readonly OperatorDelegate Max = Mathf.Max;
        public static readonly OperatorDelegate Min = Mathf.Min;


        public static readonly OperatorDelegate Add = (value, execValue) => value + execValue;
        public static readonly OperatorDelegate Subtraction = (value, execValue) => value - execValue;

        public static readonly OperatorDelegate Multiply = (value, execValue) => value * execValue;
        public static readonly OperatorDelegate Division = (value, execValue) => value / execValue;

        public static void InitOperations()
        {
            OperatorDelegates.Clear();
            OperatorDelegates.Add(Max);
            OperatorDelegates.Add(Min);
            OperatorDelegates.Add(Add);
            OperatorDelegates.Add(Subtraction);
            OperatorDelegates.Add(Multiply);
            OperatorDelegates.Add(Division);
        }

        public static void AddNewOperation(OperatorDelegate op, int priority = -1)
        {
            if (priority == -1)
                OperatorDelegates.Add(op);
            if (priority < OperatorDelegates.Count)
                OperatorDelegates.Insert(priority, op);
        }

        public static SlugcatStats Original(this SlugcatStats stats)
        {
            if (SlugcatStatsModifiedStack.TryGetValue(stats, out var stack))
                return stack.GetOriginal();
            return stats;
        }


        public static SlugcatStats Clone(this SlugcatStats origStats)
        {
            return new SlugcatStats(origStats.name, origStats.malnourished)
            {
                bodyWeightFac = origStats.bodyWeightFac,
                corridorClimbSpeedFac = origStats.corridorClimbSpeedFac,
                foodToHibernate = origStats.foodToHibernate,
                generalVisibilityBonus = origStats.generalVisibilityBonus,
                loudnessFac = origStats.loudnessFac,
                lungsFac = origStats.lungsFac,
                malnourished = origStats.malnourished,
                maxFood = origStats.maxFood,
                poleClimbSpeedFac = origStats.poleClimbSpeedFac,
                throwingSkill = origStats.throwingSkill,
                visualStealthInSneakMode = origStats.visualStealthInSneakMode,
                runspeedFac = origStats.runspeedFac

            };
        }
        public static void CopyTo(this SlugcatStats origStats, SlugcatStats destStats)
        {
            destStats.runspeedFac = origStats.runspeedFac;
            destStats.bodyWeightFac = origStats.bodyWeightFac;
            destStats.corridorClimbSpeedFac = origStats.corridorClimbSpeedFac;
            destStats.foodToHibernate = origStats.foodToHibernate;
            destStats.generalVisibilityBonus = origStats.generalVisibilityBonus;
            destStats.loudnessFac = origStats.loudnessFac;
            destStats.lungsFac = origStats.lungsFac;
            destStats.malnourished = origStats.malnourished;
            destStats.maxFood = origStats.maxFood;
            destStats.poleClimbSpeedFac = origStats.poleClimbSpeedFac;
            destStats.throwingSkill = origStats.throwingSkill;
            destStats.visualStealthInSneakMode = origStats.visualStealthInSneakMode;
        }


        public static void Apply(SlugcatStats stats)
        {
            if(SlugcatStatsModifiedStack.TryGetValue(stats, out var stack))
                stack.Apply();
        }

        public static SlugcatStatModifer Modify(this SlugcatStats stats, object signalKey, OperatorDelegate op,
            string fieldName, float value)
        {
            return Modify(stats, op, fieldName, value, signalKey);
        }

        public static SlugcatStatModifer Modify(this SlugcatStats stats, OperatorDelegate op, string fieldName, float value, object signalKey = null)
        {
            if (typeof(SlugcatStats).GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance) == null)
            {
                BuffUtils.LogError("PlayerUtils", $"can't find field named [{fieldName}] in slugcatStats");
                return null;
            }
            if(!SlugcatStatsModifiedStack.TryGetValue(stats,out var stack))
                SlugcatStatsModifiedStack.Add(stats, stack = new(stats));

            if(!OperatorDelegates.Contains(op))
                OperatorDelegates.Add(op);

            var re = new SlugcatStatModifer(stack, op, fieldName, value, signalKey);

            if (signalKey != null)
                SignalKeyMap.GetValue(signalKey,(key => new HashSet<SlugcatStatModifer>())).Add(re);
            

            stack.Add(re);
            return re;
        }


        public static int Undo(this SlugcatStats stats,[NotNull] object signalKey)
        {
            if (SlugcatStatsModifiedStack.TryGetValue(stats, out var stack))
            {
                var re = stack.RemoveAll(signalKey);

                if (SignalKeyMap.TryGetValue(signalKey, out var modiferList))
                    modiferList.RemoveWhere(i => i.IsDeletion);

                return re;
            }

            return 0;
        }

        public static int UndoAll([NotNull] object signalKey)
        {
            if (SignalKeyMap.TryGetValue(signalKey, out var list))
            {
                SignalKeyMap.Remove(signalKey);
                foreach (var modifer in list)
                    modifer.Undo();
                return list.Count;
                
            }

            return 0;
        }


        public class SlugcatStatStack
        {

            public SlugcatStatStack(SlugcatStats origStats)
            {
                this.origStats = origStats.Clone();
                targetRef = new WeakReference<SlugcatStats>(origStats);
            }

            public SlugcatStats GetOriginal() => origStats.Clone();

            private readonly SlugcatStats origStats;

            private readonly WeakReference<SlugcatStats> targetRef;

            private readonly HashSet<SlugcatStatModifer> container = new();

            public void Apply()
            {
                if (targetRef.TryGetTarget(out var target))
                {
                    //BuffUtils.Log("PlayerUtils", $"apply modify, count :{container.Count}");
                    origStats.CopyTo(target);
                    foreach (var modify in container.OrderBy(i => i.Index))
                    {
                        var field = typeof(SlugcatStats).GetField(modify.fieldName,
                            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                        field.SetValue(target,
                            Convert.ChangeType(modify.op.Invoke((float)field.GetValue(target), modify.ExecValue),
                                field.FieldType));
                    }
                }
            }

            public void Remove(SlugcatStatModifer modifer)
            {
                container.Remove(modifer);
                if (modifer.signalKey != null && SignalKeyMap.TryGetValue(modifer.signalKey, out var list))
                    list.Remove(modifer);
                Apply();
            }
            public int RemoveAll(object signalKey)
            {
                BuffUtils.Log("PlayerUtils", $"apply undo by key, Orig count :{container.Count}");
                var re = container.RemoveWhere(i =>
                {
                    if (i.signalKey != null)
                    {
                        if (i.signalKey.TryGetTarget(out var obj))
                            return obj == signalKey;
                        i.IsDeletion = true;
                        return true;
                    }

                    return false;
                });
                Apply();
                return re;
            }
            public void Add(SlugcatStatModifer modifer)
            {
                container.Add(modifer);
                Apply();
            }

        }


        public class SlugcatStatModifer
        {
            public float ExecValue
            {
                get => execValue;
                set
                {
                    if (value != this.execValue)
                    {
                        this.execValue = value;
                        if (!IsDeletion)
                            stack.Apply();
                    }
                }
            }

            private float execValue;
            public int Index => OperatorDelegates.IndexOf(op);

            public readonly OperatorDelegate op;
            public readonly string fieldName;
            public readonly SlugcatStatStack stack;
            public readonly WeakReference<object> signalKey;

            internal SlugcatStatModifer(SlugcatStatStack stack, OperatorDelegate op, string fieldName, float execValue, object signalKey = null)
            {
                this.op = op;
                this.signalKey = signalKey == null ? null : new(signalKey);
                this.fieldName = fieldName;
                this.execValue = execValue;
                this.stack = stack;
            }


            public void Undo()
            {
                stack.Remove(this);
                IsDeletion = true;
            }

            public bool IsDeletion { get; internal set; }
        }
    }
}
