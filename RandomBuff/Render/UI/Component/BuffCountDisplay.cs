using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI.Component
{
    internal class BuffCountDisplay
    {
        public readonly float width = 8f;
        public static readonly float defaultScale = 0.6f;

        public static float lerpFactor = 0.15f;

        public FContainer container;
        public BuffCardTimer.IOwnBuffTimer owner;

        int currentCount;
        public int Count
        {
            get => currentCount;
            set
            {
                if (value == currentCount) return;
                currentCount = value;
                UpdateNumbers(currentCount);
            }
        }

        List<SingleNumber> numbers = new();

        public Alightment alightment = Alightment.Center;
        public float scale = defaultScale;

        public Vector2 pos;
        public Vector2 lastPos;

        public int effectiveCurrentCount;
        public int effectiveNextCount;

        public float lastAlpha;
        public float alpha;

        public Color color;
        public string shader = "";

        public BuffCountDisplay(FContainer container, BuffCardTimer.IOwnBuffTimer owner, Color? color = null, string shader = "")
        {
            this.owner = owner;
            this.container = container;
            this.color = color ?? Color.white;
            this.shader = shader;
            numbers.Add(new SingleNumber(this, 0));
        }

        public void Update()
        {
            lastPos = pos;
            lastAlpha = alpha;

            Count = owner.Second;

            foreach (var number in numbers)
                number.Update();
        }

        public void GrafUpdate(float timeStacker)
        {
            foreach(var number in numbers)
                number.GrafUpdate(timeStacker);
        }

        public void ClearSprites()
        {
            foreach(var number in numbers)
                number.ClearSprites();
        }

        public void HardSet()
        {
            Count = owner.Second;
            foreach (var number in numbers)
                number.HardSet();
        }

        public void UpdateEffectiveCount()
        {
            effectiveCurrentCount = effectiveNextCount = 0;
            foreach(var number in numbers)
            {
                if(number.EffectiveCurrent)
                    effectiveCurrentCount++;
                if(number.EffectiveNext)
                    effectiveNextCount++;
            }
        }

        public void UpdateNumbers(int numbers)
        {
            int currentDigit = 0;
            while(numbers > 0)
            {
                if (this.numbers.Count < currentDigit + 1)
                    this.numbers.Add(new SingleNumber(this, currentDigit));

                this.numbers[currentDigit].targetNumber = numbers;

                numbers /= 10;
                currentDigit++;
            }
        }

        public bool AtTarget()
        {
            foreach(var number in numbers)
            {
                if(!number.AtTarget)
                    return false;
            }
            return true;
        }

        internal class SingleNumber
        {
            readonly BuffCountDisplay countDisplay;
            readonly int digit;

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

            public int targetNumber = 0;

            float currentNumber = -1;
            int currentNumberInt = -2;
            int nextNumberInt = -1;

            public bool EffectiveCurrent => currentNumberInt >= 0;
            public bool EffectiveNext => nextNumberInt >= 0;

            public bool AtTarget => Mathf.Approximately(currentNumber, targetNumber) || !EffectiveCurrent;


            public SingleNumber(BuffCountDisplay countDisplay, int digit)
            {
                this.countDisplay = countDisplay;
                this.digit = digit;
                InitiateSprite();
            }

            public void InitiateSprite()
            {
                num_1 = new FLabel(Custom.GetDisplayFont(), "") { isVisible = true, anchorX = 0.5f, anchorY = 0.5f ,color = countDisplay.color};
                num_2 = new FLabel(Custom.GetDisplayFont(), "") { isVisible = true, anchorX = 0.5f, anchorY = 0.5f ,color = countDisplay.color};
                if (!string.IsNullOrEmpty(countDisplay.shader))
                {
                    num_1.shader = Custom.rainWorld.Shaders[countDisplay.shader];
                    num_2.shader = Custom.rainWorld.Shaders[countDisplay.shader];
                }

                countDisplay.container.AddChild(num_1);
                countDisplay.container.AddChild(num_2);
            }

            public void Update()
            {
                currentNumber = Mathf.Lerp(currentNumber, targetNumber, lerpFactor);
                int newCurrent = Mathf.FloorToInt(currentNumber);
                newCurrent = newCurrent % 10;
                if(newCurrent != currentNumberInt)
                {
                    currentNumberInt = newCurrent;
                    nextNumberInt = (currentNumberInt + 1) % 10;

                    UpdateText();
                    countDisplay.UpdateEffectiveCount();
                }
            }

            public void GrafUpdate(float timeStacker)
            {
                Vector2 center = Vector2.Lerp(countDisplay.lastPos, countDisplay.pos, timeStacker);
                Vector2 anchorPosCurrent;
                Vector2 anchorPosNext;

                if (countDisplay.alightment == Alightment.Center)
                {
                    anchorPosCurrent = new Vector2(((countDisplay.effectiveCurrentCount - 1) / 2f - digit) * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                    anchorPosNext = new Vector2(((countDisplay.effectiveNextCount - 1) / 2f - digit) * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                }
                else if (countDisplay.alightment == Alightment.Right)
                {
                    anchorPosCurrent = new Vector2(-digit * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                    anchorPosNext = new Vector2(-digit * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                }
                else
                {
                    anchorPosCurrent = new Vector2(((countDisplay.effectiveCurrentCount - 1) - digit) * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                    anchorPosNext = new Vector2(((countDisplay.effectiveNextCount - 1) - digit) * countDisplay.width * (countDisplay.scale / defaultScale) + center.x, center.y);
                }

                float decimalPart = (currentNumber % 10f) - currentNumberInt;
                float alpha = Mathf.Lerp(countDisplay.lastAlpha, countDisplay.alpha, timeStacker);

                num_1.SetPosition(anchorPosCurrent + new Vector2(0f, -Mathf.Sin(decimalPart * Mathf.PI / 2f) * countDisplay.width * (countDisplay.scale / 0.6f)));
                num_1.scaleY = Mathf.Cos(decimalPart * Mathf.PI / 2f) * countDisplay.scale;
                num_1.alpha = Mathf.Cos(decimalPart * Mathf.PI / 2f) * alpha;
                num_1.scaleX = countDisplay.scale;

                num_2.SetPosition(anchorPosNext + new Vector2(0f, Mathf.Sin((decimalPart + 1) * Mathf.PI / 2f) * countDisplay.width * (countDisplay.scale / 0.6f)));
                num_2.scaleY = Mathf.Cos((decimalPart - 1) * Mathf.PI / 2f) * countDisplay.scale;
                num_2.scaleX = countDisplay.scale;
                num_2.alpha = Mathf.Cos((decimalPart - 1) * Mathf.PI / 2f) * alpha;
            }

            public void ClearSprites()
            {
                num_1.RemoveFromContainer();
                num_2.RemoveFromContainer();
            }

            public void HardSet()
            {
                currentNumber = targetNumber;
                Update();
            }

            void UpdateText()
            {
                CurrentText = currentNumberInt < 0 ? "" : currentNumberInt.ToString();
                NextText = nextNumberInt < 0 ? "" : nextNumberInt.ToString();
            }
        }

        internal enum Alightment
        {
            Center,
            Left,
            Right,
        }
    }
}
