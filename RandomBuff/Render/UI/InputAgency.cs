using RWCustom;
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

        public static InputAgency Current { get; private set; } = gamepadInput;
        public static AgencyType CurrentAgencyType { get; private set; } = AgencyType.Gamepad;

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
            BuffPlugin.Log($"InputAgency switch to {newType}");
        }

        public static void AllRelease()
        {
            defaultInput.Release();
            gamepadInput.Release();
        }

        internal static void StaticUpdate()
        {
            defaultInput.Update();
            gamepadInput.Update();

            if (defaultInput.AnyInput() && CurrentAgencyType != AgencyType.Default)
                SwitchAgency(AgencyType.Default);
            if (gamepadInput.AnyInput() && CurrentAgencyType != AgencyType.Gamepad)
                SwitchAgency(AgencyType.Gamepad);
        }

        internal static void Init()
        {
            On.MainLoopProcess.Update += MainLoopProcess_Update;
        }

        static void MainLoopProcess_Update(On.MainLoopProcess.orig_Update orig, MainLoopProcess self)
        {
            if (self.manager.currentMainLoop == self || self is Menu.Menu)
                StaticUpdate();
            orig.Invoke(self);
        }

        public enum AgencyType
        {
            Default,
            Gamepad
        }
    }

    internal partial class InputAgency
    {
        protected IInputAgencyFocusable lastInteractionManager;
        protected IInputAgencyFocusable currentFocus;

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
    
        public virtual void TakeFocus(IInputAgencyFocusable cardInteractionManager)
        {
            lastInteractionManager = currentFocus;
            currentFocus = cardInteractionManager;
            BuffPlugin.Log($"InputAgency {(lastInteractionManager != null ? lastInteractionManager.GetType().Name : "null")} => {(currentFocus != null ? currentFocus.GetType().Name : "null")}");
        }

        public virtual void RecoverLastFocus(bool dontRecordThis = false)
        {
            TakeFocus(lastInteractionManager);
            if(dontRecordThis) 
                lastInteractionManager = null;
        }

        public virtual void RecoverLastIfIsFocus(CardInteractionManager cardInteractionManager, bool dontRecordThis = false)
        {
            if (currentFocus == cardInteractionManager)
                RecoverLastFocus(dontRecordThis);
        }

        public virtual void Release()
        {
            BuffPlugin.Log($"InputAgency release {(lastInteractionManager != null ? lastInteractionManager.GetType().Name : "null")} | {(currentFocus != null ? currentFocus.GetType().Name : "null")}");

            lastInteractionManager = null;
            currentFocus = null;
        }

        protected virtual void SwitchBy(InputAgency inputAgency)
        {
            currentFocus = inputAgency.currentFocus;
            lastInteractionManager = inputAgency.lastInteractionManager;
        }

        //是否有输入，用于判断是否应该切换控制代理
        protected virtual bool AnyInput()
        {
            return false;
        }

        protected virtual void Update()
        {
        }
    
        //用于强制更新鼠标位置，适用于某些有动画延迟的情况
        public virtual void ForceUpdateMousePosition(Vector2 pos)
        {
        }

        public virtual void ResetToDefaultPos()
        {
        }
    }

    /// <summary>
    /// 基本的鼠标操控模式
    /// </summary>
    internal sealed class DefaultInput : InputAgency
    {
        Helper.InputButtonTracker mainFuncButtonTracker;
        bool mainFuncDown, mainFuncTrigger;

        Helper.InputButtonTracker secondaryFuncButtonTracker;
        bool secFuncDown, secFuncTrigger;

        Helper.InputButtonTracker toggleHUDButtonTracker;
        bool toggleHUDDown, toggleHUDTrigger;

        Helper.InputButtonTracker keyBinderButtonTracker;
        bool keyBinderDown, keyBinderTrigger;

        Vector2 _lastMousePosition;
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

        protected override bool AnyInput()
        {
            return _lastMousePosition != _mousePosition;
        }

        protected override void Update()
        {
            _lastMousePosition = _mousePosition;
            _mousePosition = Futile.mousePosition;

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
        Vector2 _mousePosition;
        int zeroMousePositionTimer;
        bool overrideMainButton;

        int upDownHoldCounter;
        float scroll;

        Player.InputPackage _lastPackage;
        Player.InputPackage _currentPackage;

        Helper.InputButtonTracker mainFuncButtonTracker;
        bool mainFuncDown, mainFuncTrigger;

        Helper.InputButtonTracker secondaryFuncButtonTracker;
        bool secFuncDown, secFuncTrigger;

        Helper.InputButtonTracker toggleHUDButtonTracker;
        bool toggleHUDDown, toggleHUDTrigger;

        Helper.InputButtonTracker keyBinderButtonTracker;
        bool keyBinderDown, keyBinderTrigger;

        Helper.InputButtonTracker escButtonTracker;

        public GamepadInput()
        {
            mainFuncButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(0));
            secondaryFuncButtonTracker = new Helper.InputButtonTracker(() => Input.GetMouseButton(1));
            toggleHUDButtonTracker = new Helper.InputButtonTracker(() => Input.GetKey(KeyCode.Tab));
            keyBinderButtonTracker = new Helper.InputButtonTracker(() => Input.GetKey(KeyCode.CapsLock));
            escButtonTracker = new Helper.InputButtonTracker(() => RWInput.CheckPauseButton(0));
        }

        protected override void Update()
        {
            if (overrideMainButton)
                overrideMainButton = false;
            if (zeroMousePositionTimer > 0)
                zeroMousePositionTimer--;

            _lastPackage = _currentPackage;
            _currentPackage = RWInput.PlayerInput(0);

            toggleHUDButtonTracker.Update(out toggleHUDTrigger, out _);
            toggleHUDDown = Input.GetKey(KeyCode.Tab);

            keyBinderButtonTracker.Update(out keyBinderDown, out _);
            keyBinderDown = Input.GetKey(KeyCode.CapsLock);

            escButtonTracker.Update(out bool esc, out _);

            if (_currentPackage.y != 0)
                upDownHoldCounter++;
            else
                upDownHoldCounter = 0;

            if (upDownHoldCounter > 15)
                scroll = _currentPackage.y;
            else if(scroll != 0f)
            {
                scroll = 0f;
            }

            if (currentFocus != null && Current == this) 
            {
                if (currentFocus.CurrentFocusedObjectPos() != null)
                    SetMousePos(currentFocus.CurrentFocusedObjectPos().Value);

                if(_lastPackage.AnyDirectionalInput && !_currentPackage.AnyDirectionalInput)
                    SetMousePos(currentFocus.GetNextFocusableOjectPos(_lastPackage.analogueDir));

                if(RWInput.CheckPauseButton(0))
                {
                    zeroMousePositionTimer = 5;
                    SetMousePos(Vector2.zero);
                }
                if (esc)
                    overrideMainButton = true;
            }
        }

        void SetMousePos(Vector2 vector)
        {
            if (zeroMousePositionTimer > 0)
                _mousePosition = Vector2.zero;
            else
                _mousePosition = vector;
        }

        public override float GetScroll()
        {
            return scroll * 0.5f;
        }

        public override void GetToggleHUDButton(out bool down, out bool trigger)
        {
            down = toggleHUDDown;
            trigger = toggleHUDTrigger;
        }

        public override void GetKeyBindButton(out bool down, out bool trigger)
        {
            down = keyBinderDown;
            trigger = keyBinderTrigger;
        }

        public override void GetMainFunctionButton(out bool down, out bool trigger)
        {
            down = _currentPackage.thrw;
            trigger = (_currentPackage.thrw && !_lastPackage.thrw) || overrideMainButton;
        }

        public override void GetSecondaryFunctionButton(out bool down, out bool trigger)
        {
            down = _currentPackage.pckp;
            trigger = _currentPackage.pckp && !_lastPackage.pckp;
        }

        public override Vector2 GetMousePosition()
        {
            return _mousePosition;
        }

        public override void TakeFocus(IInputAgencyFocusable cardInteractionManager)
        {
            base.TakeFocus(cardInteractionManager);
            if (currentFocus == null)
                _mousePosition = Vector2.zero;
            else
                _mousePosition = currentFocus.GetDefaultFocusableObjectPos();
        }

        protected override void SwitchBy(InputAgency inputAgency)
        {
            base.SwitchBy(inputAgency);
            _mousePosition = inputAgency.GetMousePosition();
            _currentPackage = new Player.InputPackage();
            _lastPackage = new Player.InputPackage();
        }

        protected override bool AnyInput()
        {
            return _currentPackage.AnyInput;
        }

        public override void ResetToDefaultPos()
        {
            if (currentFocus != null)
                _mousePosition = currentFocus.GetDefaultFocusableObjectPos();
        }
    }

    public interface IInputAgencyFocusable
    {
        Vector2? CurrentFocusedObjectPos();
        Vector2 GetNextFocusableOjectPos(Vector2 inputDirection);
        Vector2 GetDefaultFocusableObjectPos();

        IEnumerable<Vector2> GetAllFocusableObjectPos();
    }
}
