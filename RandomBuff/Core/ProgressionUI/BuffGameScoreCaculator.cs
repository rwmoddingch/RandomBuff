using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.Game.Settings.Conditions;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RandomBuff.Core.StaticsScreen.BuffGameScoreCaculator.ScoreBoard;
using static RandomBuff.Render.UI.Component.BuffCardTimer;
using static UnityEngine.GUI;

namespace RandomBuff.Core.StaticsScreen
{
    internal class BuffGameScoreCaculator : PositionedMenuObject
    {
        public float width = 300f;
        public static int maxShowInstance = 5;

        public WinGamePackage winPackage;
        public ScoreBoard scoreBoard;
        public BuffLevelBarDynamic expBar;
        public List<ScoreInstance> activeInstances = new();

        public ScoreCaculatorState state = ScoreCaculatorState.Prepare;

        public int indexInCurrentState = 0;


        //Dictionary<CreatureTemplate.Type, int[]> killsAndCounts = new();
        //CreatureTemplate.Type[] kills;

        //分数预计算结果
        //0:scoreindex, 1:count 2:intData
        List<KeyValuePair<CreatureTemplate.Type, int[]>> killsAndCounts = new();
        int[] defaultScores;

        public BuffGameScoreCaculator(Menu.Menu menu, MenuObject owner, Vector2 pos, WinGamePackage winGamePackage, float width = 300f) : base(menu, owner, pos)
        {
            this.width = width;
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);
            this.winPackage = winGamePackage;
            scoreBoard = new ScoreBoard(this);
            expBar = new BuffLevelBarDynamic(myContainer, pos + new Vector2(0f, 40f), width, BuffPlayerData.Instance.playerTotExp, BuffPlayerData.Exp2Level, BuffPlayerData.Level2Exp)
            {
                alpha = 0f,
                setAlpha = 0f
            };
            expBar.HardSet();

            PreCaculateScores();
        }

        //提前计算所有得分并直接应用到存档
        public void PreCaculateScores()
        {
            int totalScore = 0;
            //获取生物的分数
            defaultScores = new int[MultiplayerUnlocks.SandboxUnlockID.values.entries.Count];
            SandboxSettingsInterface.DefaultKillScores(ref defaultScores);

            foreach(var kill in winPackage.saveState.kills)
            {
                var unlock = MultiplayerUnlocks.SandboxUnlockForSymbolData(kill.Key).index;

                bool matched = false;
                foreach(var element in killsAndCounts)
                {
                    if(element.Key == kill.Key.critType && element.Value[2] == kill.Key.intData)
                    {
                        element.Value[1] += kill.Value;
                        matched = true;
                    }
                }
                if (!matched)
                {
                    killsAndCounts.Add(new KeyValuePair<CreatureTemplate.Type, int[]>(kill.Key.critType, new int[3] { unlock, kill.Value, kill.Key.intData }));
                }
            }
            foreach(var kill in killsAndCounts)
            {
                totalScore += defaultScores[kill.Value[0]] * kill.Value[1];
            }

            //获取卡牌分数
            foreach(var buffID in winPackage.winWithBuffs)
            {
                var staticData = BuffConfigManager.GetStaticData(buffID);

                int score;
                if (staticData.BuffType == BuffType.Positive)
                    score = 10;
                else if (staticData.BuffType == BuffType.Negative)
                    score = 20;
                else
                    score = 15;
                totalScore += score;
            }

            //获取条件分数
            foreach(var condition in winPackage.winWithConditions)
            {
                totalScore += condition.Exp;
            }
            BuffPlayerData.Instance.playerTotExp += totalScore;
        }

