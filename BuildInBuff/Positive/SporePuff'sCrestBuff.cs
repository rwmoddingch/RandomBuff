using RandomBuff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RWCustom;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RandomBuffUtils.ParticleSystem;
using System.Diagnostics.Eventing.Reader;
using MoreSlugcats;
using Expedition;
using IL;

namespace BuiltinBuffs.Positive
{
    internal class SporePuff_sCrestBuff : Buff<SporePuff_sCrestBuff, SporePuff_sCrestBuffData>
    {
        public override BuffID ID => SporePuff_sCrestBuffEntry.SporePuff_sCrest;
        
        public SporePuff_sCrestBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.AlivePlayers.Select(i => i.realizedCreature as Player)
                             .Where(i => i != null && i.graphicsModule != null))
                {
                    var sporePuff_sCrest = new SporePuff_sCrest(player);
                    SporePuff_sCrestBuffEntry.SporePuff_sCrestFeatures.Add(player, sporePuff_sCrest);
                }
            }
        }
    }

    internal class SporePuff_sCrestBuffData : BuffData
    {
        public override BuffID ID => SporePuff_sCrestBuffEntry.SporePuff_sCrest;
    }

    internal class SporePuff_sCrestBuffEntry : IBuffEntry
    {
        public static BuffID SporePuff_sCrest = new BuffID("SporePuff_sCrest", true);

        public static ConditionalWeakTable<Player, SporePuff_sCrest> SporePuff_sCrestFeatures = new ConditionalWeakTable<Player, SporePuff_sCrest>();

        public static int StackLayer
        {
            get
            {
                return SporePuff_sCrest.GetBuffData().StackLayer;
            }
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SporePuff_sCrestBuff, SporePuff_sCrestBuffData, SporePuff_sCrestBuffEntry>(SporePuff_sCrest);
        }
        
        public static void HookOn()
        {
            On.Player.ctor += Player_ctor;
            On.Player.NewRoom += Player_NewRoom;
            On.Player.Update += Player_Update;
        }

        #region 玩家
        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!SporePuff_sCrestFeatures.TryGetValue(self, out _))
            {
                SporePuff_sCrest sporePuff_sCrest = new SporePuff_sCrest(self);
                SporePuff_sCrestFeatures.Add(self, sporePuff_sCrest);
            }
        }
        private static void Player_NewRoom(On.Player.orig_NewRoom orig, Player self, Room newRoom)
        {
            orig(self, newRoom);

            if (SporePuff_sCrestFeatures.TryGetValue(self, out var sporePuff_sCrest))
            {
                SporePuff_sCrestFeatures.Remove(self);
                sporePuff_sCrest.Destroy();
                SporePuff_sCrest newSporePuff_sCrest = new SporePuff_sCrest(self);
                SporePuff_sCrestFeatures.Add(self, newSporePuff_sCrest);
            }
            else
            {
                SporePuff_sCrest newSporePuff_sCrest = new SporePuff_sCrest(self);
                SporePuff_sCrestFeatures.Add(self, newSporePuff_sCrest);
            }
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig.Invoke(self, eu);

            if (SporePuff_sCrestFeatures.TryGetValue(self, out var sporePuff_sCrest))
            {
                sporePuff_sCrest.Update(eu);
            }
        }
        #endregion
    }

    internal class SporePuff_sCrest : CosmeticSprite
    {
        WeakReference<Player> ownerRef;
        Color c;
        int count;
        float sporeLife;

        public int Level
        {
            get
            {
                return SporePuff_sCrestBuffEntry.StackLayer;
            }
        }

        public Color Color
        {
            get
            {
                return this.c;
            }
            set
            {
                this.c = value;
            }
        }

        public SporePuff_sCrest(Player owner)
        {
            ownerRef = new WeakReference<Player>(owner);
            this.room = owner.room;
            this.count = 10;
            this.sporeLife = 0.8f + this.Level * 0.05f;
            this.Color = new Color(227f / 255f, 171f / 255f, 78f / 255f);
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (!ownerRef.TryGetTarget(out var player) || player.room == null || player.dead)
                return;
            if (count > 0)
                count--;
            else
            {
                count = 20;
                this.Explode(player);
            }
        }

        public void Explode(Player player)
        {
            InsectCoordinator smallInsects = null;
            for (int i = 0; i < this.room.updateList.Count; i++)
            {
                if (this.room.updateList[i] is InsectCoordinator)
                {
                    smallInsects = (this.room.updateList[i] as InsectCoordinator);
                    break;
                }
            }
            for (int j = 0; j < 35f * Mathf.Pow(this.Radius(this.Level) / this.Radius(1), 2); j++)//70
            {
                this.room.AddObject(new SporeCloud(player.firstChunk.pos, Custom.RNV() * Mathf.Pow(UnityEngine.Random.value, 0.3f) * this.Radius(this.Level) / 10f, this.Color, this.sporeLife, player.abstractCreature, j % 20, smallInsects));
                //this.room.AddObject(new SporeCloud(player.firstChunk.pos, Custom.RNV() * Mathf.Sqrt(UnityEngine.Random.value) * this.Radius(this.Level) / 10f, this.Color, this.sporeRadius, player.abstractCreature, j % 20, smallInsects));
            }
            //this.room.AddObject(new SporePuffVisionObscurer(player.firstChunk.pos));
        }

        public float Radius(int level)
        {
            return (3f + 1f * level) * 10f;
        }
    }
}
