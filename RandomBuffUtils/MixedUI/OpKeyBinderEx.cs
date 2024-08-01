using Newtonsoft.Json.Linq;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace RandomBuffUtils.MixedUI
{
    public class OpKeyBinderEx : UIconfig
    {



        public static readonly string NONE = KeyCode.None.ToString();

        public const string GAMEPADICON = "GamepadIcon";

        public readonly string _controlKey;

        public readonly string _modID;

        public DyeableRect rect;

        public Color colorEdge = MenuColorEffect.rgbMediumGrey;

        public Color colorFill = MenuColorEffect.rgbBlack;

        public FLabel _label;

        public FSprite _sprite;

        public OpKeyBinder.BindController _bind;

        public bool _anyKeyDown;

        public bool _lastAnyKeyDown;

        public string _desError;

        public override string value
        {
            get => base.value;
            set
            {
                if (base.value == value)
                {
                    return;
                }
                base.value = value.Replace("Controller", "");
                Menu.PlaySound(SoundID.MENU_Button_Successfully_Assigned);
                Change();
            }
        }

        public OpKeyBinderEx(Configurable<string> config, Vector2 pos, Vector2 size, OpKeyBinder.BindController controllerNo = OpKeyBinder.BindController.AnyController)
            : base(config, pos, size)
        {
            if (config?.OI?.mod == null)
            {
                throw new ArgumentNullException("");
            }
            if (string.IsNullOrEmpty(defaultValue))
            {
                _value = NONE;
            }
            _modID = config.OI.mod.id;
            _controlKey = (cosmetic ? "_" : (_modID + "-" + Key));
            _size = new Vector2(Mathf.Max(30f, size.x), Mathf.Max(30f, size.y));
            _bind = controllerNo;
            base.defaultValue = value;
            _Initalize(defaultValue);
        }

        public void _Initalize(string defaultKey)
        {
            rect = new DyeableRect(myContainer, Vector2.zero, base.size)
            {
                fillAlpha = 0.3f
            };
            _label = UIelement.FLabelCreate(defaultKey, bigText: true);
            UIelement.FLabelPlaceAtCenter(_label, Vector2.zero, base.size);
            myContainer.AddChild(_label);
            _sprite = new FSprite("GamepadIcon")
            {
                anchorX = 0f,
                anchorY = 0.5f,
                scale = 0.333f
            };
            myContainer.AddChild(_sprite);
            Change();
        }

        public override string DisplayDescription()
        {
            if (!string.IsNullOrEmpty(_desError))
            {
                return _desError;
            }
            if (!string.IsNullOrEmpty(description))
            {
                return description;
            }
            if (base.MenuMouseMode)
            {
                if (!held)
                {
                    return OptionalText.GetText(OptionalText.ID.OpKeyBinder_MouseSelectTuto);
                }
                return OptionalText.GetText((!_IsJoystick(value)) ? OptionalText.ID.OpKeyBinder_MouseBindTuto : OptionalText.ID.OpKeyBinder_MouseJoystickBindTuto);
            }
            if (!held)
            {
                return OptionalText.GetText(OptionalText.ID.OpKeyBinder_NonMouseSelectTuto);
            }
            return OptionalText.GetText((!_IsJoystick(value)) ? OptionalText.ID.OpKeyBinder_NonMouseBindTuto : OptionalText.ID.OpKeyBinder_NonMouseJoystickBindTuto);
        }

        public static void _InitWrapped(MenuTabWrapper tabWrapper)
        {
            if (!Futile.atlasManager.DoesContainElementWithName("GamepadIcon"))
            {
                MenuIllustration menuIllustration = new MenuIllustration(tabWrapper.menu, tabWrapper, string.Empty, "GamepadIcon", Vector2.zero, crispPixels: true, anchorCenter: true);
                tabWrapper.subObjects.Add(menuIllustration);
                menuIllustration.sprite.isVisible = false;
            }
        }



        public static OpKeyBinder.BindController GetControllerForPlayer(int player)
        {
            if (player < 1 || player > 4)
            {
                throw new ElementFormatException("OpKeyBinderEx.GetControllerForPlayer threw error: Player number must be 1 ~ 4.");
            }
            return (OpKeyBinder.BindController)Custom.rainWorld.options.controls[player - 1].usingGamePadNumber;
        }

        public static KeyCode StringToKeyCode(string str)
        {
            return (KeyCode)Enum.Parse(typeof(KeyCode), str, ignoreCase: true);
        }

        public void SetController(OpKeyBinder.BindController controller)
        {
            string text = _ChangeBind(value, _bind, controller);
            _bind = controller;
            value = text;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            rect.colorEdge = bumpBehav.GetColor(colorEdge);
            rect.addSize = new Vector2(4f, 4f) * bumpBehav.AddSize;
            _sprite.color = bumpBehav.GetColor(colorEdge);
            if (greyedOut)
            {
                rect.colorFill = bumpBehav.GetColor(colorFill);
                rect.GrafUpdate(timeStacker);
                if (string.IsNullOrEmpty(_desError))
                    _label.color = bumpBehav.GetColor(colorEdge);

                else
                    _label.color = new Color(0.5f, 0f, 0f);

                return;
            }
            rect.colorFill = colorFill;
            rect.fillAlpha = bumpBehav.FillAlpha;
            rect.GrafUpdate(timeStacker);
            Color color = bumpBehav.GetColor(string.IsNullOrEmpty(_desError) ? colorEdge : Color.red);
            if (Focused || MouseOver)
            {
                color = Color.Lerp(color, Color.white, bumpBehav.Sin());
            }
            _label.color = color;
        }

        public static bool _IsJoystick(string keyCode)
        {
            if (keyCode.Length > 8)
            {
                return keyCode.ToLower().Substring(0, 8) == "joystick";
            }
            return false;
        }

        public static string _ChangeBind(string oldKey, OpKeyBinder.BindController oldBind, OpKeyBinder.BindController newBind)
        {
            if (!_IsJoystick(oldKey))
            {
                return oldKey;
            }
            string text = oldKey.Substring((oldBind != 0) ? 9 : 8);
            int num = (int)newBind;
            return "Joystick" + ((newBind != 0) ? num.ToString() : "") + text;
        }

        public override void NonMouseSetHeld(bool newHeld)
        {
            base.NonMouseSetHeld(newHeld);
            if (newHeld)
            {
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                _label.text = "?";
            }
        }

        public override void Update()
        {
            base.Update();
            rect.Update();
            if (greyedOut)
            {
                return;
            }
            if (MouseOver && Input.GetMouseButton(0))
            {
                held = true;
            }
            _lastAnyKeyDown = _anyKeyDown;
            _anyKeyDown = Input.anyKey;
            if (held)
            {
                if (_IsJoystick(value))
                {
                    if (base.MenuMouseMode)
                    {
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            bool flag = false;
                            OpKeyBinder.BindController bindController = OpKeyBinder.BindController.AnyController;
                            if (Input.GetKey(KeyCode.BackQuote) || Input.GetKey(KeyCode.Alpha0) || Input.GetKey(KeyCode.Escape))
                            {
                                flag = true;
                                bindController = OpKeyBinder.BindController.AnyController;
                            }
                            else if (Input.GetKey(KeyCode.Alpha1))
                            {
                                flag = true;
                                bindController = OpKeyBinder.BindController.Controller1;
                            }
                            else if (Input.GetKey(KeyCode.Alpha2))
                            {
                                flag = true;
                                bindController = OpKeyBinder.BindController.Controller2;
                            }
                            else if (Input.GetKey(KeyCode.Alpha3))
                            {
                                flag = true;
                                bindController = OpKeyBinder.BindController.Controller3;
                            }
                            else if (Input.GetKey(KeyCode.Alpha4))
                            {
                                flag = true;
                                bindController = OpKeyBinder.BindController.Controller4;
                            }
                            if (flag)
                            {
                                if (_bind == bindController)
                                {
                                    PlaySound(SoundID.MENU_Error_Ping);
                                    held = false;
                                }
                                else
                                {
                                    SetController(bindController);
                                    held = false;
                                    FocusMoveDisallow(this);
                                }
                            }
                            return;
                        }
                    }
                    else if (Input.GetKey(KeyCode.JoystickButton7))
                    {
                        bool flag2 = false;
                        OpKeyBinder.BindController bindController2 = OpKeyBinder.BindController.AnyController;
                        if (Input.GetKey(KeyCode.Joystick1Button7))
                        {
                            flag2 = true;
                            bindController2 = OpKeyBinder.BindController.Controller1;
                        }
                        else if (Input.GetKey(KeyCode.Joystick2Button7))
                        {
                            flag2 = true;
                            bindController2 = OpKeyBinder.BindController.Controller2;
                        }
                        else if (Input.GetKey(KeyCode.Joystick3Button7))
                        {
                            flag2 = true;
                            bindController2 = OpKeyBinder.BindController.Controller3;
                        }
                        else if (Input.GetKey(KeyCode.Joystick4Button7))
                        {
                            flag2 = true;
                            bindController2 = OpKeyBinder.BindController.Controller4;
                        }
                        if (flag2)
                        {
                            if (_bind == bindController2)
                            {
                                SetController(OpKeyBinder.BindController.AnyController);
                            }
                            else
                            {
                                SetController(bindController2);
                            }
                            held = false;
                            FocusMoveDisallow(this);
                        }
                        return;
                    }
                }
                foreach (var col in ReInput.controllers.Joysticks)
                {
                    foreach (var axis in col.Axes)
                    {
                        if (axis.timeActive != 0)
                        {
                            string name = "Axis " + axis.id;
                            if (_bind != 0)
                                name = $"{_bind} " + name;
                            value = "Joystick" + name;
                            held = false;
                            return;
                        }
                    }
                }
                if (_lastAnyKeyDown || !_anyKeyDown)
                {
                    return;
                }
                if (!bumpBehav.ButtonPress(BumpBehaviour.ButtonType.Pause))
                {
                    foreach (int code in Enum.GetValues(typeof(KeyCode)))
                    {
                        if (Input.GetKey((KeyCode)code))
                        {
                            string text = ((KeyCode)(code)).ToString();
                            if (text.Length > 4 && text.Substring(0, 5) == "Mouse")
                            {
                                if (!MouseOver)
                                {
                                    PlaySound(SoundID.MENU_Error_Ping);
                                    held = false;
                                }
                            }
                            else if (value != text)
                            {
                                if (_IsJoystick(text) && _bind != 0)
                                    text = text.Substring(0, 8) + _bind + text.Substring(8);

                                value = text;
                                held = false;
                            }
                            break;
                        }
                    }


                    return;
                }
                value = NONE;
                held = false;
            }
            else if (!held && MouseOver && Input.GetMouseButton(0))
            {
                held = true;
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                _label.text = "?";
            }
        }

        public override void Change()
        {
            _size = new Vector2(Mathf.Max(30f, base.size.x), Mathf.Max(30f, base.size.y));
            base.Change();
            _sprite.isVisible = _IsJoystick(value);
            if (_IsJoystick(value))
            {
                _sprite.SetPosition(5f, size.y / 2f);
                _label.text = value.Replace("Joystick", "");
                FLabelPlaceAtCenter(_label, new Vector2(20f, 0f), size - new Vector2(20f, 0f));
            }
            else
            {
                _label.text = value;
                FLabelPlaceAtCenter(_label, Vector2.zero, size);
            }
            rect.size = size;
        }

    }
}