        DelayCmpnt waitScoreDeletionDelay;
        public override void Update()
        {
            base.Update();
            scoreBoard.Update();
            expBar.Update();

            for(int i = activeInstances.Count - 1; i >= 0; i--)
                activeInstances[i].Update();

            if(state == ScoreCaculatorState.Prepare)
            {
                if (scoreBoard.state == ScoreBoardState.UpdateScore)
                    state = ScoreCaculatorState.AddKillScore;
            }
            else if(state == ScoreCaculatorState.AddKillScore || state == ScoreCaculatorState.AddBuffScore || state == ScoreCaculatorState.AddConditionScore)
            {
                if (activeInstances.Count == 0 || activeInstances.First().state == ScoreInstance.ScoreInstanceState.FadeOut)
                {
                    AddNextScore();
                }
            }
            else if(state == ScoreCaculatorState.WaitScoreDeletion)
            {
                BuffPlugin.Log(activeInstances.Count);
                if(activeInstances.Count == 0)
                {
                    expBar.setAlpha = 1f;
                    state = ScoreCaculatorState.ShowLevelProgression;
                }
            }
            else if(state == ScoreCaculatorState.ShowLevelProgression)
            {
                if (Mathf.Approximately(expBar.alpha, 1f))
                {
                    state = ScoreCaculatorState.AddScoreToLevel;
                    expBar.Exp += scoreBoard.score;
                    scoreBoard.score = 0;
                }
            }
            else if(state == ScoreCaculatorState.AddScoreToLevel)
            {
                if(expBar.FinishState)
                {
                    state = ScoreCaculatorState.Finish;
                    (menu as BuffGameWinScreen).OnScoreCaculateFinish();
                }
            }
            else if(state == ScoreCaculatorState.Finish)
            {
            }
        }

