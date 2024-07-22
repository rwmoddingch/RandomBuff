using System;
using MonoMod;
using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuff;
using System.Runtime.CompilerServices;

namespace TemplateGains
{

    class UnstableSlugBuff : Buff<UnstableSlugBuff, UnstableSlugBuffData>
    {
        public override BuffID ID => UnstableSlugBuffEntry.UnstableSlugID;
        public UnstableSlugBuff()
        {
            StartRandomExplotion();
        }
        
        public void StartRandomExplotion()
        {
            MyTimer = new DownCountBuffTimer((timer, game) =>
            {
                BuffUtils.Log(ID, "UnstableSlugExplotion Counter triggered");
                explotionAllPlayer(game);
                UnstableSlugBuff.Instance.TriggerSelf(true);
                StartRandomExplotion();
            }, Random.Range(3, 180));
            MyTimer.ApplyStrategy(new UnstableDsplayStrategy());
        }
        public void explotionAllPlayer(RainWorldGame game)
        {
            foreach (var item in game.Players)
            {
                if (item != null && item.state.alive && item?.realizedObject is Player player)
                {
                    player.Stun(200);
                    var explotion = new Explosion(player.room, player, player.mainBodyChunk.pos, 10, 200, 30f, 1.8f, 80, 20, player, 10, 0, 1);
                    player.room.AddObject(explotion);

                    var vector = player.mainBodyChunk.pos;
                    player.room.AddObject(new MoreSlugcats.SingularityBomb.SparkFlash(player.firstChunk.pos, 300f, new Color(0f, 0f, 1f)));
                    player.room.AddObject(new Explosion.ExplosionLight(vector, 280f, 1f, 7, player.ShortCutColor()));
                    player.room.AddObject(new Explosion.ExplosionLight(vector, 230f, 1f, 3, new Color(1f, 1f, 1f)));
                    player.room.AddObject(new Explosion.ExplosionLight(vector, 2000f, 2f, 60, player.ShortCutColor()));
                    player.room.AddObject(new ShockWave(vector, 150, 0.485f, 80, true));
                    player.room.AddObject(new ShockWave(vector, 200f, 0.185f, 40, false));
                    player.room.PlaySound(SoundID.Bomb_Explode, player.mainBodyChunk.pos);
                }
            }
            
        }
    }
    class UnstableSlugBuffData : BuffData
    {
        public override BuffID ID => UnstableSlugBuffEntry.UnstableSlugID;
    }
    class UnstableSlugBuffEntry : IBuffEntry
    {
        public static BuffID UnstableSlugID = new BuffID("UnstableSlugID", true);
        public void OnEnable()
        {
            BuffRegister.RegisterBuff<UnstableSlugBuff, UnstableSlugBuffData, UnstableSlugBuffEntry>(UnstableSlugID);
        }
        public static void HookOn()
        {

        }
    }

    class UnstableDsplayStrategy : BuffTimerDisplayStrategy
    {
        public override bool DisplayThisFrame => Second<=10;
    }

}
