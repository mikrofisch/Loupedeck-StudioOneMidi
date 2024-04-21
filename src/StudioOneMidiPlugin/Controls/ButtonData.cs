namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System.Diagnostics;

    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    public abstract class ButtonData
    {
        protected StudioOneMidiPlugin Plugin;
        public virtual void OnLoad(StudioOneMidiPlugin plugin)
        {
            this.Plugin = plugin;
        }
        public abstract BitmapImage getImage(PluginImageSize imageSize);
        public abstract void runCommand();
    }

    public class PropertyButtonData : ButtonData
    {
        public enum TrackNameMode
        {
            None,
            ShowFull,
            ShowLeftHalf,
            ShowRightHalf
        }

        public int ChannelIndex;
        public ChannelProperty.PropertyType Type;
        public TrackNameMode ShowTrackName;
        public BitmapImage Icon;

        protected const int TrackNameH = 24;

        public PropertyButtonData(int channelIndex, 
                                  ChannelProperty.PropertyType bt, 
                                  TrackNameMode tm = TrackNameMode.ShowFull, 
                                  string iconName = null)
        {
            this.ChannelIndex = channelIndex;
            this.Type = bt;
            this.ShowTrackName = tm;

            if (iconName != null)
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {

            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (cd.BoolProperty[(int)this.Type])
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(int)this.Type]);
            else
                bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));

            int yOff = this.ShowTrackName == TrackNameMode.None ? 0 : TrackNameH;

            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon, bb.Width / 2 - this.Icon.Width / 2, yOff + (bb.Height - yOff) / 2 - this.Icon.Height / 2);
            }
            else
            {
                bb.DrawText(ChannelProperty.PropertyLetter[(int)this.Type], 0, yOff, bb.Width, bb.Height - yOff, null, 32);
            }

            if (this.ShowTrackName != TrackNameMode.None)
            {
                int hPos = 0;

                if (this.ShowTrackName == TrackNameMode.ShowLeftHalf)
                    hPos = 1;
                if (this.ShowTrackName == TrackNameMode.ShowRightHalf)
                    hPos = -bb.Width - 1;

                bb.DrawText(cd.Label, hPos, 0, bb.Width * 2, TrackNameH);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];

            cd.EmitBoolPropertyPress(this.Type);
        }
    }

    public class SelectButtonData : PropertyButtonData
    {
        public const int UserButtonMidiBase = 0x6C;
        public BitmapImage IconSelMon, IconSelRec;

        public enum Mode
        {
            Select,
            Send,
            User
        }
        private Mode CurrentMode = Mode.Select;
        private bool UserButtonActive = false;

        public SelectButtonData(int channelIndex, ChannelProperty.PropertyType bt) : base(channelIndex, bt)
        {
            this.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
            this.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
        }

        public void selectModeChanged(Mode sm)
        {
            this.CurrentMode = sm;
        }

        public void userButtonChanged(bool isActive)
        {
            this.UserButtonActive = isActive;
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (this.CurrentMode == Mode.Send)
            {
                //                bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                bb.DrawText(cd.Description, 0, 0, bb.Width, TrackNameH, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TrackNameH / 2, bb.Width, TrackNameH);
            }
            else if (this.CurrentMode == Mode.User)
            {
                bb.DrawText(cd.Description, 0, 0, bb.Width, TrackNameH, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TrackNameH / 2, bb.Width, TrackNameH);
                bb.FillRectangle(0, bb.Height * 2 / 3, bb.Width, bb.Height / 3, cd.UserLabel.Length > 0 ? new BitmapColor(100, 100, 100) : new BitmapColor(30, 30, 30));
                bb.DrawText(cd.UserLabel, 0, bb.Height * 2 / 3, bb.Width, TrackNameH, this.UserButtonActive ? BitmapColor.White : BitmapColor.Black);
            }
            else
            {
                if (cd.Selected)
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Select]);
                else
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));

                int rX = 8;
                int rY = 4;
                int rS = 8;
                int rW = (bb.Width - rS) / 2 - rX;
                int rH = (bb.Height - rY - TrackNameH) / 2 - rS;
                int rX2 = rX + rW + rS;
                int rY2 = rY + rH + rS + TrackNameH;

                bb.FillRectangle(rX2 - 6, rY, 2, rH, new BitmapColor(40, 40, 40));
                bb.FillRectangle(rX2 - 6, rY2, 2, rH, new BitmapColor(40, 40, 40));

                if (cd.Muted)
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Mute]);
                if (cd.Solo)
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Solo]);
                if (cd.Armed)
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Arm]);
                if (cd.Monitor)
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Monitor]);

                bb.DrawText(ChannelProperty.PropertyLetter[(int)ChannelProperty.PropertyType.Mute], rX, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawText(ChannelProperty.PropertyLetter[(int)ChannelProperty.PropertyType.Solo], rX2, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawImage(this.IconSelRec, rX + rW / 2 - this.IconSelRec.Width / 2, rY2 + rH / 2 - this.IconSelRec.Height / 2);
                bb.DrawImage(this.IconSelMon, rX2 + rW / 2 - this.IconSelMon.Width / 2, rY2 + rH / 2 - this.IconSelRec.Height / 2);

                bb.DrawText(cd.Label, 0, bb.Height / 2 - TrackNameH / 2, bb.Width, TrackNameH);

                //int rX = 8;
                //int rY = TrackNameH + 4;
                //int rS = 8;
                //int rW = (bb.Width - rS) / 2 - rX;
                //int rH = (bb.Height - rY) / 2 - rS;
                //int rX2 = rX + rW + rS;
                //int rY2 = rY + rH + rS;

                //bb.FillRectangle(rX - 2, rY2 - 6, 2 * rW + 10, 2, new BitmapColor(40, 40, 40));
                //bb.FillRectangle(rX2 - 6, rY - 2, 2, rH * 2 + 10, new BitmapColor(40, 40, 40));

                //if (cd.Muted)
                //    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Mute]);
                //if (cd.Solo)
                //    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Solo]);
                //if (cd.Armed)
                //    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Arm]);
                //if (cd.Monitor)
                //    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Monitor]);

                //bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Mute], rX, rY, rW, rH, null, rH - 4);
                //bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Solo], rX2, rY, rW, rH, null, rH - 4);
                //bb.DrawImage(this.IconSelRec, rX + rW / 2 - this.IconSelRec.Width / 2, rY2 + rH / 2 - this.IconSelRec.Height / 2);
                //bb.DrawImage(this.IconSelMon, rX2 + rW / 2 - this.IconSelMon.Width / 2, rY2 + rH / 2 - this.IconSelRec.Height / 2);

                //bb.DrawText(cd.Label, 0, 0, bb.Width, TrackNameH);
            }

            return bb.ToImage();
        }

        public override void runCommand()
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];

            switch (this.CurrentMode)
            {
                case Mode.Select:
                    if (!cd.Selected)
                    {
                        base.runCommand();
                    }
                    Plugin.EmitSelectedButtonPressed();
                    break;
                case Mode.User:
                    NoteOnEvent e = new NoteOnEvent();
                    e.Velocity = (SevenBitNumber)127;
                    e.NoteNumber = (SevenBitNumber)(SelectButtonData.UserButtonMidiBase + this.ChannelIndex);
                    this.Plugin.mackieMidiOut.SendEvent(e);
                    break;
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

    public class CommandButtonData : ButtonData
    {
        public int Code;
        public int CodeOn = 0;              // alternative code to send when activated
        public string Name;

        public bool Activated = false;

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

            int hPos = 0;
            if (this.ButtonLocation == Location.Left)
                hPos = 1;
            else
                hPos = -bb.Width - 1;

            bb.DrawText(this.TopDisplayText, hPos, 0, bb.Width * 2, dispTxtH);

            return bb.ToImage();
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