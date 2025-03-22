
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa.WawaDevInterface
{
    internal class WawaTokenData : PlacedObject.ResizableObjectData
    {
        public Vector2 panelPos;
        public int convFile;

        public WawaTokenData(PlacedObject owner) : base(owner)
        {
        }

        public override void FromString(string s)
        {
            string[] array = Regex.Split(s, "~");
            handlePos.x = float.Parse(array[0]);
            handlePos.y = float.Parse(array[1]);
            panelPos.x = float.Parse(array[2]);
            panelPos.y = float.Parse(array[3]);
            convFile = int.Parse(array[4]);
            unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 5);
        }

        public override string ToString()
        {
            string res = $"{handlePos.x}~{handlePos.y}~{panelPos.x}~{panelPos.y}~{convFile}";
            res = SaveState.SetCustomData(this, res);
            return SaveUtils.AppendUnrecognizedStringAttrs(res, "~", unrecognizedAttributes);
        }
    }
}
