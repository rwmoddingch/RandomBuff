using RandomBuff.Core.Buff;
using RandomBuff.Render.UI.ExceptionTracker;
using RWCustom;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    /// <summary>
    /// 只渲染单个文本的卡牌
    /// 不使用 BuffStaticData
    /// </summary>
    internal class SingleTextCardRenderer : BuffCardRendererBase
    {
        public SingleTextController textController;

        public override void Init(int id, BuffStaticData buffStaticData)
        {
            try
            {
                _id = id;
                _cardTextureFront = CardBasicAssets.TextBack;
                _cardTextureBack = CardBasicAssets.TextBack;

                if (!_notFirstInit)
                {
                    FirstInit(id, buffStaticData);
                }
                else
                {
                    cardCameraController.CardDirty = true;
                }

                DuplicateInit(id, buffStaticData);
            }
            catch (Exception e)
            {
                BuffPlugin.LogError($"Exception in SingleTextCardRenderer init : {_buffStaticData.BuffID}");
                BuffPlugin.LogException(e);
                ExceptionTracker.TrackException(e, $"Exception in SingleTextCardRenderer init : {_buffStaticData.BuffID}");
            }
        }

        protected override void FirstInit(int id, BuffStaticData buffStaticData)
        {
            base.FirstInit(id, buffStaticData);

            _cardQuadFront.GetComponent<MeshRenderer>().enabled = false;
            _cardQuadBack.GetComponent<MeshRenderer>().enabled = false;

            cardHighlightFrontController.enabled = false;
            cardHighlightBackController.enabled = false;

            textController = gameObject.AddComponent<SingleTextController>();
        }

        protected override void DuplicateInit(int id, BuffStaticData buffStaticData)
        {
            //base.DuplicateInit(id, buffStaticData);
            textController.Init(this, _cardQuadFront.transform, CardBasicAssets.TitleFont, Color.white, "W", Custom.rainWorld.inGameTranslator.currentLanguage);
        }
    }

    internal class SingleTextController : MonoBehaviour
    {
        public TMP_Text textMesh;
        BuffCardRendererBase _renderer;
        internal GameObject _textObject;

        Color _opaqueColor;
        Color _transparentColor;

        bool _firstInit;
        bool _needInit;

        float _alpha = 1f;
        float _targetAlpha = 1f;
        public bool Fade
        {
            get => _targetAlpha == 0f;
            set => _targetAlpha = value ? 0f : 1f;
        }

        bool _textNeedUpdate;
        string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (value != _text)
                {
                    _textNeedUpdate = true;
                    _text = value;
                }
            }
        }

        public void Init(BuffCardRendererBase renderer, Transform parent, TMP_FontAsset font, Color color, string text, InGameTranslator.LanguageID id)
        {
            _renderer = renderer;
            _opaqueColor = color;
            _transparentColor = new Color(color.r, color.g, color.b, 0f);

            if (!_firstInit)
            {
                _textObject = new GameObject($"SingleTextObject");

                _textObject.layer = 8;
                textMesh = SetupTextMesh(_textObject, font);
                _firstInit = true;
            }

            textMesh.text = _text = " ";//刷新之前的文本
            Text = text;

            textMesh.fontSize = 30;
            var rectTransform = _textObject.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            textMesh.alignment = TextAlignmentOptions.Center;
            rectTransform.sizeDelta = new Vector2(3f, 1f);
            rectTransform.localPosition = new Vector3(0f, 0f, -0.01f);
            textMesh.enableWordWrapping = false;

            TMP_Text SetupTextMesh(GameObject obj, TMP_FontAsset font)
            {
                var rectTransform = obj.AddComponent<RectTransform>();
                rectTransform.SetParent(parent);

                obj.AddComponent<MeshRenderer>();
                var textMesh = obj.AddComponent<TextMeshPro>();

                textMesh.font = font;
                //textMesh.enableCulling = true;
                obj.transform.localEulerAngles = Vector3.zero;

                return textMesh;
            }
        }

        void OnDisable()
        {
            _alpha = 0f;
            _targetAlpha = 0f;
        }

        void Update()
        {
            if (_textNeedUpdate)
            {
                textMesh.font.HasCharacters(_text, out var missing, true, true);

                if (missing != null)
                {
                    string missed = "";
                    foreach (var character in missing)
                    {
                        missed += (char)character;
                    }
                    BuffPlugin.LogWarning($"Loading text : {_text}, missing characters : {missed}");
                }

                textMesh.text = _text;
                _textNeedUpdate = false;
                _renderer.cardCameraController.CardDirty = true;
                return;
            }

            if (_alpha != _targetAlpha)
            {
                if (_alpha > _targetAlpha)
                    _alpha -= Time.deltaTime * 0.5f;
                else if (_alpha < _targetAlpha)
                    _alpha += Time.deltaTime * 0.5f;

                if ((_targetAlpha == 1f && _alpha > _targetAlpha) || (_targetAlpha == 0f && _alpha < _targetAlpha))
                    _alpha = _targetAlpha;
                textMesh.alpha = _alpha;
            }
        }
    }
}
