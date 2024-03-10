using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia.InfoPageRender
{
    public class PediaTextRenderer : MonoBehaviour
    {
        public PediaCamera pediaCamera;
        public PediaText pediaText;

        public void Init(string contentType, int id, bool isTitle, float textSize)
        {           
            pediaCamera = gameObject.AddComponent<PediaCamera>();
            pediaText = gameObject.AddComponent<PediaText>();

            pediaCamera.Init(contentType, id);
            pediaText.Init(this, "Wa wa wa", isTitle, textSize, Color.red, id);
        }        

    }
}
