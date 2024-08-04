using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Game;
using RandomBuff.Core.SaveData;
using RandomBuff.Render.UI;
using UnityEngine;

namespace RandomBuff.Core.BuffMenu.Test
{
    internal class TGachaMenu : Menu.Menu
    {
        public static ProcessManager.ProcessID TestGachaMenu = new ("TestGachaMenu",true);
        public TGachaMenu(ProcessManager.ProcessID lastID, RainWorldGame game, ProcessManager manager) : base(manager, TestGachaMenu)
        {
            this.game = game;
            lastId = lastID;
            pages.Add(new Menu.Page(this, null, "TGachaMenu", 0));
            pages[0].subObjects.Add(new Menu.SimpleButton(this, pages[0], "Exit", "ExitButton", new Vector2(1300, 50f), new Vector2(100f, 30f)));
            interactionManager = new TestBasicInteractionManager(null);
            //获取随机增益
            var cards = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 3, BuffType.Positive);
            
            //获取中性/减益
            var cards2 = BuffPicker.GetNewBuffsOfType(game.StoryCharacter, 3,  BuffType.Negative);

            for (int i = 0; i < 3; i++)
            {
                pickCard[i] = new BuffCard(cards[i].BuffID);
                container.AddChild(pickCard[i].Container);

                pickCard[i].Position = new Vector2(300 + 300 * i, 500f);
                interactionManager.ManageCard(pickCard[i]);
                pickCard[i].Container.MoveToBack();
                var id = cards[i].BuffID;
                pickCard[i].onMouseRightClick += () => OnPositiveClicked(id);
            }


            for (int i = 0; i < 3; i++)
            {
                pickCard2[i] = new BuffCard(cards2[i].BuffID);
                container.AddChild(pickCard2[i].Container);

                pickCard2[i].Position = new Vector2(300 + 300 * i, 500f);
                pickCard2[i].Alpha = 0;
                pickCard2[i].Container.MoveToBack();
                var id = cards2[i].BuffID;
                pickCard2[i].onMouseRightClick += () => OnNegativeClicked(id);
            }
        }

        public void OnPositiveClicked(BuffID id)
        {
            //BuffDataManager.Instance.GetOrCreateBuffData(id, true);
            for (int i = 0; i < 3; i++)
            {
                interactionManager.DismanageCard(pickCard[i]);
                interactionManager.ManageCard(pickCard2[i]);
            }
            counter = 0;
        }

        public void OnNegativeClicked(BuffID id)
        {
           // BuffDataManager.Instance.GetOrCreateBuffData(id, true);
            manager.RequestMainProcessSwitch(lastId);
        }

        private int counter = -1;
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "ExitButton")
            {
                ShutDownProcess();
            }
        }

        public override void Update()
        {
            base.Update();
            if (counter != -1)
                counter++;
            interactionManager.Update();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            foreach (var card in pickCard2)
            {
                card.Alpha = Mathf.InverseLerp(40, 80, counter + timeStacker);
            }
            foreach (var card in pickCard)
            {
                card.Alpha = Mathf.InverseLerp(40, 0, counter + timeStacker);
            }
            interactionManager.GrafUpdate(timeStacker);
        }


        //把oldProcess放回去
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            manager.oldProcess = game;
        }

        private RainWorldGame game;

        CardInteractionManager interactionManager;
        BuffCard[] pickCard = new BuffCard[3];
        BuffCard[] pickCard2 = new BuffCard[3];

        private ProcessManager.ProcessID lastId;

    }
}
