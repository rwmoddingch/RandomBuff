using System;
using System.Collections.Generic;
using RandomBuff;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuff.Core.SaveData.BuffConfig;
using RandomBuffUtils;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Positive
{
    internal class DeathFreeMedallionBuff : Buff<DeathFreeMedallionBuff, DeathFreeMedallionBuffData>
    {
        public override BuffID ID => DeathFreeMedallionIBuffEntry.deathFreeMedallionBuffID;

        public bool triggerdThisCycle;

        public AbstractCreature spawnlater;
        public int counter;

        public override bool Active => !triggerdThisCycle;

        public override bool Trigger(RainWorldGame game)
        {
            if(!BuffPlugin.DevEnabled)
                triggerdThisCycle = true;
            return base.Trigger(game);
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (counter > 0 && spawnlater != null)
                counter--;
            if (counter == 0 && spawnlater != null)
            {
                spawnlater.RealizeInRoom();
                spawnlater = null;
            }
        }
    }

    internal class DeathFreeMedallionBuffData : BuffData
    {
        public override BuffID ID => DeathFreeMedallionIBuffEntry.deathFreeMedallionBuffID;

        [CustomBuffConfigEnum(typeof(CreatureTemplate.Type),"Slugcat")]
        public CreatureTemplate.Type WawaTest
        {
            get;
            set;
        }

        [CustomBuffConfigRange(1f, 0f, 3f)]
        [CustomBuffConfigInfo("RangeTest", "this is a range value test")]
        public float WawaValueRangeTest
        {
            get;
        }

        public DeathFreeMedallionBuffData()
        {
            //WawaTest = CreatureTemplate.Type.BigEel;
            //BuffPlugin.Log($"Ctor Get wawaTest : {WawaTest}, WawaValueRangeTest : {WawaValueRangeTest}");

            //try
            //{
            //    throw new Exception("Sdsdsd");
            //}
            //catch (Exception e)
            //{

            //    BuffPlugin.LogError(e);
            //}

        }

        public override void DataLoaded(bool newData)
        {
            base.DataLoaded(newData);
            //BuffPlugin.Log($"Get wawaTest : {WawaTest}");

        }
    }

    internal class DeathFreeMedallionIBuffEntry : IBuffEntry
    {
        public static BuffID deathFreeMedallionBuffID = new BuffID("DeathFreeMedallion", true);

        public static void HookOn()
        {
            On.Player.Die += Player_Die;
            On.Player.Destroy += Player_Destroy;
        }

        private static void Player_Destroy(On.Player.orig_Destroy orig, Player self)
        {
            if (DeathPreventer.Singleton != null && DeathPreventer.Singleton.bindAbPlayer == self.abstractCreature)
                return;
            else if (!DeathFreeMedallionBuff.Instance.triggerdThisCycle)
            {
                self.room.AddObject(new DeathPreventer(self, self.DangerPos + Vector2.up * 80f));
                DeathFreeMedallionBuff.Instance.TriggerSelf(true);
                return;
            }
            orig.Invoke(self);
        }

        private static void Player_Die(On.Player.orig_Die orig, Player self)
        {
            if (DeathPreventer.Singleton != null && DeathPreventer.Singleton.bindAbPlayer == self.abstractCreature)
                return;
            else if(!DeathFreeMedallionBuff.Instance.triggerdThisCycle)
            {
                self.abstractCreature.Room.realizedRoom.AddObject(new DeathPreventer(self, self.DangerPos + Vector2.up * 80f));
                DeathFreeMedallionBuff.Instance.TriggerSelf(true);
                return;
            }
            orig.Invoke(self);
        }

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<DeathFreeMedallionBuff, DeathFreeMedallionBuffData, DeathFreeMedallionIBuffEntry>(deathFreeMedallionBuffID);
        }
    }

    internal class DeathPreventer : CosmeticSprite
    {
        public static DeathPreventer Singleton { get; private set; }

        public AbstractCreature bindAbPlayer;

        public RoomSettings.RoomEffect effect;
        List<RoomSettings.RoomEffect> origEffects = new List<RoomSettings.RoomEffect>();

        Vector2 endPos;
        Vector2 startPos;

        Stage currentStage = Stage.Prepare;

        int prepareLife = 40;
        int moveLife = 40;
        int spawnLife = 40;

        int maxLife => prepareLife + moveLife + spawnLife;
        int life;

        int currentScanY;
        float bestEndPosScore = float.MaxValue;
        IntVector2 endPosTile;

        float aimAlpha;
        float alpha;

        bool playerRespawned;

        bool blink;
        Color blinkCol
        {
            get
            {
                if (!blink)
                    return RainWorld.GoldRGB;
                if(life % 10 > 5)
                    return RainWorld.GoldRGB;
                return Color.white * 0.5f + RainWorld.GoldRGB * 0.5f;
            }
        }

        public DeathPreventer(Player bindPlayer, Vector2 endPos)
        {
            bindPlayer.enteringShortCut = null;
            if(bindPlayer.dangerGrasp != null)
                bindPlayer.dangerGrasp.discontinued = true;
            bindAbPlayer = bindPlayer.abstractCreature;
            bindAbPlayer.InDen = false;
            this.room = bindAbPlayer.Room.realizedRoom;
            this.endPos = endPos;
            endPosTile = room.GetTilePosition(endPos);

            pos = bindPlayer.DangerPos;
            startPos = pos;
            lastPos = pos;
            Singleton = this;

            BuffUtils.Log(DeathFreeMedallionIBuffEntry.deathFreeMedallionBuffID,$"Init DeathPreventer : {bindPlayer}");

            bindPlayer.dead = false;
            bindPlayer.playerState.alive = true;
            bindPlayer.aerobicLevel = 0f;
            bindPlayer.stun = 4;
            bindAbPlayer.Abstractize(room.GetWorldCoordinate(endPos));
            bindPlayer.slatedForDeletetion = true;

            for(int i = room.roomSettings.effects.Count - 1; i >= 0; i--)
            {
                origEffects.Add(room.roomSettings.effects[i]);
                room.roomSettings.effects.RemoveAt(i);
            }
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites = new FSprite[1];
            sLeaser.sprites[0] = new FSprite("smallKarma9-9", true) { color = RainWorld.GoldRGB };
            base.InitiateSprites(sLeaser, rCam);

            effect = new RoomSettings.RoomEffect(RoomSettings.RoomEffect.Type.VoidMelt, 0f, false);
            room.roomSettings.effects.Add(effect);
            room.PlaySound(SoundID.SB_A14, 0f, 1f, 1f);

            AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            foreach(var sprite in sLeaser.sprites)
            {
                rCam.ReturnFContainer("HUD").AddChild(sprite);
            }
        }

        public override void Update(bool eu)
        {
            base.Update(eu);

            if (life < maxLife)
                life++;
            else
            {
                Destroy();
            }

            float tInStage = 0f;
            if (life < prepareLife)
            {
                currentStage = Stage.Prepare;
                tInStage = life / (float)prepareLife;
            }
            if (life > prepareLife && life < prepareLife + moveLife)
            {
                currentStage = Stage.Move;
                tInStage = (life - prepareLife) / (float)moveLife;
            }
            if (life > prepareLife + moveLife)
            {
                currentStage = Stage.ReSpawn;
                tInStage = (life - prepareLife - moveLife) / (float)spawnLife;
            }

            lastPos = pos;
            alpha = Mathf.Lerp(alpha, aimAlpha, 0.2f);
           

            foreach (var obj in room.updateList)
            {
                if (!(obj is Creature creature))
                    continue;
                if (obj is Player)
                    continue;

                creature.LoseAllGrasps();
              
                creature.stun = 120;
                //creature.Die();
            }

            if (room.game.cameras[0].room == room)
            {
                room.game.cameras[0].ApplyFade();

                if (currentStage == Stage.ReSpawn)
                    effect.amount = Mathf.Lerp(1f, 0f, tInStage);
                else
                    effect.amount = 1f;
            }
  
            if (currentStage == Stage.Prepare)
                PrepareUpdate();
            else if (currentStage == Stage.Move)
                MoveUpdate();
            else if (currentStage == Stage.ReSpawn)
                ReSpawnUpdate();

            void PrepareUpdate()
            {
                aimAlpha = 1f;
                blink = true;
                int yStep = Mathf.CeilToInt(room.Height / (float)prepareLife);
                int currentYStep = 0;

                while (currentYStep < yStep && currentScanY < room.Height)
                {
                    for(int x = 0; x < room.Width; x++)
                    {
                        IntVector2 testTile = new IntVector2(x, currentScanY);
                        float score = TileScore(testTile);
                        if (score < bestEndPosScore)
                        {
                            bestEndPosScore = score;
                            endPosTile = testTile;
                        }
                    }
                    currentYStep++;
                    currentScanY++;
                }

                room.AddObject(new MeltLights.MeltLight(1f, this.room.RandomPos(), this.room, RainWorld.GoldRGB));
            }

            void MoveUpdate()
            {
                blink = false;
                float t = CubicBezier(new Vector2(0.8f, 0f), new Vector2(0.2f, 1f), tInStage);
                Vector2 endpos = room.MiddleOfTile(endPosTile);
                pos = Vector2.Lerp(startPos, endpos, t);
            }

            void ReSpawnUpdate()
            {
                blink = false;
                pos = room.MiddleOfTile(endPosTile);
                aimAlpha = 0f;

                if (playerRespawned)
                    return;
                bindAbPlayer.pos = room.GetWorldCoordinate(endPosTile);
                bindAbPlayer.RealizeInRoom();
                if(!bindAbPlayer.Room.world.game.AlivePlayers.Contains(bindAbPlayer))
                    bindAbPlayer.Room.world.game.AlivePlayers.Add(bindAbPlayer);

                
                if(!bindAbPlayer.Room.creatures.Contains(bindAbPlayer))
                    bindAbPlayer.Room.AddEntity(bindAbPlayer);

                if (!bindAbPlayer.Room.realizedRoom.updateList.Contains(bindAbPlayer.realizedCreature))
                    bindAbPlayer.Room.realizedRoom.AddObject(bindAbPlayer.realizedCreature);

                bindAbPlayer.realizedCreature.deaf = 0;
                if (bindAbPlayer.realizedCreature.dead)
                    bindAbPlayer.realizedCreature.dead = false;
                if ((bindAbPlayer.realizedCreature as Player).playerState.dead)
                    (bindAbPlayer.realizedCreature as Player).playerState.alive = true;
                if (bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.gameOverMode)
                {
                    bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.gameOverMode = false;
                    bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.textPrompt.dependentOnGrasp = null;
                }
                if(bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner == null || (bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner is Player player && player.slatedForDeletetion))
                {
                    bindAbPlayer.Room.realizedRoom.game.cameras[0].hud.owner = bindAbPlayer.realizedCreature as Player;
                }
                playerRespawned = true;
            }
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
            Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker) - camPos;
            smoothPos.x = Mathf.Clamp(smoothPos.x, 0f, Custom.rainWorld.options.ScreenSize.x);
            smoothPos.y = Mathf.Clamp(smoothPos.y, 0f, Custom.rainWorld.options.ScreenSize.y);

            sLeaser.sprites[0].SetPosition(smoothPos);
            sLeaser.sprites[0].alpha = alpha;
            sLeaser.sprites[0].color = blinkCol;
        }

        public override void Destroy()
        {
            base.Destroy();
            Singleton = null;
            room.roomSettings.effects.Remove(effect);

            foreach (var effect in origEffects)
            {
                room.roomSettings.effects.Add(effect);
            }
            origEffects.Clear();
        }

        public float TileScore(IntVector2 tile)
        {
            if(room.GetTile(tile).Solid)
                return float.MaxValue;

            bool completlyInAir = true;
            for(int y = 1; y < 4; y++)
            {
                if (tile.y - y < 0)
                    break;
                if (room.GetTile(new IntVector2(tile.x, tile.y - y)).Solid)
                    completlyInAir = false;
            }
            if(completlyInAir)
                return float.MaxValue;

            Vector2 tilePos = room.MiddleOfTile(tile);
            float value = Vector2.Distance(tilePos, startPos);
            value += Custom.LerpMap(Vector2.Distance(tilePos, startPos), 0f, 160f, 400f, 0f);

            if (!room.aimap.AnyExitReachableFromTile(tile, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Fly)))
                return float.MaxValue;

            if (room.PointSubmerged(tilePos))
                value += 10f * Mathf.Abs(tilePos.y - room.waterObject.DetailedWaterLevel(pos.x));

            foreach (var obj in room.updateList)
            {
                if ((obj is Creature creature))
                {
                    if (obj is Player)
                        continue;
                    value += CreatureDangerScore(creature.abstractCreature, tilePos);
                }
                else
                    value += ObjectDangerScore(obj, tilePos);
            }

            return value;
        }

        float ObjectDangerScore(UpdatableAndDeletable obj, Vector2 testPos)
        {
            if(obj is ZapCoil zapCoil)
            {
                FloatRect rect = zapCoil.GetFloatRect;
                Vector2 mid = new Vector2((rect.left + rect.right) / 2f, (rect.top + rect.bottom) / 2f);

                float distance = Vector2.Distance(testPos, mid);
                return -distance;
            }
            if(obj is WormGrass wormGrass)
            {
                float result = 0f;

                for(int i = 0;i < 3; i++)
                {
                    WormGrass.Worm randomWorm = wormGrass.worms[Random.Range(0, wormGrass.worms.Count)];
                    if (randomWorm != null)
                    {
                        float distance = Vector2.Distance(randomWorm.basePos, testPos);
                        result -= distance;
                    }
                    else
                        result += 100000f;
                }
                return result;
            }
            if(obj is DaddyCorruption corruption)
            {
                float result = 0f;
                foreach(var tile in corruption.tiles)
                {
                    float distance = Vector2.Distance(testPos, room.MiddleOfTile(tile));
                    result -= distance;
                }    
                return result;
            }
            return 0f;
        }

        private float CreatureDangerScore(AbstractCreature creature, Vector2 testPos)
        {
            bool flag = room.ViewedByAnyCamera(creature.realizedCreature.DangerPos, 60f);
            if (creature.creatureTemplate.type == CreatureTemplate.Type.PoleMimic && (creature.realizedCreature as PoleMimic).mimic > 0.5f)
            {
                flag = false;
            }
            if (flag)
            {
                return 0f;
            }
            float num = creature.creatureTemplate.dangerousToPlayer;
            if (creature.creatureTemplate.type == CreatureTemplate.Type.Scavenger)
            {
                num = 1f;
            }
            if (num == 0f)
            {
                return 0f;
            }
            if (creature.abstractAI != null && creature.abstractAI.RealAI != null)
            {
                num *= creature.abstractAI.RealAI.CurrentPlayerAggression(bindAbPlayer);
            }
            num *= Vector2.Distance(creature.realizedCreature.DangerPos, testPos);
            if (num == 0f)
            {
                return 0f;
            }
            return -num;
        }

        enum Stage
        {
            Prepare,
            Move,
            ReSpawn
        }
        public static float CubicBezier(Vector2 a, Vector2 b, float t)
        {
            Vector2 a1 = Vector2.Lerp(Vector2.zero, a, t);
            Vector2 b1 = Vector2.Lerp(b, Vector2.one, t);
            Vector2 ab = Vector2.Lerp(a1, b1, t);

            Vector2 a2 = Vector2.Lerp(a1, ab, t);
            Vector2 b2 = Vector2.Lerp(ab, b1, t);

            Vector2 c = Vector2.Lerp(a2, b2, t);
            return c.y;
        }
    }

}