        public void AddNextScore()
        {
            if(state == ScoreCaculatorState.AddKillScore)
            {
                if(indexInCurrentState == killsAndCounts.Count)
                {
                    state = ScoreCaculatorState.AddBuffScore;
                    indexInCurrentState = 0;
                    return;
                }

                var keyValuePair = killsAndCounts[indexInCurrentState];
                var critType = keyValuePair.Key;
                int count = keyValuePair.Value[1];
                int defaultScore = defaultScores[keyValuePair.Value[0]];
                int intData = keyValuePair.Value[2];

                activeInstances.Insert(0, new KillScoreInstance(this, critType, count, count * defaultScore, intData));
                foreach (var instance in activeInstances)
                    instance.UpdateInstancePos();
                indexInCurrentState++;
            }
            else if(state == ScoreCaculatorState.AddBuffScore)
            {
                if (indexInCurrentState == winPackage.winWithBuffs.Count)
                {
                    state = ScoreCaculatorState.AddConditionScore;
                    indexInCurrentState = 0;
                    return;
                }

                var buffID = winPackage.winWithBuffs[indexInCurrentState];
                var staticData = BuffConfigManager.GetStaticData(buffID);

                int score;
                if (staticData.BuffType == BuffType.Positive)
                    score = 10;
                else if (staticData.BuffType == BuffType.Negative)
                    score = 20;
                else
                    score = 15;

                activeInstances.Insert(0, new BuffScoreInstance(this, score, buffID));
                foreach (var instance in activeInstances)
                    instance.UpdateInstancePos();
                indexInCurrentState++;
            }
            else if(state == ScoreCaculatorState.AddConditionScore)
            {
                if(indexInCurrentState == winPackage.winWithConditions.Count)
                {
                    if (waitScoreDeletionDelay != null)
                        return;

                    waitScoreDeletionDelay = AnimMachine.GetDelayCmpnt(80, autoDestroy: true).BindActions(OnAnimFinished: (d) =>
                    {
                        state = ScoreCaculatorState.WaitScoreDeletion;
                        scoreBoard.state = ScoreBoardState.Finish;
                        waitScoreDeletionDelay = null;
                    });
                    return;
                }

                var condition = winPackage.winWithConditions[indexInCurrentState];

                activeInstances.Insert(0, new BuffConditionScoreInstance(this, condition));
                foreach(var instance in activeInstances)
                    instance.UpdateInstancePos();
                indexInCurrentState++;
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            scoreBoard.GrafUpdate(timeStacker);
            expBar.GrafUpdate(timeStacker);
            for (int i = activeInstances.Count - 1; i >= 0; i--)
                activeInstances[i].GrafUpdate(timeStacker);
        }


        public enum ScoreCaculatorState
        {
            Prepare,
            AddKillScore,
            AddBuffScore,
            AddConditionScore,
            WaitScoreDeletion,
            ShowLevelProgression,
            AddScoreToLevel,
            Finish
        }


        public class ScoreInstance//buttom-left
        {
            static int UpdateScoreTime = 10;

            public int score;
            public FLabel scoreLabel;
            public BuffGameScoreCaculator caculator;

            public float height;

            public ScoreInstanceState state;

            protected float alpha = 0f;
            protected float lastAlpha = 0f;

            protected Vector2 pos;
            protected Vector2 lastPos;

            protected int updateScoreCounter;

            protected Vector2 instanceShowPos;
            protected Vector2 instanceHidePos;
         
            public ScoreInstance(BuffGameScoreCaculator caculator, int score)
            {
                this.caculator = caculator;
                this.score = score;
                this.height = 40f;
                scoreLabel = new FLabel(Custom.GetDisplayFont(), score.ToString())
                {
                    anchorX = 1f,
                    anchorY = 0f
                };

                caculator.myContainer.AddChild(scoreLabel);
            }

            public virtual void Update()
            {
                if (state == ScoreInstanceState.Prepare)
                {
                    PrepareUpdate();
                }
                else if(state == ScoreInstanceState.UpdateScore)
                {
                    UpdateScoreUpdate();
                }
                else if(state == ScoreInstanceState.FadeOut)
                {
                    FadeOutUpdate();
                }
                else if(state == ScoreInstanceState.Delete)
                {
                    DeleteUpdate();
                }
            }

            TickAnimCmpnt alphaAnim;
            float param;
            public virtual void PrepareUpdate()
            {
                if (state != ScoreInstanceState.Prepare)
                    return;

                if (alphaAnim == null && state == ScoreInstanceState.Prepare)
                {
                    alphaAnim = AnimMachine.GetTickAnimCmpnt(0, 60, autoDestroy: true).BindModifier(Helper.EaseInOutCubic)
                        .BindActions(OnAnimGrafUpdate: (t, f) =>
                        {
                            param = t.Get();
                        }, OnAnimFinished: (t) =>
                        {
                            param = 1f;
                            state = ScoreInstanceState.UpdateScore;
                            caculator.scoreBoard.score += score;
                            alphaAnim = null;

                            lastAlpha = param;
                            alpha = param;

                            lastPos = pos;
                            pos = Vector2.Lerp(instanceHidePos, instanceShowPos, alpha);
                        });
                    alphaAnim.SetEnable(true);
                }

                lastAlpha = param;
                alpha = param;

                lastPos = pos;
                pos = Vector2.Lerp(instanceHidePos, instanceShowPos, alpha);

                //lastAlpha = alpha;
                //alpha = Mathf.Lerp(alpha, 1f, 0.15f);

                //if (Mathf.Approximately(alpha, 1f))
                //{
                //    alpha = 1f;
                //    lastAlpha = 1f;
                //    state = ScoreInstanceState.UpdateScore;
                //    caculator.scoreBoard.score += score;
                //}
                //lastPos = pos;
                //pos = Vector2.Lerp(instanceHidePos, instanceShowPos, alpha);
            }

            public virtual void UpdateScoreUpdate()
            {
                updateScoreCounter++;
                if (updateScoreCounter >= UpdateScoreTime)
                    state = ScoreInstanceState.FadeOut;
            }

            public virtual void FadeOutUpdate()
            {
                if (alpha != 0.5f)
                {
                    lastAlpha = alpha;
                    alpha = Mathf.Lerp(alpha, 0.5f, 0.15f);
                    if(Mathf.Approximately(alpha, 0.5f))
                    {
                        alpha = 0.5f;
                        lastAlpha = 0.5f;
                    }
                }
                lastPos = pos;
                pos = Vector2.Lerp(pos, instanceShowPos, 0.25f);

                if(caculator.activeInstances.IndexOf(this) >= BuffGameScoreCaculator.maxShowInstance || caculator.state == ScoreCaculatorState.WaitScoreDeletion)
                {
                    state = ScoreInstanceState.Delete;
                }
            }

            public virtual void DeleteUpdate()
            {
                lastAlpha = alpha;
                alpha = Mathf.Lerp(alpha, 0f, 0.15f);
                if (alpha < 0.01f)
                {
                    ClearSprites();
                }
                lastPos = pos;
                pos = Vector2.Lerp(pos, instanceHidePos, 0.15f);
            }

            public virtual void GrafUpdate(float timeStacker)
            {
                scoreLabel.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                scoreLabel.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) + Vector2.right * caculator.width);
            }

