namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Net.Sockets;

    using Melanchall.DryWetMidi.Core;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    // This is a special button class that creates 6 buttons which are then
    // placed in the central area of the button field on the Loupedeck.
    // The functionality of the buttons changes dynamically.
    internal class ChannelModesKeypad : StudioOneButton<ButtonData>
    {
        private enum ButtonLayer
        {
            viewSelector,
            channelPropertiesPlay,
            channelPropertiesRec,
            faderModesShow,
            faderModesSend
        }

        private enum UserSendsMode
        {
            Sends,
            User
        }
        private static UserSendsMode LastUserSendsMode = UserSendsMode.Sends;

        private static readonly String idxUserButton = $"{(Int32)ButtonLayer.faderModesSend}:3";
        private static readonly String idxSendButton = $"{(Int32)ButtonLayer.faderModesSend}:5";
        private static readonly String idxPlayMuteSoloButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:0";
        private static readonly String idxPlaySelButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:1";
        private static readonly String idxRecArmMonitorButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:0";

        private IDictionary<int, string> noteReceivers = new Dictionary<int, string>();

        ButtonLayer CurrentLayer = ButtonLayer.channelPropertiesPlay;

        public ChannelModesKeypad() : base()
        {
            this.Description = "Special button for controlling Loupedeck fader modes";

            // Create UI buttons
            this.AddParameter($"{0}", "Mode Group Button 1-1", "Channel Modes");
            this.AddParameter($"{1}", "Mode Group Button 2-1", "Channel Modes");
            this.AddParameter($"{2}", "Mode Group Button 1-2", "Channel Modes");
            this.AddParameter($"{3}", "Mode Group Button 2-2", "Channel Modes");
            this.AddParameter($"{4}", "Mode Group Button 1-3", "Channel Modes");
            this.AddParameter($"{5}", "Mode Group Button 2-3", "Channel Modes");

            this.addButton(ButtonLayer.viewSelector, 0, new ModeButtonData("PLAY"));
            this.addButton(ButtonLayer.viewSelector, 1, new ModeButtonData("REC"));
            this.addButton(ButtonLayer.viewSelector, 2, new ModeButtonData("SHOW"));
            this.addButton(ButtonLayer.viewSelector, 3, new ModeButtonData("USER\rSENDS"));

            // Create button data for each layer
            //this.addButton(ButtonLayer.channelProperties, 0, new PropertyButtonData(StudioOneMidiPlugin.ChannelCount, 
            //                                                                        ChannelProperty.PropertyType.Mute, 
            //                                                                        PropertyButtonData.TrackNameMode.ShowLeftHalf));
            //this.addButton(ButtonLayer.channelProperties, 1, new PropertyButtonData(StudioOneMidiPlugin.ChannelCount, 
            //                                                                        ChannelProperty.PropertyType.Solo,
            //                                                                        PropertyButtonData.TrackNameMode.ShowRightHalf));
            //this.addButton(ButtonLayer.channelProperties, 2, new PropertyButtonData(StudioOneMidiPlugin.ChannelCount, 
            //                                                                        ChannelProperty.PropertyType.Arm, 
            //                                                                        PropertyButtonData.TrackNameMode.None,
            //                                                                        "arm"));
            //this.addButton(ButtonLayer.channelProperties, 3, new PropertyButtonData(StudioOneMidiPlugin.ChannelCount,
            //                                                                        ChannelProperty.PropertyType.Monitor, 
            //                                                                        PropertyButtonData.TrackNameMode.None,
            //                                                                        "monitor"));
            this.addButton(ButtonLayer.channelPropertiesPlay, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                                                                 ChannelProperty.PropertyType.Solo,
                                                                                                 "select-mute", "select-solo", "select-mute-solo",
                                                                                                 activated: false));
            this.addButton(ButtonLayer.channelPropertiesPlay, 1, new ModeChannelSelectButtonData());
            this.addButton(ButtonLayer.channelPropertiesPlay, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelPropertiesPlay, 5, new FlipPanVolCommandButtonData(0x32), true);

            this.addButton(ButtonLayer.channelPropertiesRec, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                                                                ChannelProperty.PropertyType.Monitor,
                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
                                                                                                activated: true));
            this.addButton(ButtonLayer.channelPropertiesRec, 4, new ModeButtonData("VIEWS"));

            this.addButton(ButtonLayer.faderModesShow, 0, new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, 1, new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, 2, new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, 3, new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.faderModesShow, 5, new CommandButtonData(0x33, "ALL", new BitmapColor(60, 60, 20), BitmapColor.White, true), true);

            this.addButton(ButtonLayer.faderModesSend, 0, new ModeTopCommandButtonData(0x51, "Previous\rPlugin", ModeTopCommandButtonData.Location.Left, "plugin_prev"));
            this.addButton(ButtonLayer.faderModesSend, 1, new ModeTopCommandButtonData(0x52, "Next\rPlugin", ModeTopCommandButtonData.Location.Right, "plugin_next"));
            this.addButton(ButtonLayer.faderModesSend, 2, new CommandButtonData(0x50, "Channel Editor"));
            this.addButton(ButtonLayer.faderModesSend, 3, new UserModeButtonData());
            this.addButton(ButtonLayer.faderModesSend, 4, new CommandButtonData(0x2A, "VIEWS"));
            this.addButton(ButtonLayer.faderModesSend, 5, new SendsCommandButtonData(0x29), isNoteReceiver: true);
        }

        // common
        protected override bool OnLoad()
        {
            base.OnLoad();

            this.plugin.Ch0NoteReceived += this.OnNoteReceived;

            this.plugin.ChannelDataChanged += (object sender, EventArgs e) => {
                this.EmitActionImageChanged();
            };

            this.plugin.SelectButtonPressed += (object sender, EventArgs e) =>
            {
                this.CurrentLayer = ButtonLayer.channelPropertiesPlay;
                this.EmitActionImageChanged();
            };

            this.plugin.FocusDeviceChanged += (object sender, string e) =>
            {
                for (int i = 0; i < 2; i++)
                {
                    var mtcbd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:{i}"] as ModeTopCommandButtonData;
                    mtcbd.setTopDisplay(e);
                }
                this.EmitActionImageChanged();
            };

            return true;
        }

        protected void OnNoteReceived(object sender, NoteOnEvent e)
        {
            if (e.NoteNumber >= 0x2B && e.NoteNumber <= 0x2D)
            {
                // User mode changed
                var umbd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:3"] as UserModeButtonData;
                umbd.setUserMode(e.NoteNumber, e.Velocity > 0);
            }
            else if (this.noteReceivers.ContainsKey(e.NoteNumber))
            {
                var cbd = this.buttonData[this.noteReceivers[e.NoteNumber]] as CommandButtonData;
                cbd.Activated = e.Velocity > 0;
            }
            this.EmitActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            string idx = $"{(int)this.CurrentLayer}:" + actionParameter;

            if (this.buttonData.TryGetValue(idx, out var bd))
            {
                return bd.getImage(imageSize);
            }

            BitmapBuilder bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            return bb.ToImage();
        }

        protected override void RunCommand(string actionParameter)
        {
            var idx = $"{(int)this.CurrentLayer}:{actionParameter}";
            if (this.buttonData.TryGetValue(idx, out var bd))
            {
                bd.runCommand();
            }

            SelectButtonMode selectMode;

            switch (this.CurrentLayer)
            {
                case ButtonLayer.viewSelector:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 0: // PLAY
                            this.CurrentLayer = ButtonLayer.channelPropertiesPlay;
                            selectMode = (this.buttonData[idxPlaySelButton] as ModeChannelSelectButtonData).Activated ? SelectButtonMode.Select
                                                                                                                      : SelectButtonMode.Property;
                            this.plugin.EmitSelectModeChanged(selectMode);
                            this.plugin.EmitPropertySelectionChanged((this.buttonData[idxPlayMuteSoloButton] as PropertySelectionButtonData).CurrentType);
                            break;
                        case 1: // REC
                            this.CurrentLayer = ButtonLayer.channelPropertiesRec;
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                            this.plugin.EmitPropertySelectionChanged((this.buttonData[idxRecArmMonitorButton] as PropertySelectionButtonData).CurrentType);
                            break;
                        case 2: // SHOW
                            this.plugin.EmitPropertySelectionChanged(ChannelProperty.PropertyType.Select);
                            this.CurrentLayer = ButtonLayer.faderModesShow;
                            break;
                        case 3: // USER/SENDS
                            this.CurrentLayer = ButtonLayer.faderModesSend;
                            if (LastUserSendsMode == UserSendsMode.Sends)
                            {
                                selectMode = SelectButtonMode.Send;
                                this.buttonData[idxSendButton].runCommand();
                            }
                            else
                            {
                                selectMode = SelectButtonMode.User;
                                this.buttonData[idxUserButton].runCommand();
                            } 
                            this.plugin.EmitSelectModeChanged(selectMode);
                            break;
                    }
                    this.EmitActionImageChanged();
                    break;
                case ButtonLayer.channelPropertiesPlay:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 0: // MUTE/SOLO
                            (this.buttonData[idxPlaySelButton] as ModeChannelSelectButtonData).Activated = false;
                            break;
                        case 1: // SEL
                            (this.buttonData[idxPlayMuteSoloButton] as PropertySelectionButtonData).Activated = false;
                            break;
                        case 4: // VIEWS
                            this.CurrentLayer = ButtonLayer.viewSelector;
                            break;
                    }
                    this.EmitActionImageChanged();
                    break;
                case ButtonLayer.channelPropertiesRec:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 0: // ARM/MONITOR
                            break;
                        case 1: 
                            break;
                        case 4: // VIEWS
                            this.CurrentLayer = ButtonLayer.viewSelector;
                            break;
                    }
                    this.EmitActionImageChanged();
                    break;
                case ButtonLayer.faderModesShow:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 4: // VIEWS
                            this.CurrentLayer = ButtonLayer.viewSelector;
                            this.EmitActionImageChanged();
                            break;
                        default :
                            for (int i = 0; i <= 5; i++)
                            {
                                if (i != 4) (this.buttonData[$"{(int)ButtonLayer.faderModesShow}:{i}"] as CommandButtonData).Activated = false;
                            }
                            var cbd = this.buttonData[idx] as CommandButtonData;
                            cbd.Activated = !cbd.Activated;
                            this.EmitActionImageChanged();
                            break;
                    }
                    break;
                case ButtonLayer.faderModesSend:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 3: // USER 1 2 3
                            LastUserSendsMode = UserSendsMode.User;
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.User);
                            break;
                        case 4: // VIEWS (BACK)
                            this.CurrentLayer = ButtonLayer.viewSelector;
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Select);
                            this.EmitActionImageChanged();
                            break;
                        case 5: // SENDS
                            LastUserSendsMode = UserSendsMode.Sends;
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Send);
                            break;
                    }
                    break;
            }
        }

        private void setPropertySelectionMode(ChannelProperty.PropertyType channelProperty)
        {
            (this.buttonData[$"{(Int32)ButtonLayer.channelPropertiesPlay}:0"] as PropertySelectionButtonData).CurrentType = channelProperty;
        }

        private void addButton(ButtonLayer buttonLayer, int buttonIndex, ButtonData bd, bool isNoteReceiver = false)
        {
            var idx = $"{(int)buttonLayer}:{buttonIndex}";
            this.buttonData[idx] = bd;

            if (isNoteReceiver)
            {
                var cbd = bd as CommandButtonData;
                this.noteReceivers[cbd.Code] = idx;
            }
        }
    }

}


