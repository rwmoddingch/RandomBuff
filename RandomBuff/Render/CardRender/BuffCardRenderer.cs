using System;
using RandomBuff.Core.Buff;
using System.Collections;
using System.Collections.Generic;
using RWCustom;
using UnityEngine;
using System.Linq;

namespace RandomBuff.Render.CardRender
{
    internal class BuffCardRenderer : MonoBehaviour
    {
        //基础信息
        internal BuffStaticData _buffStaticData;
        internal int _id;
        internal Texture _cardTextureFront;
        internal Texture _cardTextureBack;
        //

        internal CardCameraController cardCameraController;
        internal CardHighlightController cardHighlightFrontController;
        internal CardHighlightController cardHighlightBackController;

        internal CardTextController cardTextFrontController;
        internal CardTextController cardTextBackController;

        internal CardNumberTextController cardStackerTextController;
        internal CardNumberTextController cardCycleCounterTextController;

        GameObject _cardQuadFront;
        MeshFilter _meshFilterFront;

        GameObject _cardQuadBack;

        /// <summary>
        /// 表示卡牌物体的旋转
        /// </summary>
        public Vector2 Rotation
        {
            get => _rotation;
            set
            {
                if (value == _rotation)
                    return;
                _rotation = value;
                _cardQuadFront.transform.rotation = Quaternion.Euler(new Vector3(value.x, value.y, 0f));
                _cardQuadBack.transform.rotation = Quaternion.Euler(new Vector3(-value.x, value.y + 180, 0f));
                cardHighlightFrontController.UpdateRotaiton(value);
                cardHighlightBackController.UpdateRotaiton(new Vector3(-value.x, value.y + 180, 0f));
                cardCameraController.CardDirty = true;

                var lst = GetVertexs(_meshFilterFront);
                normal = Vector3.Cross(lst[0] - lst[1], lst[0] - lst[2]).normalized;
            }
        }
        Vector2 _rotation = Vector2.one;
        internal Vector3 normal;

        /// <summary>
        /// 卡牌物体与相机的距离
        /// </summary>
        public float Depth
        {
            get => _depth;
            set
            {
                if (value == _depth)
                    return;
                _depth = value;
                _cardQuadFront.transform.localPosition = new Vector3(0, 0, value);
                _cardQuadBack.transform.localPosition = new Vector3(0, 0, value);
                cardCameraController.CardDirty = true;
            }
        }
        float _depth;

        public bool Grey
        {
            get => cardHighlightFrontController.Grey;
            set => cardHighlightFrontController.Grey = value;
        }

        public bool DisplayTitle
        {
            get => !cardTextFrontController.Fade;
            set => cardTextFrontController.Fade = !value;
        }

        public bool DisplayDiscription
        {
            get => !cardTextBackController.Fade;
            set => cardTextBackController.Fade = !value;
        }

        public bool EdgeHighlight
        {
            get => cardHighlightFrontController.EdgeHighlight;
            set
            {
                cardHighlightFrontController.EdgeHighlight = value;
                cardHighlightBackController.EdgeHighlight = value;
            }
        }

        //停用时的计时器，用来决定什么时候彻底移除该对象
        internal float inactiveTimer;
        bool _notFirstInit;

        public Vector2 testRotation;

        public void Init(int id, BuffStaticData buffStaticData)
        {
            try
            {
                _id = id;
                _buffStaticData = buffStaticData;
                _cardTextureFront = buffStaticData.GetFaceTexture();
                _cardTextureBack = buffStaticData.GetBackTexture();


                var info = _buffStaticData.GetCardInfo(Custom.rainWorld.inGameTranslator.currentLanguage);

                if (!_notFirstInit)
                {
                    cardCameraController = gameObject.AddComponent<CardCameraController>();
                    //初始化卡面和卡背
                    _cardQuadFront = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _cardQuadFront.transform.parent = transform;
                    _cardQuadFront.transform.localScale = new Vector3(3f, 5f, 1f);
                    _cardQuadFront.name = $"BuffCardQuadFront_{id}";
                    _cardQuadFront.layer = 8;
                    _cardQuadFront.transform.localPosition = new Vector3(0, 0, 0);
                    _cardQuadFront.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardHighlightShader;
                    cardHighlightFrontController = _cardQuadFront.AddComponent<CardHighlightController>();
                    _meshFilterFront = _cardQuadFront.GetComponent<MeshFilter>();
                    //cardHighlightFrontController.Init(this, _cardTextureFront);

                    _cardQuadBack = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    _cardQuadBack.transform.parent = transform;
                    _cardQuadBack.transform.localScale = new Vector3(3f, 5f, 1f);
                    _cardQuadBack.name = $"BuffCardQuadBack_{id}";
                    _cardQuadBack.layer = 8;
                    _cardQuadBack.transform.localPosition = new Vector3(0, 0, 0);
                    _cardQuadBack.GetComponent<MeshRenderer>().material.shader = CardBasicAssets.CardHighlightShader;
                    cardHighlightBackController = _cardQuadBack.AddComponent<CardHighlightController>();
                    cardHighlightBackController.Init(this, _cardTextureBack);

                    Depth = 8.5f;
                    Rotation = Vector2.zero;

                    //初始化文本
                    cardTextFrontController = _cardQuadFront.AddComponent<CardTextController>();

                    cardTextBackController = _cardQuadBack.AddComponent<CardTextController>();

                    //初始化堆叠层数和轮回数显示
                    cardStackerTextController = gameObject.AddComponent<CardNumberTextController>();
                    cardCycleCounterTextController = gameObject.AddComponent<CardNumberTextController>();

                    //初始化专有相机
                    cardCameraController.Init(id);

                    _notFirstInit = true;
                }
                cardTextFrontController.Init(this, _cardQuadFront.transform, CardBasicAssets.TitleFont, _buffStaticData.Color, info.info.BuffName, true, info.id);
                cardTextBackController.Init(this, _cardQuadBack.transform, CardBasicAssets.DiscriptionFont, Color.white, info.info.Description, false, info.id);
                
                cardStackerTextController.Init(this, _cardQuadFront.transform, null, _buffStaticData.Color, (_buffStaticData.BuffID.GetBuffData()?.StackLayer ?? 1).ToString());
                cardCycleCounterTextController.Init(this, _cardQuadFront.transform, null, _buffStaticData.Color, (_buffStaticData.BuffID.GetBuffData()?.StackLayer ?? 1).ToString(), CardNumberTextController.InternalPrimitiveType.Circle, 0.618f * 0.3f);

                _cardQuadFront.GetComponent<MeshRenderer>().material.mainTexture = _cardTextureFront;
                _cardQuadBack.GetComponent<MeshRenderer>().material.mainTexture = _cardTextureBack;

                cardHighlightFrontController.Init(this, _cardTextureFront);
                cardHighlightBackController.Init(this, _cardTextureBack);
            }
            catch(Exception e)
            {
                BuffPlugin.LogError($"Exception in buffcard init : {_buffStaticData.BuffID}");
                BuffPlugin.LogException(e);
            }
        }

        List<Vector3> GetVertexs(MeshFilter meshFilter)
        {
            return meshFilter.sharedMesh.vertices.Select(v => meshFilter.gameObject.transform.TransformPoint(v)).ToList();
        }
    }
}
