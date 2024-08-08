using Menu;
using Menu.Remix;
using MoreSlugcats;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Credit.CreditObject;
using RandomBuff.Render.CardRender;
using RandomBuff.Render.UI;
using RandomBuff.Render.UI.Component;
using Rewired;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Credit
{
    internal class BuffCreditMenu : Menu.Menu
    {
        RainEffect rainEffect;
        public float Time { get; private set; }

        BuffCreditStage stage;
        CreditFileReader creditFileReader;

        int currentStageIndex;
        bool quitCredit;
        bool songSwitched;

        public BuffCreditMenu(ProcessManager manager)
            : base(manager, BuffEnums.ProcessID.CreditID)
        {
            creditFileReader = new CreditFileReader();
            foreach(var line in creditFileReader.lines)
            {
                foreach (var chara in line)
                    CardBasicAssets.TitleFont.HasCharacter(chara, false, true);
            }
            CardBasicAssets.PauseLoadFont = true;

            pages.Add(new Page(this, null, "main", 0));
            rainEffect = new RainEffect(this, pages[0]);
            pages[0].subObjects.Add(rainEffect);

            int count = 0;
            List<BuffCardRenderer> renderers = new();
            foreach(var idValue in BuffID.values.entries)
            {
                var id = new BuffID(idValue);
                if (!BuffConfigManager.ContainsId(id))
                    continue;
                renderers.Add(CardRendererManager.GetRenderer(id));
                count++;
                if (count == 30)
                    break;
            }

            foreach(var renderer in renderers)
            {
                CardRendererManager.RecycleCardRenderer(renderer);
            }

            manager.musicPlayer.MenuRequestsSong("RW_63 - Wandering Cut", 1f, 1f);
        }

        public override void Update()
        {
            base.Update();
            if (quitCredit)
                return;

            if(stage == null)
            {
                if (currentStageIndex < creditFileReader.creditStagesAndData.Count)
                {
                    NextStage(creditFileReader.creditStagesAndData[currentStageIndex].Key, creditFileReader.creditStagesAndData[currentStageIndex].Value);
                    currentStageIndex++;
                }
                else
                    EndCredit();
            }
            else
            {
                if (stage.AllStageObjectRemoved)
                {
                    BuffPlugin.Log($"stage allow to remove");
                    stage.RemoveSprites();
                    pages[0].RemoveSubObject(stage);
                    stage = null;

                    if(currentStageIndex >= creditFileReader.creditStagesAndData.Count)
                    {
                        EndCredit();
                    }
                }
            }

            if (UnityEngine.Random.value < 0.00625f)
            {
                rainEffect.LightningSpike(Mathf.Pow(UnityEngine.Random.value, 2f) * 0.85f, Mathf.Lerp(20f, 120f, UnityEngine.Random.value));
            }

            if (RWInput.CheckPauseButton(0))
            {
                EndCredit();
                if (stage != null)
                {
                    stage.RequestRemove();
                    stage.RemoveSprites();
                }
            }

            if (!songSwitched && Time > 152f)
            {
                manager.musicPlayer.MenuRequestsSong("RW_77 - Fragile", 2f, 6f);
                songSwitched = true;
            }    
        }


        public void NextStage(CreditStageType creditStageType, CreditFileReader.CreditStageData stageData)
        {
            stage = new BuffCreditStage(this, pages[0], Time);
            pages[0].subObjects.Add(stage);
            Vector2 screenSize = Custom.rainWorld.options.ScreenSize;
            rainEffect.LightningSpike(Mathf.Pow(1f, 2f) * 0.85f, Mathf.Lerp(20f, 120f, 1f));

            int entriesAPage = 8;
            int y = 0;
            float inStageEnterTime = 4f;
            bool titleHasAnim = true;
            bool titleLongString = false;
            string? repalceTitle = null;
            BuffPlugin.Log($"inStage : {creditStageType} {inStageEnterTime + 1f * y}");


            if (creditStageType == CreditStageType.Coding)
            {
                int page = 0;
                var data = stageData as CreditFileReader.CodingStageData;
                for (int i = 0; i < data.names.Count; i++)
                {
                    int maxCount = Mathf.Min(entriesAPage, data.names.Count - page);
                    stage.AddObjectToStage(new NameDetailLabel(this, stage, new Vector2(150f, screenSize.y - 80f * (y + 2)), inStageEnterTime + 1f * y, 6f + 0.2f * (maxCount - y), data.names[i], data.details[i]));
                    //BuffPlugin.Log($"label {data.names[i]}, {inStageEnterTime + 1f * y}");
                    y++;
                    if (y == entriesAPage)
                    {
                        inStageEnterTime += entriesAPage * 1f + 14f;
                        y = 0;
                    }
                }
                inStageEnterTime += 6f;
            }
            else if(creditStageType == CreditStageType.ArtWorks)
            {
                var data = stageData as CreditFileReader.ArtWorksStageData;

                for(int i = 0;i < data.names.Count; i++)
                {
                    var shelf = new BuffCardDisplayShelf(this, stage, new Vector2(150f, screenSize.y - 80f * 3f), inStageEnterTime,  data.buffIDs[i]);
                    float buffTime = shelf.lifeTime;
                    stage.AddObjectToStage(new NameDetailLabel(this, stage, new Vector2(150f, screenSize.y - 80f * 2f), inStageEnterTime, buffTime, data.names[i], data.details[i]));
                    stage.AddObjectToStage(shelf);
                    inStageEnterTime += buffTime + 1f;
                }
            }
            else if(creditStageType == CreditStageType.PlayTest)
            {
                entriesAPage = 11;
                int page = 0;
                var data = stageData as CreditFileReader.PlayTestStageData;
                for (int i = 0; i < data.names.Count; i++)
                {
                    int maxCount = Mathf.Min(entriesAPage, data.names.Count - page);
                    stage.AddObjectToStage(new NameDetailLabel(this, stage, new Vector2(150f, screenSize.y - 60f * (y + 2)), inStageEnterTime + 1f * y, 8f + 0.2f * (maxCount - y), data.names[i], data.details[i]));
                    y++;
                    if (y == entriesAPage)
                    {
                        inStageEnterTime += entriesAPage * 1f + 6f + 0.2f * maxCount;
                        y = 0;
                        page += entriesAPage;
                    }
                }
                inStageEnterTime += 8f;
            }
            else if (creditStageType == CreditStageType.SpecialThanks)
            {
                int page = 0;
                var data = stageData as CreditFileReader.SpecialThanksStageData;

                for(int j = 0;j < data.entries.Count; j++)
                {
                    if (data.entries[j].Contains("NextPage"))
                    {
                        page++;
                        y = 0;
                        inStageEnterTime += 8f;
                        continue;
                    }

                    int currentEntireItems = data.entryNames[j].Count();
                    float currentEntryLastTime = (currentEntireItems + 1) * 0.5f + 8f;
                    stage.AddObjectToStage(new EntryDetailLabel(this, stage, new Vector2(150f, screenSize.y - 60f * (y + 2)), inStageEnterTime, currentEntryLastTime, data.entries[j] + ":", ""));
                    y++;
                    for (int i = 0;i < currentEntireItems; i++)
                    {
                        stage.AddObjectToStage(new NameDetailLabel(this, stage, new Vector2(200f, screenSize.y - 60f * (y + 2)), inStageEnterTime + 0.5f * (i + 1), currentEntryLastTime - (i + 1) * 0.5f, data.entryNames[j][i], data.entryDetails[j][i]));
                        y++;
                    }
                    inStageEnterTime += 0.5f * (currentEntireItems + 1);
                }
                inStageEnterTime += 8f;
            }
            else if(creditStageType == CreditStageType.Intro)
            {
                stage.AddObjectToStage(new FlagHolder(this, stage, 0f, 10f));
                inStageEnterTime = 8f;
                titleHasAnim = false;
            }
            else if(creditStageType == CreditStageType.ThankYou)
            {
                titleHasAnim = false;
                titleLongString = true;
                repalceTitle = GetCreditThanksString();
            }

            
            stage.AddObjectToStage(new IndividualCardTitle(this, stage, repalceTitle ?? BuffResourceString.Get($"CreditTitle_{creditStageType.value}",true), 0f,1f + inStageEnterTime, !titleHasAnim, titleLongString));
        }

        public string GetCreditThanksString()
        {
            return BuffResourceString.Get("CreditTitle_ThanksForPlaying");
        }

        public void EndCredit()
        {
            quitCredit = true;
            CardBasicAssets.PauseLoadFont = false;
            manager.musicPlayer.FadeOutAllSongs(3f);
            manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
        }

        public override void RawUpdate(float dt)
        {
            Time += dt;
            if (Input.GetKey(KeyCode.S))
                Time += dt * 5f;
            base.RawUpdate(dt);
            rainEffect.rainFade = Custom.SCurve(Mathf.InverseLerp(0f, 6f, Time), 0.8f) * 0.5f;
        }
    }
}
