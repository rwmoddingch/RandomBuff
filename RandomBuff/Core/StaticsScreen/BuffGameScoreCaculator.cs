using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RandomBuff.Core.StaticsScreen.BuffGameScoreCaculator.ScoreBoard;
using static RandomBuff.Render.UI.BuffCardTimer;
using static UnityEngine.GUI;

namespace RandomBuff.Core.StaticsScreen
{
    internal class BuffGameScoreCaculator : PositionedMenuObject
    {
        public static float width = 300f;
        public static int maxShowInstance = 5;

        public BuffPoolManager.WinGamePackage winPackage;
        public ScoreBoard scoreBoard;
        public List<ScoreInstance> activeInstances = new();

        public ScoreCaculatorState state = ScoreCaculatorState.Prepare;

        public int indexInCurrentState = 0;

        public BuffGameScoreCaculator(Menu.Menu menu, MenuObject owner, Vector2 pos, BuffPoolManager.WinGamePackage winGamePackage) : base(menu, owner, pos)
        {
            myContainer = new FContainer();
            menu.container.AddChild(myContainer);
            this.winPackage = winGamePackage;
            scoreBoard = new ScoreBoard(this);

        }

        public override void Update()
        {
            base.Update();
            scoreBoard.Update();
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
            else if(state == ScoreCaculatorState.Finish)
            {
                if(scoreBoard.state == ScoreBoardState.Finish)
                {

                }
            }
        }

        public void AddNextScore()
        {
            if(state == ScoreCaculatorState.AddKillScore)
            {
                if(indexInCurrentState == winPackage.sessionRecord.kills.Count)
                {
                    state = ScoreCaculatorState.AddBuffScore;
                    indexInCurrentState = 0;
                    return;
                }

                var kill = winPackage.sessionRecord.kills[indexInCurrentState];
                activeInstances.Insert(0, new KillScoreInstance(this, 1));
                foreach (var instance in activeInstances)
                    instance.UpdateInstancePos();
                indexInCurrentState++;
            }
            else if(state == ScoreCaculatorState.AddBuffScore)
            {
                if (indexInCurrentState == winPackage.winWithBuffs.Count)
                {
                    state = ScoreCaculatorState.Finish;
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
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            scoreBoard.GrafUpdate(timeStacker);
            for (int i = activeInstances.Count - 1; i >= 0; i--)
                activeInstances[i].GrafUpdate(timeStacker);
        }


        public enum ScoreCaculatorState
        {
            Prepare,
            AddKillScore,
            AddBuffScore,
            AddConditionScore,
            Finish
        }


        public class ScoreInstance//buttom-left
        {
            static int UpdateScoreTime = 40;

            public int score;
            public FLabel scoreLabel;
            public BuffGameScoreCaculator caculator;

            public float height;

            public ScoreInstanceState state;

            float alpha = 0f;
            float lastAlpha = 0f;

            Vector2 pos;
            Vector2 lastPos;

            int updateScoreCounter;

            Vector2 instanceShowPos;
            Vector2 instanceHidePos;
         
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

            public virtual void PrepareUpdate()
            {
                lastAlpha = alpha;
                alpha = Mathf.Lerp(alpha, 1f, 0.15f);

                if (Mathf.Approximately(alpha, 1f))
                {
                    alpha = 1f;
                    lastAlpha = 1f;
                    state = ScoreInstanceState.UpdateScore;
                    caculator.scoreBoard.score += score;
                }
                lastPos = pos;
                pos = Vector2.Lerp(instanceHidePos, instanceShowPos, alpha);
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

                if(caculator.activeInstances.IndexOf(this) >= BuffGameScoreCaculator.maxShowInstance || caculator.state == ScoreCaculatorState.Finish)
                {
                    state = ScoreInstanceState.Delete;
                }
            }

            public virtual void DeleteUpdate()
            {
                lastAlpha = alpha;
                alpha = Mathf.Lerp(alpha, 0f, 0.15f);
                if (Mathf.Approximately(alpha, 0f))
                {
                    ClearSprites();
                }
                lastPos = pos;
                pos = Vector2.Lerp(pos, instanceHidePos, 0.15f);
            }

            public virtual void GrafUpdate(float timeStacker)
            {
                scoreLabel.alpha = Mathf.Lerp(lastAlpha, alpha, timeStacker);
                scoreLabel.SetPosition(Vector2.Lerp(lastPos, pos, timeStacker) + Vector2.right * BuffGameScoreCaculator.width);
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
                instanceHidePos = instanceShowPos + Vector2.right * BuffGameScoreCaculator.width;

                BuffPlugin.Log($"{indexInList}, {instanceShowPos}");
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
            public KillScoreInstance(BuffGameScoreCaculator buffGameScoreCaculator, int score) : base(buffGameScoreCaculator, score)
            {

            }
        }

        public class BuffScoreInstance : ScoreInstance
        {
            public BuffScoreInstance(BuffGameScoreCaculator buffGameScoreCaculator, int score, BuffID buffID) : base(buffGameScoreCaculator, score)
            {

            }
        }

        public class ScoreBoard : IOwnBuffTimer//top-left
        {
            public int score = -1;
            public BuffGameScoreCaculator caculator;
            public BuffCardTimer scoreDisplay;
            public FSprite lineSprite;

            public ScoreBoardState state = ScoreBoardState.Prepare;

            public int Second => score;

            float alpha = 0f;
            float lastAlpha = 0f;

            public ScoreBoard(BuffGameScoreCaculator caculator)
            {
                this.caculator = caculator;
                scoreDisplay = new BuffCardTimer(caculator.Container, this);
                lineSprite = new FSprite("pixel") 
                { 
                    anchorX = 0f, 
                    anchorY = 1f,
                    scaleX = BuffGameScoreCaculator.width,
                    scaleY = 2f,
                    alpha = alpha,
                };
                lineSprite.SetPosition(caculator.pos);
                scoreDisplay.pos = scoreDisplay.lastPos = caculator.pos + new Vector2(BuffGameScoreCaculator.width, -40f);
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
                    scoreDisplay.setAlpha = alpha;
                    scoreDisplay.alpha = alpha;
                    score = 0;
                    scoreDisplay.pos = caculator.pos + new Vector2(BuffGameScoreCaculator.width, -40f);

                    if (Mathf.Approximately(alpha, 1f))
                    {
                        alpha = 1f;
                        lastAlpha = 1f;
                        scoreDisplay.setAlpha = 1f;
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
                scoreDisplay.DrawSprites(timeStacker);
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
