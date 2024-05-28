namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;

    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Common;

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

        private enum PlayLayerMode
        {
            All,
            LayersActivated,
            AutomationActivated
        }
        private PlayLayerMode CurrentPlayLayerMode = PlayLayerMode.All;

        private enum RecLayerMode
        {
            All,
            PreModeActivated,
            PanelsActivated
        }
        private RecLayerMode CurrentRecLayerMode = RecLayerMode.All;

        private enum UserSendsLayerMode
        {
            All,
            PluginSelectionActivated
        }
        private UserSendsLayerMode CurrentUserSendsLayerMode = UserSendsLayerMode.All;

        private static readonly String idxUserButton = $"{(Int32)ButtonLayer.faderModesSend}:3";
        private static readonly String idxSendButton = $"{(Int32)ButtonLayer.faderModesSend}:5";
        private static readonly String idxPlayMuteSoloButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:0";
        private static readonly String idxPlaySelButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:1";
        private static readonly String idxPlayAutomationModeButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:2";
        private static readonly String idxRecArmMonitorButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:0";
        private static readonly String idxRecAutomationModeButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:2";
        private static readonly String idxRecPanelsButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:3";
        private static readonly String idxUserSendsPluginsButton = $"{(Int32)ButtonLayer.faderModesSend}:2";
        private static readonly String idxUserSendsU1Button = $"{(Int32)ButtonLayer.faderModesSend}:0";
        private static readonly String idxUserSendsU2Button = $"{(Int32)ButtonLayer.faderModesSend}:1";

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

            this.addButton(ButtonLayer.viewSelector, "0", new ModeButtonData("PLAY"));
            this.addButton(ButtonLayer.viewSelector, "1", new ModeButtonData("REC"));
            this.addButton(ButtonLayer.viewSelector, "2", new ModeButtonData("SHOW"));
            this.addButton(ButtonLayer.viewSelector, "3", new ModeButtonData("USER\rSENDS"));

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
            this.addButton(ButtonLayer.channelPropertiesPlay, "0", new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                                                                 ChannelProperty.PropertyType.Solo,
                                                                                                 "select-mute", "select-solo", "select-mute-solo",
                                                                                                 activated: false));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1", new ModeChannelSelectButtonData());
            this.addButton(ButtonLayer.channelPropertiesPlay, "2", new AutomationModeButtonData());
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-2", new AutomationModeCommandButtonData(AutomationMode.Off));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-2", new AutomationModeCommandButtonData(AutomationMode.Read));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-2", new AutomationModeCommandButtonData(AutomationMode.Touch));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-2", new AutomationModeCommandButtonData(AutomationMode.Latch));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-2", new AutomationModeCommandButtonData(AutomationMode.Write));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3", new ModeButtonData("LAYERS", "layers"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-1", new ModeButtonData("LAYERS", "layers_inv_80px"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-1", new OneWayCommandButtonData(0x30, "LAY UP", "layer_up_inv"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "2-1", new OneWayCommandButtonData(0x31, "LAY DN", "layer_down_inv"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-1", new OneWayCommandButtonData(0x34, "LAY EXP", "layers_expand_inv"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-1", new OneWayCommandButtonData(0x32, "LAY +", "layer_add_inv"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-1", new OneWayCommandButtonData(0x33, "LAY -", "layer_remove_inv"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5", new FlipPanVolCommandButtonData(0x32), true);

            this.addButton(ButtonLayer.channelPropertiesRec, "0", new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                                                                ChannelProperty.PropertyType.Monitor,
                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
                                                                                                activated: true));
            this.addButton(ButtonLayer.channelPropertiesRec, "1", new OneWayCommandButtonData(0x0B, "Time", "time_display"));
            this.addButton(ButtonLayer.channelPropertiesRec, "2", new RecPreModeButtonData());
            this.addButton(ButtonLayer.channelPropertiesRec, "1-1", new CommandButtonData(0x57, "Preroll", "preroll", AutomationModeCommandButtonData.BgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "3-1", new CommandButtonData(0x58, "Autopunch", "autopunch", AutomationModeCommandButtonData.BgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "5-1", new CommandButtonData(0x56, "Precount", "precount", AutomationModeCommandButtonData.BgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "3", new ModeButtonData("PANELS", "panels"));
            this.addButton(ButtonLayer.channelPropertiesRec, "0-2", new OneWayCommandButtonData(0x1D, "Toggle Height", "toggle_height", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "2-2", new OneWayCommandButtonData(0x1E, "Toggle Width", "toggle_width", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "1-2", new OneWayCommandButtonData(0x05, "Rec Panel", "rec_panel", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "4-2", new OneWayCommandButtonData(0x10, "Show Inputs", "show_inputs", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "5-2", new OneWayCommandButtonData(0x09, "Show Groups", "show_groups", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelPropertiesRec, "5", new CommandButtonData(0x59, "Click", "click"), isNoteReceiver: true);

            this.addButton(ButtonLayer.faderModesShow, "0", new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "1", new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "2", new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "3", new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.faderModesShow, "5", new CommandButtonData(0x33, "ALL", new BitmapColor(60, 60, 20), BitmapColor.White, true), true);

            this.addButton(ButtonLayer.faderModesSend, "0", new ModeTopUserButtonData(0x6C, "", ModeTopCommandButtonData.Location.Left), isNoteReceiver: true);
            this.addButton(ButtonLayer.faderModesSend, "1", new ModeTopUserButtonData(0x6D, "", ModeTopCommandButtonData.Location.Right), isNoteReceiver: true);
            this.addButton(ButtonLayer.faderModesSend, "2", new ModeButtonData("Plugins", "plugins"));
            this.addButton(ButtonLayer.faderModesSend, "0-1", new ModeTopCommandButtonData(0x51, "Previous\rPlugin", ModeTopCommandButtonData.Location.Left, "plugin_prev", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.faderModesSend, "1-1", new ModeTopCommandButtonData(0x52, "Next\rPlugin", ModeTopCommandButtonData.Location.Right, "plugin_next", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.faderModesSend, "3-1", new CommandButtonData(0x50, "Channel Editor", "channel_editor", AutomationModeCommandButtonData.BgColor));
            this.addButton(ButtonLayer.faderModesSend, "3", new UserModeButtonData());
            this.addButton(ButtonLayer.faderModesSend, "4", new CommandButtonData(0x2A, "VIEWS"));
            this.addButton(ButtonLayer.faderModesSend, "5", new SendsCommandButtonData(0x29), isNoteReceiver: true);
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
                var pluginName = getPluginName(e);

                for (int i = 0; i < 2; i++)
                {
                    var bd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:{i}"] as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                    bd = this.buttonData[$"{(int)ButtonLayer.faderModesSend}:{i}-1"] as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                }
                this.EmitActionImageChanged();
            };

            this.plugin.AutomationModeChanged += (object sender, AutomationMode e) =>
            {
                var ambd = this.buttonData[idxPlayAutomationModeButton] as AutomationModeButtonData;
                ambd.CurrentMode = e;
                this.EmitActionImageChanged();
            };
            this.plugin.FunctionKeyChanged += (object sender, FunctionKeyParams fke) =>
            {
                if (fke.KeyID == 12 || fke.KeyID == 13)
                {
                    (this.buttonData[$"{(Int32)ButtonLayer.faderModesSend}:{fke.KeyID - 12}"] as CommandButtonData).Name = fke.FunctionName;
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
                // Any other command button
                var cbd = this.buttonData[this.noteReceivers[e.NoteNumber]] as CommandButtonData;
                cbd.Activated = e.Velocity > 0;
            }
            this.EmitActionImageChanged();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            if (this.buttonData.TryGetValue(this.getButtonIndex(actionParameter), out var bd))
            {
                return bd.getImage(imageSize);
            }

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            return bb.ToImage();
        }

        protected override void RunCommand(string actionParameter)
        {
            var idx = this.getButtonIndex(actionParameter);
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
                    if (this.CurrentPlayLayerMode == PlayLayerMode.AutomationActivated)
                    {
                        (this.buttonData[idxPlayAutomationModeButton] as AutomationModeButtonData).SelectionModeActivated = false;
                        this.CurrentPlayLayerMode = PlayLayerMode.All;
                    }
                    else
                    {
                        switch (Int32.Parse(actionParameter))
                        {
                            case 0: // MUTE/SOLO
                                if (this.CurrentPlayLayerMode == PlayLayerMode.All)
                                {
                                    (this.buttonData[idxPlaySelButton] as ModeChannelSelectButtonData).Activated = false;
                                }
                                break;
                            case 1: // SEL
                                if (this.CurrentPlayLayerMode == PlayLayerMode.All)
                                {
                                    (this.buttonData[idxPlayMuteSoloButton] as PropertySelectionButtonData).Activated = false;
                                }
                                break;
                            case 2: // AUTO
                                if (this.CurrentPlayLayerMode == PlayLayerMode.All)
                                {
                                    (bd as AutomationModeButtonData).SelectionModeActivated = true;
                                    this.CurrentPlayLayerMode = PlayLayerMode.AutomationActivated;
                                }
                                break;
                            case 3: // LAYERS
                                this.CurrentPlayLayerMode = this.CurrentPlayLayerMode == PlayLayerMode.All ? PlayLayerMode.LayersActivated
                                                                                                           : PlayLayerMode.All;
                                break;
                            case 4: // VIEWS
                                if (this.CurrentPlayLayerMode == PlayLayerMode.All)
                                {
                                    this.CurrentLayer = ButtonLayer.viewSelector;
                                }
                                break;
                        }
                    }
                    this.EmitActionImageChanged();
                    break;
                case ButtonLayer.channelPropertiesRec:
                    if (this.CurrentRecLayerMode == RecLayerMode.PreModeActivated)
                    {
                        (this.buttonData[idxRecAutomationModeButton] as RecPreModeButtonData).SelectionModeActivated = false;
                        this.CurrentRecLayerMode = RecLayerMode.All;
                    }
                    else
                    {
                        switch (Int32.Parse(actionParameter))
                        {
                            case 2: // PRE MODE
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    (this.buttonData[idxRecAutomationModeButton] as RecPreModeButtonData).SelectionModeActivated = true;
                                    this.CurrentRecLayerMode = RecLayerMode.PreModeActivated;
                                }
                                break;
                            case 3: // PANELS
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.PanelsActivated;
                                    (this.buttonData[idxRecPanelsButton] as ModeButtonData).Activated = true;
                                }
                                else if (this.CurrentRecLayerMode == RecLayerMode.PanelsActivated)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.All;
                                    (this.buttonData[idxRecPanelsButton] as ModeButtonData).Activated = false;
                                }
                                break;
                            case 4: // VIEWS
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentLayer = ButtonLayer.viewSelector;
                                }
                                break;
                        }
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
                        case 2: // PLUGINS
                            this.CurrentUserSendsLayerMode = this.CurrentUserSendsLayerMode == UserSendsLayerMode.All ? UserSendsLayerMode.PluginSelectionActivated
                                                                                                                      : UserSendsLayerMode.All;
                            (this.buttonData[idxUserSendsPluginsButton] as ModeButtonData).Activated = this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated;
                            this.EmitActionImageChanged();
                            break;
                        case 3: // USER 1 2 3
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                LastUserSendsMode = UserSendsMode.User;
                                this.plugin.EmitSelectModeChanged(SelectButtonMode.User);
                            }
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

//        private void setPropertySelectionMode(ChannelProperty.PropertyType channelProperty)
//        {
//            (this.buttonData[$"{(Int32)ButtonLayer.channelPropertiesPlay}:0"] as PropertySelectionButtonData).CurrentType = channelProperty;
//        }

        private String getButtonIndex(String actionParameter)
        {
            var idx = $"{(Int32)this.CurrentLayer}:{actionParameter}";
            if (this.CurrentLayer == ButtonLayer.channelPropertiesPlay)
            {
                if (this.CurrentPlayLayerMode == PlayLayerMode.LayersActivated && "012345".Contains(actionParameter))
                {
                    idx += "-1";
                }
                else if (this.CurrentPlayLayerMode == PlayLayerMode.AutomationActivated && "01345".Contains(actionParameter))
                {
                    idx += "-2";
                }
            }
            else if (this.CurrentLayer == ButtonLayer.channelPropertiesRec)
            {
                if (this.CurrentRecLayerMode == RecLayerMode.PreModeActivated && "135".Contains(actionParameter))
                {
                    idx += "-1";
                }
                else if (this.CurrentRecLayerMode == RecLayerMode.PanelsActivated && "01245".Contains(actionParameter))
                {
                    idx += "-2";
                }
            }
            else if (this.CurrentLayer == ButtonLayer.faderModesSend)
            {
                if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated && "013".Contains(actionParameter))
                {
                    idx += "-1";
                }
            }
            return idx;
        }

        private void addButton(ButtonLayer buttonLayer, String buttonIndex, ButtonData bd, bool isNoteReceiver = false)
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


