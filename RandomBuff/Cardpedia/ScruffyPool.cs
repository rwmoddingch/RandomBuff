using RandomBuff.Cardpedia.InfoPageRender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Cardpedia
{
    public static class ScruffyPool
    {
        public static Queue<PediaTextRenderer> textRendererPool = new Queue<PediaTextRenderer>();

        public static PediaTextRenderer GetRenderer(int id)
        {
            if (textRendererPool.Count == 0)
            {
                var pediaTextManager = new GameObject($"PediaTextManager_" + id);
                var textRenderer = pediaTextManager.AddComponent<PediaTextRenderer>();
                textRenderer.gameObject.SetActive(true);
                return textRenderer;
            }
            else { return textRendererPool.Dequeue(); }
        }

        public static void RecycleRenderer(PediaTextRenderer pediaTextRenderer)
        {
            //pediaTextRenderer.gameObject.SetActive(false);
            textRendererPool.Enqueue(pediaTextRenderer);
        }
    }
}
