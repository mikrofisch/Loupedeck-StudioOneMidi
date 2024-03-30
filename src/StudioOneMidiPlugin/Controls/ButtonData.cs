namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.UI.WebControls;
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
        public ChannelProperty.BoolType Type;
        public TrackNameMode ShowTrackName;
        public BitmapImage Icon;

        protected const int TrackNameH = 24;

        public PropertyButtonData(int channelIndex, ChannelProperty.BoolType bt, TrackNameMode tm = TrackNameMode.ShowFull, string iconName = null)
        {
            this.ChannelIndex = channelIndex;
            this.Type = bt;
            ShowTrackName = tm;

            if (iconName != null)
                Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {

            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (cd.BoolProperty[(int)this.Type])
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.boolPropertyColor[(int)this.Type]);
            else
                bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));

            int yOff = this.ShowTrackName == TrackNameMode.None ? 0 : TrackNameH;

            if (this.Icon != null)
            {
                bb.DrawImage(this.Icon, bb.Width / 2 - this.Icon.Width / 2, yOff + (bb.Height - yOff) / 2 - this.Icon.Height / 2);
            }
            else
            {
                bb.DrawText(ChannelProperty.boolPropertyLetter[(int)this.Type], 0, yOff, bb.Width, bb.Height - yOff, null, 32);
            }

            if (this.ShowTrackName != TrackNameMode.None)
            {
                int hPos = 0;

                if (this.ShowTrackName == TrackNameMode.ShowLeftHalf)  hPos = bb.Width / 2 - 1;
                if (this.ShowTrackName == TrackNameMode.ShowRightHalf) hPos = -bb.Width / 2 + 1;

                bb.DrawText(cd.Label, hPos, 0, bb.Width, TrackNameH);
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
        public BitmapImage IconSelMon, IconSelRec;
        private bool sendMode = false;

        public SelectButtonData(int channelIndex, ChannelProperty.BoolType bt) : base(channelIndex, bt)
        {
            this.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
            this.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
        }

        public void sendModeChanged(bool sm)
        {
            this.sendMode = sm;
        }
        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            BitmapBuilder bb = new BitmapBuilder(imageSize);

            if (!this.sendMode)
            {
                if (cd.Selected)
                    bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Select]);
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
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Mute]);
                if (cd.Solo)
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Solo]);
                if (cd.Armed)
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Arm]);
                if (cd.Monitor)
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Monitor]);

                bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Mute], rX, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
                bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Solo], rX2, rY, rW, rH, new BitmapColor(175, 175, 175), rH - 4);
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
            else
            {
//                bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                bb.DrawText(cd.Description, 0, 0, bb.Width, TrackNameH, new BitmapColor(175,175,175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TrackNameH / 2, bb.Width, TrackNameH);
            }


            return bb.ToImage();
        }

        public override void runCommand()
        {
            MackieChannelData cd = this.Plugin.mackieChannelData[this.ChannelIndex.ToString()];
            if (!cd.Selected)
            {
                base.runCommand();
            }
            Plugin.EmitSelectedButtonPressed();
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
        public string IconName;

        public bool Activated = false;

        public BitmapColor OffColor = BitmapColor.Black;
        public BitmapColor OnColor = BitmapColor.Black;
        public BitmapImage Icon, IconOn;

        public CommandButtonData(int code, string name, string iconName = null)
        {
            this.init(code, name, iconName);
        }

        public CommandButtonData(int code, int codeOn, string name, string iconName = null)
        {
            this.init(code, name, iconName);
            this.CodeOn = codeOn;
        }
        public CommandButtonData(int code, string name, BitmapColor onColor, bool isActivatedByDefault = false)
        {
            this.init(code, name, null);
            this.OnColor = onColor;
            this.Activated = isActivatedByDefault;
        }

        private void init (int code, string name, string iconName)
        {
            this.Name = name;
            this.Code = code;

            if (iconName != null)
                this.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
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
                bb.DrawText(this.Name, 0, 0, bb.Width, bb.Height, null, 16);
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
            e.Velocity = (SevenBitNumber)(127);
            e.NoteNumber = (SevenBitNumber)(param);
            this.Plugin.mackieMidiOut.SendEvent(e);
        }
    }

    public class FlipPanVolCommandButtonData : CommandButtonData
    {
        public FlipPanVolCommandButtonData() : base(0x32, "Flip Vol/Pan")
        {
        }

        public override BitmapImage getImage(PluginImageSize imageSize)
        {
            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);

            BitmapColor cRectOn = BitmapColor.White;
            BitmapColor cTextOn = BitmapColor.Black;
            BitmapColor cRectOff = new BitmapColor(50, 50, 50);
            BitmapColor cTextOff = new BitmapColor(160, 160, 160);

            int rY = 16;
            int rS = 8;
            int rW = bb.Width - 24;
            int rH = (bb.Height - 2 * rY - rS) / 2;
            int rX = (bb.Width - rW) / 2;

            bb.FillRectangle(rX, rY, rW, rH, this.Activated ? cRectOff : cRectOn);
            bb.DrawText("VOL", rX, rY, rW, rH, this.Activated ? cTextOff : cTextOn, rH - 6);

            bb.FillRectangle(rX, rY + rH + rS, rW, rH, this.Activated ? cRectOn : cRectOff);
            bb.DrawText("PAN", rX, rY + rH + rS, rW, rH, this.Activated ? cTextOn : cTextOff, rH - 6);

            return bb.ToImage();

        }
    }

}