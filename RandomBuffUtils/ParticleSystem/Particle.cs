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
        
        public List<SpriteInitParam> spriteInitParams = new List<SpriteInitParam>();

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

        public float scale
        {
            get => scaleXY.x;
            set
            {
                scaleXY.x = value;
                scaleXY.y = value;
            }
        }

        public Vector2 scaleXY;
        public Vector2 setScaleXY;
        public Vector2 lastScaleXY;

        public Color setColor;
        public Color color;
        public Color lastColor;

        public float rotation;
        public float lastRotation;
        public float setRotation;

        public int life;
        float setLife;
        public float LifeParam => 1f - life / setLife;
        
        public bool inStage;
        public int spriteLayer = 8;
        public FSprite[] sprites = new FSprite[0];

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
            spriteInitParams.Clear();
            moveType = MoveType.Relative;
        }

        public virtual void InitSpritesAndAddToContainer()
        {
            if (inStage)
                return;

            inStage = true;

            if (sprites.Length != spriteInitParams.Count)
            {
                Array.Resize(ref sprites, spriteInitParams.Count);
                for(int i = 0; i< sprites.Length; i++)
                {
                    if (sprites[i] == null)
                        sprites[i] = new FSprite("pixel");
                }
            }

            for(int i = 0;i < spriteInitParams.Count;i++)
            {
                sprites[i].SetElementByName(spriteInitParams[i].element);
                if (!string.IsNullOrEmpty(spriteInitParams[i].shader))
                    sprites[i].shader = Custom.rainWorld.Shaders[spriteInitParams[i].shader];
                else
                    sprites[i].shader = Custom.rainWorld.Shaders["Basic"];
                emitter.system.Containers[spriteLayer].AddChild(sprites[i]);
            }

            
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
            lastScaleXY = scaleXY;
            lastColor = color;
            lastRotation = rotation;
        }

        public virtual void DrawSprites(RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if(!inStage) return;

            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);
            Vector2 smoothScaleXY = Vector2.Lerp(lastScaleXY, scaleXY, timeStacker);
            Color smoothColor = Color.Lerp(lastColor, color, timeStacker);
            float smoothRotation = Mathf.Lerp(lastRotation, rotation, timeStacker);

            for(int i = 0; i < sprites.Length; i++)
            {
                sprites[i].SetPosition(smoothPos - camPos);
                sprites[i].scaleX = smoothScaleXY.x * spriteInitParams[i].scale;
                sprites[i].scaleY = smoothScaleXY.y * spriteInitParams[i].scale;
                sprites[i].color = smoothColor;
                sprites[i].alpha = spriteInitParams[i].alpha;
                sprites[i].rotation = smoothRotation;
            }
        }

        public virtual void ClearSprites()
        {
            if (!inStage)
                return;

            inStage = false;
            for (int i = 0; i < sprites.Length; i++)
                sprites[i].RemoveFromContainer();
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
            HardSetScale(new Vector2(scale, scale));
        }

        public void HardSetScale(Vector2 scale)
        {
            setScaleXY = scale;
            scaleXY = scale;
            lastScaleXY = scale;
        }

        public void HardSetRotation(float rotation)
        {
            this.rotation = setRotation = lastRotation = rotation;
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

        public struct SpriteInitParam
        {
            public string element;
            public string shader;
            public float alpha;
            public float scale;
            public SpriteInitParam(string element, string shader, float alpha = 1f, float scale = 1f)
            {
                this.element = element;
                this.shader = shader;
                this.alpha = alpha;
                this.scale = scale;
            }
        }
    }
}
