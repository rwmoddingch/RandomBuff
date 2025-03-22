using RandomBuff.Core.Buff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.CardRender
{
    internal class PlainWhiteCardRenderer : BuffCardRendererBase
    {
        public override void Init(int id, BuffStaticData buffStaticData)
        {
            _id = id;
            _cardTextureFront = CardBasicAssets.PlainWiteTex;
            _cardTextureBack = CardBasicAssets.PlainWiteTex;

            if (!_notFirstInit)
            {
                FirstInit(id, buffStaticData);
            }
            else
            {
                cardCameraController.CardDirty = true;
            }

            DuplicateInit(id, buffStaticData);
        }

        protected override void FirstInit(int id, BuffStaticData buffStaticData)
        {
            base.FirstInit(id, buffStaticData);

            _cardQuadFront.GetComponent<MeshRenderer>().enabled = true;
            _cardQuadBack.GetComponent<MeshRenderer>().enabled = true;

            cardHighlightFrontController.enabled = true;
            cardHighlightBackController.enabled = true;
        }

        protected override void DuplicateInit(int id, BuffStaticData buffStaticData)
        {
            base.DuplicateInit(id, buffStaticData);
        }

        public override string Salt()
        {
            return $"PlainWhiteCardRenderer{_id}";
        }
    }
}
