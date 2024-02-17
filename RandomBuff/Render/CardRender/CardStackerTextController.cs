using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardStackerTextController : MonoBehaviour
    {
        BuffCardRenderer _renderer;
        GameObject _hoverPoint;
        GameObject _lozengeQuadOuter;
        GameObject _lozengeQuadInner;
        GameObject _stackerTextObj;

        TextMesh stackerTextMesh;

        Vector3 _origScale;
        float _targetWidth;
        float _currentWidth;

        bool _firstInit;

        public bool Show
        {
            get => _targetWidth != 0;
            set
            {
                _targetWidth = value ? _origScale.x : 0f;
            }
        }

        int _value = -1;
        public int Value
        {
            get => _value;
            set
            {
                if(value != _value)
                {
                    _value = value;
                    UpdateText();
                }
            }
        }

        bool _addOne;
        public bool AddOne
        {
            get => AddOne;
            set
            {
                if(value != _addOne)
                {
                    _addOne = value;
                    UpdateText();
                }
            }
        }

        public void Init(BuffCardRenderer renderer, Transform parent, Font font, Color color, string text)
        {
            _renderer = renderer;

            if (!_firstInit)
            {
                _hoverPoint = new GameObject("StackerTextHoverPoint");
                _hoverPoint.transform.parent = parent;
                _hoverPoint.transform.localPosition = new Vector3(-0.5f, 0.618f * 0.5f, 0f);
                _origScale = _hoverPoint.transform.localScale;

                _lozengeQuadOuter = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _lozengeQuadOuter.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                _lozengeQuadOuter.transform.parent = _hoverPoint.transform;
                _lozengeQuadOuter.transform.localPosition = Vector3.forward * -0.001f;
                _lozengeQuadOuter.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 45f));
                _lozengeQuadOuter.layer = 8;
                _lozengeQuadOuter.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardBasicShader;


                _lozengeQuadInner = GameObject.CreatePrimitive(PrimitiveType.Quad);
                _lozengeQuadInner.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                _lozengeQuadInner.transform.parent = _hoverPoint.transform;
                _lozengeQuadInner.transform.localPosition = Vector3.forward * -0.002f;
                _lozengeQuadInner.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 45f));
                _lozengeQuadInner.layer = 8;
                _lozengeQuadInner.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardBasicShader;
                _lozengeQuadInner.GetComponent<MeshRenderer>().material.color = Color.black * 0.8f + Color.white * 0.2f;
                _lozengeQuadInner.GetComponent<MeshRenderer>().material
                    .SetColor("_Color", Color.black * 0.8f + Color.white * 0.2f);


                _stackerTextObj = new GameObject("StackerText");
                _stackerTextObj.transform.parent = _hoverPoint.transform;
                _stackerTextObj.transform.localPosition = Vector3.forward * -0.003f;
                _stackerTextObj.layer = 8;
                stackerTextMesh = _stackerTextObj.AddComponent<TextMesh>();
                stackerTextMesh.font = font;
                stackerTextMesh.font.material.shader = CardBasicAssets.CardTextShader;

                stackerTextMesh.fontSize = 100;
                stackerTextMesh.characterSize = 0.01f * 2f;
                stackerTextMesh.anchor = TextAnchor.MiddleCenter;
                stackerTextMesh.alignment = TextAlignment.Center;
                stackerTextMesh.color = Color.white;
              

                _hoverPoint.transform.localScale = new Vector3(0f, _origScale.y, _origScale.z);
                _firstInit = true;

            }
            _lozengeQuadOuter.GetComponent<MeshRenderer>().material.color = color;
            _lozengeQuadOuter.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            UpdateText();
        }

        void Start()
        {

        }

        void UpdateText()
        {
            string result = "";
            if (AddOne)
            {
                if (Value > 0)
                    result = $"{Value}+1";
                else
                    result = $"+1";
            }
            else
                result = Value.ToString();
            stackerTextMesh.text = result;
        }

        // Update is called once per frame
        void Update()
        {
            if (_currentWidth != _targetWidth)
            {
                _currentWidth = Mathf.Lerp(_currentWidth, _targetWidth, 0.1f);
                if (Mathf.Abs(_currentWidth - _targetWidth) < 0.01f)
                {
                    _currentWidth = _targetWidth;
                }
                _hoverPoint.transform.localScale = new Vector3(_currentWidth, _origScale.y, _origScale.z);
                _renderer.cardCameraController.CardDirty = true;
            }
        }
    }

}