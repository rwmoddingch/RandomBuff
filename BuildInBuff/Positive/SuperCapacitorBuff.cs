using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MoreSlugcats;
using RWCustom;
using RandomBuffUtils;
using Newtonsoft.Json;
using RandomBuff;

namespace BuiltinBuffs.Positive
{
    internal class SuperCapacitorBuff : Buff<SuperCapacitorBuff, SuperCapacitanceBuffData>
    {
        public override BuffID ID => SuperCapacitanceBuffEntry.SuperCapacitance;
    }

    class SuperCapacitanceBuffData : CountableBuffData
    {
        public override int MaxCycleCount => 5;
        public override BuffID ID => SuperCapacitanceBuffEntry.SuperCapacitance;

        [JsonProperty] public float energyRecord_1;
        [JsonProperty] public float energyRecord_2;
        [JsonProperty] public float energyRecord_3;
        [JsonProperty] public float energyRecord_4;
    }

    class SuperCapacitanceBuffEntry : IBuffEntry
    {
        public static BuffID SuperCapacitance = new BuffID("SuperCapacitance", true);
        public static ConditionalWeakTable<Player, BatteryModule> batteryModule = new ConditionalWeakTable<Player, BatteryModule>();

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<SuperCapacitorBuff, SuperCapacitanceBuffData, SuperCapacitanceBuffEntry>(SuperCapacitance);
        }

        public static void HookOn()
        {
            //On.Player.ctor += Player_ctor;
            On.Player.Update += Player_Update;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.Creature.Violence += Creature_Violence;
            On.Centipede.Shock += Centipede_Shock;
            On.DaddyLongLegs.Eat += DaddyLongLegs_Eat;
        }

        private static void DaddyLongLegs_Eat(On.DaddyLongLegs.orig_Eat orig, DaddyLongLegs self, bool eu)
        {
            bool flag = false;
            for (int i = 0; i < self.eatObjects.Count; i++)
            {
                if (self.eatObjects[i].chunk.owner is Player player && batteryModule.TryGetValue(player, out var battery))
                {
                    if (player.room != null && battery.energy > 0)
                    {
                        player.room.AddObject(new ZapCoil.ZapFlash(self.firstChunk.pos, Mathf.Clamp(2.5f * battery.energy, 1f, 10f)));
                        player.room.AddObject(new SimpleRangeDamage(self.room, Creature.DamageType.Electric, self.firstChunk.pos, 40f + 2f * battery.energy, 0.5f * battery.energy, 2f * battery.energy, self, 1f));
                        flag = true;
                    }
                }
            }
            if (flag) return;
            orig(self, eu);
        }

        private static void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            try
            {
                var buffData = BuffCore.GetBuffData(SuperCapacitance);
                if (buffData != null)
                {
                    for (int i = 0; i < self.Players.Count; i++)
                    {
                        if (self.Players[i].realizedCreature == null) continue;
                        switch (i)
                        {
                            case 0:
                                {
                                    if (batteryModule.TryGetValue(self.Players[0].realizedCreature as Player, out var data0))
                                    {
                                        (buffData as SuperCapacitanceBuffData).energyRecord_1 = data0.energy;
                                    }
                                    continue;
                                }
                            case 1:
                                {
                                    if (batteryModule.TryGetValue(self.Players[0].realizedCreature as Player, out var data1))
                                    {
                                        (buffData as SuperCapacitanceBuffData).energyRecord_2 = data1.energy;
                                    }
                                    continue;
                                }
                            case 2:
                                {
                                    if (batteryModule.TryGetValue(self.Players[0].realizedCreature as Player, out var data2))
                                    {
                                        (buffData as SuperCapacitanceBuffData).energyRecord_3 = data2.energy;
                                    }
                                    continue;
                                }
                            case 3:
                                {
                                    if (batteryModule.TryGetValue(self.Players[0].realizedCreature as Player, out var data3))
                                    {
                                        (buffData as SuperCapacitanceBuffData).energyRecord_4 = data3.energy;
                                    }
                                    continue;
                                }
                        }
                    }
                }               
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            orig(self, malnourished);
        }

