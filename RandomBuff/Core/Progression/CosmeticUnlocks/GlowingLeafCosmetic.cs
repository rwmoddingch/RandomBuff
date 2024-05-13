using RandomBuffUtils;
using RandomBuffUtils.ParticleSystem;
using RandomBuffUtils.ParticleSystem.EmitterModules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Core.Progression.CosmeticUnlocks
{
    internal class GlowingLeafCosmetic : CosmeticUnlock, PlayerUtils.IOWnPlayerUtilsPart
    {
        public override CosmeticUnlockID UnlockID => CosmeticUnlockID.GlowingLeaf;

        public override string IconElement => "BuffCosmetic_GlowingLeaf";

        public override SlugcatStats.Name BindCat => SlugcatStats.Name.Yellow;

        public PlayerUtils.PlayerModuleGraphicPart InitGraphicPart(PlayerUtils.PlayerModule module)
        {
            return null;
        }

        public PlayerUtils.PlayerModulePart InitPart(PlayerUtils.PlayerModule module)
        {
            if (module.Name == BindCat)
                return new LeafModule();
            return null;
        }

        public override void StartGame(RainWorldGame game)
        {
            base.StartGame(game);
            PlayerUtils.AddPart(this);

            BuffPlugin.Log("GlowingLeafCosmetic StartGame");
        }

        public class LeafModule : PlayerUtils.PlayerModulePart
        {
            ParticleEmitter emitter;

            public LeafModule()
            {
                BuffPlugin.Log("LeafModule init");
            }

            public override void Update(Player player, bool eu)
            {
                base.Update(player, eu);
                if(emitter == null || emitter.slateForDeletion)
                {
                    if (player.room == null)
                        return;
                    CreateEmitter(player);
                }
            }

            void CreateEmitter(Player player)
            {
                emitter = new ParticleEmitter(player.room);
                emitter.ApplyEmitterModule(new RateSpawnerModule(emitter, 40, 2));
                emitter.ApplyEmitterModule(new SetEmitterLife(emitter, 100, true, false));
                emitter.ApplyEmitterModule(new BindEmitterToPhysicalObject(emitter, player, 1));

                emitter.ApplyParticleModule(new AddElement(emitter, new Particle.SpriteInitParam("Futile_White", "FlatLight", scale: 2f)));

                Particle.SpriteInitParam[] leafs = new Particle.SpriteInitParam[5];
                for (int i = 0; i < leafs.Length; i++)
                    leafs[i] = new Particle.SpriteInitParam("Leaf" + i.ToString(), "", constCol: Color.white);
                emitter.ApplyParticleModule(new AddElement(emitter, leafs));

                emitter.ApplyParticleModule(new SetMoveType(emitter, Particle.MoveType.Global));
                emitter.ApplyParticleModule(new SetRandomPos(emitter, 40f));
                emitter.ApplyParticleModule(new SetRandomLife(emitter, 70, 80));
                emitter.ApplyParticleModule(new SetRandomRotation(emitter, 0, 360f));
                emitter.ApplyParticleModule(new SetConstColor(emitter, Helper.GetRGBColor(255, 192, 0)));
                emitter.ApplyParticleModule(new SetVelociyFromEmitter(emitter, 0.1f));
                emitter.ApplyParticleModule(new SetRandomScale(emitter, 0f, 0f));

                emitter.ApplyParticleModule(new ConstantAcc(emitter, new Vector2(0, -0.15f)));
                emitter.ApplyParticleModule(new ScaleOverLife(emitter, (particle, lifeParam) =>
                {
                    float t = Mathf.Max(0f, Mathf.Min(lifeParam * 5f, 1f - lifeParam));
                    return Mathf.Lerp(1f, 1.5f, particle.randomParam) * t;
                }));

                emitter.ApplyParticleModule(new ColorOverLife(emitter, (particle, lifeParam) =>
                {
                    float t = Mathf.Max(0f, Mathf.Min(lifeParam * 5f, 1f - lifeParam));
                    Color result = particle.setColor;
                    result.a = t;
                    return result;
                }));

                emitter.ApplyParticleModule(new VelocityOverLife(emitter, (particle, lifeParam) =>
                {
                    return particle.setVel * (1f - lifeParam) + new Vector2(0f, -0.1f);
                }));

                emitter.ApplyParticleModule(new RotationOverLife(emitter, (p, l) =>
                {
                    return p.setRotation + l * 180f * (p.randomParam * 2f - 1f);
                }));

                ParticleSystem.ApplyEmitterAndInit(emitter);
            }
        }
    }
}
