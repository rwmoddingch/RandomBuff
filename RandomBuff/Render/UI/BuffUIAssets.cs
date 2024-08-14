using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RandomBuff.Render.UI
{
    internal static class BuffUIAssets
    {
        public static string CardPickIconElement { get; private set; }
        public static string UILozengeElement { get; private set; }
        public static string CardInfo20 { get; private set; }
        public static string CardInfo40 { get; private set; }
        public static string Gradient30 { get; private set; }

        public static void LoadUIAssets()
        {
            CardPickIconElement = Futile.atlasManager.LoadImage("buffassets/illustrations/cardpickicon").name;
            UILozengeElement = Futile.atlasManager.LoadImage("buffassets/illustrations/uilozenge").name;
            CardInfo20 = Futile.atlasManager.LoadImage("buffassets/illustrations/cardinfo_20").name;
            CardInfo40 = Futile.atlasManager.LoadImage("buffassets/illustrations/cardinfo_40").name;
            Gradient30 = Futile.atlasManager.LoadImage("buffassets/illustrations/gradient30").name;
        }
    }
}
