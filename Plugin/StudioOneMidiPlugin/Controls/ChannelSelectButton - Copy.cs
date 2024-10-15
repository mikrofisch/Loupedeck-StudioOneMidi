namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Common;

    // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ChannelSelectButton : ActionEditorCommand
    {
        private StudioOneMidiPlugin plugin;

        private const String ChannelSelector = "channelSelector";
        private const int TitleHeight = 24;

        private BitmapImage IconSelMon, IconSelRec;

        private SelectButtonMode  CurrentMode = SelectButtonMode.Select;
        private bool  UserButtonActive = false;
        private Int32 ChannelIndex = -1;

        public ChannelSelectButton()
        {
            this.DisplayName = "Channel Select Button";
            this.Description = "Button for selecting a channel";
            this.GroupName = "";

            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ChannelSelector, labelText: "Channel:"/*,"Select the property to control"*/)
                    .SetRequired()
                );

            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;

            this.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
            this.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
        }

        protected override Boolean OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            this.plugin.UserButtonChanged += (object sender, UserButtonParams e) =>
            {
                if (e.channelIndex == this.ChannelIndex)
                {
                    this.UserButtonActive = e.isActive;
                    this.ActionImageChanged();
                }
            };

            this.plugin.ChannelDataChanged += (object sender, EventArgs e) => {
                this.ActionImageChanged();
            };

            this.plugin.SelectModeChanged += (object sender, SelectButtonMode e) =>
            {
                this.CurrentMode = e;
                this.ActionImageChanged();
            };

            return true;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ButtonTitleSelector, isEnabled);
            */

            if (e.ControlName.EqualsNoCase(ChannelSelector))
            {
                int i;
                for (i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    e.AddItem($"{i}", $"Bank Channel {i + 1}", $"Channel {i + 1} of the current bank of 6 channels controlled by the Loupedeck device");
                }
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return null;
            this.ChannelIndex = (int)channelIndex.ParseInt32();

            MackieChannelData cd = this.plugin.mackieChannelData[channelIndex];
            //if (!this.Plugin.mackieChannelData.TryGetValue(this.ChannelIndex.ToString(), out MackieChannelData cd))
            //    return;

            var bb = new BitmapBuilder(imageWidth, imageHeight);

            if (this.CurrentMode == SelectButtonMode.Send)
            {
                //                bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }
            else if (this.CurrentMode == SelectButtonMode.User)
            {
                bb.DrawText(cd.Description, 0, 0, bb.Width, TitleHeight, new BitmapColor(175, 175, 175));
                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
                bb.FillRectangle(0, bb.Height * 2 / 3, bb.Width, bb.Height / 3, cd.UserLabel.Length > 0 ? new BitmapColor(100, 100, 100) : new BitmapColor(30, 30, 30));
                bb.DrawText(cd.UserLabel, 0, bb.Height * 2 / 3, bb.Width, TitleHeight, this.UserButtonActive ? BitmapColor.White : BitmapColor.Black);
            }
            else
            {
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

                bb.DrawText(cd.Label, 0, bb.Height / 2 - TitleHeight / 2, bb.Width, TitleHeight);
            }

            return bb.ToImage();
        }
        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;

            switch (this.CurrentMode)
            {
                case SelectButtonMode.Select:
                    MackieChannelData cd = this.plugin.mackieChannelData[channelIndex];
                    if (!cd.Selected)
                    {
                        cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Select);
                    }
                    this.plugin.EmitSelectedButtonPressed();
                    break;
                case SelectButtonMode.User:
                    NoteOnEvent e = new NoteOnEvent();
                    e.Velocity = (SevenBitNumber)127;
                    e.NoteNumber = (SevenBitNumber)(UserButtonMidiBase + channelIndex.ParseInt32());
                    this.plugin.mackieMidiOut.SendEvent(e);
                    break;
            }
            return true;
        }
    }
}