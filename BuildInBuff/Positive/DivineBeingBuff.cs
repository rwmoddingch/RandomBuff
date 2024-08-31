using RandomBuff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;
using IL;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class DivineBeingBuff : Buff<DivineBeingBuff, DivineBeingBuffData>
    {
        public override BuffID ID => DivineBeingIBuffEntry.DivineBeingBuffID;

        public DivineBeingBuff()
        {
            if (BuffCustom.TryGetGame(out var game))
            {
                foreach (var player in game.Players.Where(i =>
                             i.state.alive && i.realizedCreature?.room != null))
                {
                    player.realizedCreature.room.AddObject(new DivineRing(player.realizedCreature as Player));
                }
            }
        }
    }

    internal class DivineBeingBuffData : CountableBuffData
    {
        public override BuffID ID => DivineBeingIBuffEntry.DivineBeingBuffID;

        public override int MaxCycleCount => 3;
    }

    internal class DivineBeingIBuffEntry : IBuffEntry
    {
        public static BuffID DivineBeingBuffID = new BuffID("DivineBeing", true);
        public static Shader RingShaderInstance { get; private set; }

        public void OnEnable()
        {
            //Custom.rainWorld.Shaders.Add("DivineBeingRing", FShader.CreateShader("DivineBeingRing", ));
            BuffRegister.RegisterBuff<DivineBeingBuff, DivineBeingBuffData, DivineBeingIBuffEntry>(DivineBeingBuffID);
        }

        public static void LoadAssets()
        {
            var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("buffassets/assetBundles/builtinbundle"));
            RingShaderInstance = bundle.LoadAsset<Shader>("divinering");
        }

        public static void HookOn()
        {
            On.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSprites;
        }

        private static void PlayerGraphics_InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig.Invoke(self, sLeaser, rCam);
            self.player.room.AddObject(new DivineRing(self.player));
        }
    }

    public class DivineRing : CosmeticSprite
    {
        public static Color colorA;
        public static Color colorB;

        public static int totalParamsCount = 20;
        public static float ringRad = 160f;

        Player bindPlayer;
        public Vector4[] Params;
        public ParamInstance[] ParamInstances;

        int nextUpdatParamCounter;

        float lastRingStrength;
        public float ringStrength;
        float targetRingStrength;

        static DivineRing()
        {
            ColorUtility.TryParseHtmlString("#FF8500", out colorA);
            ColorUtility.TryParseHtmlString("#FFFFBF", out colorB);
        }

        public DivineRing(Player bindPlayer)
        {
            this.bindPlayer = bindPlayer;
            room = bindPlayer.room;
            Params = new Vector4[totalParamsCount];
            ParamInstances = new ParamInstance[totalParamsCount];
            for(int i = 0; i < totalParamsCount; i++)
            {
                ParamInstances[i] = new ParamInstance(this, i);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (!rCam.game.rainWorld.Shaders.ContainsKey($"DivineBeingRing_{bindPlayer.playerState.playerNumber}"))//生成shader防止合批处理
            {
                Custom.rainWorld.Shaders.Add($"DivineBeingRing_{bindPlayer.playerState.playerNumber}", FShader.CreateShader($"DivineBeingRing_{bindPlayer.playerState.playerNumber}", DivineBeingIBuffEntry.RingShaderInstance));
            }

            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("Futile_White", true)
            {
                shader = rCam.game.rainWorld.Shaders[$"DivineBeingRing_{bindPlayer.playerState.playerNumber}"]
            };

            
            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Water");

            foreach(var sprite in sLeaser.sprites)
            {
                newContatiner.AddChild(sprite);
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);

            for (int i = 0; i < totalParamsCount; i++)
            {
                ParamInstances[i].Draw(timeStacker);
            }

            sLeaser.sprites[0].SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) - camPos);
            sLeaser.sprites[0].width = ringRad * 2f;
            sLeaser.sprites[0].height = ringRad * 2f;

            if (sLeaser.sprites[0]._renderLayer != null)
            {
                sLeaser.sprites[0]._renderLayer._material.SetColor("colorA", colorA);
                sLeaser.sprites[0]._renderLayer._material.SetColor("colorB", colorB);

                sLeaser.sprites[0]._renderLayer._material.SetVectorArray("params", Params);
            }

            sLeaser.sprites[0].alpha = Mathf.Lerp(lastRingStrength, ringStrength, timeStacker);
        }

        public override void Update(bool eu)
        {
            if (slatedForDeletetion)
                return;

            base.Update(eu);

            if (bindPlayer.slatedForDeletetion)
                Destroy();

            if (bindPlayer.room == null || room != bindPlayer.room)
                Destroy();

            lastPos = pos;
            pos = bindPlayer.DangerPos;

            for (int i = 0; i < totalParamsCount; i++)
            {
                ParamInstances[i].Update();
            }

            if (nextUpdatParamCounter > 0)
                nextUpdatParamCounter--;
            else
            {
                int index = Random.Range(0, totalParamsCount);
                ParamInstances[index].ResetParam();
            }

            targetRingStrength = 0;
            foreach (var obj in room.updateList)
            {
                if (!(obj is Creature creature))
                    continue;
                if (obj is Player)
                    continue;
                if(Vector2.Distance(pos, creature.DangerPos) < ringRad)
                {
                    if (creature.stun < 20)
                        creature.stun = 80;
                    targetRingStrength = 1;
                }
            }

            lastRingStrength = ringStrength;
            ringStrength = Mathf.Lerp(ringStrength, targetRingStrength, 0.1f);
        }

        public class ParamInstance
        {
            public DivineRing ring;
            public int bindIndex;

            public float angle;

            public int counter;
            public int lastCounter;

            public float strength;
            public float lastStrenth;

            bool update = false;

            public ParamInstance(DivineRing ring, int bindIndex)
            {
                this.ring = ring;
                this.bindIndex = bindIndex;

                strength = 0f;
            }

            public void ResetParam()
            {
                if(counter == 0)
                    angle = Random.value;
                counter = 40;
                update = true;
            }

            public void Update()
            {
                lastCounter = counter;

                if (update)
                {
                    lastStrenth = strength;
                    strength = Mathf.Lerp(lastStrenth, counter / 40f, 0.05f);
                }

                if (counter == 0)
                    return;
                counter--;
                
            }

            public void Draw(float timeStacker)
            {
                if (counter == 0 && lastCounter == 0 && strength < 0.001f)
                {
                    if (update)
                        update = false;
                    return;
                }

                ring.Params[bindIndex].x = angle;
                ring.Params[bindIndex].y = Mathf.Lerp(lastStrenth, strength, timeStacker) * ring.ringStrength;
            }
        }
    }
}
