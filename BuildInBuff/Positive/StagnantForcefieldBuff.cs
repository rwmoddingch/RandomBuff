using Mono.Cecil.Cil;
using MonoMod.Cil;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class StagnantForcefieldBuff : Buff<StagnantForcefieldBuff, StagnantForcefieldBuffData>, PlayerUtils.IOWnPlayerUtilsPart
    {
        public static int maxStagnantCount = 40 * 10;
        public static float rad = 160f;
        public override BuffID ID => StagnantForcefieldBuffEntry.stagnantForcefieldBuffID;
        public override bool Triggerable => stagnantCount == 0;

        public int stagnantCount;

        public StagnantForcefieldBuff()
        {
            PlayerUtils.AddPart(this);
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if(stagnantCount > 0)
            {
                stagnantCount--;
            }
        }

        public override void Destroy()
        {
            PlayerUtils.RemovePart(this);
        }

        public override bool Trigger(RainWorldGame game)
        {
            stagnantCount = maxStagnantCount;
            return false;
        }

        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            return new StagnantForcefieldPlayerModule();
        }

        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }
    }

    internal class StagnantForcefieldPlayerModule : PlayerUtils.PlayerModulePart
    {
        int StagnantCount => StagnantForcefieldBuff.Instance.stagnantCount;

        ParticleEmitter stagnantForceFieldEmitter;

        public override void Update(Player player, bool eu)
        {
            base.Update(player, eu);

            if(StagnantCount > 0)
            {
                if(stagnantForceFieldEmitter == null)
                {
                    if(player.room != null)
                    {
                        CreateStagnantForcefieldEmitter(player);
                    }
                }
                else if(stagnantForceFieldEmitter.slateForDeletion)
                {
                    stagnantForceFieldEmitter = null;
                }
            }
            else if(stagnantForceFieldEmitter != null)
            {
                stagnantForceFieldEmitter.Die();
                stagnantForceFieldEmitter = null;
            }
        }

        void CreateStagnantForcefieldEmitter(Player player)
        {
            var emitter = new ParticleEmitter(player.room);

            emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, player));
            emitter.ApplyEmitterModule(new SetEmitterLife(emitter, StagnantCount, false));

            emitter.ApplyParticleSpawn(new BurstSpawnerModule(emitter, 160));

            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", alpha:0.5f)));
            emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("pixel", "", constCol:Color.white)));
            emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Relative));
            emitter.ApplyParticleModule(new SetRandomLife(emitter, StagnantForcefieldBuff.maxStagnantCount - 40, StagnantForcefieldBuff.maxStagnantCount));
            emitter.ApplyParticleModule(new StagnantForceFieldSetStartLife(emitter, this));
            emitter.ApplyParticleModule(new SetConstColor(emitter, Color.blue * 0.3f + Color.white * 0.7f));
            emitter.ApplyParticleModule(new SetRandomScale(emitter, 2f, 2.5f));
            emitter.ApplyParticleModule(new PositionOverLife(emitter,
                (p, l) =>
                {
                    Vector2 dir = Custom.DegToVec(p.randomParam1 * 360f);
                    float radParam = (1f - Mathf.Pow(p.randomParam2, 4)) * 0.4f + 0.6f;
                    if (l < 0.8f)
                    {
                        return dir * StagnantForcefieldBuff.rad * radParam * Mathf.Min(1f, Helper.LerpEase(l * 5f));
                    }
                    else
                    {
                        float t = Mathf.Pow((l - 0.8f) * 5f, 4f);
                        return dir * StagnantForcefieldBuff.rad * radParam + Vector2.down * t * Mathf.Lerp(10f, 120f, p.randomParam3);
                    }
                }));

            emitter.ApplyParticleModule(new AlphaOverLife(emitter, 
                (p, l) =>
                {
                    if(l < 0.2f)
                    {
                        return l * 5f;
                    }
                    else if(l > 0.8f)
                    {
                        return 1f - (l - 0.8f) * 5f;
                    }
                    return 1f;
                }));
            emitter.ApplyParticleModule(new StagnantForceFieldBlink(emitter));

            ParticleSystem.ApplyEmitterAndInit(emitter);
            stagnantForceFieldEmitter = emitter;
        }

        public class StagnantForceFieldSetStartLife : EmitterModule, IParticleInitModule
        {
            StagnantForcefieldPlayerModule playerModule;
            public StagnantForceFieldSetStartLife(ParticleEmitter emitter, StagnantForcefieldPlayerModule module) : base(emitter)
            {
                this.playerModule = module;
            }

            public void ApplyInit(Particle particle)
            {
                particle.life = playerModule.StagnantCount;
            }
        }

        public class StagnantForceFieldBlink : EmitterModule, IParticleUpdateModule, IOwnParticleUniqueData
        {
            float easeOutRate;
            public StagnantForceFieldBlink(ParticleEmitter emitter, float easeOutRate = 0.02f) : base(emitter)
            {
                this.easeOutRate = easeOutRate;
            }

            public void ApplyUpdate(Particle particle)
            {
                var uniqueData = particle.GetUniqueData<BlinkParam>(this);
                if (uniqueData == null)
                    return;

                if(Random.value < easeOutRate/ 2f)
                {
                    uniqueData.blinkParam = 1f;
                }
                else
                {
                    if (uniqueData.blinkParam > 0f)
                        uniqueData.blinkParam -= easeOutRate;
                }

                particle.alpha *= uniqueData.blinkParam;
            }

            public Particle.ParticleUniqueData GetUniqueData(Particle particle)
            {
                return new BlinkParam();
            }

            public class BlinkParam : Particle.ParticleUniqueData
            {
                public float blinkParam;
            }
        }
    }

    internal class StagnantForcefieldBuffData : BuffData
    {
        public override BuffID ID => StagnantForcefieldBuffEntry.stagnantForcefieldBuffID;
    }

    internal class StagnantForcefieldBuffEntry : IBuffEntry
    {
        public static BuffID stagnantForcefieldBuffID = new BuffID("StagnantForcefield", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<StagnantForcefieldBuff, StagnantForcefieldBuffData, StagnantForcefieldBuffEntry>(stagnantForcefieldBuffID);
        }

        public static void HookOn()
        {
            IL.BodyChunk.Update += BodyChunk_Update;
        }

        private static void BodyChunk_Update(MonoMod.Cil.ILContext il)
        {
            ILCursor c1 = new ILCursor(il);
            if (c1.TryGotoNext(MoveType.After,
                (i) => i.MatchLdarg(0),
                (i) => i.MatchLdflda<BodyChunk>("vel"),
                (i) => i.MatchLdflda<Vector2>("y"),
                (i) => i.MatchDup(),
                (i) => i.MatchLdindR4(),
                (i) => i.MatchLdarg(0),
                (i) => i.MatchCall<BodyChunk>("get_owner"),
                (i) => i.MatchCallvirt<PhysicalObject>("get_gravity"),
                (i) => i.MatchSub(),
                (i) => i.MatchStindR4()))
            {
                c1.Emit(OpCodes.Ldarg_0);
                c1.EmitDelegate<Action<BodyChunk>>((self) =>
                {
                    if (StagnantForcefieldBuff.Instance == null || StagnantForcefieldBuff.Instance.stagnantCount == 0)
                        return;

                    if (self.owner.room == null)
                        return;

                    bool insideAnyPlayerRange = false;
                    var playerLst = self.owner.room.game.Players;
                    foreach(var player in playerLst)
                    {
                        if (player.realizedCreature == null)
                            continue;
                        if (self.owner == player.realizedCreature)
                            return;

                        if(Vector2.Distance(self.pos, player.realizedCreature.mainBodyChunk.pos) < StagnantForcefieldBuff.rad)
                            insideAnyPlayerRange = true;
                    }

                    if (insideAnyPlayerRange)
                    {
                        Vector2 velDir = self.vel.normalized;
                        float magnitude = Mathf.Min(self.vel.magnitude, 5f);
                        self.vel = velDir * magnitude;
                    }
                });
            }
        }
    }
}
