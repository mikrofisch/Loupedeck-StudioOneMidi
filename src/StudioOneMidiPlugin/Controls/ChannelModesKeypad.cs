namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;

    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Common;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;
    using System.Drawing;

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
            Sends = 0,
            User = 1
        }
        private static UserSendsMode LastUserSendsMode = UserSendsMode.Sends;

        private enum PlayLayerMode
        {
            PropertySelect = 0,
            ChannelSelect = 1,
            AutomationActivated = 2,
            LayersActivated = 3,
            ArrangerActivated = 4,
            ConsoleActivated = 5,
            AddActivated = 6
        }
        private PlayLayerMode CurrentPlayLayerMode = PlayLayerMode.PropertySelect;

        private enum RecLayerMode
        {
            All = 0,
            PreModeActivated = 1,
            PanelsActivated = 2
        }
        private RecLayerMode CurrentRecLayerMode = RecLayerMode.All;

        private enum UserSendsLayerMode
        {
            Sends = 0,
            User = 1,
            PluginSelectionActivated = 2
        }
        private UserSendsLayerMode CurrentUserSendsLayerMode = UserSendsLayerMode.Sends;

        private static readonly String idxUserButton = $"{(Int32)ButtonLayer.faderModesSend}:3";
        private static readonly String idxSendButton = $"{(Int32)ButtonLayer.faderModesSend}:5";
        private static readonly String idxPlayMuteSoloSelectButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:0";
        private static readonly String idxPlayMuteSoloButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:0-1";
        private static readonly String idxPlaySelButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:1";
        private static readonly String idxPlayAutomationModeButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:2-1";
        private static readonly String idxPlayLayersCollapseButton = $"{(Int32)ButtonLayer.channelPropertiesPlay}:1-3";
        private static readonly String idxRecArmMonitorButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:0";
        private static readonly String idxRecAutomationModeButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:2";
        private static readonly String idxRecPanelsButton = $"{(Int32)ButtonLayer.channelPropertiesRec}:3";
        private static readonly String idxUserSendsPluginsButton = $"{(Int32)ButtonLayer.faderModesSend}:2-1";
        private static readonly String idxUserSendsUserModeButton = $"{(Int32)ButtonLayer.faderModesSend}:3";
        private static readonly String idxUserSendsViewsButton = $"{(Int32)ButtonLayer.faderModesSend}:4";
        private static readonly String idxUserSendsU1Button = $"{(Int32)ButtonLayer.faderModesSend}:0-1";
        private static readonly String idxUserSendsU2Button = $"{(Int32)ButtonLayer.faderModesSend}:1-1";

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
                                                                                                 activated: true));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-1", new PropertyButtonData(PropertyButtonData.SelectedChannel,
                                                                                            ChannelProperty.PropertyType.Mute,
                                                                                            PropertyButtonData.TrackNameMode.ShowFull));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1", new ModeChannelSelectButtonData(activated: false));
            var arrangerBgColor = new BitmapColor(60, 60, 60);
            this.addButton(ButtonLayer.channelPropertiesPlay, "2", new ModeButtonData("ARRANGER", "arranger", new BitmapColor(arrangerBgColor, 190)));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-4", new OneWayCommandButtonData(14, 0x06, "Track List", "track_list", arrangerBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-4", new OneWayCommandButtonData(14, 0x04, "Inspector", "inspector", arrangerBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-4", new OneWayCommandButtonData(14, 0x38, "Show Automation", "show_automation", arrangerBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "2-1", new AutomationModeButtonData());
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-2", new AutomationModeCommandButtonData(AutomationMode.Off, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-2", new AutomationModeCommandButtonData(AutomationMode.Read, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-2", new AutomationModeCommandButtonData(AutomationMode.Touch, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-2", new AutomationModeCommandButtonData(AutomationMode.Latch, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-2", new AutomationModeCommandButtonData(AutomationMode.Write, ButtonData.DefaultSelectionBgColor));
            var consoleBgColor = new BitmapColor(60, 60, 60);
            this.addButton(ButtonLayer.channelPropertiesPlay, "3", new ModeButtonData("PANELS", "panels", new BitmapColor(consoleBgColor, 190)));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-5", new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", consoleBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "2-5", new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", consoleBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-5", new OneWayCommandButtonData(14, 0x00, "Mix", null, consoleBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-5", new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", consoleBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-5", new OneWayCommandButtonData(14, 0x1F, "Show Outputs", "show_outputs", consoleBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-1", new ModeButtonData("LAYERS", "layers"));
            var layersBgColor = new BitmapColor(180, 180, 180);
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-3", new ModeButtonData("LAYERS", "layers_52px", new BitmapColor(layersBgColor, 128)));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-3", new OneWayCommandButtonData(14, 0x30, "LAY UP", "layer_up_inv", layersBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "2-3", new OneWayCommandButtonData(14, 0x31, "LAY DN", "layer_dn_inv", layersBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-3", new OneWayCommandButtonData(14, 0x34, "LAY EXP", "layers_expand_inv", layersBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-3", new OneWayCommandButtonData(14, 0x32, "LAY +", "layer_add_inv", layersBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-3", new OneWayCommandButtonData(14, 0x33, "LAY -", "layer_remove_inv", layersBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelPropertiesPlay, "5", new FlipPanVolCommandButtonData(0x35), true);
            var addBgColor = new BitmapColor(60, 60, 60);
            this.addButton(ButtonLayer.channelPropertiesPlay, "5-1", new ModeButtonData("ADD", "button_add", new BitmapColor(addBgColor, 190)));
            this.addButton(ButtonLayer.channelPropertiesPlay, "0-6", new OneWayCommandButtonData(14, 0x15, "Add Insert", "add_insert", addBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "1-6", new OneWayCommandButtonData(14, 0x16, "Add Send", "add_send", addBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "2-6", new OneWayCommandButtonData(14, 0x18, "Add FX Channel", "add_fx", addBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "3-6", new OneWayCommandButtonData(14, 0x17, "Add Bus Channel", "add_bus", addBgColor));
            this.addButton(ButtonLayer.channelPropertiesPlay, "4-6", new OneWayCommandButtonData(14, 0x3C, "Add Track", null, addBgColor));

            this.addButton(ButtonLayer.channelPropertiesRec, "0", new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                                                                ChannelProperty.PropertyType.Monitor,
                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
                                                                                                activated: true));
            this.addButton(ButtonLayer.channelPropertiesRec, "1", new OneWayCommandButtonData(14, 0x0B, "Time", "time_display"));
            this.addButton(ButtonLayer.channelPropertiesRec, "2", new RecPreModeButtonData());
            this.addButton(ButtonLayer.channelPropertiesRec, "1-1", new CommandButtonData(0x57, "Preroll", "preroll", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "3-1", new CommandButtonData(0x58, "Autopunch", "autopunch", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "5-1", new CommandButtonData(0x56, "Precount", "precount", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.channelPropertiesRec, "3", new ModeButtonData("PANELS", "panels"));
            this.addButton(ButtonLayer.channelPropertiesRec, "0-2", new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "2-2", new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "1-2", new OneWayCommandButtonData(14, 0x05, "Rec Panel", "rec_panel", ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "4-2", new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "5-2", new OneWayCommandButtonData(14, 0x09, "Show Groups", "show_groups", ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.channelPropertiesRec, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.channelPropertiesRec, "5", new CommandButtonData(0x59, "Click", "click"), isNoteReceiver: true);

            this.addButton(ButtonLayer.faderModesShow, "0", new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "1", new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "2", new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "3", new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.faderModesShow, "4", new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.faderModesShow, "5", new CommandButtonData(0x36, "ALL", new BitmapColor(60, 60, 20), BitmapColor.White, true), true);

            this.addButton(ButtonLayer.faderModesSend, "0", new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height"));
            this.addButton(ButtonLayer.faderModesSend, "2", new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width"));
            this.addButton(ButtonLayer.faderModesSend, "1", new OneWayCommandButtonData(14, 0x00, "Mix"));
            var pluginBgColor = new BitmapColor(60, 60, 60);
            this.addButton(ButtonLayer.faderModesSend, "0-1", new ModeTopUserButtonData(0, 0x76, "", ModeTopCommandButtonData.Location.Left), isNoteReceiver: true);
            this.addButton(ButtonLayer.faderModesSend, "1-1", new ModeTopUserButtonData(0, 0x77, "", ModeTopCommandButtonData.Location.Right), isNoteReceiver: true);
            this.addButton(ButtonLayer.faderModesSend, "2-1", new ModeButtonData("Plugins", "plugins", new BitmapColor(pluginBgColor, 190)));
            this.addButton(ButtonLayer.faderModesSend, "0-2", new ModeTopCommandButtonData(14, 0x74, "Previous Plugin", ModeTopCommandButtonData.Location.Left, "plugin_prev", pluginBgColor));
            this.addButton(ButtonLayer.faderModesSend, "1-2", new ModeTopCommandButtonData(14, 0x75, "Next Plugin", ModeTopCommandButtonData.Location.Right, "plugin_next", pluginBgColor));
            this.addButton(ButtonLayer.faderModesSend, "3-2", new OneWayCommandButtonData(14, 0x12, "Channel Editor", "channel_editor", pluginBgColor));
            this.addButton(ButtonLayer.faderModesSend, "3", new UserModeButtonData());
            this.addButton(ButtonLayer.faderModesSend, "4", new PanCommandButtonData("VIEWS"));
            this.addButton(ButtonLayer.faderModesSend, "5", new SendsCommandButtonData(), isNoteReceiver: true);
        }

        // common
        protected override bool OnLoad()
        {
            base.OnLoad();

            this.plugin.CommandNoteReceived += this.OnNoteReceived;

            this.plugin.ActiveUserPagesReceived += (Object sender, Int32 e) =>
            {
                (this.buttonData[idxUserSendsUserModeButton] as UserModeButtonData).ActiveUserPages = e;
                this.EmitActionImageChanged();
            };

            this.plugin.ChannelDataChanged += (Object sender, EventArgs e) =>
            {
                this.EmitActionImageChanged();
            };

            this.plugin.SelectButtonPressed += (Object sender, EventArgs e) =>
            {
                this.CurrentLayer = ButtonLayer.channelPropertiesPlay;
                this.EmitActionImageChanged();
            };

            this.plugin.FocusDeviceChanged += (Object sender, string e) =>
            {
                var pluginName = getPluginName(e);

                for (var i = 0; i < 2; i++)
                {
                    var bd = this.buttonData[$"{(Int32)ButtonLayer.faderModesSend}:{i}-1"] as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                    bd = this.buttonData[$"{(Int32)ButtonLayer.faderModesSend}:{i}-2"] as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                }
                var ubd = this.buttonData[idxUserSendsUserModeButton] as UserModeButtonData;
                ubd.resetUserPage(); 
                this.EmitActionImageChanged();
            };

            this.plugin.AutomationModeChanged += (Object sender, AutomationMode e) =>
            {
                var ambd = this.buttonData[idxPlayAutomationModeButton] as AutomationModeButtonData;
                ambd.CurrentMode = e;
                this.EmitActionImageChanged();
            };
            this.plugin.FunctionKeyChanged += (Object sender, FunctionKeyParams fke) =>
            {
                if (fke.KeyID == 12 || fke.KeyID == 13)
                {
                    (this.buttonData[$"{(Int32)ButtonLayer.faderModesSend}:{fke.KeyID - 12}-1"] as CommandButtonData).Name = fke.FunctionName;
                }
                this.EmitActionImageChanged();
            };
            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                (this.buttonData[idxPlayMuteSoloButton] as PropertyButtonData).setPropertyType(e);
                // this.EmitActionImageChanged();
            };

            return true;
        }

        protected void OnNoteReceived(object sender, NoteOnEvent e)
        {
            if (e.NoteNumber >= UserModeButtonData.BaseNote && e.NoteNumber < UserModeButtonData.BaseNote + UserModeButtonData.MaxUserPages)
            {
                // User mode changed
                var umbd = this.buttonData[$"{(Int32)ButtonLayer.faderModesSend}:3"] as UserModeButtonData;
                umbd.setUserPage(e.NoteNumber, e.Velocity > 0);
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
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            selectMode = (this.buttonData[idxPlaySelButton] as ModeChannelSelectButtonData).Activated ? SelectButtonMode.Select
                                                                                                                      : SelectButtonMode.Property;
                            this.plugin.EmitSelectModeChanged(selectMode);
                            this.plugin.EmitPropertySelectionChanged((this.buttonData[idxPlayMuteSoloSelectButton] as PropertySelectionButtonData).CurrentType);
                            break;
                        case 1: // REC
                            this.CurrentLayer = ButtonLayer.channelPropertiesRec;
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                            this.plugin.EmitPropertySelectionChanged((this.buttonData[idxRecArmMonitorButton] as PropertySelectionButtonData).CurrentType);
                            break;
                        case 2: // SHOW
                            this.CurrentLayer = ButtonLayer.faderModesShow;
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            this.plugin.EmitPropertySelectionChanged(ChannelProperty.PropertyType.Select);
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
                        this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                    }
                    else
                    {
                        switch (Int32.Parse(actionParameter))
                        {
                            case 0: // MUTE/SOLO, PROPERTY
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                }
                                break;
                            case 1: // SEL
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                    (this.buttonData[idxPlayMuteSoloSelectButton] as PropertySelectionButtonData).Activated = false;
                                    this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                                    this.plugin.EmitSelectModeChanged(SelectButtonMode.Select);
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    (this.buttonData[idxPlayMuteSoloSelectButton] as PropertySelectionButtonData).Activated = true;
                                    (this.buttonData[idxPlaySelButton] as ModeChannelSelectButtonData).Activated = false;
                                    this.CurrentPlayLayerMode = PlayLayerMode.PropertySelect;
                                    this.plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                                }
                                break;
                            case 2: // ARRANGER / AUTO
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.ArrangerActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    (bd as AutomationModeButtonData).SelectionModeActivated = true;
                                    this.CurrentPlayLayerMode = PlayLayerMode.AutomationActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ArrangerActivated)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.PropertySelect;
                                }
                                break;
                            case 3: // CONSOLE / LAYERS
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.ConsoleActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.LayersActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ConsoleActivated)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.PropertySelect;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.LayersActivated)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                                }
                                break;
                            case 4: // VIEWS
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect || this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    this.CurrentLayer = ButtonLayer.viewSelector;
                                }
                                break;
                            case 5: // VOL/PAN / ADD
                                if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.AddActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.AddActivated)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
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
                            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.User)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.PluginSelectionActivated;
                            }
                            else if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                            }
                            (this.buttonData[idxUserSendsPluginsButton] as ModeButtonData).Activated = this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated;
                            this.EmitActionImageChanged();
                            break;
                        case 3: // USER 1 2 3...
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
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
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.Sends;
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

        protected override Boolean ProcessTouchEvent(String actionParameter, DeviceTouchEvent touchEvent)
        {
            if (touchEvent.EventType.IsLongPress())
            {
                if (this.CurrentLayer == ButtonLayer.channelPropertiesPlay &&
                    this.CurrentPlayLayerMode == PlayLayerMode.LayersActivated
                    && actionParameter == "1")
                {
                    this.plugin.SendMidiNote(14, 0x3B);
                    return true;
                }
            }

            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }


        private String getButtonIndex(String actionParameter)
        {
            var idx = $"{(Int32)this.CurrentLayer}:{actionParameter}";
            switch (this.CurrentLayer)
            {
                case ButtonLayer.channelPropertiesPlay:
                    switch (this.CurrentPlayLayerMode)
                    {
                        case PlayLayerMode.ChannelSelect:
                            if ("0235".Contains(actionParameter)) idx += "-1";
                            break;
                        case PlayLayerMode.AutomationActivated:
                            if ("01345".Contains(actionParameter))
                                idx += "-2";
                            break;
                        case PlayLayerMode.LayersActivated:
                            if ("012345".Contains(actionParameter)) idx += "-3";
                            break;
                        case PlayLayerMode.ArrangerActivated:
                            if ("013".Contains(actionParameter))
                                idx += "-4";
                            break;
                        case PlayLayerMode.ConsoleActivated:
                            if ("01245".Contains(actionParameter))
                                idx += "-5";
                            break;
                        case PlayLayerMode.AddActivated:
                            if ("01234".Contains(actionParameter)) idx += "-6";
                            if ("5".Contains(actionParameter)) idx += "-1";
                            break;
                    }
                    break;
                case ButtonLayer.channelPropertiesRec:
                    switch (this.CurrentRecLayerMode)
                    {
                        case RecLayerMode.PreModeActivated:
                            if ("135".Contains(actionParameter)) idx += "-1";
                            break;
                        case RecLayerMode.PanelsActivated:
                            if ("01245".Contains(actionParameter)) idx += "-2";
                            break;
                    }
                    break;
                case ButtonLayer.faderModesSend:
                    switch (this.CurrentUserSendsLayerMode)
                    {
                        case UserSendsLayerMode.User:
                            if ("012".Contains(actionParameter)) idx += "-1";
                            break;
                        case UserSendsLayerMode.PluginSelectionActivated:
                            if ("013".Contains(actionParameter)) idx += "-2";
                            if ("2".Contains(actionParameter)) idx += "-1";
                            break;
                    }
                    break;
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


