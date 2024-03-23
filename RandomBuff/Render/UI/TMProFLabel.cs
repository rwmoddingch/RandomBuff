using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal class TMProFLabel : FGameObjectNode
    {
        GameObject tmproObject;
        RectTransform rectTransform;
        public TMP_Text tmpText;

        public string Text
        {
            get => tmpText.text;
            set => tmpText.text = value;
        }

        public TextAlignmentOptions Alignment
        {
            get => tmpText.alignment;
            set => tmpText.alignment = value;
        }

        public Color color
        {
            get => tmpText.color;
            set => tmpText.color = value;
        }

        public Vector2 Rect
        {
            get => rectTransform.sizeDelta * SizeFactor;
            set => rectTransform.sizeDelta = value / SizeFactor;
        }

        public Vector2 Pivot
        {
            get => rectTransform.pivot;
            set => rectTransform.pivot = value;
        }

        public bool AutoWrap
        {
            get => tmpText.enableWordWrapping;
            set => tmpText.enableWordWrapping = value;
        }

        float fontSize;
        public float FontSize
        {
            get => fontSize;
            set
            {
                if(value != fontSize)
                {
                    fontSize = value;
                    _isMatrixDirty = true;
                }
            }
        }

        Vector2 realRect;
        Vector2 scaledRealRect;
        public Vector2 TextRect
        {
            get
            {
                var renderValue = tmpText.GetRenderedValues();
                if (realRect != renderValue)
                {
                    realRect = renderValue;
                    float t = Camera.main.orthographicSize;
                    scaledRealRect = new Vector2(renderValue.x * (_scaleX * t), renderValue.y * (_scaleY * t));
                }
                return scaledRealRect;
            }
        }

        float SizeFactor => Camera.main.orthographicSize;

        public TMProFLabel(TMP_FontAsset font, string text, Vector2 rect, float fontSize = 1f)
        {
            tmproObject = new GameObject("TMProFLabel");
            rectTransform = tmproObject.AddComponent<RectTransform>();

            tmproObject.AddComponent<MeshRenderer>();
            tmpText = tmproObject.AddComponent<TextMeshPro>();

            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = rect / SizeFactor;

            tmpText.alignment = TextAlignmentOptions.Center;
            tmpText.font = font;
            FontSize = fontSize;
            tmpText.text = text;

            tmproObject.transform.localEulerAngles = Vector3.zero;

            tmpText.GetRenderedHeight();

            Init(tmproObject, true, true, false);
        }

        public override void Redraw(bool shouldForceDirty, bool shouldUpdateDepth)
        {
            shouldForceDirty = true;
            bool isMatrixDirty = this._isMatrixDirty;
            bool isAlphaDirty = this._isAlphaDirty;
            bool flag = false;
            this.UpdateDepthMatrixAlpha(shouldForceDirty, shouldUpdateDepth);
            if (shouldUpdateDepth)
            {
                flag = true;
                this.UpdateDepth();
            }
            if (isMatrixDirty || shouldForceDirty || shouldUpdateDepth)
            {
                flag = true;
            }
            if (isAlphaDirty || shouldForceDirty)
            {
                flag = true;
            }
            if (flag)
            {
                this.UpdateGameObject();
            }

            if (isMatrixDirty)
            {
                float t = Camera.main.orthographicSize;
                rectTransform.localScale = new Vector3(_scaleX * t, _scaleY * t, 1f);
                tmpText.fontSize = fontSize;
            }
            if(isAlphaDirty)tmpText.alpha = alpha;
        }
    }
}
