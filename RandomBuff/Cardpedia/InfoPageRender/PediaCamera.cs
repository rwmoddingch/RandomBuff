using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia.InfoPageRender
{
    public class PediaCamera : MonoBehaviour
    {
        public Camera _camera;
        public RenderTexture targetTexture;
        public Vector2Int renderRange = new Vector2Int(700,500);

        [SerializeField] bool _isDirtyChanged = true;
        [SerializeField] bool _isDirty = false;

        public bool SetDirty
        {
            get => _isDirty;
            set
            {
                if( _isDirty != value)
                {
                    _isDirtyChanged = true;
                    _isDirty = value;
                }
            }
        }

        public void Init(string contentType, int id)
        {
            targetTexture = new RenderTexture(renderRange.x, renderRange.y, 0);
            RenderTexture.active = targetTexture;

            var camObject = new GameObject($"PediaCamera_" + contentType + "_" + id);
            camObject.transform.parent = transform;
            camObject.transform.localPosition = new Vector3(2000f * id, 0, 0);
            _camera = camObject.AddComponent<Camera>();
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = Color.green;
            _camera.targetTexture = targetTexture;
            _camera.cullingMask = 1 << 9;           
            Camera.main.cullingMask &= ~(1 << 9);

            SetDirty = false;
        }

        void Update()
        {
            if (_isDirtyChanged)
            {
                _camera.gameObject.SetActive(SetDirty);
                _isDirtyChanged = false;
                SetDirty = false;
            }
        }

        void OnDestroy()
        {
            targetTexture.Release();
        }
    }
}
