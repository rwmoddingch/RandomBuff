using RWCustom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using TMPro;

namespace RandomBuff.Render.CardRender
{
    internal class CardTextController : MonoBehaviour
    {
        static float fadeInOutModeLength = 1f;
        static float changeScrollDirModeLength = 4f;

        public TMP_Text textMesh;
        BuffCardRenderer _renderer;

        internal GameObject _textObjectA;

        bool _firstInit;
        bool _isTitle;

        Color _opaqueColor;
        Color _transparentColor;

        bool textNeedRefresh;
        string Text
        {
            get => textMesh.text;
            set
            {
                if(value != textMesh.text)
                {
                    textMesh.font.HasCharacters(value,out var _,false, true);
                    textMesh.text = value;
                    textNeedRefresh = true;
                }
            }
        }
        float textMeshLength;

        float _modeTimer;
        Mode currentMode = Mode.Scroll;
        float _maxScrollVel = 1f;
        float _scrolledLength;
        float _minScrolledLength;
        float _maxScrolledLength;
        bool _scrollReverse;


        float _alpha;
        float _targetAlpha = 0f;
        public bool Fade
        {
            get => _targetAlpha == 0f;
            set => _targetAlpha = value ? 0f : 1f;
        }

        TMP_CharacterInfo[] origCharInfos;
        Vector3[] origVertices;

