namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Diagnostics;
    using System.Security.Cryptography.X509Certificates;

    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using static Loupedeck.StudioOneMidiPlugin.Controls.PropertyButtonDataBase;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public abstract class ButtonData
    {
        protected StudioOneMidiPlugin Plugin;
        protected const int TrackNameH = 24;

        public virtual void OnLoad(StudioOneMidiPlugin plugin) => this.Plugin = plugin;
        public abstract BitmapImage getImage(PluginImageSize imageSize);
        public abstract void runCommand();
    }

    public abstract class PropertyButtonDataBase : ButtonData
    {
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

        public PropertyButtonDataBase(int channelIndex,
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

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            ChannelProperty.PropertyType type,
                                            Boolean isSelected,
                                            TrackNameMode showTrackName,
                                            String trackName,
                                            BitmapImage icon)
        {
            if (isSelected)
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(int)type]);

            int yOff = showTrackName == TrackNameMode.None ? 0 : icon == null ? TrackNameH : TrackNameH - 8;

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
                int hPos = 0;
                int width = bb.Width;

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
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            // bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
            return PropertyButtonDataBase.drawImage(new BitmapBuilder(imageSize),
                                                    this.Type,
                                                    cd.BoolProperty[(Int32)this.Type],
                                                    this.ShowTrackName,
                                                    cd.Label,
                                                    this.Icon);
        }

        public override void runCommand()
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];

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
            BitmapBuilder bb = new BitmapBuilder(imageSize);

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
        public SelectButtonMode CurrentMode = SelectButtonMode.Select;
        public bool UserButtonActive = false;
        public static ChannelProperty.PropertyType SelectionPropertyType = ChannelProperty.PropertyType.Select;

        private Int32 ChannelIndex = -1;

        private const int TitleHeight = 24;
        private static BitmapImage IconSelMon, IconSelRec;
        private static readonly BitmapColor CommandPropertyColor = new BitmapColor(40, 40, 40);

        public SelectButtonData(int channelIndex)
        {
            this.ChannelIndex = channelIndex;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            var bb = new BitmapBuilder(imageSize);

            return SelectButtonData.drawImage(bb, cd, this.CurrentMode, this.UserButtonActive, SelectionPropertyType);
        }

        public static BitmapImage drawImage(BitmapBuilder bb,
                                            MackieChannelData cd,
                                            SelectButtonMode buttonMode,
                                            Boolean userButtonActive,
                                            ChannelProperty.PropertyType commandProperty = ChannelProperty.PropertyType.Select)
        {
            if (SelectButtonData.IconSelMon == null)
            {
                SelectButtonData.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
                SelectButtonData.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
            }

            if (buttonMode == SelectButtonMode.Send)
            {
                //                bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            else if (buttonMode == SelectButtonMode.User)
            {
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
                bb.FillRectangle(0, bb.Height * 2 / 3, bb.Width, bb.Height / 3, cd.UserLabel.Length > 0 ? new BitmapColor(100, 100, 100) : new BitmapColor(30, 30, 30));
                bb.DrawText(cd.UserLabel, 0, bb.Height * 2 / 3, bb.Width, TitleHeight, userButtonActive ? BitmapColor.White : BitmapColor.Black);
            }
            else
            {
                if (buttonMode == SelectButtonMode.Select) commandProperty = ChannelProperty.PropertyType.Select;

                if (cd.Selected)
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Select]);
                }
                else
                {
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));
                }

                int rX = 8;
                int rY = 4;
                int rS = 8;
                int rW = (bb.Width - rS) / 2 - rX;
                int rH = (bb.Height - rY - TitleHeight) / 2 - rS;
                int rX2 = rX + rW + rS;
                int rY2 = rY + rH + rS + TitleHeight;

                bb.FillRectangle(rX2 - 5, rY, 2, rH, new BitmapColor(40, 40, 40));
                bb.FillRectangle(rX2 - 5, rY2, 2, rH, new BitmapColor(40, 40, 40));

                if (cd.Muted || commandProperty == ChannelProperty.PropertyType.Mute)
                {
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4,
                        cd.Muted ? ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Mute]
                                 : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Solo || commandProperty == ChannelProperty.PropertyType.Solo)
                {
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4,
                        cd.Solo ? ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Solo]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Armed || commandProperty == ChannelProperty.PropertyType.Arm)
                {
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Armed ? ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Arm]
                                : SelectButtonData.CommandPropertyColor);
                }
                if (cd.Monitor || commandProperty == ChannelProperty.PropertyType.Monitor)
                {
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4,
                        cd.Monitor ? ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Monitor]
                                : SelectButtonData.CommandPropertyColor);
                }
                bb.DrawText(ChannelProperty.PropertyLetter[(int)ChannelProperty.PropertyType.Mute], rX, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawText(ChannelProperty.PropertyLetter[(int)ChannelProperty.PropertyType.Solo], rX2, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawImage(SelectButtonData.IconSelRec, rX + rW / 2 - SelectButtonData.IconSelRec.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);
                bb.DrawImage(SelectButtonData.IconSelMon, rX2 + rW / 2 - SelectButtonData.IconSelMon.Width / 2, rY2 + rH / 2 - SelectButtonData.IconSelRec.Height / 2);

                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            return bb.ToImage();
        }

        public override void runCommand()
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
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
                    NoteOnEvent e = new NoteOnEvent();
                    e.Velocity = (SevenBitNumber)127;
                    e.NoteNumber = (SevenBitNumber)(UserButtonMidiBase + this.ChannelIndex);
                    this.Plugin.mackieMidiOut.SendEvent(e);
                    break;
            }
        }
    }

    public class CommandButtonData : ButtonData
    {
        public int Code;
        public int CodeOn = 0;              // alternative code to send when activated
        public string Name;

        public virtual Boolean Activated { get; set; } = false;

        public BitmapColor OffColor = BitmapColor.Black;
        public BitmapColor OnColor = BitmapColor.Black;
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


        public CommandButtonData(int code, string name, string iconName = null)
        {
            this.init(code, name, iconName);
        }

        public CommandButtonData(int code, int codeOn, string name, string iconName = null)
        {
            this.init(code, name, iconName);
            this.CodeOn = codeOn;
        }
        public CommandButtonData(int code, string name, BitmapColor onColor, BitmapColor textOnColor, bool isActivatedByDefault = false)
        {
            this.init(code, name, null);
            this.OnColor = onColor;
            this.TextOnColor = textOnColor;
            this.Activated = isActivatedByDefault;
        }

        private void init(int code, string name, string iconName)
        {
            this.Name = name;
            this.Code = code;

            if (iconName != null)
            {
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
                var iconResOn = EmbeddedResources.FindFile($"{iconName}_on_52px.png");
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
            int param = (SevenBitNumber)(this.Code);
            if (this.Activated && (this.CodeOn > 0))
            {
                param = (SevenBitNumber)(this.CodeOn);
            }

            NoteOnEvent e = new NoteOnEvent();
            e.Channel = (FourBitNumber)this.MidiChannel;
            e.Velocity = (SevenBitNumber)(127);
            e.NoteNumber = (SevenBitNumber)(param);
            this.Plugin.mackieMidiOut.SendEvent(e);
        }
    }

    public class FlipPanVolCommandButtonData : CommandButtonData
    {
        public FlipPanVolCommandButtonData(int code) : base(code, "Flip Vol/Pan")
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

    public class ModeButtonData : ButtonData
    {
        public string Name;
        public BitmapImage Icon = null;

        public ModeButtonData(string name, string iconName = null)
        {
            this.Name = name;

            if (iconName != null)
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon, 0, 0);
            }
            else
            {
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, null, 16);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {

        }
    }

    public class ModeTopCommandButtonData : CommandButtonData
    {
        public enum Location
        {
            Left,
            Right
        }
        Location ButtonLocation = Location.Left;
        string TopDisplayText;


        public ModeTopCommandButtonData(int code, string name, Location bl, string iconName) : base(code, name, iconName)
        {
            this.ButtonLocation = bl;
        }

        public void setTopDisplay(string text)
        {
            this.TopDisplayText = text;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);

            int dispTxtH = 24;

            bb.FillRectangle(0, 0, bb.Width, bb.Height, this.Activated ? this.OnColor : this.OffColor);

            if (this.Activated && this.IconOn != null)
            {
                bb.DrawImage(this.IconOn, (bb.Width - this.IconOn.Width) / 2, dispTxtH);
            }
            else if (this.Icon != null)
            {
                bb.DrawImage(this.Icon, (bb.Width - this.Icon.Width) / 2, dispTxtH);
            }
            else
            {
                bb.DrawText(this.Name, 0, dispTxtH, bb.Width, bb.Height - dispTxtH, null, 16);
            }

            int hPos;
            if (this.ButtonLocation == Location.Left)
                hPos = 1;
            else
                hPos = -bb.Width - 1;

            bb.DrawText(this.TopDisplayText, hPos, 0, bb.Width * 2, dispTxtH);

            return bb.ToImage();
        }
    }

    public class ModeChannelSelectButtonData : ButtonData
    {
        public BitmapImage Icon, IconOn;
        public Boolean Activated = true;

        public ModeChannelSelectButtonData()
        {
            this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_80px.png"));
            this.IconOn = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("select-select_on_80px.png"));
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

    public class SendsCommandButtonData : CommandButtonData
    {
        public SendsCommandButtonData(int code) : base(code, "SENDS")
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

    }

    public class UserModeButtonData : ButtonData
    {
        private int UserMode = 0;
        public bool UserMode1Activated = false;
        public bool UserMode2Activated = false;
        public bool UserMode3Activated = false;

        public UserModeButtonData()
        {
        }

        public void setUserMode(int userMode, bool activated)
        {
            switch (userMode)
            {
                case 0x2B:
                    this.UserMode1Activated = activated;
                    break;
                case 0x2C:
                    this.UserMode2Activated = activated;
                    break;
                case 0x2D:
                    this.UserMode3Activated = activated;
                    break;
            }
            if (this.UserMode1Activated)
                this.UserMode = 1;
            else if (this.UserMode2Activated)
                this.UserMode = 2;
            else if (this.UserMode3Activated)
                this.UserMode = 3;
            else
                this.UserMode = 0;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            int rY = 12;
            int rW = bb.Width - 2 * rY;
            int rH = (bb.Height - 2 * rY) / 2;
            int rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.UserMode == 0 ? CommandButtonData.cRectOff : CommandButtonData.cRectOn);
            bb.DrawText("USER", rX, rY, rW, rH, this.UserMode == 0 ? CommandButtonData.cTextOff : CommandButtonData.cTextOn, 16);

            rY += rH;
            bb.FillRectangle(rX, rY, rW, rH, CommandButtonData.cRectOff);

            int rW2 = rW / 3;
            int rW3 = rW - 2 * rW2;
            if (this.UserMode > 0)
            {
                bb.FillRectangle(rX + (this.UserMode - 1) * rW2, rY, this.UserMode == 3 ? rW3 : rW2, rH, CommandButtonData.cRectOn);
            }
            for (int i = 1; i <= 3; i++)
            {
                bb.DrawText(i.ToString(), rX + (i - 1) * rW2, rY, rW2, rH, this.UserMode == i ? CommandButtonData.cTextOn : CommandButtonData.cTextOff, 16);
            }

            return bb.ToImage();

        }
        public override void runCommand()
        {
            this.UserMode += 1;
            if (this.UserMode > 3)
            {
                this.UserMode = 1;
            }

            int Code = 0x2A + this.UserMode;

            NoteOnEvent e = new NoteOnEvent();
            e.Velocity = (SevenBitNumber)(127);
            e.NoteNumber = (SevenBitNumber)Code;
            this.Plugin.mackieMidiOut.SendEvent(e);
        }

    }


    // Triggers a command from a list in LoupedeckCT.surface.xml on the Studio One side.
    // These are one-shot commands that do not provide feedback.
    //
    public class OneWayCommandButtonData : CommandButtonData
    {
        public OneWayCommandButtonData(int code, string name, string iconName=null) : base(0, name, iconName)
        {
            this.MidiChannel = 1;
            this.Code = code;
        }
        public OneWayCommandButtonData(int code, string name, BitmapColor textColor) : base(0, name, null)
        {
            this.MidiChannel = 1;
            this.Code = code;
            this.TextColor = textColor;
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
}