            public virtual void ClearSprites()
            {
                scoreLabel.RemoveFromContainer();
                caculator.activeInstances.Remove(this);
            }

            public void UpdateInstancePos()
            {
                int indexInList = caculator.activeInstances.IndexOf(this);
                float heightBias = 0f;
                for (int i = 0; i < indexInList; i++)
                {
                    heightBias += caculator.activeInstances[i].height;
                }
                instanceShowPos = new Vector2(caculator.pos.x, caculator.pos.y + heightBias);
                instanceHidePos = instanceShowPos + Vector2.right * caculator.width;
            }

            public enum ScoreInstanceState
            {
                Prepare,
                UpdateScore,
                FadeOut,
                Delete
            }
        }

        public class KillScoreInstance : ScoreInstance
        {
            FLabel countLabel;
            FSprite creatureSymbol;
            float span;

            public KillScoreInstance(BuffGameScoreCaculator buffGameScoreCaculator, CreatureTemplate.Type type, int count, int score, int intData) : base(buffGameScoreCaculator, score)
            {
                countLabel = new FLabel(Custom.GetDisplayFont(), $"x{count}")
                {
                    anchorX = 0f,
                    anchorY = 0f,
                };
                var iconData = new IconSymbol.IconSymbolData(type, AbstractPhysicalObject.AbstractObjectType.Creature, intData);
                string spriteName = CreatureSymbol.SpriteNameOfCreature(iconData);
                Color color = CreatureSymbol.ColorOfCreature(iconData);
                span = Futile.atlasManager.GetElementWithName(spriteName).sourcePixelSize.x + 5f;

                creatureSymbol = new FSprite(spriteName)
                {
                    color = color,
                    anchorX = 0f,
                    anchorY = 0f
                };

                buffGameScoreCaculator.Container.AddChild(countLabel);
                buffGameScoreCaculator.Container.AddChild(creatureSymbol);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);

                countLabel.alpha = smoothAlpha;
                countLabel.SetPosition(smoothPos + new Vector2(span, 0f));

                creatureSymbol.alpha = smoothAlpha;
                creatureSymbol.SetPosition(smoothPos + new Vector2(0f, 5f));
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                countLabel.RemoveFromContainer();
                creatureSymbol.RemoveFromContainer();
            }
        }

        public class BuffScoreInstance : ScoreInstance
        {
            BuffCard card;//偷个懒，就不用完整的流程来处理动画了（跪下

            public BuffScoreInstance(BuffGameScoreCaculator buffGameScoreCaculator, int score, BuffID buffID) : base(buffGameScoreCaculator, score)
            {
                height = 80f;
                card = new BuffCard(buffID);
                BuffPlugin.Log($"BuffScoreInstance - {buffID}-{card._cardRenderer._id}");
                card.Scale = 0.1f;
                card.DisplayTitle = false;
                card.DisplayDescription = false;
                buffGameScoreCaculator.Container.AddChild(card.Container);
                card.Rotation = new Vector3(0f, 180f, 0f);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                card.Position = Vector2.Lerp(lastPos, pos, timeStacker) + new Vector2(15f, 30f);
                float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                card.Rotation = Vector3.Lerp(new Vector3(0f, 180f, 0f), Vector3.zero, smoothAlpha * (state == ScoreInstanceState.Prepare ? 1f : 2f));
                card.Alpha = smoothAlpha;
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                card.Destroy();
            }
        }

        public class BuffConditionScoreInstance : ScoreInstance
        {
            FLabel conditionLabel;
            public BuffConditionScoreInstance(BuffGameScoreCaculator caculator, Condition condition) : base(caculator, condition.Exp)
            {
                conditionLabel = new FLabel(Custom.GetDisplayFont(), condition.DisplayName(Custom.rainWorld.inGameTranslator))
                {
                    anchorX = 0f,
                    anchorY = 0f,
                };
                caculator.Container.AddChild(conditionLabel);
            }

            public override void GrafUpdate(float timeStacker)
            {
                base.GrafUpdate(timeStacker);
                float smoothAlpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                Vector2 smoothPos = Vector2.Lerp(lastPos, pos, timeStacker);

                conditionLabel.alpha = smoothAlpha;
                conditionLabel.SetPosition(smoothPos);
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                conditionLabel.RemoveFromContainer();
            }
        }

        public class ScoreBoard : IOwnBuffTimer//top-left
        {
            public int score = -1;
            public BuffGameScoreCaculator caculator;
            public BuffCountDisplay scoreDisplay;
            public FSprite lineSprite;

            public ScoreBoardState state = ScoreBoardState.Prepare;

            public int Second => score;

            float alpha = 0f;
            float lastAlpha = 0f;

            public ScoreBoard(BuffGameScoreCaculator caculator)
            {
                this.caculator = caculator;
                scoreDisplay = new BuffCountDisplay(caculator.Container, this) { alightment = BuffCountDisplay.Alightment.Right};
                lineSprite = new FSprite("pixel") 
                { 
                    anchorX = 0f, 
                    anchorY = 1f,
                    scaleX = caculator.width,
                    scaleY = 2f,
                    alpha = alpha,
                };
                lineSprite.SetPosition(caculator.pos);
                scoreDisplay.pos = scoreDisplay.lastPos = caculator.pos + new Vector2(caculator.width, -40f);
                scoreDisplay.scale = 1f;

                caculator.Container.AddChild(lineSprite);
            }

            public void Update()
            {
                scoreDisplay.Update();
                
                if(state == ScoreBoardState.Prepare)
                {
                    lastAlpha = alpha;
                    alpha = Mathf.Lerp(alpha, 1f, 0.15f);
                    scoreDisplay.alpha = alpha;
                    score = 0;
                    scoreDisplay.pos = caculator.pos + new Vector2(caculator.width - 5f, -40f);

                    if (Mathf.Approximately(alpha, 1f))
                    {
                        alpha = 1f;
                        lastAlpha = 1f;
                        scoreDisplay.alpha = alpha;
                        state = ScoreBoardState.UpdateScore;
                    }
                }
                else if(state == ScoreBoardState.UpdateScore)
                {
                    //do nothing
                }
                else if(state == ScoreBoardState.Finish)
                {

                }
            }

            public void GrafUpdate(float timeStacker)
            {
                scoreDisplay.GrafUpdate(timeStacker);
                lineSprite.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
            }

            public void ClearSprites()
            {
                scoreDisplay.ClearSprites();
                lineSprite.RemoveFromContainer();
            }

            public enum ScoreBoardState
            {
                Prepare,
                UpdateScore,
                Finish
            }
        }
    }
}
