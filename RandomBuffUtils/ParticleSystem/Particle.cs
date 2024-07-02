using RandomBuffUtils.ParticleSystem.EmitterModules;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RandomBuffUtils.ParticleSystem
{
    public class Particle
    {
        public ParticleEmitter emitter;
        
        public List<SpriteInitParam> spriteInitParams = new List<SpriteInitParam>();
        public Dictionary<IOwnParticleUniqueData, ParticleUniqueData> uniqueDatas = new Dictionary<IOwnParticleUniqueData, ParticleUniqueData>();

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
                    _pos = value /*- emitter.pos*/;
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

        public float alpha = 1f;

        public float rotation;
        public float lastRotation;
        public float setRotation;

        public int life;
        float setLife;
        public float LifeParam => 1f - life / setLife;

        public float randomParam1;
        public float randomParam2;
        public float randomParam3;
        
        public bool inStage;
        public int spriteLayer = 8;
        public FNode[] fNodes = new FNode[0];
        public int index;

        public void Init(ParticleEmitter emitter, int index)
        {
            this.emitter = emitter;
            this.index = index;

            setVel = vel = Vector2.zero;
            setColor = color = lastColor = Color.white;
            HardSetScale(0f);
            _pos = lastPos = Vector2.zero;
            setLife = life = 1;
            inStage = false;
            spriteLayer = 8;
            spriteInitParams.Clear();
            moveType = MoveType.Relative;
            randomParam1 = Random.value;
            randomParam2 = Random.value;
            randomParam3 = Random.value;

            foreach (var module in emitter.PInitModules)
                module.ApplyInit(this);

            foreach(var module in emitter.PUniqueDatas)
                uniqueDatas.Add(module, module.GetUniqueData(this));

            fNodes = new FNode[spriteInitParams.Count];
        }

        public virtual void InitSpritesAndAddToContainer()
        {
            if (inStage)
                return;
            inStage = true;

            foreach (var iIniSpriteAndAddToContainer in emitter.PInitSpritesAndAddToContainerModules)
                iIniSpriteAndAddToContainer.ApplyInitSpritesAndAddToContainer(this);

            //if (fNodes.Length != spriteInitParams.Count)
            //{
            //    Array.Resize(ref fNodes, spriteInitParams.Count);
            //    for(int i = 0; i< fNodes.Length; i++)
            //    {
            //        if (fNodes[i] == null)
            //            fNodes[i] = new FSprite("pixel");
            //    }
            //}

            //for(int i = 0;i < spriteInitParams.Count;i++)
            //{
            //    fNodes[i].SetElementByName(spriteInitParams[i].element);
            //    if (!string.IsNullOrEmpty(spriteInitParams[i].shader))
            //        fNodes[i].shader = Custom.rainWorld.Shaders[spriteInitParams[i].shader];
            //    else
            //        fNodes[i].shader = Custom.rainWorld.Shaders["Basic"];
            //    emitter.system.Containers[spriteLayer].AddChild(fNodes[i]);
            //}

            
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

            foreach (var pModule in emitter.PUpdateModules)
                pModule.ApplyUpdate(this);
        }

        public virtual void DrawSprites(RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if(!inStage) return;

            foreach (var iDraw in emitter.PDrawModules)
                iDraw.ApplyDrawSprites(this, rCam, timeStacker, camPos);
        }

        public virtual void ClearSprites()
        {
            if (!inStage)
                return;

            inStage = false;

            //for (int i = 0; i < fNodes.Length; i++)
            //    fNodes[i].RemoveFromContainer();

            foreach (var iClearSprites in emitter.PClearSpritesModules)
                iClearSprites.ApplyClearSprites(this);
        }

        public virtual void Die()
        {
            ClearSprites();
            emitter.OnParticleDieEvent?.Invoke(this);

            foreach (var iDie in emitter.PDieModules)
                iDie.ApplyDie(this);

            emitter.Particles.Remove(this);
            uniqueDatas.Clear();
            fNodes = null;

            ParticlePool.RecycleParticle(this);
        }

        public virtual void HardSetPos(Vector2 pos)
        {
            this.pos = pos;
            lastPos = this.pos;
        }
        public virtual void HardSetColor(Color color)
        {
            this.color = color;
            lastColor = color;
            setColor = color;
        }
        public virtual void HardSetScale(float scale)
        {
            HardSetScale(new Vector2(scale, scale));
        }

        public virtual void HardSetScale(Vector2 scale)
        {
            setScaleXY = scale;
            scaleXY = scale;
            lastScaleXY = scale;
        }

        public virtual void HardSetRotation(float rotation)
        {
            this.rotation = setRotation = lastRotation = rotation;
        }

        public virtual void SetLife(int life)
        {
            this.life = life;
            setLife = life;
        }

        public virtual void SetVel(Vector2 vel)
        {
            this.vel = vel;
            this.setVel = vel;
        }

        public T GetUniqueData<T>(IOwnParticleUniqueData owner) where T : ParticleUniqueData
        {
            if (uniqueDatas.TryGetValue(owner, out var data))
                return data as T;
            return null;
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
            public int layer;
            public Color? constCol;

            public SpriteInitParam(string element, string shader, int layer = 8, float alpha = 1f, float scale = 1f, Color? constCol = null)
            {
                this.element = element;
                this.shader = shader;
                this.alpha = alpha;
                this.scale = scale;
                this.layer = layer;
                this.constCol = constCol;
            }
        }
    
        public abstract class ParticleUniqueData
        {
        }
    }
}
