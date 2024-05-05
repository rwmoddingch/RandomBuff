using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuffUtils.ParticleSystem
{
    public class Particle
    {
        public ParticleEmitter emitter;
        public string element;
        public string shader;

        public MoveType moveType;
        Vector2 _pos;
        public Vector2 pos
        {
            get
            {
                if (moveType == MoveType.Relative)
                    return emitter.pos + _pos;
                else
                    return _pos;
            }
            set
            {
                if(moveType == MoveType.Relative)
                    _pos = value - emitter.pos;
                else
                    _pos = value;
            }
        }
        public Vector2 lastPos;

        public Vector2 vel;
        public Vector2 setVel;

        public float setScale;
        public float scale;
        public float lastScale;

        public Color setColor;
        public Color color;
        public Color lastColor;

        public int life;
        float setLife;
        public float LifeParam => 1f - life / setLife;
        
        public bool inStage;
        public int spriteLayer = 8;
        public FSprite sprite;

        public void Init(ParticleEmitter emitter)
        {
            this.emitter = emitter;
            setVel = vel = Vector2.zero;
            setColor = color = lastColor = Color.white;
            HardSetScale(0f);
            _pos = lastPos = Vector2.zero;
            setLife = life = 1;
            inStage = false;
            spriteLayer = 8;
            element = string.Empty;
            shader = string.Empty;
            moveType = MoveType.Relative;
        }

        public virtual void InitSpritesAndAddToContainer()
        {
            if (inStage)
                return;

            inStage = true;
            if (sprite == null)
                sprite = new FSprite(element);
            else
                sprite.SetElementByName(element);

            if(!string.IsNullOrEmpty(shader))
                sprite.shader = Custom.rainWorld.Shaders[shader];

            emitter.system.Containers[spriteLayer].AddChild(sprite);
            //BuffUtils.Log("Particle", "InitSpritesAndAddToContainer");
        }

        public void Update()
        {
            if (life > 0)
                life--;
            else if (life == 0)
                Die();

            lastPos = pos;
            pos += vel;
            lastScale = scale;
            lastColor = color;
        }

        public virtual void DrawSprites(RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if(!inStage) return;

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            float smoothScale = Mathf.Lerp(lastScale, scale, timeStacker);
            Color smoothColor = Color.Lerp(lastColor, color, timeStacker);

            sprite.SetPosition(smoothPos - camPos);
            sprite.scale = smoothScale;
            sprite.color = smoothColor;
        }

        public virtual void ClearSprites()
        {
            if (!inStage)
                return;

            inStage = false;
            sprite.RemoveFromContainer();
        }

        public virtual void Die()
        {
            ClearSprites();
            emitter.OnParticleDieEvent?.Invoke(this);
            emitter.Particles.Remove(this);

            ParticlePool.RecycleParticle(this);
        }

        public void HardSetPos(Vector2 pos)
        {
            this.pos = pos;
            lastPos = this.pos;
        }
        public void HardSetColor(Color color)
        {
            this.color = color;
            lastColor = color;
            setColor = color;
        }
        public void HardSetScale(float scale)
        {
            this.scale = scale;
            lastScale = scale;
            setScale = scale;
        }
        public void SetLife(int life)
        {
            this.life = life;
            setLife = life;
        }

        public void SetVel(Vector2 vel)
        {
            this.vel = vel;
            this.setVel = vel;
        }


        public enum MoveType
        {
            Relative,
            Global
        }
    }
}
