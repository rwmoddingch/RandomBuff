using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardGraphTextController : MonoBehaviour
    {
        public static Color paleBlack = Color.black * 0.85f + Color.white * 0.15f;

        protected BuffCardRenderer _renderer;
        protected GameObject _hoverPoint;
        protected GameObject _graphOuter;
        protected GameObject _graphInner;
        protected MeshRenderer _graphInnerRenderer;
        protected GameObject _stackerTextObj;

        protected TextMesh graphTextMesh;

        protected Vector3 _origScale;
        protected float _targetWidth;
        protected float _currentWidth;

        protected bool _firstInit;

        public bool Show
        {
            get => _targetWidth != 0;
            set
            {
                _targetWidth = value ? _origScale.x : 0f;
            }
        }

        public virtual void Init(BuffCardRenderer renderer, Transform parent, Font font, Color color, string text, Vector3 rotation, InternalPrimitiveType primitiveType = InternalPrimitiveType.Quad,  float posFactor = 0.309f )
        {
            _renderer = renderer;

            if (!_firstInit)
            {
                _hoverPoint = new GameObject("StackerTextHoverPoint");
                _hoverPoint.transform.parent = parent;
                _hoverPoint.transform.localPosition = new Vector3(-0.5f, posFactor, 0f);
                _origScale = _hoverPoint.transform.localScale;

                _graphOuter = CreatePrimitiveObject(primitiveType);
                _graphOuter.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
                _graphOuter.transform.parent = _hoverPoint.transform;
                _graphOuter.transform.localPosition = Vector3.forward * -0.001f;
                _graphOuter.transform.localRotation = Quaternion.Euler(rotation);
                _graphOuter.layer = 8;
                _graphOuter.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardBasicShader;
                if (_graphOuter.TryGetComponent<Collider>(out var collider))
                    Destroy(collider);

                _graphInner = CreatePrimitiveObject(primitiveType);
                _graphInner.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                _graphInner.transform.parent = _hoverPoint.transform;
                _graphInner.transform.localPosition = Vector3.forward * -0.002f;
                _graphInner.transform.localRotation = Quaternion.Euler(rotation);
                _graphInner.layer = 8;
                _graphInnerRenderer = _graphInner.GetComponent<MeshRenderer>();
                _graphInnerRenderer.material.shader = CardBasicAssets.CardBasicShader;
                _graphInnerRenderer.material.color = Color.black * 0.8f + Color.white * 0.2f;
                _graphInnerRenderer.material
                    .SetColor("_Color", paleBlack);
                if (_graphInnerRenderer.TryGetComponent<Collider>(out var collider1))
                    Destroy(collider1);

                _stackerTextObj = new GameObject("StackerText");
                _stackerTextObj.transform.parent = _hoverPoint.transform;
                _stackerTextObj.transform.localPosition = Vector3.forward * -0.01f;
                _stackerTextObj.layer = 8;
                graphTextMesh = _stackerTextObj.AddComponent<TextMesh>();
                graphTextMesh.font = font;
                graphTextMesh.font.material.shader = CardBasicAssets.CardTextShader;

                graphTextMesh.fontSize = 100;
                graphTextMesh.characterSize = 0.01f * 2f;
                graphTextMesh.anchor = TextAnchor.MiddleCenter;
                graphTextMesh.alignment = TextAlignment.Center;
                graphTextMesh.color = Color.white;


                _hoverPoint.transform.localScale = new Vector3(0f, _origScale.y, _origScale.z);
                _firstInit = true;

            }
            _graphOuter.GetComponent<MeshRenderer>().material.color = color;
            _graphOuter.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            UpdateText();
        }

        public virtual void UpdateText()
        {
        }

        protected virtual void Update()
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

        protected GameObject CreatePrimitiveObject(InternalPrimitiveType internalPrimitiveType)
        {
            if (internalPrimitiveType == InternalPrimitiveType.Quad)
                return GameObject.CreatePrimitive(PrimitiveType.Quad);
            else if (internalPrimitiveType == InternalPrimitiveType.Circle)
                return CreatePrimitiveCircle(0.5f * 1.2f, 20, Vector3.zero);
            else if (internalPrimitiveType == InternalPrimitiveType.Hexagon)
                return CreatePrimitiveCircle(0.5f * 1.3f, 6, Vector3.zero);
            return null;
        }

        protected GameObject CreatePrimitiveCircle(float radius, int segments, Vector3 centerCircle)
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
            Circle,
            Hexagon
        }
    }

    internal class CardKeyBinderTextController : CardGraphTextController
    {
        string _bindKey;
        public string BindKey
        {
            get => _bindKey;
            set
            {
                if(value == _bindKey) return;
                _bindKey = value;
                UpdateText();
            }
        }

        bool _flash;
        public bool Flash
        {
            get => _flash;
            set
            {
                if (_flash == value)
                    return;

                if (!_flash && value)
                {
                    counter = 0f;
                }
                _flash = value;
            }
        }

        float counter;//0~0.2f~1f;

        Color curretColor;
        Color nextColor;

        public override void Init(BuffCardRenderer renderer, Transform parent, Font font, Color color, string text, Vector3 rotation, InternalPrimitiveType primitiveType = InternalPrimitiveType.Quad, float posFactor = 0.309F)
        {
            base.Init(renderer, parent, font, color, text, rotation, primitiveType, posFactor);
            BindKey = null;
            _graphInnerRenderer.material.color = paleBlack;   
        }

        public override void UpdateText()
        {
            string result = _bindKey;
            if (string.IsNullOrEmpty(result))
                result = ">>";
            graphTextMesh.text = result;
            //BuffPlugin.Log($"KeyBinder text set to {result}");
        }

        protected override void Update()
        {
            base.Update();
            if(_flash)
            {
                counter += Time.deltaTime;
                while (counter > 1f)
                    counter--;
            }
            else
            {
                if(counter < 1f)
                    counter += Time.deltaTime;
                else if(counter > 1f)
                {
                    counter = 1f;
                    UpdateColor();
                }        
            }

            if(counter < 1f || _flash)
            {
                UpdateColor();
            }

            void UpdateColor()
            {
                float t = 0.1f;
                float a = Mathf.Clamp(counter - t, 0f, t);
                float b = Mathf.Clamp(t - counter, 0f, t);
                Color next = Color.Lerp(paleBlack, Color.white, 1f - Mathf.Max(a, b) / t);
                if (next != curretColor)
                {
                    curretColor = next;
                    _graphInnerRenderer.material.color = curretColor;
                    _renderer.cardCameraController.CardDirty = true;
                }
            }
        }
    }

    internal class CardNumberTextController : CardGraphTextController
    {
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

        public override void UpdateText()
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
            graphTextMesh.text = result;
        }
    }
}