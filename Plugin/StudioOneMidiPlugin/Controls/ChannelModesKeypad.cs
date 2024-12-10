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
        private static readonly BitmapColor ClickBgColor = new BitmapColor(50, 114, 134);

        public class ModeData
        {
            public ButtonData[] ButtonDataList = new ButtonData[6];
        }
        private enum ButtonLayer
        {
            ViewSelector,
            ChannelPropertiesPlay,
            ChannelPropertiesRec,
            FaderModesShow,
            FaderModesSend
        }
        ButtonLayer CurrentLayer = ButtonLayer.ChannelPropertiesPlay;
        private ButtonLayer LastButtonLayer1 = ButtonLayer.ViewSelector;
        private ButtonLayer LastButtonLayer2 = ButtonLayer.ViewSelector;

        private enum UserSendsMode
        {
            Sends = 0,
            User = 1
        }
        private static UserSendsMode LastUserSendsMode = UserSendsMode.User;

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
        private PlayLayerMode CurrentPlayLayerMode = PlayLayerMode.PropertySelect;  // Needs to match defaults in SelectButtonData class

        private enum RecLayerMode
        {
            All = 0,
            PreModeActivated = 1,
            PanelsActivated = 2,
            ClickActivated = 3
        }
        private RecLayerMode CurrentRecLayerMode = RecLayerMode.All;

        private enum UserSendsLayerMode
        {
            Sends = 0,
            User = 1,
            PluginSelectionActivated = 2,
            UserMenuActivated = 3,          // User defined selection menu for plugin control
            UserPageMenuActivated = 4       // User page switching via menu
        }
        private UserSendsLayerMode CurrentUserSendsLayerMode = UserSendsLayerMode.User;
        private Boolean DeactivateUserMenu = false;

        private class LayerData
        {
            public Dictionary<Int32, ModeData> ModeDataDict = new Dictionary<Int32, ModeData>();

            public void AddMode(Int32 modeID) => this.ModeDataDict.Add(modeID, new ModeData());
            public void AddModeButtonData(Int32 modeID, Int32 buttonIndex, ButtonData bd) => this.ModeDataDict[modeID].ButtonDataList[buttonIndex] = bd;
        }

        private readonly Dictionary<ButtonLayer, LayerData> ButtonLayerDict = new Dictionary<ButtonLayer, LayerData>();

        private class ButtonDataIndex
        {
            public ButtonDataIndex(ButtonLayer bl, Int32 modeID, Int32 buttonIndex)
            {
                this.ButtonLayerID = bl;
                this.ModeID = modeID;
                this.ButtonIndex = buttonIndex;
            }
            public ButtonLayer ButtonLayerID { get; private set; }
            public Int32 ModeID { get; private set; }
            public Int32 ButtonIndex { get; private set; }
        }


        private static readonly ButtonDataIndex idxUserButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 0, 3);
        private static readonly ButtonDataIndex idxSendButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 0, 5);
        private static readonly ButtonDataIndex idxPlayMuteSoloSelectButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, 0, 0);
        private static readonly ButtonDataIndex idxPlayMuteSoloButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, 1, 0);
        private static readonly ButtonDataIndex idxPlaySelButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, 0, 1);
        private static readonly ButtonDataIndex idxPlayAutomationModeButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, 1 , 2);
        private static readonly ButtonDataIndex idxRecArmMonitorButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 0);
        private static readonly ButtonDataIndex idxRecAutomationModeButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 2);
        private static readonly ButtonDataIndex idxRecPanelsButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 3);
        private static readonly ButtonDataIndex idxRecClickButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 5);
        private static readonly ButtonDataIndex idxUserSendsPluginsButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 1, 2);
        private static readonly ButtonDataIndex idxUserSendsUserModeButton = new ButtonDataIndex(ButtonLayer.FaderModesSend,0, 3);

        private class NoteReceiverEntry
        {
            public Int32 Note;
            public ButtonDataIndex Index;
        }
        private readonly List<NoteReceiverEntry> NoteReceivers = new List<NoteReceiverEntry>();

        public ChannelModesKeypad() : base()
        {
            this.DisplayName = "Channel Modes Button";
            this.Description = "Special button for controlling Loupedeck fader modes";

            // Create UI buttons
            this.AddParameter("0", "Mode Group Button 1-1", "Channel Modes");
            this.AddParameter("1", "Mode Group Button 2-1", "Channel Modes");
            this.AddParameter("2", "Mode Group Button 1-2", "Channel Modes");
            this.AddParameter("3", "Mode Group Button 2-2", "Channel Modes");
            this.AddParameter("4", "Mode Group Button 1-3", "Channel Modes");
            this.AddParameter("5", "Mode Group Button 2-3", "Channel Modes");

            for (var i = 0; i < 6; i++)
            {
                this.buttonData.Add($"{i}", null);
            }

            this.ButtonLayerDict.Add(ButtonLayer.ViewSelector, new LayerData());

            this.ButtonLayerDict[ButtonLayer.ViewSelector].AddMode(0);
            this.addButton(ButtonLayer.ViewSelector, 0, 0, new ModeButtonData("PLAY"));
            this.addButton(ButtonLayer.ViewSelector, 0, 1, new ModeButtonData("REC"));
            this.addButton(ButtonLayer.ViewSelector, 0, 2, new ModeButtonData("SHOW"));
            this.addButton(ButtonLayer.ViewSelector, 0, 3, new ModeButtonData("USER\rSENDS"));
            this.addButton(ButtonLayer.ViewSelector, 0, 4, new ModeButtonData("LAST", "view_last"));

            Int32 modeID;

            var arrangerBgColor = new BitmapColor(60, 60, 60);
            var consoleBgColor = new BitmapColor(60, 60, 60);
            var layersBgColor = new BitmapColor(180, 180, 180);
            var addBgColor = new BitmapColor(60, 60, 60);

            this.ButtonLayerDict.Add(ButtonLayer.ChannelPropertiesPlay, new LayerData());

            // Mute/Solo
            modeID = (Int32)PlayLayerMode.PropertySelect;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                                                                    ChannelProperty.PropertyType.Solo,
                                                                                                    "select-mute", "select-solo", "select-mute-solo",
                                                                                                    activated: this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new ModeChannelSelectButtonData(activated: this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new ModeButtonData("ARRANGER", "arranger", new BitmapColor(arrangerBgColor, 190), isMenu: true));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new ModeButtonData("CONSOLE", "panels", new BitmapColor(consoleBgColor, 190), isMenu: true));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new FlipPanVolCommandButtonData(0x35), true);

            // Select
            modeID = (Int32)PlayLayerMode.ChannelSelect;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new PropertyButtonData(PropertyButtonData.SelectedChannel,
                                                                                           ChannelProperty.PropertyType.Mute,
                                                                                           PropertyButtonData.TrackNameMode.ShowFull));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new AutomationModeButtonData());
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new ModeButtonData("LAYERS", "layers", new BitmapColor(layersBgColor, 128), isMenu: true));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new ModeButtonData("ADD", "button_add", new BitmapColor(addBgColor, 190), isMenu: true));

            // Automation
            modeID = (Int32)PlayLayerMode.AutomationActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new AutomationModeCommandButtonData(AutomationMode.Off, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new AutomationModeCommandButtonData(AutomationMode.Read, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, this.getButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 2));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new AutomationModeCommandButtonData(AutomationMode.Touch, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new AutomationModeCommandButtonData(AutomationMode.Write, ButtonData.DefaultSelectionBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new AutomationModeCommandButtonData(AutomationMode.Latch, ButtonData.DefaultSelectionBgColor));

            // Layers
            modeID = (Int32)PlayLayerMode.LayersActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x30, "LAY UP", "layer_up_inv", layersBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x34, "LAY EXP", "layers_expand_inv", layersBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x31, "LAY DN", "layer_dn_inv", layersBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, this.getButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 3));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x32, "LAY +", "layer_add_inv", layersBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new OneWayCommandButtonData(14, 0x33, "LAY -", "layer_remove_inv", layersBgColor));

            // Arranger
            modeID = (Int32)PlayLayerMode.ArrangerActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x06, "Track List", "track_list", arrangerBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x04, "Inspector", "inspector", arrangerBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new OneWayCommandButtonData(14, 0x38, "Show Automation", "show_automation", arrangerBgColor));

            // Console
            modeID = (Int32)PlayLayerMode.ConsoleActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", consoleBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x00, "Mix", null, consoleBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", consoleBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", consoleBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new OneWayCommandButtonData(14, 0x1F, "Show Outputs", "show_outputs", consoleBgColor));

            // Add Tracks
            modeID = (Int32)PlayLayerMode.AddActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x15, "Add Insert", "add_insert", addBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x16, "Add Send", "add_send", addBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x18, "Add FX Channel", "add_fx", addBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new OneWayCommandButtonData(14, 0x17, "Add Bus Channel", "add_bus", addBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x3C, "Add Track", null, addBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, this.getButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 5));

            this.ButtonLayerDict.Add(ButtonLayer.ChannelPropertiesRec, new LayerData());

            var panelsBgColor = new BitmapColor(60, 60, 60);

            // Rec Layer
            modeID = (Int32)RecLayerMode.All;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                                                                ChannelProperty.PropertyType.Monitor,
                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
                                                                                                activated: true));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new OneWayCommandButtonData(14, 0x0B, "Time", "time_display"));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 2, new RecPreModeButtonData());
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new ModeButtonData("PANELS", "panels", new BitmapColor(panelsBgColor, 190), isMenu: true));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new MenuCommandButtonData(0x3A, 0x59, "CLICK", "click", new BitmapColor(ClickBgColor, 100)), isNoteReceiver: true);

            // Punch
            modeID = (Int32)RecLayerMode.PreModeActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(1);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new CommandButtonData(0x57, "Preroll", "preroll", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new CommandButtonData(0x58, "Autopunch", "autopunch", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new CommandButtonData(0x56, "Precount", "precount", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);

            // Mixer Panels
            modeID = (Int32)RecLayerMode.PanelsActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", panelsBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new OneWayCommandButtonData(14, 0x05, "Rec Panel", "rec_panel", panelsBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", panelsBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 4, new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", panelsBgColor));
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new OneWayCommandButtonData(14, 0x09, "Show Groups", "show_groups", panelsBgColor));

            // Click
            modeID = (Int32)RecLayerMode.ClickActivated;
            this.ButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.addButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new OneWayCommandButtonData(14, 0x4F, "Click Settings", "click_settings", ClickBgColor));


            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "0", new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
            //                                                                                                ChannelProperty.PropertyType.Monitor,
            //                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
            //                                                                                                activated: true));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "1", new OneWayCommandButtonData(14, 0x0B, "Time", "time_display"));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "2", new RecPreModeButtonData());
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "1-1", new CommandButtonData(0x57, "Preroll", "preroll", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "3-1", new CommandButtonData(0x58, "Autopunch", "autopunch", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "5-1", new CommandButtonData(0x56, "Precount", "precount", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "3", new ModeButtonData("PANELS", "panels", new BitmapColor(panelsBgColor, 190), isMenu: true));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "0-2", new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", panelsBgColor));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "2-2", new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", panelsBgColor));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "1-2", new OneWayCommandButtonData(14, 0x05, "Rec Panel", "rec_panel", panelsBgColor));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "4-2", new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", panelsBgColor));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "5-2", new OneWayCommandButtonData(14, 0x09, "Show Groups", "show_groups", panelsBgColor));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "4", new ModeButtonData("VIEWS"));
            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "5", new MenuCommandButtonData(0x3A, 0x59, "CLICK", "click", new BitmapColor(ClickBgColor, 100)), isNoteReceiver: true);

            //            this.addButton(ButtonLayer.ChannelPropertiesRec, "3-3", new OneWayCommandButtonData(14, 0x4F, "Click Settings", "click_settings", ClickBgColor));

            this.ButtonLayerDict.Add(ButtonLayer.FaderModesShow, new LayerData());

            // Fader Visibility
            this.ButtonLayerDict[ButtonLayer.FaderModesShow].AddMode(0);
            this.addButton(ButtonLayer.FaderModesShow, 0, 0, new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.FaderModesShow, 0, 1, new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.FaderModesShow, 0, 2, new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.FaderModesShow, 0, 3, new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.addButton(ButtonLayer.FaderModesShow, 0, 4, new ModeButtonData("VIEWS"));
            this.addButton(ButtonLayer.FaderModesShow, 0, 5, new ViewAllRemoteCommandButtonData(), isNoteReceiver: false);


            //            this.addButton(ButtonLayer.FaderModesShow, "0", new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            //            this.addButton(ButtonLayer.FaderModesShow, "1", new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            //            this.addButton(ButtonLayer.FaderModesShow, "2", new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            //            this.addButton(ButtonLayer.FaderModesShow, "3", new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            //            this.addButton(ButtonLayer.FaderModesShow, "4", new ModeButtonData("VIEWS"));
            //            this.addButton(ButtonLayer.FaderModesShow, "5", new ViewAllRemoteCommandButtonData(), isNoteReceiver: false);


            this.ButtonLayerDict.Add(ButtonLayer.FaderModesSend, new LayerData());

            var pluginBgColor = new BitmapColor(60, 60, 60);

            // Sends
            modeID = (Int32)UserSendsLayerMode.Sends;
            this.ButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height"));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 1, new OneWayCommandButtonData(14, 0x00, "Mix"));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width"));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 3, new UserModeButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 4, new PanCommandButtonData("VIEWS"));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 5, new SendsCommandButtonData(), isNoteReceiver: true);

            // User
            modeID = (Int32)UserSendsLayerMode.User;
            this.ButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 0, new ModeTopUserButtonData(0, 0x76, "", ModeTopCommandButtonData.Location.Left), isNoteReceiver: true);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 1, new ModeTopUserButtonData(0, 0x77, "", ModeTopCommandButtonData.Location.Right), isNoteReceiver: true);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 2, new ModeButtonData("Plugins", "plugins", new BitmapColor(pluginBgColor, 190), isMenu: true, midiCode: 0x39));

            // Plugin Navigation
            modeID = (Int32)UserSendsLayerMode.PluginSelectionActivated;
            this.ButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 0, new ModeTopCommandButtonData(14, 0x74, "Previous Plugin", ModeTopCommandButtonData.Location.Left, "plugin_prev", pluginBgColor));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 1, new ModeTopCommandButtonData(14, 0x75, "Next Plugin", ModeTopCommandButtonData.Location.Right, "plugin_next", pluginBgColor));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 2, this.getButtonData(ButtonLayer.FaderModesSend, (Int32)UserSendsMode.User, 2));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 3, new OneWayCommandButtonData(14, 0x12, "Channel Editor", "channel_editor", pluginBgColor));
            this.addButton(ButtonLayer.FaderModesSend, modeID, 5, new OneWayCommandButtonData(14, 0x0D, "Reset Window Positions", "reset_window_positions", pluginBgColor));

            // User Menu
            modeID = (Int32)UserSendsLayerMode.UserMenuActivated;
            this.ButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 0, new UserMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 1, new UserMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 2, new UserMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 3, new UserMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 4, new UserMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 5, new UserMenuSelectButtonData());

            // User Pages
            modeID = (Int32)UserSendsLayerMode.UserPageMenuActivated;
            this.ButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.addButton(ButtonLayer.FaderModesSend, modeID, 0, new UserPageMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 1, new UserPageMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 2, new UserPageMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 3, new UserPageMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 4, new UserPageMenuSelectButtonData());
            this.addButton(ButtonLayer.FaderModesSend, modeID, 5, new UserPageMenuSelectButtonData());

            //            this.addButton(ButtonLayer.FaderModesSend, "0", new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height"));
            //            this.addButton(ButtonLayer.FaderModesSend, "2", new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width"));
            //            this.addButton(ButtonLayer.FaderModesSend, "1", new OneWayCommandButtonData(14, 0x00, "Mix"));
            //            this.addButton(ButtonLayer.FaderModesSend, "0-1", new ModeTopUserButtonData(0, 0x76, "", ModeTopCommandButtonData.Location.Left), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.FaderModesSend, "1-1", new ModeTopUserButtonData(0, 0x77, "", ModeTopCommandButtonData.Location.Right), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.FaderModesSend, "2-1", new ModeButtonData("Plugins", "plugins", new BitmapColor(pluginBgColor, 190), isMenu: true, midiCode: 0x39));
            //            this.addButton(ButtonLayer.FaderModesSend, "0-2", new ModeTopCommandButtonData(14, 0x74, "Previous Plugin", ModeTopCommandButtonData.Location.Left, "plugin_prev", pluginBgColor));
            //            this.addButton(ButtonLayer.FaderModesSend, "1-2", new ModeTopCommandButtonData(14, 0x75, "Next Plugin", ModeTopCommandButtonData.Location.Right, "plugin_next", pluginBgColor));
            //            this.addButton(ButtonLayer.FaderModesSend, "3-2", new OneWayCommandButtonData(14, 0x12, "Channel Editor", "channel_editor", pluginBgColor));
            //            this.addButton(ButtonLayer.FaderModesSend, "5-2", new OneWayCommandButtonData(14, 0x0D, "Reset Window Positions", "reset_window_positions", pluginBgColor));
            // this.addButton(ButtonLayer.faderModesSend, "5-2", new CommandButtonData(0x39, "FX", pluginBgColor, BitmapColor.White));
            //            this.addButton(ButtonLayer.FaderModesSend, "3", new UserModeButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "4", new PanCommandButtonData("VIEWS"));
            //            this.addButton(ButtonLayer.FaderModesSend, "5", new SendsCommandButtonData(), isNoteReceiver: true);
            //            this.addButton(ButtonLayer.FaderModesSend, "0-3", new UserMenuSelectButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "1-3", new UserMenuSelectButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "2-3", new UserMenuSelectButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "3-3", new UserMenuSelectButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "4-3", new UserMenuSelectButtonData());
            //            this.addButton(ButtonLayer.FaderModesSend, "5-3", new UserMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "0-4", new UserPageMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "1-4", new UserPageMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "2-4", new UserPageMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "3-4", new UserPageMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "4-4", new UserPageMenuSelectButtonData());
