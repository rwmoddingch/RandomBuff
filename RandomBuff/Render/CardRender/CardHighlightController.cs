using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardHighlightController : MonoBehaviour
    {
        //边缘高光控制
        bool _edgeHighlight;
        internal bool EdgeHighlight
        {
            get => _edgeHighlight;
            set
            {
                _edgeHighlight = value;
                _targetEdgeHighLightStrength = value ? 1f : 0f;
                //if (value)
                //{
                //    _EdgeHighLightTimeFactor = 0f;
                //    _EdgeHighlightBreakTimer = 0f;
                //}
            }
        }

        float _saturation = 1f;
        float _targetSaturation = 1f;
        internal bool Grey
        {
            get => _targetSaturation != 1f;
            set
            {
                _targetSaturation = value ? 0.1f : 1f;
            }
        }

        float _darkGradient;
        float _targetDarkGradient;
        internal bool DarkGradient
        {
            get => _targetDarkGradient == 1f;
            set => _targetDarkGradient = value ? 1f : 0f;
        }

        [SerializeField] float _targetEdgeHighLightStrength;
        [SerializeField] float _EdgeHighLightStrength;
        [SerializeField] float _EdgeHighLightTimeFactor;
        [SerializeField] float _EdgeHighlightBreakTimer;


        MeshRenderer _meshRenderer;
        MeshRenderer _MeshRenderer
        {
            get
            {
                if (_meshRenderer == null)
                {
                    _meshRenderer = GetComponent<MeshRenderer>();
                }
                return _meshRenderer;
            }
        }

        BuffCardRendererBase _renderer;

        public void Init(BuffCardRendererBase buffCardRenderer, Texture texture, bool isFront)
        {
            _renderer = buffCardRenderer;
            //float widthFactor = texture.width / 300f;
            //BuffPlugin.Log($"{buffCardRenderer._buffStaticData.BuffID} width factor : {widthFactor}");
            _MeshRenderer.material.SetFloat("_HighlighWidth", 60f);
            _MeshRenderer.material.SetFloat("_HighlighExpose", 4f);
            _MeshRenderer.material.SetFloat("_EdgeHighLightStrength", 0f);
            _MeshRenderer.material.SetFloat("_EdgeHighLightStrength", _EdgeHighLightStrength);
            _MeshRenderer.material.SetFloat("_EdgeHighLightTimeFactor", _EdgeHighLightTimeFactor);
            _MeshRenderer.material.SetFloat("_Saturation", _saturation);

            if(buffCardRenderer._buffStaticData != null && buffCardRenderer._buffStaticData.MultiLayerFace && isFront)
            {
                BuffPlugin.Log($"Enable key world : {"MultiLayer"}");
                _MeshRenderer.material.EnableKeyword("MultiLayer");
                _MeshRenderer.material.SetInt("_LayerCount", buffCardRenderer._buffStaticData.FaceLayer);
                _MeshRenderer.material.SetFloat("_MaxLayerDepth", buffCardRenderer._buffStaticData.MaxFaceDepth);
                _MeshRenderer.material.SetColor("_BackgroundCol", buffCardRenderer._buffStaticData.FaceBackgroundColor);
            }
            else
            {
                _MeshRenderer.material.DisableKeyword("MultiLayer");
            }

        }

        void Update()
        {
            if (_EdgeHighLightStrength > 0f || _EdgeHighLightStrength != _targetEdgeHighLightStrength)
            {
                if (_EdgeHighLightStrength != _targetEdgeHighLightStrength)
                {
                    _EdgeHighLightStrength = Mathf.Lerp(_EdgeHighLightStrength, _targetEdgeHighLightStrength, 0.05f);

                    if (Mathf.Abs(_EdgeHighLightStrength - _targetEdgeHighLightStrength) < 0.01f)
                        _EdgeHighLightStrength = _targetEdgeHighLightStrength;

                    _MeshRenderer.material.SetFloat("_EdgeHighLightStrength", _EdgeHighLightStrength);
                    _renderer.cardCameraController.CardDirty = true;
                }

                if (_EdgeHighlightBreakTimer == 0f)
                {
                    _EdgeHighLightTimeFactor += Time.deltaTime / 2f;

                    if (_EdgeHighLightTimeFactor >= 1)
                        _EdgeHighLightTimeFactor = 0f;

                    _MeshRenderer.material.SetFloat("_EdgeHighLightTimeFactor", _EdgeHighLightTimeFactor);
                    _renderer.cardCameraController.CardDirty = true;
                }

                if (_EdgeHighLightTimeFactor == 0f)
                {
                    _EdgeHighlightBreakTimer += Time.deltaTime;

                    if (_EdgeHighlightBreakTimer >= 1f)
                        _EdgeHighlightBreakTimer = 0f;
                }
            }

            if(_saturation != _targetSaturation)
            {
                _saturation = Mathf.Lerp(_saturation, _targetSaturation, 0.05f);
                if(Mathf.Abs(_saturation -  _targetSaturation) < 0.01f)
                    _saturation = _targetSaturation;

                _MeshRenderer.material.SetFloat("_Saturation", _saturation);
                _renderer.cardCameraController.CardDirty = true;
            }

            if(_targetDarkGradient != _darkGradient)
            {
                _darkGradient = Mathf.Lerp(_darkGradient, _targetDarkGradient, 0.05f);
                if (Mathf.Abs(_darkGradient - _targetDarkGradient) < 0.01f)
                    _darkGradient = _targetDarkGradient;

                _MeshRenderer.material.SetFloat("_DarkGradient", _darkGradient);
                _renderer.cardCameraController.CardDirty = true;
            }
        }

        public void UpdateRotaiton(Vector3 direction)
        {
            float rotMax = direction.x + direction.y;
            float rotation = rotMax / 90f;

            while (rotation > 1)
                rotation--;

            while (rotation < 0)
                rotation++;

            _MeshRenderer.material.SetFloat("_HighlighBias", rotation);
        }
    }
}
