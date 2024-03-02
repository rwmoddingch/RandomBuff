using HUD;
using RandomBuff.Core.Buff;
using RandomBuff.Core.Entry;
using RandomBuffUtils;
using RandomBuffUtils.BuffEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;

namespace BuiltinBuffs.Duality
{
    internal class ParrotFashionBuff : Buff<ParrotFashionBuff, ParrotFashionBuffData>
    {
        public override BuffID ID => ParrotFashionBuffEntry.repetitionBuffID;
        ExtraDialogBoxInstance[] currentextraDialogInstances;

        public ParrotFashionBuff()
        {
            BuffEvent.OnExtraDialogsCreated += BuffEvent_OnExtraDialogsCreated;
        }

        public override void Destroy()
        {
            BuffEvent.OnExtraDialogsCreated -= BuffEvent_OnExtraDialogsCreated;
        }

        private void BuffEvent_OnExtraDialogsCreated(ExtraDialogBoxInstance[] extraDialogInstances)
        {
            currentextraDialogInstances = extraDialogInstances;
            foreach(var instance in extraDialogInstances)
                instance.ActuallyCreateDialogBox();
        }

        public override void Update(RainWorldGame game)
        {
            base.Update(game);
            if (Input.GetKeyDown(KeyCode.M))
            {
                foreach(var instance in currentextraDialogInstances)
                {
                    instance.NewMessage("wawa instance", 0f, 0f, 0);
                }
            }
        }



        public void NewMessage(string text, float xOrientation, float yPos, int extraLinger)
        {
            foreach(var instance in currentextraDialogInstances)
            {
                instance.NewMessage(text, xOrientation, yPos, extraLinger);
            }
        }

        public void Interrupt()
        {
            foreach (var instance in currentextraDialogInstances)
            {
                instance.InterruptWithoutNewMessage();
            }
        }
    }

    internal class ParrotFashionBuffData : BuffData
    {
        public override BuffID ID => ParrotFashionBuffEntry.repetitionBuffID;
    }

    internal class ParrotFashionBuffEntry : IBuffEntry
    {
        public static BuffID repetitionBuffID = new BuffID("ParrotFashion", true);

        public void OnEnable()
        {
            BuffRegister.RegisterBuff<ParrotFashionBuff, ParrotFashionBuffData, ParrotFashionBuffEntry>(repetitionBuffID);
        }

        public static void HookOn()
        {
            On.HUD.DialogBox.NewMessage_string_float_float_int += DialogBox_NewMessage_string_float_float_int;
            On.HUD.DialogBox.Interrupt += DialogBox_Interrupt;
        }

        private static void DialogBox_Interrupt(On.HUD.DialogBox.orig_Interrupt orig, DialogBox self, string text, int extraLinger)
        {
            if (!ExtraDialogBoxInstance.IsExtraDialogBox(self))
                ParrotFashionBuff.Instance.Interrupt();
            orig.Invoke(self, text, extraLinger);
        }

        private static void DialogBox_NewMessage_string_float_float_int(On.HUD.DialogBox.orig_NewMessage_string_float_float_int orig, DialogBox self, string text, float xOrientation, float yPos, int extraLinger)
        {
            orig.Invoke(self, text, xOrientation, yPos, extraLinger);
            if (!ExtraDialogBoxInstance.IsExtraDialogBox(self))
                ParrotFashionBuff.Instance.NewMessage(text, xOrientation, yPos, extraLinger);
        }
    }
}
