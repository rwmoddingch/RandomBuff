using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;

namespace RandomBuffUtils
{
    public class PlayerUtils
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

        public static bool TryGetGraphicPart<T>(Player player, IOWnPlayerUtilsPart owner, out T modulePart) where T : PlayerModuleGraphicPart
        {
            if (weakTable.TryGetValue(player, out var module) && module.graphicParts.TryGetValue(owner, out var part))
            {
                modulePart = part as T;
                return true;
            }
            modulePart = null;
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
}
