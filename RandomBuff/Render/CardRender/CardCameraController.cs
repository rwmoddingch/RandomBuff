using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class CardCameraController : MonoBehaviour
    {
        public RenderTexture targetTexture;
        public Camera myCamera;

        [SerializeField] bool _cardDirtyChangeFlag = true;
        [SerializeField] bool _cardDirty = false;
        public bool forceUpdate;

        /// <summary>
        /// 控制相机是否启用渲染，只在必要的时候（比如卡牌发生变换）启用一帧渲染。
        /// </summary>
        public bool CardDirty
        {
            get => _cardDirty;
            set
            {
                if (value != _cardDirty)
                {
                    _cardDirtyChangeFlag = true;
                    _cardDirty = value;
                }
            }
        }

        public void Init(int id)
        {
            targetTexture = new RenderTexture(CardBasicAssets.RenderTextureSize.x, CardBasicAssets.RenderTextureSize.y, 0);

            var subObj = new GameObject($"CardCamera_{id}");
            subObj.transform.parent = transform;
            subObj.transform.localPosition = new Vector3(0f, 0f, 0f);
            myCamera = subObj.AddComponent<Camera>();
            myCamera.clearFlags = CameraClearFlags.SolidColor;
            myCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            myCamera.targetTexture = targetTexture;
            myCamera.cullingMask = 1 << 8;//第八层为卡牌渲染
            Camera.main.cullingMask &= ~(1 << 8);

            CardDirty = false;
        }

        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (_cardDirtyChangeFlag)
            {
                myCamera.gameObject.SetActive(CardDirty || forceUpdate);
                _cardDirtyChangeFlag = false;
                CardDirty = false;
            }
        }

        private void OnDestroy()
        {
            targetTexture.Release();
        }
    }
}