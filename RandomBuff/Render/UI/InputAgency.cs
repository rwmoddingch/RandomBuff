using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RandomBuff.Render.UI
{
    internal partial class InputAgency
    {
        static DefaultInput defaultInput = new DefaultInput();
        static GamepadInput gamepadInput = new GamepadInput();

        public static InputAgency Current { get; private set; } = defaultInput;
        public static AgencyType CurrentAgencyType { get; private set; } = AgencyType.Default;

        public static void SwitchAgency(AgencyType newType)
        {
            if (CurrentAgencyType == newType)
                return;
            CurrentAgencyType = newType;

            InputAgency last = Current;
            switch (CurrentAgencyType)
            {
                case AgencyType.Default:
                    Current = defaultInput;
                    break;
                case AgencyType.Gamepad:
                    Current = gamepadInput;
                    break;
            }
            Current.SwitchBy(last);
        }

        public static void AllRelease()
        {
            defaultInput.Release();
            gamepadInput.Release();
        }

        internal static void StaticUpdate()
        {
            Current.Update();
        }

        internal static void Init()
        {
            On.MainLoopProcess.Update += MainLoopProcess_Update;
        }

        static void MainLoopProcess_Update(On.MainLoopProcess.orig_Update orig, MainLoopProcess self)
        {
            orig.Invoke(self);
            if (self.manager.currentMainLoop == self)
                StaticUpdate();
        }

        public enum AgencyType
        {
            Default,
            Gamepad
        }
    }

    internal partial class InputAgency
    {
        protected CardInteractionManager lastInteractionManager;
        protected CardInteractionManager currentFocusInteraction;

        public virtual Vector2 GetMousePosition()
        {
            throw new NotImplementedException();
        }

        public virtual void GetMainFunctionButton(out bool down, out bool trigger)
        {
            throw new NotImplementedException();
        }

        public virtual void GetSecondaryFunctionButton(out bool down, out bool trigger)
        {
            throw new NotImplementedException();
        }

        public virtual void GetToggleHUDButton(out bool down, out bool trigger)
        {
            throw new NotImplementedException();
        }

        public virtual void GetKeyBindButton(out bool down, out bool trigger)
        {
            throw new NotImplementedException();
        }

        public virtual float GetScroll()
        {
            throw new NotImplementedException();
        }
    
        public virtual void TakeFocus(CardInteractionManager cardInteractionManager)
        {
            lastInteractionManager = currentFocusInteraction;
            currentFocusInteraction = cardInteractionManager;
            BuffPlugin.Log($"InputAgency {(lastInteractionManager != null ? lastInteractionManager.GetType().Name : "null")} => {(currentFocusInteraction != null ? currentFocusInteraction.GetType().Name : "null")}");
        }

        public virtual void RecoverLastFocus(bool dontRecordThis = false)
        {
            TakeFocus(lastInteractionManager);
            if(dontRecordThis) 
                lastInteractionManager = null;
        }

        public virtual void RecoverLastIfIsFocus(CardInteractionManager cardInteractionManager, bool dontRecordThis = false)
        {
            if (currentFocusInteraction == cardInteractionManager)
                RecoverLastFocus(dontRecordThis);
        }

        public virtual void Release()
        {
            BuffPlugin.Log($"InputAgency release {(lastInteractionManager != null ? lastInteractionManager.GetType().Name : "null")} | {(currentFocusInteraction != null ? currentFocusInteraction.GetType().Name : "null")}");

            lastInteractionManager = null;
            currentFocusInteraction = null;
        }

        protected virtual void SwitchBy(InputAgency inputAgency)
        {
        }

        protected virtual void Update()
        {
        }
    }

    /// <summary>
    /// 基本的鼠标操控模式
    /// </summary>
    internal class DefaultInput : InputAgency
    {
        Helper.InputButtonTracker mainFuncButtonTracker;
        bool mainFuncDown, mainFuncTrigger;

        Helper.InputButtonTracker secondaryFuncButtonTracker;
        bool secFuncDown, secFuncTrigger;

        Helper.InputButtonTracker toggleHUDButtonTracker;
        bool toggleHUDDown, toggleHUDTrigger;

        Helper.InputButtonTracker keyBinderButtonTracker;
        bool keyBinderDown, keyBinderTrigger;

        Vector2 _mousePosition;

        public DefaultInput()
        {
            mainFuncButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(0));
            secondaryFuncButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(1));
            toggleHUDButtonTracker = new Helper.InputButtonTracker(() => Input.GetKey(KeyCode.Tab));
            keyBinderButtonTracker = new Helper.InputButtonTracker(() => Input.GetKey(KeyCode.CapsLock));
        }

        public override Vector2 GetMousePosition()
        {
            return _mousePosition;
        }

        public override void GetKeyBindButton(out bool down, out bool trigger)
        {
            down = keyBinderDown;
            trigger = keyBinderTrigger;
        }

        public override void GetMainFunctionButton(out bool down, out bool trigger)
        {
            down = mainFuncDown;
            trigger = mainFuncTrigger;
        }

        public override void GetSecondaryFunctionButton(out bool down, out bool trigger)
        {
            down = secFuncDown; 
            trigger = secFuncTrigger;
        }

        public override void GetToggleHUDButton(out bool down, out bool trigger)
        {
            down = toggleHUDDown; 
            trigger = toggleHUDTrigger;
        }

        public override float GetScroll()
        {
            return Input.GetAxis("Mouse ScrollWheel");
        }

        protected override void Update()
        {
            _mousePosition = Input.mousePosition;

            mainFuncButtonTracker.Update(out mainFuncTrigger, out _);
            mainFuncDown = Input.GetMouseButton(0);

            secondaryFuncButtonTracker.Update(out secFuncTrigger, out _);
            secFuncDown = Input.GetMouseButton(1);

            toggleHUDButtonTracker.Update(out toggleHUDTrigger, out _);
            toggleHUDDown = Input.GetKey(KeyCode.Tab);

            keyBinderButtonTracker.Update(out keyBinderDown, out _);
            keyBinderDown = Input.GetKey(KeyCode.CapsLock);
        }
    }

    //TODO: 手柄控制器支持
    internal class GamepadInput : InputAgency
    {
    }
}
