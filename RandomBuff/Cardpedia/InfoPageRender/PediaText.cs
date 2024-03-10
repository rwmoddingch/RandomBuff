using BepInEx;
using RandomBuff.Render.CardRender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RWCustom;

namespace RandomBuff.Cardpedia.InfoPageRender
{
    public class PediaText : MonoBehaviour
    {
        public bool inited;
        public string _content;
        public PediaTextRenderer _renderer;
        public Color _textColor;
        public bool _isTitle;
        public float _textSize;
        public TextMesh textMesh;

        public Vector3 meshScale = new Vector3(1,1,0);
        public float TextSize
        {
            set
            {
                _textSize = 0.05f * value;
            }
        }

        public void Init(PediaTextRenderer renderer, string defaultContent, bool isTitle, float textSize, Color textColor, int id)
        {
            _renderer = renderer;
            _content = defaultContent;

            _isTitle = isTitle;
            _textSize = 0.03f * textSize;
            _textColor = textColor;

            var textObject = new GameObject($"PediaText_" + id);
            textObject.layer = 9;
            textObject.transform.localScale = meshScale;
            textObject.transform.localPosition = new Vector3(2000f * id,0,10);

            InitTextMesh(textObject);

            inited = true;
        }

        public void InitTextMesh(GameObject textObj)
        {
            textMesh = textObj.AddComponent<TextMesh>();           
            textMesh.text = _content;
            textMesh.color = _textColor;
            textMesh.font = _isTitle? CardBasicAssets.TitleOrigFont : CardBasicAssets.DiscriptionOrigFont;
            textMesh.alignment = _isTitle? TextAlignment.Center : TextAlignment.Left;
            textMesh.anchor = TextAnchor.UpperCenter;
            textMesh.fontSize = 100;
            textMesh.characterSize = 2.5f * _textSize;
            textObj.GetComponent<MeshRenderer>().material = textMesh.font.material;
        }

        
        #region
        public void RefreshText(string newContent, int rollLength)
        {
            if (!_isTitle)
            {                
                _content = GenerateLongText(newContent, rollLength);
            }
            else
            {
                _content = newContent;
            }
            
            textMesh.text = _content;
            if (_renderer.pediaCamera != null)
            {
                _renderer.pediaCamera.SetDirty = true;
            }
        }

        public static string GenerateLongText(string text, int rollLength)
        {
            if (rollLength == 0) return text;
            if (text.Length <= 0) return text;
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                builder.Append(text[i]);
                if (i != 0 && i % rollLength == 0)
                {                   
                    if(Custom.rainWorld.inGameTranslator.currentLanguage != InGameTranslator.LanguageID.Chinese)
                    {
                        if (!string.IsNullOrWhiteSpace(text[i].ToString()) && (i + 1) < text.Length && !string.IsNullOrWhiteSpace(text[i + 1].ToString()))
                        {
                            builder.Append("-");
                        }
                    }                   
                    builder.Append('\n');
                }               
            }
            return builder.ToString();
        }

        public static float GetPowLerpParam(float t, float pow = 3)
        {
            float a = 1f / Mathf.Pow(t + 1, pow);
            float b = 1f / Mathf.Pow(2, pow);

            return (1f - a) / (1f - b);
        }
        #endregion
    }
}
