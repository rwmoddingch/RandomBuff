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

        BuffCardRenderer _renderer;

        public void Init(BuffCardRenderer buffCardRenderer, Texture texture)
        {
            _renderer = buffCardRenderer;
            float widthFactor = texture.width / 300f;
            //BuffPlugin.Log($"{buffCardRenderer._buffStaticData.BuffID} width factor : {widthFactor}");
            _MeshRenderer.material.SetFloat("_HighlighWidth", 30f * widthFactor);
            _MeshRenderer.material.SetFloat("_HighlighExpose", 4f);
            _MeshRenderer.material.SetFloat("_EdgeHighLightStrength", 0f);
            _MeshRenderer.material.SetFloat("_EdgeHighLightStrength", _EdgeHighLightStrength);
            _MeshRenderer.material.SetFloat("_EdgeHighLightTimeFactor", _EdgeHighLightTimeFactor);
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
