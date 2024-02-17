using RWCustom;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardTextController : MonoBehaviour
    {
        static float waitModeLength = 5f;
        static float stepModeLength = 1f;
        static float fadeInOutModeLength = 1f;


        public TextMesh textMeshA;
        public TextMesh textMeshB;
        BuffCardRenderer _renderer;

        internal GameObject _textObjectA;
        internal GameObject _textObjectB;

        bool _firstInit;
        bool _isTitle;

        Color _opaqueColor;
        Color _transparentColor;

        [SerializeField] string[] splitedTexts;

        Vector3 _origLocalScaleA;
        Vector3 _origLocalScaleB;

        float _modeTimer;
        int _currentTextIndex;
        int CurrentTextIndex
        {
            get => _currentTextIndex;
            set
            {
                if (_currentTextIndex == value)
                    return;
                _currentTextIndex = value;
                SetText();
            }
        }
        internal Mode currentMode;

        public string[] words;

        public void Init(BuffCardRenderer renderer, Transform parent, Font font, Color color, string text, bool isTitle, float size,InGameTranslator.LanguageID id)
        {
            _renderer = renderer;
            _opaqueColor = color;
            _isTitle = isTitle;
            _transparentColor = new Color(color.r, color.g, color.b, 0f);

            if (!_firstInit)
            {
                _textObjectA = new GameObject($"BuffCardTextObjectA");
                _textObjectA.transform.parent = parent;
                _origLocalScaleA = _textObjectA.transform.localScale;
                _textObjectA.layer = 8;
                textMeshA = SetupTextMesh(_textObjectA, font, isTitle, size);

                _textObjectB = new GameObject($"BuffCardTextObjectB");
                _textObjectB.transform.parent = parent;
                _origLocalScaleB = _textObjectB.transform.localScale;
                _textObjectB.layer = 8;
                textMeshB = SetupTextMesh(_textObjectB, font, isTitle, size);

                _firstInit = true;
            }

            SplitText(text, isTitle, id);
            SetText();

            SwitchMode(Mode.FadeOut);
            UpdateTextMesh(0f);

            TextMesh SetupTextMesh(GameObject obj, Font font, bool isTitle, float size)
            {
                var textMesh = obj.AddComponent<TextMesh>();
                textMesh.font = font;
                textMesh.font.material.shader = CardBasicAssets.CardTextShader;
                obj.GetComponent<MeshRenderer>().material = textMesh.font.material;
                //obj.GetComponent<MeshRenderer>().material.renderQueue = 2000;

                textMesh.fontSize = 100;
                textMesh.characterSize = 0.01f * size;
                obj.transform.localEulerAngles = Vector3.zero;

                if (isTitle)
                {
                    textMesh.anchor = TextAnchor.LowerCenter;
                    textMesh.alignment = TextAlignment.Center;
                    obj.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                }
                else
                {
                    textMesh.anchor = TextAnchor.UpperLeft;
                    textMesh.alignment = TextAlignment.Left;
                    obj.transform.localPosition = new Vector3(-0.5f, 0.5f, 0f);
                }
                return textMesh;
            }
        }

        // Update is called once per frame
        void Update()
        {
            switch (currentMode)
            {
                case Mode.Step:
                    if (splitedTexts.Length == 1)
                    {
                        SwitchMode(Mode.Wait);
                    }
                    else
                    {
                        _modeTimer += Time.deltaTime;
                        if (_modeTimer >= stepModeLength)
                        {
                            SwitchMode(Mode.Wait);
                            CurrentTextIndex = GetNextTextIndex(1);
                        }
                        UpdateTextMesh(SmoothCurve(_modeTimer / stepModeLength));
                    }

                    break;
                case Mode.Wait:
                    _modeTimer += Time.deltaTime;
                    if (_modeTimer >= waitModeLength)
                    {
                        SwitchMode(Mode.Step);
                    }

                    break;
                case Mode.FadeIn:
                    _modeTimer += Time.deltaTime;
                    if (_modeTimer >= fadeInOutModeLength)
                    {
                        SwitchMode(Mode.Wait);
                    }
                    UpdateTextMesh((_modeTimer / fadeInOutModeLength));

                    break;
                case Mode.FadeOut:
                    if (_modeTimer > 0)
                    {
                        _modeTimer -= Time.deltaTime;
                        UpdateTextMesh((_modeTimer / fadeInOutModeLength));
                    }
                    else if (_modeTimer < 0)
                    {
                        _modeTimer = 0f;
                        UpdateTextMesh(0f);
                    }

                    break;
            }
        }

        internal void SwitchMode(Mode newMode)
        {
            if (currentMode == newMode)
                return;

            if (newMode == Mode.FadeOut)
            {
                if (_modeTimer > fadeInOutModeLength)
                    _modeTimer = fadeInOutModeLength;
            }
            else
            {
                _modeTimer = 0f;
            }
            currentMode = newMode;
        }

        void SplitText(string origText, bool isTitle, InGameTranslator.LanguageID id)
        {
            if (string.IsNullOrEmpty(origText))
            {
                splitedTexts = new string[1] { "" };
                return;
            }

            if (isTitle)
            {
                int maxCharInLine = 7;
                splitedTexts = new string[Mathf.CeilToInt(origText.Length / (float)maxCharInLine)];

                for (int i = 0; i < splitedTexts.Length; i++)
                {
                    splitedTexts[i] = origText.Substring(i * maxCharInLine, Mathf.Min(origText.Length - i * maxCharInLine, maxCharInLine)).Trim();
                }
            }
            else
            {
                if(id == InGameTranslator.LanguageID.Chinese)
                {
                    int maxCharInLine = 10;
                    int maxLine = 14;

                    List<string> pages = new List<string>();
                    StringBuilder builder = new StringBuilder();

                    origText = "  " + origText;

                    int charCounter = 0;
                    int lineCounter = 0;
                    for(int i = 0;i < origText.Length;i++)
                    {
                        builder.Append(origText[i]);
                        charCounter++;

                        if(lineCounter == maxCharInLine - 1)//最后一行预留省略号
                        {
                            if (charCounter == maxCharInLine - 1)
                                builder.Append("...\n");
                        }
                        else
                        {
                            if (charCounter == maxCharInLine)
                                builder.Append("\n");
                        }


                        if (builder[builder.Length - 1] == '\n')
                        {
                            lineCounter++;
                            charCounter = 0;
                        }

                        if(lineCounter == maxLine)
                        {
                            pages.Add(builder.ToString());
                            builder.Clear();

                            lineCounter = 0;
                            charCounter = 0;
                        }
                    }
                    if(builder.Length != 0)
                        pages.Add(builder.ToString());

                    splitedTexts = pages.ToArray();
                }
                else
                {
                    int maxCharInLine = 18;
                    int maxLine = 14;

                    string[] words = origText.Split(' ');
                    this.words = words;
                    words[0] = "  " + words[0];//第一行加缩进


                    int lineCounter = 0;
                    int charCount = 0;
                    List<string> pages = new List<string>();
                    StringBuilder builder = new StringBuilder();

                    for (int i = 0; i < words.Length; i++)
                    {
                        if (builder.Length == 0 && words[i].Length >= maxCharInLine)//过长的单词放到一行内
                        {
                            builder.AppendLine(words[i]);
                            lineCounter++;
                            charCount = 0;
                        }

                        int extraLength = lineCounter + 1 >= maxLine ? 5 : 1;//若为最后一行，则需要为省略号(...)预留空间,否则只需要计算额外空格的空间
                        if (charCount + words[i].Length + extraLength > maxCharInLine)//无法再放下一个单词,则直接添加行
                        {
                            if (lineCounter + 1 >= maxLine)
                                builder.Append(" ...");

                            builder.Append("\n");
                            charCount = 0;
                            lineCounter++;
                        }

                        if (lineCounter >= maxLine)//完成当前页
                        {
                            pages.Add(builder.ToString());
                            lineCounter = 0;
                            charCount = 0;
                            builder.Clear();
                        }

                        if (charCount > 0)
                        {
                            builder.Append(" ");
                            charCount++;
                        }

                        charCount += words[i].Length;
                        builder.Append(words[i]);
                    }
                    if (builder.Length != 0)
                    {
                        pages.Add(builder.ToString());
                    }

                    splitedTexts = pages.ToArray();
                }
            }
        }

        /// <summary>
        /// 根据参数t更新文本的状态，t在0到1之间
        /// </summary>
        /// <param name="t"></param>
        void UpdateTextMesh(float t)
        {
            if (_isTitle)
            {
                switch (currentMode)
                {
                    case Mode.Step:
                    case Mode.Wait:
                        textMeshA.color = Color.Lerp(_opaqueColor, _transparentColor, t);

                        if (splitedTexts.Length > 1)
                            textMeshB.color = Color.Lerp(_transparentColor, _opaqueColor, t);
                        else
                            textMeshB.color = _transparentColor;

                        _textObjectA.transform.localPosition = new Vector3(-0.5f * t, -0.5f, 0f);
                        _textObjectA.transform.localScale = new Vector3((1f - t) * _origLocalScaleA.x, _origLocalScaleA.y, _origLocalScaleA.z);
                        _textObjectB.transform.localPosition = new Vector3(0.5f * (1f - t), -0.5f, 0f);
                        _textObjectB.transform.localScale = new Vector3(t * _origLocalScaleB.x, _origLocalScaleB.y, _origLocalScaleB.z);

                        _renderer.cardCameraController.CardDirty = true;

                        break;

                    case Mode.FadeIn:
                    case Mode.FadeOut:
                        textMeshA.color = Color.Lerp(_transparentColor, _opaqueColor, t);
                        textMeshB.color = _transparentColor;

                        _renderer.cardCameraController.CardDirty = true;

                        break;
                }
            }
            else
            {
                switch (currentMode)
                {
                    case Mode.Step:
                    case Mode.Wait:
                        textMeshA.color = Color.Lerp(_opaqueColor, _transparentColor, t);

                        if (splitedTexts.Length > 1)
                            textMeshB.color = Color.Lerp(_transparentColor, _opaqueColor, t);
                        else
                            textMeshB.color = _transparentColor;

                        _textObjectA.transform.localPosition = new Vector3(-0.5f, 0.5f, 0f);
                        _textObjectA.transform.localScale = new Vector3(_origLocalScaleA.x, (1f - t) * _origLocalScaleA.y, _origLocalScaleA.z);
                        _textObjectB.transform.localPosition = new Vector3(-0.5f, -0.5f + t, 0f);
                        _textObjectB.transform.localScale = new Vector3(_origLocalScaleB.x, t * _origLocalScaleB.y, _origLocalScaleB.z);

                        _renderer.cardCameraController.CardDirty = true;

                        break;

                    case Mode.FadeIn:
                    case Mode.FadeOut:
                        textMeshA.color = Color.Lerp(_transparentColor, _opaqueColor, t);
                        textMeshB.color = _transparentColor;

                        _renderer.cardCameraController.CardDirty = true;

                        break;
                }
            }
        }

        int GetNextTextIndex(int step)
        {
            int result = _currentTextIndex + step;
            while (result >= splitedTexts.Length)
            {
                result -= splitedTexts.Length;
            }
            while (result < 0)
            {
                result += splitedTexts.Length;
            }
            return result;
        }

        void SetText()
        {
            textMeshA.text = splitedTexts[CurrentTextIndex];
            textMeshB.text = splitedTexts[GetNextTextIndex(1)];
        }

        float SmoothCurve(float t)
        {
            float res = 6f * Mathf.Pow(t, 5f) - 15f * Mathf.Pow(t, 4f) + 10 * Mathf.Pow(t, 3);
            return Mathf.Pow(res, 2f);
        }

        internal enum Mode
        {
            Wait,
            Step,
            FadeIn,
            FadeOut
        }
    }
}