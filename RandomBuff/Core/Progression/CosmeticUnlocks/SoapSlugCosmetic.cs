using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using static RandomBuffUtils.PlayerUtils.PlayerModuleGraphicPart;
using static RandomBuffUtils.PlayerUtils;
using RandomBuffUtils;
using MoreSlugcats;
using System.Runtime.CompilerServices;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class SoapSlugCosmetic : CosmeticUnlock
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.SoapSlug;

        public override string IconElement => "BuffCosmetic_SoapSlug";

        public override SlugcatStats.Name BindCat => MoreSlugcatsEnums.SlugcatStatsName.Rivulet;

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(new SoapSlugUtils());
        }
    }

    public class SoapSlugUtils : IOWnPlayerUtilsPart
    {
        public PlayerModulePart InitPart(PlayerModule module)
        {
            return new SoapSlugModule();
        }

        public PlayerModuleGraphicPart InitGraphicPart(PlayerModule module)
        {
            return null;
        }
    }

    public class SoapSlugModule : PlayerUtils.PlayerModulePart
    {
        public float coolDown = 100f;
        public bool bursting;
        public override void Update(Player player, bool eu)
        {
            base.Update(player, eu);
            if (player.animation == Player.AnimationIndex.Flip || player.animation == Player.AnimationIndex.BellySlide
                || player.animation == Player.AnimationIndex.RocketJump)
            {
                bursting = true;
            }
            else bursting = false;

            if (coolDown > 0)
            {
                if (UnityEngine.Random.value < 0.025f)
                {
                    coolDown = 0;
                }
                else
                {
                    float num = UnityEngine.Random.Range(0.625f, 1.25f);
                    coolDown -= bursting ? 50f : num;
                }                
            }
            else
            {
                if (player.room != null)
                {
                    bool flag = false;
                    bool flag2 = false;
                    Vector2 initVel;
                    if (player.animation == Player.AnimationIndex.DeepSwim || player.bodyMode == Player.BodyModeIndex.ZeroG)
                    {
                        initVel = 5f * Custom.RNV();
                    }
                    else if (player.animation == Player.AnimationIndex.Flip)
                    {
                        initVel = 10f * player.flipDirection * Custom.PerpendicularVector(player.mainBodyChunk.vel).normalized;
                        flag = true;
                        flag2 = true;
                    }
                    else if (player.animation == Player.AnimationIndex.BellySlide || player.animation == Player.AnimationIndex.RocketJump)
                    {
                        initVel = 10f * -player.mainBodyChunk.vel.normalized;
                        flag = true;
                    }
                    else
                    {
                        initVel = 5f * Custom.DegToVec(UnityEngine.Random.Range(-60, 61));
                    }

                    if (flag)
                    {
                        ShootBubbles(player.room, player.mainBodyChunk.pos, 0.5f * initVel);
                        if (flag2)
                        {
                            ShootBubbles(player.room, player.mainBodyChunk.pos, 0.25f * initVel);
                        }
                    }
                    player.room.AddObject(SoapBubblePool.GetBubble(player.room, player.mainBodyChunk.pos, initVel, bursting));                   
                    coolDown = 100f;
                }
            }
        }

        public void ShootBubbles(Room room, Vector2 initPos, Vector2 initVel)
        {
            if (room != null)
            {
                room.AddObject(SoapBubblePool.GetBubble(room, initPos, initVel, bursting, true));
            }
        }
    }

    public class SoapBubble : UpdatableAndDeletable, IDrawable
    {
        public float hue;
        public float life;
        public float fragile;
        public float maxRad;
        public int inactiveLife;
        public bool burstMode;
        public bool shootMode;
        public bool lastActive;
        public bool active;
        public Vector2 lastPos;
        public Vector2 pos;
        public Vector2 vel;
        public bool Active
        {
            set
            {
                if (active != value)
                {
                    lastActive = !value;
                }
                else
                {
                    lastActive = value;
                }
                active = value;

                if (value)
                {
                    inactiveLife = 0;
                }
            }
        }

        public SoapBubble(Room room, Vector2 initPos, Vector2 initVel, bool burstMode = false, bool shootMode = false)
        {
            life = 1f;
            this.burstMode = burstMode;
            this.shootMode = shootMode;
            InitRandomBubble();
            this.room = room;
            lastActive = true;
            active = true;
            pos = initPos;
            lastPos = pos;
            vel = initVel;
            
        }

        public void InitRandomBubble()
        {
            fragile = shootMode ? 0.025f : (burstMode ? 0.025f : UnityEngine.Random.Range(0.00625f, 0.0125f));
            float num = shootMode ? 0.8f : (burstMode ? 1.4f : 0.6f);
            float num2 = shootMode ? 0.1f : 0.4f;
            if (shootMode) maxRad = num + num2;
            else maxRad = num2 + num * (1f + UnityEngine.Random.Range(-0.4f, 0.4f));
            hue = UnityEngine.Random.Range(0.5f, 0.72f);
        }

        public void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White");
            sLeaser.sprites[0].shader = rCam.game.rainWorld.Shaders["WaterNut"];
            this.AddToContainer(sLeaser, rCam, null);
        }

        public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (slatedForDeletetion)
            {
                sLeaser.CleanSpritesAndRemove();
                return;
            }
            if (!active || life <= 0f)
            {
                sLeaser.RemoveAllSpritesFromContainer();
                return;
            }
            if (!lastActive && active)
            {
                this.Active = true;
                this.AddToContainer(sLeaser, rCam, null);
            }

            sLeaser.sprites[0].SetPosition(Vector2.Lerp(this.lastPos, this.pos, timeStacker) - camPos);            
            sLeaser.sprites[0].scale = maxRad * Mathf.Lerp(0.2f, 1f, 1 - life);

        }

        public void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sLeaser.sprites[0].color = Custom.HSL2RGB(hue, 0.75f, shootMode ? 0.85f : 0.7f);
        }

        public void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
            {
                rCam.ReturnFContainer("Water").AddChild(sLeaser.sprites[0]);
            }
            else
            {
                newContatiner.AddChild(sLeaser.sprites[0]);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);
            if (slatedForDeletetion) return;
            if (!active)
            {
                /*
                if (inactiveLife < 1200)
                {
                    inactiveLife++;
                }
                */
                return;
            }
            life -= fragile;
            if (this.room == null || !this.room.ViewedByAnyCamera(pos, 0f) || life <= 0f)
            {
                if (this.room != null && this.room.ViewedByAnyCamera(pos, 0f))
                {
                    this.room.AddObject(SoapBubblePool.GetExplosionSpike(this.room, pos, 15f * maxRad));
                }
                SoapBubblePool.RecycleBubble(this);                
                return;
            }

            lastPos = pos;
            pos += vel;
            if (!shootMode)
            {
                vel += Vector2.up * 0.1f * (2 - this.room.gravity);
                if (!burstMode)
                {
                    vel += 0.5f * Custom.RNV() * (1f - life);
                }
            }

            vel *= this.room.PointSubmerged(pos) ? 0.8f : 0.9f;
        }

        public override void Destroy()
        {
            base.Destroy();
            SoapBubblePool.RecycleBubble(this);
        }
    }

    public class StoredExplosionSpike
    {
        public ExplosionSpikes explosionSpikes;
        public int inactiveTime;        

        public StoredExplosionSpike(ExplosionSpikes explosionSpikes)
        {
            this.explosionSpikes = explosionSpikes;
        }
    }

    public static class SoapBubblePool
    {
        public static List<SoapBubble> soapBubbles = new List<SoapBubble>();
        public static ConditionalWeakTable<ExplosionSpikes, StoredExplosionSpike> explosionSpikeData = new ConditionalWeakTable<ExplosionSpikes, StoredExplosionSpike>();
        public static List<ExplosionSpikes> explosionSpikes = new List<ExplosionSpikes>();
        
        public static void Hook()
        {
            On.ExplosionSpikes.ctor += ExplosionSpikes_ctor;
            On.UpdatableAndDeletable.Destroy += UpdatableAndDeletable_Destroy;            
        }
        
        private static void UpdatableAndDeletable_Destroy(On.UpdatableAndDeletable.orig_Destroy orig, UpdatableAndDeletable self)
        {
            orig(self);
            if (self is ExplosionSpikes)
            {
                //UnityEngine.Debug.Log("Recycle explosion spikes");
                RecycleExplosionSpike(self as ExplosionSpikes);
            }
        }

        private static void ExplosionSpikes_ctor(On.ExplosionSpikes.orig_ctor orig, ExplosionSpikes self, Room room, Vector2 pos, int _spikes, float innerRad, float lifeTime, float width, float length, Color color)
        {
            orig(self, room, pos, _spikes, innerRad, lifeTime, width, length, color);
            explosionSpikeData.Add(self, new StoredExplosionSpike(self));
        }

        public static void UpdateInactiveItems()
        {
            try
            {
                if(soapBubbles.Count > 0)
                {
                    for(int i = soapBubbles.Count - 1; i >= 0; i--)
                    {
                        soapBubbles[i].inactiveLife++;
                        if (soapBubbles[i].inactiveLife >= 1200)
                        {
                            soapBubbles.RemoveAt(i);
                        }
                    }
                }
                if (explosionSpikes.Count > 0)
                {
                    for (int i = explosionSpikes.Count - 1; i >= 0; i--)
                    {
                        explosionSpikeData.TryGetValue(explosionSpikes[i], out var storedExplosionSpike);
                        storedExplosionSpike.inactiveTime++;
                        if (storedExplosionSpike.inactiveTime >= 1200)
                        {                          
                            explosionSpikes.RemoveAt(i);
                        }
                    }
                }       
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
            }
            
        }

        public static SoapBubble GetBubble(Room room, Vector2 initPos, Vector2 initVel, bool burstMode = false, bool shootMode = false)
        {
            if (soapBubbles.Count == 0)
            {
                return new SoapBubble(room, initPos, initVel, burstMode);
            }

            SoapBubble soapBubble = soapBubbles.Pop();
            if (soapBubble.inactiveLife >= 1200)
            {
                soapBubble.Destroy();
                return new SoapBubble(room, initPos, initVel, burstMode);
            }
            soapBubble.life = 1f;
            soapBubble.pos = initPos;
            soapBubble.lastPos = initPos;
            soapBubble.vel = initVel;
            soapBubble.burstMode = burstMode;
            soapBubble.shootMode = shootMode;
            soapBubble.InitRandomBubble();
            soapBubble.slatedForDeletetion = false;
            soapBubble.Active = true;
            //UnityEngine.Debug.Log("Spawn Stinky Bubble");
            return soapBubble;
        }

        public static ExplosionSpikes GetExplosionSpike(Room room, Vector2 pos, float length, int _spikes = 4, float lifeTime = 8f, float innerRad = 5f, float width = 4.5f)
        {
            if (explosionSpikes.Count == 0)
            {
                return new ExplosionSpikes(room, pos, _spikes, innerRad, lifeTime, width, length, new Color(1f, 1f, 1f, 0.5f));
            }
            else
            {
                ExplosionSpikes _explosionSpikes = explosionSpikes.Pop();
                explosionSpikeData.TryGetValue(_explosionSpikes, out var storedExplosionSpike);
                if (storedExplosionSpike.inactiveTime >= 1200f)
                {
                    explosionSpikeData.Remove(_explosionSpikes);
                    return new ExplosionSpikes(room, pos, _spikes, innerRad, lifeTime, width, length, new Color(1f, 1f, 1f, 0.5f));
                }
                _explosionSpikes.time = 0;
                _explosionSpikes.room = room;
                _explosionSpikes.pos = pos;
                _explosionSpikes.lastPos = pos;
                for (int i = 0; i < _explosionSpikes.spikes; i++)
                {
                    float num = UnityEngine.Random.value * 360f;
                    float num2 = (float)i / (float)_explosionSpikes.spikes * 360f + num;
                    _explosionSpikes.dirs[i] = Custom.DegToVec(num2 + Mathf.Lerp(-0.5f, 0.5f, UnityEngine.Random.value) * 360f / (float)_explosionSpikes.spikes);
                    if (room.GetTile(pos + _explosionSpikes.dirs[i] * (innerRad + length * 0.4f)).Solid)
                    {
                        _explosionSpikes.values[i, 0] = length * Mathf.Lerp(0.6f, 1.4f, UnityEngine.Random.value) * 0.5f;
                    }
                    else
                    {
                        _explosionSpikes.values[i, 0] = length * Mathf.Lerp(0.6f, 1.4f, UnityEngine.Random.value);
                    }                    
                }
                _explosionSpikes.slatedForDeletetion = false;
                //UnityEngine.Debug.Log("Spawn stinky explosion spikes");
                return _explosionSpikes;
            }
        }

        public static void RecycleBubble(SoapBubble soapBubble)
        {
            //UnityEngine.Debug.Log("Recycle Bubble");
            soapBubble.Active = false;
            soapBubble.RemoveFromRoom();
            soapBubbles.Add(soapBubble);
        }

        public static void RecycleExplosionSpike(ExplosionSpikes _explosionSpikes)
        {
            _explosionSpikes.RemoveFromRoom();
            explosionSpikes.Add(_explosionSpikes);
        }
    }
}
