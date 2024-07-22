using HUD;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static RandomBuffUtils.BuffEvent;
using static System.Net.Mime.MediaTypeNames;
using Random = UnityEngine.Random;

namespace RandomBuffUtils.BuffEvents
{
    internal partial class BuffExtraDialogBoxEvent
    {
        static event ExtraDialogBoxHandler onAllExtraDialogInit;
        static bool hudFiredUp;
        public static event ExtraDialogBoxHandler OnExtraDialogsCreated
        {
            remove => onAllExtraDialogInit -= value;
            add
            {
                onAllExtraDialogInit += value;
                if (dialogBoxInstances.Count > 0)
                    value.Invoke(dialogBoxInstances.ToArray());

                if (!hudFiredUp)
                    return;
                if(BuffCustom.TryGetGame(out var game))
                {
                    var roomCamera = game.cameras[0];
                    foreach (var p in roomCamera.room.game.Players)
                    {
                        var player = p.realizedCreature as Player;
                        CreateDialogBoxSingle(roomCamera, player);
                    }
                    foreach (var instance in dialogBoxInstances)
                        instance.actuallyCreateDialogBox = false;
                }
            }
        } 
    }

    internal partial class BuffExtraDialogBoxEvent
    {
        internal static Dictionary<SlugcatStats.Name, ExtraDialogBox> initedDialogBoxes = new Dictionary<SlugcatStats.Name, ExtraDialogBox>();
        internal static List<ExtraDialogBoxInstance> dialogBoxInstances = new List<ExtraDialogBoxInstance>();
        internal static List<ExtraDialogBox> extraDialogBoxes = new List<ExtraDialogBox>();

        internal static void OnEnable()
        {
            On.RoomCamera.FireUpSinglePlayerHUD += RoomCamera_FireUpSinglePlayerHUD;
            On.HUD.HUD.ClearAllSprites += HUD_ClearAllSprites;
        }

        private static void HUD_ClearAllSprites(On.HUD.HUD.orig_ClearAllSprites orig, HUD.HUD self)
        {
            ClearDialogs();
            orig.Invoke(self);
        }

        private static void RoomCamera_FireUpSinglePlayerHUD(On.RoomCamera.orig_FireUpSinglePlayerHUD orig, RoomCamera self, Player player)
        {
            if(extraDialogBoxes.Count > 0)
                ClearDialogs();
            orig.Invoke(self, player);
            InitDialogs(self);
        }

        static void InitDialogs(RoomCamera roomCamera)
        {
            hudFiredUp = true;
            foreach (var p in roomCamera.room.game.Players)
            {
                var player = p.realizedCreature as Player;
                CreateInstanceSingle(player);
            }
            onAllExtraDialogInit?.SafeInvoke("OnAllExtraDialogInit", new object[] { dialogBoxInstances.ToArray() });
            foreach(var p in roomCamera.room.game.Players)
            {
                var player = p.realizedCreature as Player;
                CreateDialogBoxSingle(roomCamera, player);
            }
            foreach (var instance in dialogBoxInstances)
                instance.actuallyCreateDialogBox = false;
        }

        static void CreateInstanceSingle(Player player)
        {
            bool missingInstance = true;
            foreach (var instance in dialogBoxInstances)
            {
                if (instance.BindSlugcat == player.slugcatStats.name)
                {
                    missingInstance = false;
                    break;
                }
            }
            if (missingInstance)
                dialogBoxInstances.Add(new ExtraDialogBoxInstance(player.slugcatStats.name));
        }

        static void CreateDialogBoxSingle(RoomCamera roomCamera, Player player)
        {
            var bindInstance = dialogBoxInstances.Find((instance) => instance.BindSlugcat == player.slugcatStats.name);

            if (bindInstance.actuallyCreateDialogBox)
            {
                var dialogBox = new ExtraDialogBox(roomCamera.hud, roomCamera, player, Color.white);

                initedDialogBoxes.Add(player.slugcatStats.name, dialogBox);
                extraDialogBoxes.Add(dialogBox);
                
                roomCamera.hud.parts.Add(dialogBox);
                bindInstance.Reset(dialogBox);
                BuffUtils.Log("BuffExtraDialogBoxEvent", $"Actually create DialogBox for {bindInstance.BindSlugcat}");
            }
        }