//            this.addButton(ButtonLayer.FaderModesSend, "5-4", new UserPageMenuSelectButtonData());
        }

        // common
        protected override Boolean OnLoad()
        {
            base.OnLoad();
            
            foreach (var bl in this.ButtonLayerDict.Values)
            {
                foreach (var md in bl.ModeDataDict.Values)
                {
                    foreach (var bd in md.ButtonDataList)
                    {
                        bd?.OnLoad(this.plugin);
                    }
                }
            }

            this.plugin.CommandNoteReceived += this.OnNoteReceived;

            this.plugin.ActiveUserPagesReceived += (Object sender, Int32 e) =>
            {
                (this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData).ActiveUserPages = e;
                this.UpdateAllActionImages();
            };

            this.plugin.UserPageChanged += (Object sender, Int32 e) =>
            {
                // User mode changed
                var umbd = this.getButtonData(ButtonLayer.FaderModesSend, 0, 3) as UserModeButtonData;
                umbd.setUserPage(e);
            };

            this.plugin.ChannelDataChanged += (Object sender, EventArgs e) =>
            {
                this.UpdateAllActionImages();
            };

            this.plugin.SelectButtonPressed += (Object sender, EventArgs e) =>
            {
                if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                {
                    this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                    (this.getButtonData(idxUserSendsPluginsButton) as ModeButtonData).Activated = false;
                    (this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData).sendUserPage();
                }
                else
                {
                    this.CurrentLayer = ButtonLayer.ChannelPropertiesPlay;
                }
                this.UpdateAllActionImages();
            };

            this.plugin.FocusDeviceChanged += (Object sender, string e) =>
            {
                var pluginName = getPluginName(e);

                for (var i = 0; i < 2; i++)
                {
                    var bd = this.getButtonData(ButtonLayer.FaderModesSend, 1, i) as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                    bd = this.getButtonData(ButtonLayer.FaderModesSend, 2, i) as ModeTopCommandButtonData;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                }
                var ubd = this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData;
                ubd.resetUserPage();
                ubd.setPageNames(new PlugSettingsFinder().GetParamSettings(pluginName, "Loupedeck User Pages", false).UserMenuItems);

                if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                {
//                    this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
//                    (this.buttonData[idxUserSendsPluginsButton] as ModeButtonData).Activated = false;
                    this.plugin.EmitSelectModeChanged(SelectButtonMode.User);
                }
                
                this.UpdateAllActionImages();
            };

            this.plugin.AutomationModeChanged += (Object sender, AutomationMode e) =>
            {
                var ambd = this.getButtonData(idxPlayAutomationModeButton) as AutomationModeButtonData;
                ambd.CurrentMode = e;
                this.UpdateAllActionImages();
            };
            this.plugin.FunctionKeyChanged += (Object sender, FunctionKeyParams fke) =>
            {
                if (fke.KeyID == 12 || fke.KeyID == 13)
                {
                    (this.getButtonData(ButtonLayer.FaderModesSend, 1, fke.KeyID - 12) as CommandButtonData).Name = fke.FunctionName;
                }
                this.UpdateAllActionImages();
            };
            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                (this.getButtonData(idxPlayMuteSoloButton) as PropertyButtonData).setPropertyType(e);
                // this.EmitActionImageChanged();
            };
            this.plugin.UserButtonMenuActivated += (object sender, UserButtonMenuParams e) =>
            {
                if (e.IsActive)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        Int32 value;

                        if (e.ChannelIndex < 0)
                        {
                            // Channel index not set, assuming user page menu
                            value = i + 1;
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.UserPageMenuActivated;
                        }
                        else
                        {
                            value = (UInt16)(127 / (e.MenuItems.Length - 1) * i);
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.UserMenuActivated;
                        }

                        var ubd = this.getButtonData(ButtonLayer.FaderModesSend, (Int32)this.CurrentUserSendsLayerMode, i) as UserMenuSelectButtonData;
                        if (i < e.MenuItems.Length)
                        {
                            ubd.init(e.ChannelIndex, value, e.MenuItems[i]);
                        }
                        else
                        {
                            // Initializing with channel index only will result in empty
                            // buttons to be drawn which will still trigger the UserButtonMenuActivated
                            // event with IsActive set to false so that the menu torn down properly.
                            ubd.init(e.ChannelIndex);
                        }
                    }
                    this.DeactivateUserMenu = false;
                    this.UpdateAllActionImages();
                }
                else
                {
                    this.DeactivateUserMenu = true;
                }
            };

            return true;
        }

        protected void OnNoteReceived(object sender, NoteOnEvent e)
        {
            foreach (NoteReceiverEntry n in this.NoteReceivers)
            {
                if (n.Note == e.NoteNumber)
                {
                    var cbd = this.getButtonData(n.Index) as CommandButtonData;
                    cbd.Activated = e.Velocity > 0;
                }
            }
            this.UpdateAllActionImages();
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null)
                return null;

            var bd = this.getButtonData(this.CurrentLayer, this.getCurrentMode(), Int32.Parse(actionParameter));
            
            if (bd != null)
            {
                return bd.getImage(imageSize);
            }

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            return bb.ToImage();
        }

        protected override void RunCommand(String actionParameter)
        {
            var actionParameterNum = Int32.Parse(actionParameter);

            var bd = this.getButtonData(this.CurrentLayer, this.getCurrentMode(), actionParameterNum);

            bd?.runCommand();

            if (this.CurrentLayer != ButtonLayer.ViewSelector 
                && this.CurrentLayer != this.LastButtonLayer1)
            {
                this.LastButtonLayer2 = this.LastButtonLayer1;
                this.LastButtonLayer1 = this.CurrentLayer;
            }

            SelectButtonMode selectMode;

            switch (this.CurrentLayer)
            {
                case ButtonLayer.ViewSelector:
                    switch (actionParameterNum)
                    {
                        case 0: // PLAY
                            this.CurrentLayer = ButtonLayer.ChannelPropertiesPlay;
                            break;
                        case 1: // REC
                            this.CurrentLayer = ButtonLayer.ChannelPropertiesRec;
                            break;
                        case 2: // SHOW
                            this.CurrentLayer = ButtonLayer.FaderModesShow;
                            break;
                        case 3: // USER/SENDS
                            this.CurrentLayer = ButtonLayer.FaderModesSend;
                            break;
                        case 4: // LAST
                            this.CurrentLayer = this.LastButtonLayer2;
                            break;
                    }
                    switch (this.CurrentLayer)
                    {
                        case ButtonLayer.ChannelPropertiesPlay:
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            selectMode = (this.getButtonData(idxPlaySelButton) as ModeChannelSelectButtonData).Activated ? SelectButtonMode.Select
                                                                                                                      : SelectButtonMode.Property;
                            this.plugin.EmitSelectModeChanged(selectMode);
                            this.plugin.EmitPropertySelectionChanged((this.getButtonData(idxPlayMuteSoloSelectButton) as PropertySelectionButtonData).CurrentType);
                            break;
                        case ButtonLayer.ChannelPropertiesRec:
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                            this.plugin.EmitPropertySelectionChanged((this.getButtonData(idxRecArmMonitorButton) as PropertySelectionButtonData).CurrentType);
                            break;
                        case ButtonLayer.FaderModesShow:
                            this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            this.plugin.EmitPropertySelectionChanged(ChannelProperty.PropertyType.Select);
                            break;
                        case ButtonLayer.FaderModesSend:
                            if (LastUserSendsMode == UserSendsMode.Sends)
                            {
                                selectMode = SelectButtonMode.Send;
                                this.getButtonData(idxSendButton).runCommand();
                            }
                            else
                            {
                                selectMode = SelectButtonMode.User;
                                this.getButtonData(idxUserButton).runCommand();
                            }
                            this.plugin.EmitSelectModeChanged(selectMode);
                            break;
                    }
                    this.UpdateAllActionImages();
                    break;
                case ButtonLayer.ChannelPropertiesPlay:
                    if (this.CurrentPlayLayerMode == PlayLayerMode.AutomationActivated)
                    {
                        (this.getButtonData(idxPlayAutomationModeButton) as AutomationModeButtonData).SelectionModeActivated = false;
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
                                    (this.getButtonData(idxPlayMuteSoloSelectButton) as PropertySelectionButtonData).Activated = false;
                                    this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    (this.getButtonData(idxPlayMuteSoloSelectButton) as PropertySelectionButtonData).Activated = true;
                                    (this.getButtonData(idxPlaySelButton) as ModeChannelSelectButtonData).Activated = false;
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
                                    this.CurrentLayer = ButtonLayer.ViewSelector;
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
                    this.UpdateAllActionImages();
                    break;
                case ButtonLayer.ChannelPropertiesRec:
                    if (this.CurrentRecLayerMode == RecLayerMode.PreModeActivated)
                    {
                        (this.getButtonData(idxRecAutomationModeButton) as RecPreModeButtonData).SelectionModeActivated = false;
                        this.CurrentRecLayerMode = RecLayerMode.All;
                    }
                    else
                    {
                        switch (Int32.Parse(actionParameter))
                        {
                            case 2: // PRE MODE
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    (this.getButtonData(idxRecAutomationModeButton) as RecPreModeButtonData).SelectionModeActivated = true;
                                    this.CurrentRecLayerMode = RecLayerMode.PreModeActivated;
                                }
                                break;
                            case 3: // PANELS
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.PanelsActivated;
                                    (this.getButtonData(idxRecPanelsButton) as ModeButtonData).Activated = true;
                                }
                                else if (this.CurrentRecLayerMode == RecLayerMode.PanelsActivated)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.All;
                                    (this.getButtonData(idxRecPanelsButton) as ModeButtonData).Activated = false;
                                }
                                break;
                            case 4: // VIEWS
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentLayer = ButtonLayer.ViewSelector;
                                }
                                break;
                            case 5: // CLICK
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.ClickActivated;
                                    (this.getButtonData(idxRecClickButton) as MenuCommandButtonData).MenuActivated = true;
                                    this.plugin.EmitSelectButtonCustomModeChanged(new SelectButtonCustomParams { ButtonIndex = 4, 
                                        MidiChannel = 14, MidiCode = 0x58, IconName = "tempo_tap", BgColor = ClickBgColor, BarColor = BitmapColor.Transparent });
                                    this.plugin.EmitSelectButtonCustomModeChanged(new SelectButtonCustomParams { ButtonIndex = 5, 
                                        MidiChannel = 0, MidiCode = 0x59, IconName = "click_vol", BgColor = ClickBgColor, BarColor = BitmapColor.White
                                    });
                                }
                                else if (this.CurrentRecLayerMode == RecLayerMode.ClickActivated)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.All;
                                    (this.getButtonData(idxRecClickButton) as MenuCommandButtonData).MenuActivated = false;
                                    this.plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                                    this.plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                                }
                                break;
                        }
                    }
                    this.UpdateAllActionImages();
                    break;
                case ButtonLayer.FaderModesShow:
                    switch (Int32.Parse(actionParameter))
                    {
                        case 4: // VIEWS
                            this.CurrentLayer = ButtonLayer.ViewSelector;
                            this.UpdateAllActionImages();
                            break;
                        default :
                            for (var i = 0; i <= 5; i++)
                            {
                                if (i != 4) (this.getButtonData(ButtonLayer.FaderModesShow, 0, i) as CommandButtonData).Activated = false;
                            }
                            var cbd = this.getButtonData(this.CurrentLayer, 0, actionParameterNum) as CommandButtonData;
                            cbd.Activated = !cbd.Activated;
                            this.UpdateAllActionImages();
                            break;
                    }
                    break;
                case ButtonLayer.FaderModesSend:
                    if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.UserMenuActivated ||
                        this.CurrentUserSendsLayerMode == UserSendsLayerMode.UserPageMenuActivated)
                    {
                        if (this.DeactivateUserMenu)
                        {
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                            this.DeactivateUserMenu = false;
                            this.UpdateAllActionImages();
                        }
                        break;
                    }

                    switch (Int32.Parse(actionParameter))
                    {
                        case 2: // PLUGINS
                            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.User)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.PluginSelectionActivated;
                                this.plugin.EmitSelectModeChanged(SelectButtonMode.FX);
                            }
                            else if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                                this.plugin.EmitSelectModeChanged(SelectButtonMode.User);
                                (this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData).sendUserPage();
                            }
                            (this.getButtonData(idxUserSendsPluginsButton) as ModeButtonData).Activated = this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated;
                            this.UpdateAllActionImages();
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
                            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                                (this.getButtonData(idxUserSendsPluginsButton) as ModeButtonData).Activated = false;
                            }
                            this.CurrentLayer = ButtonLayer.ViewSelector;
                            this.plugin.EmitSelectModeChanged(SelectButtonMode.Select);
                            (this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData).clearActive();
                            this.UpdateAllActionImages();
                            break;
                        case 5: // SENDS
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.Sends;
                                LastUserSendsMode = UserSendsMode.Sends;
                                (this.getButtonData(idxUserSendsUserModeButton) as UserModeButtonData).clearActive();
                                this.plugin.EmitSelectModeChanged(SelectButtonMode.Send);
                            }
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
                if (this.CurrentLayer == ButtonLayer.ChannelPropertiesPlay &&
                    this.CurrentPlayLayerMode == PlayLayerMode.LayersActivated
                    && actionParameter == "1")
                {
                    this.plugin.SendMidiNote(14, 0x3B);
                    return true;
                }
            }

            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }


        private Int32 getCurrentMode()
        {
            switch (this.CurrentLayer)
            {
                case ButtonLayer.ChannelPropertiesPlay:
                    return (Int32)this.CurrentPlayLayerMode;
                case ButtonLayer.ChannelPropertiesRec:
                    return (Int32)this.CurrentRecLayerMode;
                case ButtonLayer.FaderModesSend:
                    return (Int32)this.CurrentUserSendsLayerMode;
                default:
                    return 0;
            }
        }

        private void addButton(ButtonLayer buttonLayer, Int32 modeID, Int32 buttonIndex, ButtonData bd, Boolean isNoteReceiver = false)
        {
            this.ButtonLayerDict[buttonLayer].AddModeButtonData(modeID, buttonIndex, bd);
            if (isNoteReceiver)
            {
                var cbd = bd as CommandButtonData;
                this.NoteReceivers.Add(new NoteReceiverEntry { Note = cbd.Code, Index = new ButtonDataIndex(buttonLayer, modeID, buttonIndex) });
            }
        }

        private ButtonData getButtonData(ButtonDataIndex bdi) => this.getButtonData(bdi.ButtonLayerID, bdi.ModeID, bdi.ButtonIndex);

        private ButtonData getButtonData (ButtonLayer buttonLayer, Int32 modeID, Int32 buttonIndex)
        {
            return this.ButtonLayerDict[buttonLayer].ModeDataDict[modeID].ButtonDataList[buttonIndex] 
                ?? this.ButtonLayerDict[buttonLayer].ModeDataDict[0].ButtonDataList[buttonIndex];
        }
    }
}


