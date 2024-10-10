namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Windows.Forms;

    using Loupedeck.StudioOneMidiPlugin.Helpers;

    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public abstract class ButtonData
    {
        public static readonly BitmapColor DefaultSelectionBgColor = new BitmapColor(60, 60, 60);

        protected StudioOneMidiPlugin Plugin;
        protected const int TrackNameH = 24;

        public virtual void OnLoad(StudioOneMidiPlugin plugin) => this.Plugin = plugin;
        public abstract BitmapImage getImage(PluginImageSize imageSize);
        public abstract void runCommand();
    }

    public class PropertyButtonData : ButtonData
    {
        public const Int32 SelectedChannel = StudioOneMidiPlugin.ChannelCount;
        public enum TrackNameMode
        {
            None,
            NoneOffset,
            ShowFull,
            ShowLeftHalf,
            ShowRightHalf
        }

        private int ChannelIndex;
        private ChannelProperty.PropertyType Type;
        private TrackNameMode ShowTrackName;
        private BitmapImage Icon;

        public PropertyButtonData(int channelIndex,
                                      ChannelProperty.PropertyType bt,
                                      TrackNameMode tm = TrackNameMode.ShowFull,
                                      string iconName = null)
        {
            this.ChannelIndex = channelIndex;
            this.Type = bt;
            this.ShowTrackName = tm;

            if (iconName != null)
            {
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
            }
        }

        public void setPropertyType(ChannelProperty.PropertyType bt) => this.Type = bt;

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            ChannelProperty.PropertyType type,
                                            Boolean isSelected,
                                            TrackNameMode showTrackName,
                                            String trackName,
                                            BitmapImage icon)
        {
            if (isSelected)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(Int32)type]);
            }

            var yOff = showTrackName == TrackNameMode.None ? 0 : icon == null ? TrackNameH : TrackNameH - 8;

            if (icon != null)
            {
                bb.DrawImage(icon, bb.Width / 2 - icon.Width / 2, yOff + (bb.Height - yOff) / 2 - icon.Height / 2);
            }
            else
            {
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)type], 0, yOff, bb.Width, bb.Height - yOff, null, 32);
            }

            if (showTrackName != TrackNameMode.None && showTrackName != TrackNameMode.NoneOffset)
            {
                var hPos = 0;
                var width = bb.Width;

                if (showTrackName == TrackNameMode.ShowLeftHalf)
                {
                    hPos = 1;
                    width = bb.Width * 2;
                }
                else if (showTrackName == TrackNameMode.ShowRightHalf)
                {
                    hPos = -bb.Width - 1;
                    width = bb.Width * 2;
                }
                bb.DrawText(trackName, hPos, 0, width, TrackNameH);
            }

            return bb.ToImage();
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            // bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
            return PropertyButtonData.drawImage(new BitmapBuilder(imageSize),
                                                    this.Type,
                                                    cd.BoolProperty[(Int32)this.Type],
                                                    this.ShowTrackName,
                                                    cd.Label,
                                                    this.Icon);
        }

        public override void runCommand()
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];

            cd.EmitChannelPropertyPress(this.Type);
        }
    }

    public class PropertySelectionButtonData : ButtonData
    {
        public ChannelProperty.PropertyType TypeA, TypeB, CurrentType;
        public Boolean Activated;
        private BitmapImage IconA, IconB, IconOff;

        public PropertySelectionButtonData(ChannelProperty.PropertyType typeA,
                                           ChannelProperty.PropertyType typeB,
                                           String iconNameA,
                                           String iconNameB,
                                           String iconNameOff,
                                           Boolean activated = false)
        {
            this.TypeA = typeA;
            this.TypeB = typeB;
            this.CurrentType = typeA;

            this.IconA = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameA}_80px.png"));
            this.IconB = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameB}_80px.png"));
            this.IconOff = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconNameOff}_80px.png"));
            this.Activated = activated;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            if (!this.Activated)
            {
                bb.DrawImage(this.IconOff, 0, 0);
            }
            else if (this.CurrentType == this.TypeA)
            {
                bb.DrawImage(this.IconA, 0, 0);
            }
            else
            {
                bb.DrawImage(this.IconB, 0, 0);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (!this.Activated)
            {
                this.Activated = true;
                this.Plugin.EmitSelectModeChanged(SelectButtonMode.Property);
            }
            else
            {
                this.CurrentType = this.CurrentType == this.TypeA ? this.TypeB : this.TypeA;
            }
            this.Plugin.EmitPropertySelectionChanged(this.CurrentType);
        }
    }

    public class SelectButtonData : ButtonData
    {
        public SelectButtonMode CurrentMode = SelectButtonMode.Property;
        public Boolean UserButtonActive = false;
        public Boolean UserButtonEnabled = true;
        public Boolean UserButtonMenuActive = false;
        public static ChannelProperty.PropertyType SelectionPropertyType = ChannelProperty.PropertyType.Mute;

        private readonly Int32 ChannelIndex = -1;
        public String UserLabel { get; set; }

        private const Int32 TitleHeight = 24;
        private static BitmapImage IconSelMon, IconSelRec;
        private static readonly BitmapColor CommandPropertyColor = new BitmapColor(40, 40, 40);
        public static readonly BitmapColor TextDescColor = new BitmapColor(175, 175, 175);
        public static readonly FinderColor BgColorAssigned =   new FinderColor(80, 80, 80);
        public static readonly FinderColor BgColorUnassigned = new FinderColor(40, 40, 40);
        public static readonly BitmapColor BgColorUserCircle = new BitmapColor(60, 60, 60);

        public static String PluginName { get; set; }

        public static readonly ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings
        {
            OnColor = BgColorAssigned,
            OffColor = BgColorAssigned,
            TextOnColor = FinderColor.White,
            TextOffColor = FinderColor.Black
        });

        public SelectButtonData(Int32 channelIndex)
        {
            this.ChannelIndex = channelIndex;
        }