        static void ClearDialogs()
        {
            hudFiredUp = false;
            foreach (var dialog in extraDialogBoxes)
            {
                dialog.DeleteDialogBox();
            }
            initedDialogBoxes.Clear();
            extraDialogBoxes.Clear();
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"DeleteDialogBox");
        }
    }

    public class ExtraDialogBoxInstance//与slugcat绑定
    {
        public SlugcatStats.Name BindSlugcat { get; private set; }

        public Color DefaultColor { get; internal set; } = Color.white;
        public Color CurrentColor => HasValue ? Value.currentColor : DefaultColor;

        public bool HasValue => BuffExtraDialogBoxEvent.initedDialogBoxes.ContainsKey(BindSlugcat);
        internal ExtraDialogBox Value => BuffExtraDialogBoxEvent.initedDialogBoxes[BindSlugcat];

        internal bool actuallyCreateDialogBox;

        internal ExtraDialogBoxInstance(SlugcatStats.Name name)
        {
            BindSlugcat = name;
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"Create instance for {name}, HasValue : {HasValue}");
        }

        public void NewMessage(string text, float xOrientation, float yPos, int extraLinger)
        {
            if (!HasValue)
                return;
            Value.NewMessage(text, xOrientation, yPos, extraLinger);
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"NewMessage : {text}, HasValue : {HasValue}");
        }

        public void Interrrupt(string text, int extraLinger, bool clearAll = false)
        {
            if (!HasValue)
                return;
            InterruptWithoutNewMessage(clearAll);
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"Interrrupt : {text}, HasValue : {HasValue}");
            NewMessage(text, Value.defaultXOrientation, Value.defaultYPos, extraLinger);
        }

        public void InterruptWithoutNewMessage(bool clearAll = false) 
        {
            if (!HasValue)
                return;
            if (Value.messages.Count > 0)
            {
                if (clearAll)
                {
                    Value.messages = new List<DialogBox.Message>();
                    Value.lingerCounter = 0;
                    Value.showCharacter = 0;
                }
                else
                {
                    Value.messages = new List<DialogBox.Message> { Value.messages[0] };
                    Value.lingerCounter = Value.messages[0].linger + 1;
                    Value.showCharacter = Value.messages[0].text.Length + 2;
                }
            }
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"InterruptWithoutNewMessage : {clearAll}, HasValue : {HasValue}");
        }

        public void ModifyColor(Color newColor)
        {
            if (!HasValue)
                return;
            Value.Color = newColor;
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"modify color : {newColor}, HasValue : {HasValue}");
        }

        public void ActuallyCreateDialogBox()
        {
            actuallyCreateDialogBox = true;
            BuffUtils.Log("BuffExtraDialogBoxEvent", $"Call actually create DialogBox for {BindSlugcat}, HasValue : {HasValue}");
        }

        internal void Reset(ExtraDialogBox extraDialogBox)
        {
            DefaultColor = extraDialogBox.baseColor;
        }
    
        public static bool IsExtraDialogBox(DialogBox dialogBox)
        {
            return dialogBox is ExtraDialogBox;
        }
    }

    internal class ExtraDialogBox : DialogBox
    {
        public ExtraDialogBox(HUD.HUD hud, RoomCamera camera, Creature target, Color baseColor) : base(hud)
        {
            this.camera = camera;
            offest = new Vector2(0, 75.0f + Random.Range(-10, 10));
            lastPos = pos = target.mainBodyChunk.pos + offest;
            this.target = target;
            this.baseColor = baseColor;
            Color = baseColor;
        }
        public override void Update()
        {
            base.Update();
            lastPos = pos;
            pos = target.mainBodyChunk.pos + offest;
        }

        public override void Draw(float timeStacker)
        {
            if (messages.Count > 0)
            {
                Vector2 vector = Vector2.Lerp(lastPos, pos, timeStacker) - camera.pos;

                messages[0].yPos = vector.y;
                messages[0].xOrientation = vector.x / hud.rainWorld.screenSize.x;
            }
            base.Draw(timeStacker);
            foreach (var sprite in sprites)
            {
                sprite.isVisible &= !paused;
            }

            label.isVisible &= !paused;

            if (colorDirty)
            {
                for (int i = 0; i < 4; i++)
                {
                    sprites[SideSprite(i)].color = color;
                    sprites[CornerSprite(i)].color = color;
                }
                colorDirty = false;
            }
        }

        public void DeleteDialogBox()
        {
            slatedForDeletion = true;
            camera = null;
            messages.Clear();
            label.RemoveFromContainer();
        }

        private Vector2 pos;
        private Vector2 lastPos;
        private RoomCamera camera;
        private Creature target;
        private Vector2 offest;

        public bool paused = false;

        int cycle;
        public Color specialColor;
        public Color baseColor;

        bool colorDirty;
        Color color;
        public Color Color
        {
            get => color;
            set
            {
                if (value != Color)
                {
                    colorDirty = true;
                    color = value;
                }
            }
        }

        public static ExtraDialogBox CreateDialog(Creature target, Color baseColor)
        {
            var hud = target.abstractCreature.world.game.cameras[0].hud;
            if (hud != null && hud.owner is Player)
            {
                var camera = target.abstractCreature.world.game.cameras[0];
                var re = new ExtraDialogBox(hud, camera, target, baseColor);
                camera.hud.AddPart(re);
                return re;
            }
            return null;
        }
    }
}
