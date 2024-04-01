using RandomBuff.Core.Buff;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal class BuffCardTimer
    {
        public readonly float width = 8f;

        public FContainer container;
        public IOwnBuffTimer owner;

        int lastNumber;
        int adder;
        public int Number => Mathf.FloorToInt(floatCounter);
        public int NextNumber => Mathf.FloorToInt(floatCounter) + 1;

        public float lastFloatCounter;
        public float floatCounter;

        public int effectiveCurrentCount;
        public int effectiveNextCount;

        public Vector2 lastPos;
        public Vector2 pos;

        public float scale = 0.6f;

        public float setAlpha;
        public float lastAlpha;
        public float alpha;

        public List<SingleNumber> numbers = new List<SingleNumber>();

        public BuffCardTimer(FContainer container, IOwnBuffTimer owner)
        {
            this.container = container;
            this.owner = owner;
            floatCounter = Number;
            lastFloatCounter = floatCounter;
            lastNumber = Number;
        }

        public void Update()
        {
            lastPos = pos;
            lastFloatCounter = floatCounter;
            floatCounter = Mathf.Lerp(floatCounter, owner.Second, 0.15f);

            lastAlpha = alpha;
            //alpha = Mathf.Lerp(lastAlpha, setAlpha, 0.15f);

            if (lastNumber < Number)
                adder = -1;
            else if (lastNumber > Number)
                adder = 1;
            lastNumber = Number;

            foreach (var number in numbers)
            {
                number.Update();
            }
        }

        public void HardSetNumber()
        {
            floatCounter = owner.Second;
            lastFloatCounter = floatCounter;
        }

        public void DrawSprites(float timeStacker)
        {
            UpdateNumberText();
            foreach (var number in numbers)
            {
                number.DrawSprites(timeStacker);
            }
        }

        public void ClearSprites()
        {
            foreach (var number in numbers)
            {
                number.ClearSprites();
            }
            numbers.Clear();
        }

        void UpdateNumberText()
        {
            string currentText = Number.ToString();
            string nextText = NextNumber.ToString();

            effectiveCurrentCount = currentText.Length;
            effectiveNextCount = nextText.Length;

            if (numbers.Count < Mathf.Max(currentText.Length, nextText.Length))
            {
                for (int i = 0; i < Mathf.Max(currentText.Length, nextText.Length) - numbers.Count; i++)
                {
                    var number = new SingleNumber(this, numbers.Count);
                    numbers.Add(number);
                    number.InitiateSprite();
                }
            }

            foreach (var number in numbers)
            {
                int indexInTextCurrent = currentText.Length - number.digit - 1;
                int indexInTextNext = nextText.Length - number.digit - 1;

                if (indexInTextCurrent < 0)
                    number.CurrentText = "";
                else
                    number.CurrentText = currentText[indexInTextCurrent].ToString();

                if (indexInTextNext < 0)
                    number.NextText = "";
                else
                    number.NextText = nextText[indexInTextNext].ToString();
            }
        }


        public class SingleNumber
        {
            BuffCardTimer timer;
            public readonly int digit;

            FLabel num_1;
            FLabel num_2;

            public string CurrentText
            {
                get => num_1.text;
                set => num_1.text = value;
            }

            public string NextText
            {
                get => num_2.text;
                set => num_2.text = value;
            }

            /// <summary>
            ///
            /// </summary>
            /// <param name="cardTimer"></param>
            /// <param name="Digit">位数</param>
            public SingleNumber(BuffCardTimer cardTimer, int digit)
            {
                this.timer = cardTimer;
                this.digit = digit;
            }

            public void InitiateSprite()
            {
                num_1 = new FLabel(Custom.GetDisplayFont(), "") { isVisible = true, anchorX = 0.5f, anchorY = 0.5f };
                num_2 = new FLabel(Custom.GetDisplayFont(), "") { isVisible = true, anchorX = 0.5f, anchorY = 0.5f };

                timer.container.AddChild(num_1);
                timer.container.AddChild(num_2);
            }

            public void Update()
            {
            }

            public void DrawSprites(float timeStacker)
            {
                Vector2 center = Vector2.Lerp(timer.lastPos, timer.pos, timeStacker);
                Vector2 anchorPosCurrent = new Vector2(((timer.effectiveCurrentCount - 1) / 2f - digit) * timer.width * (timer.scale / 0.6f) + center.x, center.y);
                Vector2 anchirPosNext = new Vector2(((timer.effectiveNextCount - 1) / 2f - digit) * timer.width * (timer.scale / 0.6f) + center.x, center.y);

                float decimalPart = Mathf.Lerp(timer.lastFloatCounter, timer.floatCounter, timeStacker) - timer.NextNumber;

                while (decimalPart > 1)
                    decimalPart--;
                while (decimalPart < 0)
                    decimalPart++;

                float alpha = Mathf.Lerp(timer.lastAlpha, timer.alpha, timeStacker);
                if (num_1.text == num_2.text)
                    decimalPart = 0f;//两数相同的时候该位没有变化。

                num_1.SetPosition(anchorPosCurrent + new Vector2(0f, -Mathf.Sin(decimalPart * Mathf.PI / 2f) * timer.width * (timer.scale / 0.6f)));
                num_1.scaleY = Mathf.Cos(decimalPart * Mathf.PI / 2f) * timer.scale;
                num_1.alpha = Mathf.Cos(decimalPart * Mathf.PI / 2f) * alpha;
                num_1.scaleX = timer.scale;

                num_2.SetPosition(anchirPosNext + new Vector2(0f, Mathf.Sin((decimalPart + 1) * Mathf.PI / 2f) * timer.width * (timer.scale / 0.6f)));
                num_2.scaleY = Mathf.Cos((decimalPart - 1) * Mathf.PI / 2f) * timer.scale;
                num_2.scaleX = timer.scale;
                num_2.alpha = Mathf.Cos((decimalPart - 1) * Mathf.PI / 2f) * alpha;
            }

            public void ClearSprites()
            {
                num_1.RemoveFromContainer();
                num_2.RemoveFromContainer();
            }
        }

        internal interface IOwnBuffTimer
        {
            public int Second { get; }
        }
    }
}