        public void Init(BuffCardRenderer renderer, Transform parent, TMP_FontAsset font, Color color, string text, bool isTitle, InGameTranslator.LanguageID id)
        {
            _renderer = renderer;
            _opaqueColor = color;
            _isTitle = isTitle;
            _transparentColor = new Color(color.r, color.g, color.b, 0f);

            if (!_firstInit)
            {
                _textObjectA = new GameObject($"BuffCardTextObjectA");

                _textObjectA.layer = 8;
                textMesh = SetupTextMesh(_textObjectA, font, isTitle);
                _firstInit = true;
            }

            Text = text;

            SwitchMode(Mode.Scroll);
            UpdateTextMesh();

            TMP_Text SetupTextMesh(GameObject obj, TMP_FontAsset font, bool isTitle)
            {
                var rectTransform = obj.AddComponent<RectTransform>();
                rectTransform.SetParent(parent);

                obj.AddComponent<MeshRenderer>();
                var textMesh = obj.AddComponent<TextMeshPro>();

                textMesh.font = font;
                //textMesh.enableCulling = true;
                obj.transform.localEulerAngles = Vector3.zero;

                if (isTitle)
                {
                    textMesh.fontSize = id == InGameTranslator.LanguageID.Chinese ? 8 : 6;
                    rectTransform.pivot = new Vector2(0.5f, 0f);
                    textMesh.alignment = TextAlignmentOptions.Center;
                    rectTransform.sizeDelta = new Vector2(3f, 1f);
                    rectTransform.localPosition = new Vector3(0f, -0.5f, -0.01f);
                    textMesh.enableWordWrapping = false;
                    textMesh.margin = new Vector4(0.1f, 0f, 0f, 0f);

                    _maxScrollVel = 1f;
                }
                else
                {
                    textMesh.fontSize = 3;

                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    textMesh.alignment = TextAlignmentOptions.TopLeft;
                    rectTransform.sizeDelta = new Vector2(2.8f, 4.8f);
                    rectTransform.localPosition = new Vector3(0f, 0f, -0.01f);

                    _maxScrollVel = 0.25f;
                }
                return textMesh;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (textNeedRefresh)
            {
                RefreshTextInfo();
            }

            if (textMesh == null || origCharInfos == null || origVertices == null || textNeedRefresh)
                return;

            if (_alpha != _targetAlpha)
            {
                if (_alpha > _targetAlpha)
                    _alpha -= Time.deltaTime * 0.5f;
                else if (_alpha < _targetAlpha)
                    _alpha += Time.deltaTime * 0.5f;

                if ((_targetAlpha == 1f && _alpha > _targetAlpha) || (_targetAlpha == 0f && _alpha < _targetAlpha))
                    _alpha = _targetAlpha;
                UpdateTextMesh(_alpha == _targetAlpha);
            }

            if (_isTitle)
            {
                if (_renderer.normal.z < 0)
                    return;
            }
            else
            {
                if (_renderer.normal.z >= 0)
                    return;
            }

            if (_alpha == 0f)
                return;

            if (currentMode == Mode.Scroll)
            {
                if (!_scrollReverse)
                {
                    if (_scrolledLength < _maxScrolledLength)
                    {
                        float lerped = Mathf.Lerp(_scrolledLength, _scrolledLength + _maxScrollVel, 0.05f) - _scrolledLength;
                        float maxStep = _maxScrollVel * Time.deltaTime;
                        if (lerped > maxStep)
                            lerped = maxStep;

                        _scrolledLength += lerped;
                        if (_scrolledLength >= _maxScrolledLength)
                        {
                            _scrolledLength = _maxScrolledLength;
                            SwitchMode(Mode.ChangeScrollDir);
                        }
                        UpdateTextMesh();
                    }
                }
                else
                {
                    if (_scrolledLength > _minScrolledLength)
                    {
                        float lerped = Mathf.Lerp(_scrolledLength, _scrolledLength - _maxScrollVel, 0.05f) - _scrolledLength;
                        float maxStep = -_maxScrollVel * Time.deltaTime;
                        if (lerped < maxStep)
                            lerped = maxStep;

                        _scrolledLength += lerped;
                        if (_scrolledLength <= _minScrolledLength)
                        {
                            _scrolledLength = _minScrolledLength;
                            SwitchMode(Mode.ChangeScrollDir);
                        }
                        UpdateTextMesh();
                    }
                }
            }
            if (currentMode == Mode.ChangeScrollDir)
            {
                if (_modeTimer < changeScrollDirModeLength)
                    _modeTimer += Time.deltaTime;
                else
                {
                    _scrollReverse = !_scrollReverse;
                    SwitchMode(Mode.Scroll);
                }
            }
        }

        internal void SwitchMode(Mode newMode)
        {
            if (newMode == currentMode)
                return;
            currentMode = newMode;
            _modeTimer = 0f;
        }

        /// <summary>
        /// 根据参数t更新文本的状态，t在0到1之间
        /// </summary>
        /// <param name="t"></param>
        void UpdateTextMesh(bool forceUpdate = false)
        {
            if (_isTitle)
            {
                if (_renderer.normal.z < 0 && !forceUpdate)
                    return;

                for (int i = 0; i < textMesh.textInfo.characterCount; i++)
                {
                    var charInfo = textMesh.textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                        continue;

                    var verts = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                    var colors = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                    for (int v = 0; v < 4; v++)
                    {
                        var orig = origVertices[charInfo.vertexIndex + v];
                        verts[charInfo.vertexIndex + v] = orig + new Vector3(-_scrolledLength, 0f, 0f);

                        colors[charInfo.vertexIndex + v] = Color.Lerp(_transparentColor, _opaqueColor, _alpha * (1.4f - Mathf.Abs(verts[charInfo.vertexIndex + v].x)) / 0.2f);
                    }
                }
            }
            else
            {
                if (_renderer.normal.z >= 0 && !forceUpdate)
                    return;

                for (int i = 0; i < textMesh.textInfo.characterCount; i++)
                {
                    var charInfo = textMesh.textInfo.characterInfo[i];
                    if (!charInfo.isVisible)
                        continue;

                    var verts = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
                    var colors = textMesh.textInfo.meshInfo[charInfo.materialReferenceIndex].colors32;
                    for (int v = 0; v < 4; v++)
                    {
                        var orig = origVertices[charInfo.vertexIndex + v];
                        verts[charInfo.vertexIndex + v] = orig + new Vector3(0f, _scrolledLength, 0f);

                        colors[charInfo.vertexIndex + v] = Color.Lerp(_transparentColor, _opaqueColor, _alpha * (2.45f - Mathf.Abs(verts[charInfo.vertexIndex + v].y)) / 0.2f);
                    }
                }
            }

            textMesh.textInfo.meshInfo[0].mesh.vertices = textMesh.textInfo.meshInfo[0].vertices;
            textMesh.textInfo.meshInfo[0].mesh.colors32 = textMesh.textInfo.meshInfo[0].colors32;
            for (int i = 0; i < textMesh.textInfo.meshInfo.Length; i++)
            {
                textMesh.UpdateGeometry(textMesh.textInfo.meshInfo[i].mesh, i);
            }

            _renderer.cardCameraController.CardDirty = true;
        }

        void RefreshTextInfo()
        {
            textMesh.ForceMeshUpdate(true, true);
            origCharInfos = new TMP_CharacterInfo[textMesh.textInfo.characterInfo.Length];
            Array.Copy(textMesh.textInfo.characterInfo, origCharInfos, origCharInfos.Length);

            var vert = textMesh.textInfo.meshInfo[0].vertices;
            origVertices = new Vector3[vert.Length];
            Array.Copy(vert, origVertices, vert.Length);

            BuffPlugin.LogDebug($"copied vertices : orig length : {textMesh.textInfo.meshInfo[0].vertices.Length}, {origVertices.Length}");

            if (_isTitle)
            {
                float xLeft = float.MaxValue;
                float xRight = float.MinValue;

                foreach (var chara in origCharInfos)
                {
                    if (!chara.isVisible)
                        continue;

                    for (int v = 0; v < 4 && chara.vertexIndex + v < origVertices.Length; v++)
                    {
                        Debug.Log($"vertex : {chara.vertexIndex + v} , chara : {chara.vertexIndex}");
                        var vertex = origVertices[chara.vertexIndex + v];
                        if (vertex.x < xLeft)
                            xLeft = vertex.x;
                        if (vertex.x > xRight)
                            xRight = vertex.x;
                    }
                }
                textMeshLength = xRight - xLeft;
                _maxScrolledLength = Mathf.Max(xRight - 1.4f, 0f);
                _minScrolledLength = Mathf.Min(xLeft + 1.4f, 0f);
                _scrolledLength = _minScrolledLength;
            }
            else
            {
                float yDown = float.MaxValue;
                float yUp = float.MinValue;

                foreach (var chara in origCharInfos)
                {
                    if (!chara.isVisible)
                        continue;

                    for (int v = 0; v < 4 && chara.vertexIndex + v < origVertices.Length; v++)
                    {
                        var vertex = origVertices[chara.vertexIndex + v];
                        if (vertex.y < yDown)
                            yDown = vertex.y;
                        if (vertex.y > yUp)
                            yUp = vertex.y;
                    }
                }
                textMeshLength = yUp - yDown;
                _maxScrolledLength = Mathf.Max(textMeshLength - 4.8f, 0f);
                _minScrolledLength = 0f;
                _scrolledLength = _minScrolledLength;

                Debug.Log($"{yUp}, {yDown}");
            }

            Debug.Log($"Refresh Text Info, length:{textMeshLength}");
            textNeedRefresh = false;
        }

        internal enum Mode
        {
            Wait,
            Scroll,
            ChangeScrollDir,
        }
    }
}