//        public void setPluginName(String text) => this.PluginName = text;

        public override void OnLoad(StudioOneMidiPlugin plugin)
        {
            base.OnLoad(plugin);
            UserColorFinder.Init(plugin);
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            var bb = new BitmapBuilder(imageSize);

            return SelectButtonData.drawImage(bb, 
                                              cd,
                                              this.CurrentMode,
                                              this.UserButtonActive,
                                              this.UserButtonEnabled,
                                              this.UserButtonMenuActive,
                                              SelectionPropertyType,
                                              PluginName);
        }

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            ChannelData cd,
                                            SelectButtonMode buttonMode,
                                            Boolean userButtonActive,
                                            Boolean userButtonEnabled = true,
                                            Boolean userButtonMenuActive = false,
                                            ChannelProperty.PropertyType commandProperty = ChannelProperty.PropertyType.Select,
                                            String pluginName = "")
        {
            if (SelectButtonData.IconSelMon == null)
            {
                SelectButtonData.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
                SelectButtonData.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
            }

            if (buttonMode == SelectButtonMode.Send)
            {
                //                bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, TextDescColor);
                bb.DrawText(ColorFinder.stripLabel(cd.Label), 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            else if (buttonMode == SelectButtonMode.User)
            {
                UserColorFinder.CurrentChannel = cd.ChannelID + 1;

                var ubh = bb.Height / 3;
                var uby = bb.Height - ubh;

                // User Pot
                //
                if (UserColorFinder.getLabel(pluginName, cd.Label).Length > 0)
                {
                    if (UserColorFinder.getPaintLabelBg(pluginName, cd.Label))
                    {
                        bb.FillRectangle(0, 0, bb.Width, uby, new BitmapColor(UserColorFinder.getOnColor(pluginName, cd.Label), 80));
                    }
                }
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, TextDescColor);
                bb.DrawText(UserColorFinder.getLabelShort(pluginName, cd.Label), 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight, 
                            UserColorFinder.getTextOnColor(pluginName, cd.Label));

                // User Button
                //
                var drawCircle = cd.UserLabel.Length > 0 && UserColorFinder.showUserButtonCircle(pluginName, cd.UserLabel);
                var tx = 0;
                var tw = bb.Width;
                if (UserColorFinder.hasMenu(pluginName, cd.UserLabel))
                {
                    userButtonActive = true;    // Use 'on' colour variants for all menu items
                }
                var tc = userButtonEnabled ? userButtonActive ? drawCircle ? UserColorFinder.getOnColor(pluginName, cd.UserLabel, isUser: true)
                                                                           : UserColorFinder.getTextOnColor(pluginName, cd.UserLabel, isUser: true)
                                                              : UserColorFinder.getTextOffColor(pluginName, cd.UserLabel, isUser: true)
                                           : BitmapColor.Black;
                if (userButtonMenuActive) tc = BitmapColor.White;
                var bc = (cd.UserLabel.Length > 0 || UserColorFinder.getLabel(pluginName, cd.UserLabel, isUser: true).Length > 0)
                                                  && userButtonEnabled ? userButtonActive ? UserColorFinder.getOnColor(pluginName, cd.UserLabel, isUser: true)
                                                                       : UserColorFinder.getOffColor(pluginName, cd.UserLabel, isUser: true)
                                                 : BgColorUnassigned;
                if (userButtonMenuActive)
                {
                    var stroke = 2;
                    bb.FillRectangle(0, uby, bb.Width, ubh, tc);
                    bb.FillRectangle(0, uby + stroke, bb.Width, ubh - 2 * stroke, new BitmapColor(40, 40, 40));
                }
                else
                {
                    bb.FillRectangle(0, uby, bb.Width, ubh, drawCircle ? BgColorUserCircle : bc);
                }
                if (drawCircle)
                {
                    var cx = ubh/2;
                    if (cd.ChannelID >= 3) cx = bb.Width - ubh / 2;
                    var cy = uby + ubh/2;
                    var cr = ubh/2 - 5;
                    if (userButtonActive) bb.FillCircle(cx, cy, cr, tc);
                    else                  bb.DrawArc(cx, cy, cr, 0, 360, tc, 2);
                    tx = ubh;
                    tw = bb.Width - ubh * 2;
                }
                var labelText = userButtonActive ? UserColorFinder.getLabelOnShort(pluginName, cd.UserLabel, isUser: true)
                                                 : UserColorFinder.getLabelShort(pluginName, cd.UserLabel, isUser: true);
                var menuItems = UserColorFinder.getMenuItems(pluginName, cd.UserLabel, isUser: true);
                if (menuItems != null)
                {
                    if (labelText.Length > 0) labelText += ": "; 
                    labelText += menuItems[cd.UserValue / (127 / (menuItems.Length - 1))];
                }
                bb.DrawImage(new LabelImageLoader(labelText).GetImage(tw, TitleHeight, tc), tx, uby);
            }
            else
            {
                if (buttonMode == SelectButtonMode.Select) commandProperty = ChannelProperty.PropertyType.Select;

                if (cd.Selected)
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Select]);
                }
                else
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
                }

                var rX = 8;
                var rY = 4;
                var rS = 8;
                var rW = (bb.Width - rS) / 2 - rX;
                var rH = (bb.Height - rY - TitleHeight) / 2 - rS;
                var rX2 = rX + rW + rS;
                var rY2 = rY + rH + rS + TitleHeight;

                bb.FillRectangle(rX2 - 5, rY, 2, rH, new BitmapColor(40, 40, 40));
                bb.FillRectangle(rX2 - 5, rY2, 2, rH, new BitmapColor(40, 40, 40));

                if (cd.Muted || commandProperty == ChannelProperty.PropertyType.Mute)
                {
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4,
                        cd.Muted ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Mute]
                                 : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Solo || commandProperty == ChannelProperty.PropertyType.Solo)
                {
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4,
                        cd.Solo ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Solo]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Armed || commandProperty == ChannelProperty.PropertyType.Arm)
                {
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Armed ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Arm]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Monitor || commandProperty == ChannelProperty.PropertyType.Monitor)
                {
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Monitor ? ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Monitor]
                                : SelectButtonData.CommandPropertyColor);
                }
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)ChannelProperty.PropertyType.Mute], rX, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawText(ChannelProperty.PropertyLetter[(Int32)ChannelProperty.PropertyType.Solo], rX2, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawImage(SelectButtonData.IconSelRec, rX + rW / 2 - SelectButtonData.IconSelRec.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);
                bb.DrawImage(SelectButtonData.IconSelMon, rX2 + rW / 2 - SelectButtonData.IconSelMon.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);

                bb.DrawText(ColorFinder.stripLabel(cd.Label), 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            ChannelData cd = this.Plugin.channelData[this.ChannelIndex.ToString()];
            switch (this.CurrentMode)
            {
                case SelectButtonMode.Select:
                    if (!cd.Selected)
                    {
                        cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Select);
                    }
                    this.Plugin.EmitSelectedButtonPressed();
                    break;
                case SelectButtonMode.Property:
                    cd.EmitChannelPropertyPress(SelectButtonData.SelectionPropertyType);
                    break;
                case SelectButtonMode.User:
                    var menuItems = UserColorFinder .getMenuItems(PluginName, cd.UserLabel, isUser: true);
                    if (menuItems != null)
                    {
                        // Display value selection menu.
                        var ubmp = new UserButtonMenuParams();
                        ubmp.ChannelIndex = this.ChannelIndex;
                        ubmp.MenuItems = menuItems;
                        this.Plugin.EmitUserButtonMenuActivated(ubmp);
                    }
                    else
                    {
                        // Toggle the button value.
                        this.Plugin.SendMidiNote(0, UserButtonMidiBase + this.ChannelIndex, this.UserButtonActive ? 0 : 127);
                    }
                    break;
            }
        }
    }

    public class UserMenuSelectButtonData : ButtonData
    {
        //        Int32 MidiChannel = -1;
        Int32 ChannelIndex = 0;
        protected Int32 Value = 0;
        String Label;

        public UserMenuSelectButtonData()
        {
        }

        public void init(Int32 channelIndex = 0, Int32 value = 0, String label = null)
        {
            this.ChannelIndex = channelIndex;
            this.Value = value;
            this.Label = label;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);


            if (this.Label != null)
            {
                //            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.White);
                //            bb.FillRectangle(stroke, stroke, bb.Width - 2 * stroke, bb.Height - 2 * stroke, BitmapColor.Black);

                var height = bb.Height / 2;
                bb.FillRectangle(0, (bb.Height - height - 4) / 2, bb.Width, height + 4, BitmapColor.White);
                bb.FillRectangle(0, (bb.Height - height) / 2, bb.Width, height, new BitmapColor(40, 40, 40));
                bb.DrawImage(new LabelImageLoader(this.Label).GetImage(bb.Width, bb.Height));
            }
            return bb.ToImage();
        }
        public override void runCommand()
        {
            if (this.ChannelIndex >= 0 && this.Label != null)
            {
                this.Plugin.SendMidiNote(0, UserButtonMidiBase + this.ChannelIndex, this.Value);
            }
            this.Plugin.EmitUserButtonMenuActivated(new UserButtonMenuParams { ChannelIndex = this.ChannelIndex, IsActive = false });
        }
    }

    public class CommandButtonData : ButtonData
    {
        public Int32 Code;
        public Int32 CodeOn = 0;              // alternative code to send when activated
        public String Name;

        public virtual Boolean Activated { get; set; } = false;

        public BitmapColor OffColor = BitmapColor.Transparent;
        public BitmapColor OnColor = BitmapColor.Transparent;
        public BitmapColor TextColor = BitmapColor.White;
        public BitmapColor TextOnColor = BitmapColor.White;
        public BitmapImage Icon, IconOn;

        public static readonly BitmapColor cRectOn = new BitmapColor(200, 200, 200);
        public static readonly BitmapColor cTextOn = BitmapColor.Black;
        public static readonly BitmapColor cRectOff = new BitmapColor(50, 50, 50);
        public static readonly BitmapColor cTextOff = new BitmapColor(160, 160, 160);

        protected int MidiChannel = 0;
        public int midiChannel
        {
            get => this.MidiChannel;
        }


        public CommandButtonData(Int32 code, String name, String iconName = null)
        {
            this.init(code, name, iconName);
        }

        public CommandButtonData(Int32 code, String name, String iconName, BitmapColor bgColor)
        {
            this.init(code, name, iconName);
            this.OnColor = bgColor;
            this.OffColor = bgColor;
        }

        public CommandButtonData(Int32 code, Int32 codeOn, String name, String iconName = null)
        {
            this.init(code, name, iconName);
            this.CodeOn = codeOn;
        }
        public CommandButtonData(Int32 code, String name, BitmapColor onColor, BitmapColor textOnColor, bool isActivatedByDefault = false)
        {
            this.init(code, name, null);
            this.OnColor = onColor;
            this.TextOnColor = textOnColor;
            this.Activated = isActivatedByDefault;
        }

        private void init(Int32 code, String name, String iconName)
        {
            this.Name = name;
            this.Code = code;

            if (iconName != null)
            {
                var iconResExt = "_52px.png";
                if (EmbeddedResources.FindFile(iconName + iconResExt) == null) iconResExt = "_80px.png";
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile(iconName + iconResExt));
                var iconResOn = EmbeddedResources.FindFile(iconName + "_on" + iconResExt);
                if (iconResOn != null)
                {
                    this.IconOn = EmbeddedResources.ReadImage(iconResOn);
                }
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            // Debug.WriteLine("CommandButtonData.getImage " + this.Code.ToString() + ", name: " + this.Name);

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.Activated ? this.OnColor : this.OffColor);

            if (this.Activated && this.IconOn != null)
            {
                bb.DrawImage(this.IconOn);
            }
            else if (this.Icon != null)
            {
                bb.DrawImage(this.Icon);
            }
            else
            {
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, this.Activated ? this.TextOnColor : this.TextColor, 16);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            var note = this.Code;
            if (this.Activated && (this.CodeOn > 0))
            {
                note = this.CodeOn;
            }
            this.Plugin.SendMidiNote(this.MidiChannel, note);
        }
    }

    public class SnapStepCommandButtonData : CommandButtonData
    {
        public enum StepDir { StepFwd, StepRev };
        private StepDir DirMode;
        public SnapStepCommandButtonData(StepDir stepDir) : base(stepDir == StepDir.StepFwd ? 0x37 : 0x38, 
                                                                 "Snap Step " + (stepDir == StepDir.StepFwd ? "Fwd" : "Rev"))
        {
            this.DirMode = stepDir;
        }
        public override void runCommand()
        {
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                this.Plugin.SendMidiNote(15, this.DirMode == StepDir.StepFwd ? 0x00 : 0x01);
            }
            else
            {
                base.runCommand();
            }
        }
    }

    public class FlipPanVolCommandButtonData : CommandButtonData
    {
        public FlipPanVolCommandButtonData(Int32 code) : base(code, "Flip Vol/Pan")
        {
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            int rY = 16;
            int rS = 8;
            int rW = bb.Width - 24;
            int rH = (bb.Height - 2 * rY - rS) / 2;
            int rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.Activated ? CommandButtonData.cRectOff : CommandButtonData.cRectOn);
            bb.DrawText("VOL", rX, rY, rW, rH, this.Activated ? CommandButtonData.cTextOff : CommandButtonData.cTextOn, rH - 6);

            bb.FillRectangle(rX, rY + rH + rS, rW, rH, this.Activated ? CommandButtonData.cRectOn : CommandButtonData.cRectOff);
            bb.DrawText("PAN", rX, rY + rH + rS, rW, rH, this.Activated ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, rH - 6);

            return bb.ToImage();
        }

        public override Boolean Activated 
        { 
            get => base.Activated; 
            set {
                base.Activated = value;
                this.Plugin.EmitFaderModeChanged(value == true ? StudioOneMidiPlugin.FaderMode.Pan : StudioOneMidiPlugin.FaderMode.Volume);
            }
        }
    }

    // Triggers a command from a list in LoupedeckCT.surface.xml on the Studio One side.
    // These are one-shot commands that do not provide feedback.
    //
    public class OneWayCommandButtonData : CommandButtonData
    {
        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, String iconName = null) : base(code, name, iconName)
        {
            this.MidiChannel = channel;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, String iconName, BitmapColor bgColor) : base(code, name, iconName, bgColor)
        {
            this.MidiChannel = channel;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, BitmapColor textColor) : base(code, name, null)
        {
            this.MidiChannel = channel;
            this.TextColor = textColor;
        }

        public OneWayCommandButtonData(Int32 channel, Int32 code, String name, BitmapColor onColor, BitmapColor textOnColor, Boolean isActivatedByDefault = false)
            : base(code, name, onColor, textOnColor, isActivatedByDefault)
        {
            this.MidiChannel = channel;
        }

        // The code below is an alternative method for invoking commands by setting
        // a command parameter via a MIDI controller and then triggering it by a MIDI note.
        // This provides 127 addresses for command triggering per MIDI controller, but it requires
        // two separate MIDI messages to be sent which is not ideal.
        //
        // public override void runCommand()
        // {
        //
        //    var ccSet = new ControlChangeEvent();
        //    ccSet.ControlNumber = (SevenBitNumber)0x00;
        //    ccSet.ControlValue = (SevenBitNumber)this.CommandCode;
        //    this.Plugin.mackieMidiOut.SendEvent(ccSet);
        //
        //    var eTrigger = new NoteOnEvent();
        //    eTrigger.Velocity = (SevenBitNumber)127;
        //    eTrigger.NoteNumber = (SevenBitNumber)0x72;    // controlCommandTrigger
        //    this.Plugin.mackieMidiOut.SendEvent(eTrigger);
        // }
    }

    
    public abstract class NumberedSelectionButtonData : OneWayCommandButtonData
    {
        private readonly Int32 SelectionNo;
        private readonly Int32 OffsetX, OffsetY;

        public NumberedSelectionButtonData(Int32 midiChannel, Int32 midiBaseNote, 
                                           Int32 selectionNo, String label, String iconName, 
                                           Int32 offsetX, Int32 offsetY, Int32 bgR, Int32 bgG, Int32 bgB) 
               : base(midiChannel, midiBaseNote + selectionNo, $"{label} {selectionNo}", iconName)
        {
            this.SelectionNo = selectionNo;
            this.OffsetX = offsetX;
            this.OffsetY = offsetY;
            this.OffColor = new BitmapColor(bgR, bgG, bgB, 80);
        }
        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.OffColor);

            bb.DrawImage(this.Icon);
            bb.DrawText(this.SelectionNo.ToString(), this.OffsetX, this.OffsetY, bb.Width, bb.Height, this.TextColor, 14);

            return bb.ToImage();
        }
    }

    public class GroupSuspendButtonData : NumberedSelectionButtonData
    {
        public GroupSuspendButtonData(Int32 groupNumber) : base(14, 0x21, groupNumber, "Group", "group_suspend_no", 0, 7, 191, 255, 144)
        {
        }
    }
    public class MarkerGotoButtonData : NumberedSelectionButtonData
    {
        public static readonly BitmapColor BgColor = new BitmapColor(169, 146, 255, 80);
        public MarkerGotoButtonData(Int32 markerNumber) : base(14, 0x65, markerNumber, "Marker", "marker_goto_no", 7, 9, BgColor.R, BgColor.G, BgColor.B)
        {
        }
    }
    public class SceneSelectButtonData : NumberedSelectionButtonData
    {
        public SceneSelectButtonData(Int32 sceneNumber) : base(15, 0x37, sceneNumber, "Scene", "scene_select_no", 0, 7, 52, 116, 187)
        {
        }
    }

    public class ModeButtonData : ButtonData
    {
        public String Name;
        public BitmapImage Icon = null;
        public Boolean Activated = false;
        private Boolean IsMenu = false;
        private BitmapColor BgColor = ButtonData.DefaultSelectionBgColor;

        public ModeButtonData(String name, String iconName = null, Boolean isMenu = false)
        {
            this.init(name, iconName, BitmapColor.Transparent);
            this.IsMenu = isMenu;
        }
        public ModeButtonData(String name, String iconName, BitmapColor bgColor, Boolean isMenu = false)
        {
            this.init(name, iconName, bgColor);
            this.IsMenu = isMenu;
        }

        private void init(String name, String iconName, BitmapColor bgColor)
        {
            this.Name = name;
            this.BgColor = bgColor;

            if (iconName != null)
            {
                if (!iconName.Contains("px"))
                {
                    iconName += "_52px";
                }
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}.png"));
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.Activated ? this.BgColor : BitmapColor.Transparent);

            if (this.IsMenu && this.Activated)
            {
                bb.FillRectangle(0, 0, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bb.Height - 5, bb.Width, 5, BitmapColor.White);
            }
            
            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon);
            }
            else
            {
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, null, 16);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            this.Activated = !this.Activated;
        }
    }

    public class ModeTopCommandButtonData : OneWayCommandButtonData
    {
        public enum Location
        {
            Left,
            Right
        }
        Location ButtonLocation = Location.Left;
        String TopDisplayText;
        protected Boolean IsUserButton = false;
        protected String PluginName;
        protected ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings { OnColor = FinderColor.Transparent,
                                                                                                OffColor = FinderColor.Transparent,
                                                                                                TextOnColor = FinderColor.White,
                                                                                                TextOffColor = FinderColor.Black});

        public ModeTopCommandButtonData(Int32 channel, Int32 code, String name, Location bl, String iconName = null) : base(channel, code, name, iconName)
        {
            this.ButtonLocation = bl;
        }

        public ModeTopCommandButtonData(Int32 channel, Int32 code, String name, Location bl, String iconName, BitmapColor offColor) : base(channel, code, name, iconName)
        {
            this.ButtonLocation = bl;
            this.OffColor = offColor;
        }

        public override void OnLoad(StudioOneMidiPlugin plugin)
        {
            base.OnLoad(plugin);
            this.UserColorFinder.Init(plugin);
        }

        public void setTopDisplay(String text) => this.TopDisplayText = text;
        public void setPluginName(String text)
        {
            this.PluginName = text;

            if (this.IsUserButton && this.Name.Length > 0)
            {
                this.Icon = this.UserColorFinder.getIcon(this.PluginName, this.Name);
                this.IconOn = this.UserColorFinder.getIconOn(this.PluginName, this.Name);
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            var dispTxtH = 24;
            var bgX = this.IsUserButton ? dispTxtH + 4 : 0;
            var bgH = bb.Height - bgX;

            if (this.IsUserButton)
            {
                if (this.Name.Length == 0)
                {
                    bb.FillRectangle(0, bgX, bb.Width, bgH, SelectButtonData.BgColorUnassigned);
                }
                else
                {
                    bb.FillRectangle(0, bgX, bb.Width, bgH, this.Activated ? this.UserColorFinder.getOnColor(this.PluginName, this.Name, isUser: true)
                                                                           : this.UserColorFinder.getOffColor(this.PluginName, this.Name, isUser: true));
                }
            }
            else
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, this.OffColor);
            }

            if (this.Activated && this.IconOn != null)
            {
                bb.DrawImage(this.IconOn, (bb.Width - this.IconOn.Width) / 2, dispTxtH);
            }
            else if (this.Icon != null && !(this.IsUserButton && this.Name.Length == 0))
            {
                bb.DrawImage(this.Icon, (bb.Width - this.Icon.Width) / 2, dispTxtH + (bb.Height - dispTxtH - this.Icon.Height) / 2);
            }
            else
            {
                bb.DrawText(this.Activated ? this.UserColorFinder.getLabelOn(this.PluginName, this.Name, isUser: true) :
                                             this.UserColorFinder.getLabel(this.PluginName, this.Name, isUser: true), 
                                             0, dispTxtH, bb.Width, bb.Height - dispTxtH, 
                            this.Activated ? this.UserColorFinder.getTextOnColor(this.PluginName, this.Name, isUser: true)
                                           : this.UserColorFinder.getTextOffColor(this.PluginName, this.Name, isUser: true), 16);
            }

            int hPos;
            if (this.ButtonLocation == Location.Left) hPos = 1;
            else                                      hPos = -bb.Width - 1;

            bb.DrawText(this.TopDisplayText, hPos, 0, bb.Width * 2, dispTxtH);

            return bb.ToImage();
        }
    }

    public class ModeTopUserButtonData : ModeTopCommandButtonData
    {
        public ModeTopUserButtonData(Int32 channel, Int32 code, String name, Location bl) : base(channel, code, name, bl)
        {
            this.IsUserButton = true;
            this.UserColorFinder.DefaultColorSettings.OnColor = SelectButtonData.BgColorAssigned;
            this.UserColorFinder.DefaultColorSettings.OffColor = SelectButtonData.BgColorAssigned;
        }
    }


    public class ModeChannelSelectButtonData : ButtonData
    {
        public BitmapImage Icon, IconOn;
        public Boolean Activated = true;

        public ModeChannelSelectButtonData(Boolean activated = true)
        {
            this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_80px.png"));
            this.IconOn = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_on_80px.png"));
            this.Activated = activated;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (!this.Activated)
            {
                bb.DrawImage(this.Icon, 0, 0);
            }
            else
            {
                bb.DrawImage(this.IconOn, 0, 0);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            if (!this.Activated)
            {
                this.Activated = true;
                this.Plugin.EmitSelectModeChanged(SelectButtonMode.Select);
            }
        }
    }

    public class AutomationModeButtonData : ButtonData
    {
        public static String[] AutomationText =
        {
             "Auto: Off", // Off
			 "Read",      // Read
			 "Touch",     // Touch
			 "Latch",     // Latch
			 "Write",     // Write
		};
        public static BitmapColor[] AutomationBgColor =
        {
             BitmapColor.Transparent, // Off
			 new BitmapColor(129, 171, 115), // Read
			 new BitmapColor(216, 176,  82), // Touch
			 new BitmapColor(194,  90,  95), // Latch
			 new BitmapColor(208,  64,  71), // Write
		};
        public static BitmapColor[] AutomationTextColor =
        {
             BitmapColor.White, // Off
			 BitmapColor.Black, // Read
			 BitmapColor.Black, // Touch
			 BitmapColor.White, // Latch
			 BitmapColor.White, // Write
		};

        public AutomationMode CurrentMode = AutomationMode.Off;
        public Boolean SelectionModeActivated = false;

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            const Int32 fillHeight = 24;
            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (this.SelectionModeActivated)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ButtonData.DefaultSelectionBgColor);
                bb.DrawText("Auto\rMode", 0, 0, bb.Width, bb.Height, null, 16);
            }
            else
            {
                bb.FillRectangle(0, (bb.Height - fillHeight) / 2, bb.Width, fillHeight, AutomationBgColor[(Int32)this.CurrentMode]);
                bb.DrawText(AutomationText[(Int32)this.CurrentMode], 0, 0, bb.Width, bb.Height, AutomationTextColor[(Int32)this.CurrentMode], 16);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
        }
    }

    public class AutomationModeCommandButtonData : ButtonData
    {
        private readonly BitmapColor BgColor = BitmapColor.Transparent;
        private readonly AutomationMode Mode;

        public AutomationModeCommandButtonData(AutomationMode am, BitmapColor bgColor)
        {
            this.Mode = am;
            this.BgColor = bgColor;
        }
        public AutomationModeCommandButtonData(AutomationMode am)
        {
            this.Mode = am;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            const Int32 fillHeight = 24;
            BitmapBuilder bb = new BitmapBuilder(imageSize);
            var bY = (bb.Height - fillHeight) / 2;

            bb.FillRectangle(0, 0, bb.Width, bb.Height, BgColor);
            bb.FillRectangle(0, bY, bb.Width, fillHeight, AutomationModeButtonData.AutomationBgColor[(Int32)this.Mode]);
            bb.DrawText(this.Mode == AutomationMode.Off ? "Off" : AutomationModeButtonData.AutomationText[(Int32)this.Mode], 
                        0, 0, bb.Width, bb.Height, AutomationModeButtonData.AutomationTextColor[(Int32)this.Mode], 16);

            if (this.Mode == this.Plugin.CurrentAutomationMode)
            {
                bb.FillRectangle(0, bY - 4, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bY + fillHeight, bb.Width, 4, BitmapColor.White);
            }

            return bb.ToImage();
        }
        
        public override void runCommand()
        {
            if (this.Mode == this.Plugin.CurrentAutomationMode)
            {
                return;
            }

            if (this.Mode == AutomationMode.Off)
            {
                this.Plugin.SendMidiNote(0, 0x4A + (Int32)this.Plugin.CurrentAutomationMode - 1);
            }
            else
            {
                this.Plugin.SendMidiNote(0, 0x4A + (Int32)this.Mode - 1);
            }
        }
    }

    public class RecPreModeButtonData : ButtonData
    {
        public Boolean SelectionModeActivated = false;

        private BitmapImage[] Icon = new BitmapImage[3];
        private BitmapImage[] IconOn = new BitmapImage[3];

        public RecPreModeButtonData()
        {
            this.loadIcon(RecPreMode.Precount, "precount");
            this.loadIcon(RecPreMode.Preroll, "preroll");
            this.loadIcon(RecPreMode.Autopunch, "autopunch");
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (this.SelectionModeActivated)
            {
                bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(ButtonData.DefaultSelectionBgColor, 190));
                bb.FillRectangle(0, 0, bb.Width, 4, BitmapColor.White);
                bb.FillRectangle(0, bb.Height - 5, bb.Width, 5, BitmapColor.White);
            }

            for (int i = 0; i < this.Icon.Length; i++)
            {
                if (i == (Int32)this.Plugin.CurrentRecPreMode) bb.DrawImage(this.IconOn[i]);
                else                                           bb.DrawImage(this.Icon[i]);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
        }

        private void loadIcon(RecPreMode mode, String baseName)
        {
            this.Icon[(Int32)mode]   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{baseName}_sm_52px.png"));
            this.IconOn[(Int32)mode] = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{baseName}_sm_on_52px.png"));
        }
    }

    public class SendsCommandButtonData : CommandButtonData
    {
        public const Int32 Note = 0x29;
        public SendsCommandButtonData() : base(Note, "SENDS")
        {
            this.Activated = true;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            int rY = 12;
            int rW = bb.Width - 2 * rY;
            int rH = bb.Height - 2 * rY;
            int rX = rY;

            bb.FillRectangle(rX, rY, rW, rH, this.Activated ? CommandButtonData.cRectOn : CommandButtonData.cRectOff);

            bb.DrawText(this.Name, rX, rY, rW, rH, this.Activated ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, 16);

            return bb.ToImage();
        }
        public override void runCommand() => this.Plugin.SetChannelFaderMode(ChannelFaderMode.Send);
    }

    public class PanCommandButtonData : CommandButtonData
    {
        public const Int32 Note = 0x2A;
        public PanCommandButtonData(String name = "PAN") : base(Note, name)
        {
            this.Activated = true;
        }
        public override void runCommand() => this.Plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
    }

    public class UserModeButtonData : ButtonData
    {
        public Int32 ActiveUserPages { get; set; } = 3;
        public Int32 UserPage { get; private set; } = 0;
        String[] PageNames;

        public UserModeButtonData()
        {
        }

        public void setPageNames(String[] pageNames) => this.PageNames = pageNames;

        public void setUserPage(Int32 userPage)
        {
            this.UserPage = userPage;
        }

        public void resetUserPage()
        {
            this.UserPage = 0;
            if (this.Plugin.CurrentChannelFaderMode == ChannelFaderMode.User)
            {
                this.runCommand();
            }
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            var rY = 12;
            var rW = bb.Width - 2 * rY;
            var rH = (bb.Height - 2 * rY) / 2;
            var rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.UserPage == 0 ? CommandButtonData.cRectOff : CommandButtonData.cRectOn);
            bb.DrawText("USER", rX, rY, rW, rH, this.UserPage == 0 ? CommandButtonData.cTextOff : CommandButtonData.cTextOn, 16);

            rY += rH;
            bb.FillRectangle(rX, rY, rW, rH, CommandButtonData.cRectOff);

            var rW2 = rW / 3 + 1;

            if (this.UserPage > 0 && this.PageNames != null && this.PageNames.Length >= this.UserPage)
            {
                bb.DrawText(this.PageNames[this.UserPage - 1], rX, rY, rW, rH, CommandButtonData.cTextOff);
            }
            else
            {
                bb.DrawText("1", rX, rY, rW2, rH, CommandButtonData.cTextOff);

                if (this.ActiveUserPages > 1)
                {
                    if (this.ActiveUserPages > 2)
                    {
                        bb.DrawText(this.ActiveUserPages.ToString(), rX + rW - rW2, rY, rW2, rH, CommandButtonData.cTextOff);
                    }
                    if (this.ActiveUserPages < 4)
                    {
                        bb.DrawText("2", rX + rW2, rY, rW2, rH, CommandButtonData.cTextOff);
                    }
                    else
                    {
                        var sp = (rW2 + 6) / 3;
                        for (var i = 0; i < 3; i++)
                        {
                            bb.FillCircle(rX + rW2 + sp * i, rY + rH / 2 + 2, 1, CommandButtonData.cTextOff);
                        }
                    }
                    if (this.ActiveUserPages < 3)
                    {
                        rX += rW2 * (this.UserPage - 1);
                    }
                    else
                    {
                        if (this.UserPage == this.ActiveUserPages)
                            rX = rX + rW - rW2;
                        else
                            rX += (rW - rW2) / (this.ActiveUserPages - 1) * (this.UserPage - 1);
                    }
                }

                if (this.UserPage > 0)
                {
                    bb.FillRectangle(rX, rY, rW2, rH, CommandButtonData.cRectOn);
                    bb.DrawText(this.UserPage.ToString(), rX, rY, rW2, rH, CommandButtonData.cTextOn);
                }
            }
//            for (int i = 1; i <= 3; i++)
//            {
//                bb.DrawText(i.ToString(), rX + (i - 1) * rW2, rY, rW2, rH, this.UserPage == i ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, 16);
//            }

            return bb.ToImage();

        }
        public override void runCommand()
        {
            if (this.PageNames != null)
            {
                // Display value selection menu.
                var ubmp = new UserButtonMenuParams();
                ubmp.MenuItems = this.PageNames;
                this.Plugin.EmitUserButtonMenuActivated(ubmp);
            }
            else
            {
                this.UserPage = (Control.ModifierKeys & Keys.Shift) == Keys.Shift
                    ? this.UserPage <= 1 ? this.ActiveUserPages : this.UserPage - 1
                    : this.UserPage > this.ActiveUserPages - 1 ? 1 : this.UserPage + 1;

                this.Plugin.SetChannelFaderMode(ChannelFaderMode.User, this.UserPage);
            }
        }
    }
    public class UserPageMenuSelectButtonData : UserMenuSelectButtonData
    { 
//        public UserPageMenuSelectButtonData() { }
        public override void runCommand()
        {
            this.Plugin.SetChannelFaderMode(ChannelFaderMode.User, this.Value);

            this.Plugin.EmitUserButtonMenuActivated(new UserButtonMenuParams { ChannelIndex = -1, IsActive = false });
        }
    }

}