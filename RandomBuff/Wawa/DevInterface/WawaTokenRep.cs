using DevInterface;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Wawa.WawaDevInterface
{
    internal class WawaTokenRep : ResizeableObjectRepresentation
    {
        int lineSprite;
        public WawaTokenRep(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj)
            : base(owner, IDstring, parentNode, pObj, "WawaToken", false)
        {
            subNodes.Add(new WawaTokenPanel(owner, "Wawa_Token_Panel", this, new Vector2(0f, 100f)));
            (subNodes.Last() as WawaTokenPanel).pos = (pObj.data as WawaTokenData).panelPos;
            fSprites.Add(new FSprite("pixel", true));
            lineSprite = fSprites.Count - 1;
            owner.placedObjectsContainer.AddChild(fSprites[lineSprite]);
            fSprites[lineSprite].anchorY = 0f;
        }

        public override void Refresh()
        {
            base.Refresh();
            MoveSprite(lineSprite, absPos);
            fSprites[lineSprite].scaleY = (subNodes[1] as WawaTokenPanel).pos.magnitude;
            fSprites[lineSprite].rotation = Custom.AimFromOneVectorToAnother(absPos, (subNodes[1] as WawaTokenPanel).absPos);
            (pObj.data as WawaTokenData).panelPos = (subNodes[1] as Panel).pos;
        }
    }

    internal class WawaTokenPanel : Panel, IDevUISignals
    {
        Button lButton, rButton;
        DevUILabel tLabel;

        WawaTokenData TokenData => (parentNode as WawaTokenRep).pObj.data as WawaTokenData;

        public WawaTokenPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(120f, 30f), "Wawa Token")
        {
            subNodes.Add(tLabel = new DevUILabel(owner, "Wawa_Token_Label", this, new Vector2(45f, 5f), 35f, ""));
            subNodes.Add(lButton = new Button(owner, "Wawa_Button_L", this, new Vector2(5f, 5f),  35f,"-"));
            subNodes.Add(rButton = new Button(owner, "Wawa_Button_R", this, new Vector2(85f, 5f), 35f, "+"));
            
            tLabel.Text = TokenData.convFile.ToString();
        }

        public void Signal(DevUISignalType type, DevUINode sender, string message)
        {
            if(lButton == sender)
            {
                TokenData.convFile--;
                TokenData.convFile = Mathf.Max(0, TokenData.convFile);
            }
            else if(rButton == sender)
            {
                TokenData.convFile++;
            }
            tLabel.Text = TokenData.convFile.ToString();
        }
    }
}