        private static void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            batteryModule.Add(self, new BatteryModule(new EnergyIndicator(self, BatteryModule.Capacity(self.slugcatStats.name, self.isSlugpup))));
        }

        private static void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (!batteryModule.TryGetValue(self, out var data))
            {
                batteryModule.Add(self, new BatteryModule(new EnergyIndicator(self, BatteryModule.Capacity(self.slugcatStats.name, self.isSlugpup))));
                batteryModule.TryGetValue(self, out var module);
                var buffData = BuffCore.GetBuffData(SuperCapacitance);
                if (buffData != null)
                {
                    float capacity = BatteryModule.Capacity(self.slugcatStats.name, self.isSlugpup);
                    if (self.IsJollyPlayer)
                    {
                        switch (self.playerState.playerNumber)
                        {
                            case 1:
                                {
                                    module.Encharge((buffData as SuperCapacitanceBuffData).energyRecord_1, capacity);
                                    (buffData as SuperCapacitanceBuffData).energyRecord_1 = 0;
                                    break;
                                }
                            case 2:
                                {
                                    module.Encharge((buffData as SuperCapacitanceBuffData).energyRecord_2, capacity);
                                    (buffData as SuperCapacitanceBuffData).energyRecord_2 = 0;
                                    break;
                                }
                            case 3:
                                {
                                    module.Encharge((buffData as SuperCapacitanceBuffData).energyRecord_3, capacity);
                                    (buffData as SuperCapacitanceBuffData).energyRecord_3 = 0;
                                    break;
                                }
                            case 4:
                                {
                                    module.Encharge((buffData as SuperCapacitanceBuffData).energyRecord_4, capacity);
                                    (buffData as SuperCapacitanceBuffData).energyRecord_4 = 0;
                                    break;
                                }
                        }
                    }
                    else
                        module.Encharge((buffData as SuperCapacitanceBuffData).energyRecord_1, capacity);
                }                
            }

            if (batteryModule.TryGetValue(self, out var battery))
            {
                if (self.room != null && battery.energyIndicator != null && (battery.energyIndicator.room == null || battery.energyIndicator.room != self.room || battery.energyIndicator.slatedForDeletetion))
                {
                    battery.energyIndicator.Destroy();
                    battery.energyIndicator.slatedForDeletetion = false;                    
                    self.room.AddObject(battery.energyIndicator);
                }

                if (self.grabbedBy != null && self.grabbedBy.Count > 0)
                {
                    if (battery.energy > 0)
                    {
                        bool flag = false;
                        for (int i = self.grabbedBy.Count - 1 ; i >= 0; i--)
                        {
                            if (self.grabbedBy.Count == 0 || self.grabbedBy[0].grabber is Centipede || self.grabbedBy[0].grabber is Player)
                            {
                                flag = true;
                                continue;
                            }

                            if (self.room != null && !flag)
                            {
                                //self.grabbedBy[0].grabber.Violence(self.firstChunk, null, self.grabbedBy[0].grabber.firstChunk, null, Creature.DamageType.Electric, 0.5f * battery.energy, 1f);
                                self.room.AddObject(new CreatureSpasmer(self.grabbedBy[i].grabber, false, (int)(2f * battery.energy)));
                            }                      
                        }

                        if (!flag)
                        {
                            if (self.room != null)
                            {
                                self.room.AddObject(new ZapCoil.ZapFlash(self.firstChunk.pos, Mathf.Clamp(2.5f * battery.energy, 1f, 10f)));
                                self.room.AddObject(new SimpleRangeDamage(self.room, Creature.DamageType.Electric, self.firstChunk.pos, 40f + 2f * battery.energy, 0.5f * battery.energy, 2f * battery.energy, self, 1f));
                                self.room.PlaySound(SoundID.Zapper_Zap, self.firstChunk.pos, 1f, 1.5f + UnityEngine.Random.value * 1.5f);
                            }
                            battery.energy = 0f;
                        }
                    }
                }
            }

        }

        private static void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, UnityEngine.Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (type == Creature.DamageType.Electric && self is Player player && batteryModule.TryGetValue(player, out var battery))
            {
                if (source == null || source.owner != player)
                {
                    battery.Encharge(damage, BatteryModule.Capacity(player.slugcatStats.name, player.isSlugpup));
                }               
                return;
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void Centipede_Shock(On.Centipede.orig_Shock orig, Centipede self, PhysicalObject shockObj)
        {
            if (shockObj is Player player && batteryModule.TryGetValue(player, out var battery))
            {
                if (self.room != null)
                {
                    if (self.graphicsModule != null)
                    {
                        (self.graphicsModule as CentipedeGraphics).lightFlash = 1f;
                        for (int i = 0; i < (int)Mathf.Lerp(4f, 8f, self.size); i++)
                        {
                            self.room.AddObject(new Spark(self.HeadChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
                        }
                    }

                    if (shockObj.Submersion > 0f)
                    {
                        self.room.AddObject(new UnderwaterShock(self.room, self, self.HeadChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, self.size),
                            self.AquaCenti ? 2f : 0.2f + 1.9f * self.size, self, new Color(0.7f, 0.7f, 1f)));
                    }

                    //if (ModManager.MSC) self.room.PlaySound(MoreSlugcatsEnums.MSCSoundID.Core_On, shockObj.firstChunk.pos, 1f, 1f);
                    self.room.PlaySound(SoundID.Centipede_Shock, shockObj.firstChunk.pos);
                }

                for (int j = 0; j < self.bodyChunks.Length; j++)
                {
                    self.bodyChunks[j].vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                    self.bodyChunks[j].pos += Custom.RNV() * 6f * UnityEngine.Random.value;
                }
                for (int k = 0; k < shockObj.bodyChunks.Length; k++)
                {
                    shockObj.bodyChunks[k].vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                    shockObj.bodyChunks[k].pos += Custom.RNV() * 6f * UnityEngine.Random.value;
                }

                float capacity = BatteryModule.Capacity(player.slugcatStats.name, player.isSlugpup);
                if (self.AquaCenti)
                {
                    battery.Encharge(2f, capacity);
                }
                else
                {
                    battery.Encharge(Mathf.Lerp(0.042857144f, 0.323529422f, self.bodyChunks.Length * Mathf.Pow(self.size, 1.4f)), capacity);
                }

                self.shockGiveUpCounter = 30;
                self.LoseAllGrasps();
                return;
            }
            orig(self, shockObj);
        }
    }

    public class BatteryModule
    {
        public float energy;
        public EnergyIndicator energyIndicator;

        public BatteryModule(EnergyIndicator energyIndicator)
        {
            this.energyIndicator = energyIndicator;
        }

        public void Encharge(float extraEnergy, float capacity)
        {
            energy = Mathf.Clamp(energy + extraEnergy, 0, capacity);
        }

        public static float Capacity(SlugcatStats.Name name, bool isPup)
        {
            float num = 30;
            if (name == MoreSlugcatsEnums.SlugcatStatsName.Gourmand) num *= 1.5f;
            if (name == MoreSlugcatsEnums.SlugcatStatsName.Saint) num *= 0.5f;
            if (name == SlugcatStats.Name.Yellow) num *= 0.8f;
            if (isPup) { num *= 0.5f; }
            return num;
        }
    }

    public class EnergyIndicator : CosmeticSprite
    {
        public Player bindPlayer;
        public float energy;
        public float capacity;
        public bool pupState;

        public EnergyIndicator(Player player, float capacity = 30f, float energy = 0f)
        {
            bindPlayer = player;
            this.energy = energy;
            this.capacity = capacity;
            this.pupState = bindPlayer.isSlugpup;
        }

        public Color DynamicColor()
        {
            return Custom.HSL2RGB(0.5f * energy / capacity, 1f, 0.5f);
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[4];
            sLeaser.sprites[0] = new FSprite("pixel");
            sLeaser.sprites[0].scaleX = 3f;
            sLeaser.sprites[0].scaleY = 5f;
            sLeaser.sprites[0].anchorX = 1f;
            sLeaser.sprites[1] = new FSprite("pixel");
            sLeaser.sprites[1].scaleX = 20f;
            sLeaser.sprites[1].scaleY = 10f;
            sLeaser.sprites[1].anchorX = 0f;
            sLeaser.sprites[2] = new FSprite("pixel");
            sLeaser.sprites[2].scaleY = 10f;
            sLeaser.sprites[2].anchorX = 0f;
            sLeaser.sprites[3] = new FSprite("pixel");
            sLeaser.sprites[3].scaleY = 10f;
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            FContainer fContainer = rCam.ReturnFContainer("Bloom");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                fContainer.AddChild(sLeaser.sprites[i]);
            }
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = DynamicColor();
            sLeaser.sprites[2].color = DynamicColor();
            sLeaser.sprites[1].color = new Color(0.2f, 0.2f, 0.2f);
            sLeaser.sprites[3].color = Color.white;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (bindPlayer != null && !slatedForDeletetion)
            {
                Vector2 pos = Vector2.Lerp(bindPlayer.firstChunk.lastPos, bindPlayer.firstChunk.pos, timeStacker) + new Vector2(-10f, 30f) - camPos;
                for (int i = 0; i < sLeaser.sprites.Length; i++)
                {
                    if (i < 3)
                    {
                        sLeaser.sprites[i].SetPosition(pos);
                        if (i == 0 || i == 2)
                        {
                            sLeaser.sprites[i].color = DynamicColor();
                        }
                    }
                    else
                    {
                        sLeaser.sprites[i].SetPosition(pos + 20f * Vector2.right);
                    }
                }
                sLeaser.sprites[2].scaleX = Mathf.Lerp(0f, 20f, energy / capacity);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (bindPlayer == null || room == null || slatedForDeletetion)
            {
                Destroy();
                return;
            }

            if (bindPlayer.room == null || bindPlayer.room != room)
            {
                room = null;
                Destroy();
                return;
            }            

            if (SuperCapacitanceBuffEntry.batteryModule.TryGetValue(bindPlayer, out var battery))
            {
                energy = battery.energy;
                if (pupState != bindPlayer.isSlugpup)
                {
                    pupState = bindPlayer.isSlugpup;
                    capacity = BatteryModule.Capacity(bindPlayer.slugcatStats.name, bindPlayer.isSlugpup);
                }
            }

        }
    }
}
