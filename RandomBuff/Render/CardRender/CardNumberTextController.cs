using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardNumberTextController : MonoBehaviour
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

        int _value = 0;
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
            get => _addOne;
            set
            {
                if(value != _addOne)
                {
                    _addOne = value;
                    UpdateText();
                }
            }
        }

        public virtual void Init(BuffCardRenderer renderer, Transform parent, Font font, Color color, string text, InternalPrimitiveType primitiveType = InternalPrimitiveType.Quad, float posFactor = 0.309f)
        {
            _renderer = renderer;

            if (!_firstInit)
            {
                _hoverPoint = new GameObject("StackerTextHoverPoint");
                _hoverPoint.transform.parent = parent;
                _hoverPoint.transform.localPosition = new Vector3(-0.5f, posFactor, 0f);
                _origScale = _hoverPoint.transform.localScale;

                _lozengeQuadOuter = CreatePrimitiveObject(primitiveType);
                _lozengeQuadOuter.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                _lozengeQuadOuter.transform.parent = _hoverPoint.transform;
                _lozengeQuadOuter.transform.localPosition = Vector3.forward * -0.001f;
                _lozengeQuadOuter.transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 45f));
                _lozengeQuadOuter.layer = 8;
                _lozengeQuadOuter.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardBasicShader;


                _lozengeQuadInner = CreatePrimitiveObject(primitiveType);
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

        GameObject CreatePrimitiveObject(InternalPrimitiveType internalPrimitiveType)
        {
            if (internalPrimitiveType == InternalPrimitiveType.Quad)
                return GameObject.CreatePrimitive(PrimitiveType.Quad);
            else if (internalPrimitiveType == InternalPrimitiveType.Circle)
                return CreatePrimitiveCircle(0.5f * 1.2f, 20, Vector3.zero);
            return null;
        }

        GameObject CreatePrimitiveCircle(float radius, int segments, Vector3 centerCircle)
        {
            GameObject result = new GameObject("");
            result.AddComponent<MeshFilter>();
            result.AddComponent<MeshRenderer>();
            //meshRenderer.material = new Material();

            //¶¥µã
            Vector3[] vertices = new Vector3[segments + 1];
            vertices[0] = centerCircle;
            float deltaAngle = Mathf.Deg2Rad * 360f / segments;
            float currentAngle = 0;
            for (int i = 1; i < vertices.Length; i++)
            {
                float cosA = Mathf.Cos(currentAngle);
                float sinA = Mathf.Sin(currentAngle);
                vertices[i] = new Vector3(cosA * radius + centerCircle.x, sinA * radius + centerCircle.y, 0);
                currentAngle += deltaAngle;
            }

            //Èý½ÇÐÎ
            int[] triangles = new int[segments * 3];
            for (int i = 0, j = 1; i < segments * 3 - 3; i += 3, j++)
            {
                triangles[i] = 0;
                triangles[i + 1] = j + 1;
                triangles[i + 2] = j;
            }
            triangles[segments * 3 - 3] = 0;
            triangles[segments * 3 - 2] = 1;
            triangles[segments * 3 - 1] = segments;

            //===========================UV============================
            Vector2[] newUV = new Vector2[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                newUV[i] = new Vector2(vertices[i].x / radius / 2 + 0.5f, vertices[i].y / radius / 2 + 0.5f);
            }

            Mesh mesh = result.GetComponent<MeshFilter>().mesh;
            mesh.Clear();

            mesh.vertices = vertices;
            mesh.uv = newUV;
            mesh.triangles = triangles;

            return result;
        }

        internal enum InternalPrimitiveType
        {
            Quad,
            Circle
        }
    }
}