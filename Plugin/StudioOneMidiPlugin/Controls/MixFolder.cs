﻿using Loupedeck.StudioOneMidiPlugin.Helpers;
using Melanchall.DryWetMidi.Core;
using PluginSettings;
using SharpHook;
using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;
using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;


namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    internal class MixFolder : PluginDynamicFolder
    {
        private static readonly BitmapColor ClickBgColor = new BitmapColor(50, 114, 134);
        public static readonly BitmapColor DefaultBarColor = new BitmapColor(60, 192, 232);

        // Menu buttons
        private class ModeData
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
            UserPageMenuActivated = 4,      // User page switching via menu
            PluginAddActivated = 5          // Adding plugin (showing list of favorite plugins)
        }
        private UserSendsLayerMode CurrentUserSendsLayerMode = UserSendsLayerMode.User;
        private Boolean DeactivateUserMenu = false;

        private class LayerData
        {
            public ConcurrentDictionary<Int32, ModeData> ModeDataDict = new ConcurrentDictionary<Int32, ModeData>();

            public void AddMode(Int32 modeID) => this.ModeDataDict.TryAdd(modeID, new ModeData());
            public void AddModeButtonData(Int32 modeID, Int32 buttonIndex, ButtonData bd) => this.ModeDataDict[modeID].ButtonDataList[buttonIndex] = bd;
        }

        private readonly ConcurrentDictionary<ButtonLayer, LayerData> MenuButtonLayerDict = new ConcurrentDictionary<ButtonLayer, LayerData>();

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
        private static readonly ButtonDataIndex idxPlayMuteSoloButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 0);
        private static readonly ButtonDataIndex idxPlaySelButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, 0, 1);
        private static readonly ButtonDataIndex idxPlayAutomationModeButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 2);
        private static readonly ButtonDataIndex idxRecArmMonitorButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 0);
        private static readonly ButtonDataIndex idxRecAutomationModeButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 2);
        private static readonly ButtonDataIndex idxRecPreModeButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, (Int32)RecLayerMode.All, 2);
        private static readonly ButtonDataIndex idxRecPanelsButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 3);
        private static readonly ButtonDataIndex idxRecClickButton = new ButtonDataIndex(ButtonLayer.ChannelPropertiesRec, 0, 5);
        private static readonly ButtonDataIndex idxUserSendsPluginsButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 1, 2);
        private static readonly ButtonDataIndex idxUserSendsUserModeButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 0, 3);
        private static readonly ButtonDataIndex idxUserSendsSendsModeButton = new ButtonDataIndex(ButtonLayer.FaderModesSend, 0, 5);

        private class NoteReceiverEntry
        {
            public Int32 Note;
            public ButtonDataIndex? Index;
        }
        private readonly List<NoteReceiverEntry> NoteReceivers = new List<NoteReceiverEntry>();

        private List<string> MenuButtons = new();
        private List<string> SelectButtons = new();
        private List<string> Faders = new();

        private class FavoritePluginPatch
        { 
            public string Name = ""; 
            public FinderColor Color = FinderColor.Black; 
        }
        private Dictionary<string, FavoritePluginPatch> FavoritePlugins = new();

        // Select buttons
        protected ConcurrentDictionary<int, SelectButtonData?> SelectButtonDataDict = new();
        private bool SelectButtonListenToMidi = false;

        // Faders
        private SelectButtonMode SelectMode = SelectButtonMode.Select;
        private FaderMode FaderMode = FaderMode.Volume;
        private static BitmapImage? IconVolume, IconPan;
        private String PluginName = "";
        private static readonly PlugSettingsFinder UserPlugSettingsFinder = new PlugSettingsFinder(new PlugSettingsFinder.PlugParamSetting
        {
            OnColor = new FinderColorOnColor(ColorConv.Convert(DefaultBarColor)),     // Used for volume bar
            OffColor = new FinderColor(80, 80, 80),         // Used for volume bar
            TextOnColor = FinderColor.White,
            TextOffColor = new FinderColor(80, 80, 80)
        });

        private static readonly bool[] FaderIsActive = new bool[StudioOneMidiPlugin.ChannelCount];

        // Wheel tools
        private ConcurrentDictionary<int, ButtonData?> WheelButtonData = new();

        private static bool[] ChannelDataUpdated = new bool[StudioOneMidiPlugin.ChannelCount];

        private readonly System.Timers.Timer ChannelDataUpdateTimer;
        private const int _channelDataUpdateTimeout = 30; // milliseconds

        // Custom settings for value display that is invoked when individual faders
        // get reconfigured on the fly to show specific parameters such as tempo.
        private class CustomParams
        {
            public BitmapColor FaderBgColor = BitmapColor.Black;
            public BitmapColor FaderBarColor = DefaultBarColor;
        }
        private readonly CustomParams[] CustomSettings = new CustomParams[StudioOneMidiPlugin.ChannelCount];

        public MixFolder()
        {
            this.DisplayName = "Channel Mixer";
            this.Description = "Mixer control surface";

            this.MenuButtonLayerDict.TryAdd(ButtonLayer.ViewSelector, new LayerData());

            this.MenuButtonLayerDict[ButtonLayer.ViewSelector].AddMode(0);
            this.AddButton(ButtonLayer.ViewSelector, 0, 0, new ModeButtonData("PLAY"));
            this.AddButton(ButtonLayer.ViewSelector, 0, 1, new ModeButtonData("REC"));
            this.AddButton(ButtonLayer.ViewSelector, 0, 2, new ModeButtonData("SHOW"));
            this.AddButton(ButtonLayer.ViewSelector, 0, 3, new ModeButtonData("USER\rSENDS"));
            this.AddButton(ButtonLayer.ViewSelector, 0, 4, new ModeButtonData("LAST", "view_last"));

            Int32 modeID;

            var arrangerBgColor = new BitmapColor(60, 60, 60);
            var consoleBgColor = new BitmapColor(60, 60, 60);
            var layersBgColor = new BitmapColor(180, 180, 180);
            var addBgColor = new BitmapColor(60, 60, 60);

            this.MenuButtonLayerDict.TryAdd(ButtonLayer.ChannelPropertiesPlay, new LayerData());

            // Mute/Solo
            modeID = (Int32)PlayLayerMode.PropertySelect;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                                                                    ChannelProperty.PropertyType.Solo,
                                                                                                    "select-mute", "select-solo", "select-mute-solo",
                                                                                                    activated: this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new ModeChannelSelectButtonData(activated: this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new ModeButtonData("ARRANGER", "arranger", new BitmapColor(arrangerBgColor, 190), isMenu: true));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new ModeButtonData("CONSOLE", "panels", new BitmapColor(consoleBgColor, 190), isMenu: true));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new ModeButtonData("VIEWS"));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new FlipPanVolCommandButtonData(0x35), true);

            // Select
            modeID = (Int32)PlayLayerMode.ChannelSelect;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new PropertyButtonData(PropertyButtonData.SelectedChannel,
                                                                                           ChannelProperty.PropertyType.Mute,
                                                                                           PropertyButtonData.TrackNameMode.ShowFull));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new AutomationModeButtonData());
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new ModeButtonData("LAYERS", "layers", new BitmapColor(layersBgColor, 128), isMenu: true));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new ModeButtonData("ADD", "button_add", new BitmapColor(addBgColor, 190), isMenu: true));

            // Automation
            modeID = (Int32)PlayLayerMode.AutomationActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new AutomationModeCommandButtonData(AutomationMode.Off, ButtonData.DefaultSelectionBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new AutomationModeCommandButtonData(AutomationMode.Read, ButtonData.DefaultSelectionBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, this.GetMenuButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 2));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new AutomationModeCommandButtonData(AutomationMode.Touch, ButtonData.DefaultSelectionBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new AutomationModeCommandButtonData(AutomationMode.Write, ButtonData.DefaultSelectionBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new AutomationModeCommandButtonData(AutomationMode.Latch, ButtonData.DefaultSelectionBgColor));

            // Layers
            modeID = (Int32)PlayLayerMode.LayersActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x30, "LAY UP", "layer_up_inv", layersBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x34, "LAY EXP", "layers_expand_inv", layersBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x31, "LAY DN", "layer_dn_inv", layersBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, this.GetMenuButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 3));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x32, "LAY +", "layer_add_inv", layersBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new OneWayCommandButtonData(14, 0x33, "LAY -", "layer_remove_inv", layersBgColor));

            // Arranger
            modeID = (Int32)PlayLayerMode.ArrangerActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x06, "Track List", "track_list", arrangerBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x04, "Inspector", "inspector", arrangerBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new OneWayCommandButtonData(14, 0x38, "Show Automation", "show_automation", arrangerBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(15, 0x2A, "Marker Track", "ruler_marker", arrangerBgColor));

            // Console
            modeID = (Int32)PlayLayerMode.ConsoleActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", consoleBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x00, "Mix", null, consoleBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", consoleBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", consoleBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, new OneWayCommandButtonData(14, 0x1F, "Show Outputs", "show_outputs", consoleBgColor));

            // Add Tracks
            modeID = (Int32)PlayLayerMode.AddActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesPlay].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 0, new OneWayCommandButtonData(14, 0x15, "Add Insert", "add_insert", addBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 1, new OneWayCommandButtonData(14, 0x16, "Add Send", "add_send", addBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 2, new OneWayCommandButtonData(14, 0x18, "Add FX Channel", "add_fx", addBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 3, new OneWayCommandButtonData(14, 0x17, "Add Bus Channel", "add_bus", addBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 4, new OneWayCommandButtonData(14, 0x3C, "Add Track", null, addBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesPlay, modeID, 5, this.GetMenuButtonData(ButtonLayer.ChannelPropertiesPlay, (Int32)PlayLayerMode.ChannelSelect, 5));

            this.MenuButtonLayerDict.TryAdd(ButtonLayer.ChannelPropertiesRec, new LayerData());

            var panelsBgColor = new BitmapColor(60, 60, 60);

            // Rec Layer
            modeID = (Int32)RecLayerMode.All;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 0, new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                                                                ChannelProperty.PropertyType.Monitor,
                                                                                                "select-arm", "select-monitor", "select-arm-monitor",
                                                                                                activated: true));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new OneWayCommandButtonData(14, 0x0B, "Time", "time_display"));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 2, new RecPreModeButtonData());
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new ModeButtonData("PANELS", "panels", new BitmapColor(panelsBgColor, 190), isMenu: true));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 4, new ModeButtonData("VIEWS"));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new MenuCommandButtonData(0x3A, 0x59, "CLICK", "click", new BitmapColor(ClickBgColor, 100)), isNoteReceiver: true);

            // Punch
            modeID = (Int32)RecLayerMode.PreModeActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(1);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new CommandButtonData(0x57, "Preroll", "preroll", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new CommandButtonData(0x58, "Autopunch", "autopunch", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new CommandButtonData(0x56, "Precount", "precount", ButtonData.DefaultSelectionBgColor), isNoteReceiver: true);

            // Mixer Panels
            modeID = (Int32)RecLayerMode.PanelsActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height", panelsBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 1, new OneWayCommandButtonData(14, 0x05, "Rec Panel", "rec_panel", panelsBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width", panelsBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 4, new OneWayCommandButtonData(14, 0x10, "Show Inputs", "show_inputs", panelsBgColor));
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 5, new OneWayCommandButtonData(14, 0x09, "Show Groups", "show_groups", panelsBgColor));

            // Click
            modeID = (Int32)RecLayerMode.ClickActivated;
            this.MenuButtonLayerDict[ButtonLayer.ChannelPropertiesRec].AddMode(modeID);
            this.AddButton(ButtonLayer.ChannelPropertiesRec, modeID, 3, new OneWayCommandButtonData(14, 0x4F, "Click Settings", "click_settings", ClickBgColor));

            this.MenuButtonLayerDict.TryAdd(ButtonLayer.FaderModesShow, new LayerData());

            // Fader Visibility
            this.MenuButtonLayerDict[ButtonLayer.FaderModesShow].AddMode(0);
            this.AddButton(ButtonLayer.FaderModesShow, 0, 0, new CommandButtonData(0x40, "AUDIO", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.AddButton(ButtonLayer.FaderModesShow, 0, 1, new CommandButtonData(0x42, "FX", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.AddButton(ButtonLayer.FaderModesShow, 0, 2, new CommandButtonData(0x43, "BUS", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.AddButton(ButtonLayer.FaderModesShow, 0, 3, new CommandButtonData(0x44, "OUT", new BitmapColor(0, 60, 80), BitmapColor.White));
            this.AddButton(ButtonLayer.FaderModesShow, 0, 4, new ModeButtonData("VIEWS"));
            this.AddButton(ButtonLayer.FaderModesShow, 0, 5, new ViewAllRemoteCommandButtonData(), isNoteReceiver: false);

            this.MenuButtonLayerDict.TryAdd(ButtonLayer.FaderModesSend, new LayerData());

            var pluginBgColor = new BitmapColor(60, 60, 60);

            // Sends
            modeID = (Int32)UserSendsLayerMode.Sends;
            this.MenuButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 0, new OneWayCommandButtonData(14, 0x1D, "Toggle Height", "console_height"));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new OneWayCommandButtonData(14, 0x16, "Add Send", "add_send"));
            // this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new OneWayCommandButtonData(14, 0x00, "Mix"));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 2, new OneWayCommandButtonData(14, 0x1E, "Toggle Width", "console_width"));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 3, new UserModeButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 4, new PanCommandButtonData("VIEWS"));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 5, new SendsCommandButtonData(), isNoteReceiver: true);

            // User
            modeID = (Int32)UserSendsLayerMode.User;
            this.MenuButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 0, new ModeTopUserButtonData(0, 0x76, "", ModeTopCommandButtonData.Location.Left), isNoteReceiver: true);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new ModeTopUserButtonData(0, 0x77, "", ModeTopCommandButtonData.Location.Right), isNoteReceiver: true);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 2, new ModeButtonData("Plugins", "plugins", new BitmapColor(pluginBgColor, 190), isMenu: true, midiCode: 0x39));

            // Plugin Navigation
            modeID = (Int32)UserSendsLayerMode.PluginSelectionActivated;
            this.MenuButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 0, new ModeTopCommandButtonData(14, 0x74, "Previous Plugin", ModeTopCommandButtonData.Location.Left, "plugin_prev", pluginBgColor));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new ModeTopCommandButtonData(14, 0x75, "Next Plugin", ModeTopCommandButtonData.Location.Right, "plugin_next", pluginBgColor));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 2, this.GetMenuButtonData(ButtonLayer.FaderModesSend, (Int32)UserSendsMode.User, 2));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 3, new OneWayCommandButtonData(14, 0x12, "Channel Editor", "channel_editor", pluginBgColor));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 4, new OneWayCommandButtonData(14, 0x15, "Add Insert", "add_insert", pluginBgColor));
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 5, new OneWayCommandButtonData(14, 0x0D, "Reset Window Positions", "reset_window_positions", pluginBgColor));

            // User Menu
            modeID = (Int32)UserSendsLayerMode.UserMenuActivated;
            this.MenuButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 0, new UserMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new UserMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 2, new UserMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 3, new UserMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 4, new UserMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 5, new UserMenuSelectButtonData());

            // User Pages
            modeID = (Int32)UserSendsLayerMode.UserPageMenuActivated;
            this.MenuButtonLayerDict[ButtonLayer.FaderModesSend].AddMode(modeID);
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 0, new UserPageMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 1, new UserPageMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 2, new UserPageMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 3, new UserPageMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 4, new UserPageMenuSelectButtonData());
            this.AddButton(ButtonLayer.FaderModesSend, modeID, 5, new UserPageMenuSelectButtonData());

            // Select buttons
            for (int i = 0; i < 6; i++)
            {
                this.SelectButtonDataDict[i] = new SelectButtonData(i);
            }

            // Faders
            IconVolume ??= EmbeddedResources.ReadImage(EmbeddedResources.FindFile("dial_volume_52px.png"));
            IconPan ??= EmbeddedResources.ReadImage(EmbeddedResources.FindFile("dial_pan_52px.png"));

            for (var i = 0; i < FaderIsActive.Length; i++)
            {
                FaderIsActive[i] = true;
            }

            // Wheel Tools
            WheelButtonData[0] = new CommandButtonData(0x5E, 0x5D, "Play", "play");

            this.ChannelDataUpdateTimer = new System.Timers.Timer(_channelDataUpdateTimeout);
            this.ChannelDataUpdateTimer.AutoReset = false;
            this.ChannelDataUpdateTimer.Elapsed += (Object? sender, System.Timers.ElapsedEventArgs e) =>
            {
                for (int i = 0; i < 6; i++)
                {
                    if (ChannelDataUpdated[i])
                    {
                        CommandImageChanged("select:" + i);
                        ChannelDataUpdated[i] = false;
                    }
                }
            };

        }

        public override bool Load()
        {
            var result = base.Load();

            var plugin = (StudioOneMidiPlugin)this.Plugin;

            foreach (var bl in this.MenuButtonLayerDict.Values)
            {
                foreach (var md in bl.ModeDataDict.Values)
                {
                    foreach (var bd in md.ButtonDataList)
                    {
                        bd?.OnLoad(plugin);
                    }
                }
            }
            foreach (var sbd in this.SelectButtonDataDict.Values)
            {
                sbd?.OnLoad(plugin);
            }

            plugin.CommandNoteReceived += (Object? sender, NoteOnEvent e) =>
            {
                // Menu buttons
                foreach (NoteReceiverEntry n in this.NoteReceivers)
                {
                    if (n.Note == e.NoteNumber && n.Index != null)
                    {
                        var cbd = (CommandButtonData)GetButtonData(n.Index);
                        cbd.Activated = e.Velocity > 0;

                        // Note: Could also check for ModeID, but that would require to track the current mode
                        //       as a generic Int32 in addition to the Enums for each layer. Probably not worth it
                        //       since at worst some spurious button image updates are triggered.
                        //
                        if (this.CurrentLayer == n.Index.ButtonLayerID)
                        {
                            this.CommandImageChanged($"menu:{n.Index.ButtonIndex}");
                        }
                    }
                }

                // Select buttons
                if (this.SelectButtonListenToMidi)
                {
                    foreach (var bd in this.SelectButtonDataDict.Values)
                    {
                        if (bd != null && bd.CurrentMode == SelectButtonMode.Custom && bd.CurrentCustomParams.MidiCode == e.NoteNumber)
                        {
                            bd.CustomIsActivated = e.Velocity > 0;
                            this.CommandImageChanged("select:" + bd.ChannelIndex);
                        }
                    }
//                    this.UpdateAllCommandImages(SelectButtons);
                }
            };

            plugin.ChannelDataChanged += (s, e) =>
            {
                // Menu buttons
                if (this.CurrentLayer == ButtonLayer.ChannelPropertiesPlay &&
                    (Int32)this.CurrentPlayLayerMode == idxPlayMuteSoloButton.ModeID &&
                    e == ChannelCount)
                {
                    this.CommandImageChanged($"menu:{idxPlayMuteSoloButton.ButtonIndex}");
                }

                // Select buttons
                if (e < ChannelCount)
                {
                    ChannelDataUpdated[e] = true;
                    //UpdateAllCommandImages(this.SelectButtons);
                    if (ChannelDataUpdateTimer.Enabled)
                    {
                        // If the timer is already running, extend the timeout to avoid multiple events
                        ChannelDataUpdateTimer.Interval = _channelDataUpdateTimeout;
                        // Debug.WriteLine($"Timer reset to {timeout} ms");
                        return;
                    }
                    ChannelDataUpdateTimer.Start();

                    // Faders
                    AdjustmentImageChanged("fader:" + e);
                }
            };

            plugin.ChannelValueChanged += (s, e) => AdjustmentImageChanged("fader:" + e);

            plugin.ActiveUserPagesReceived += (Object? sender, Int32 e) =>
            {
                var bd = this.GetButtonData(idxUserSendsUserModeButton) as UserModeButtonData;
                if (bd != null) bd.ActiveUserPages = e;
                this.UpdateUserPageButton();
                //                this.UpdateAllActionImages();
            };

            plugin.UserPageChanged += (Object? sender, Int32 e) =>
            {
                // Menu button
                var umbd = (UserModeButtonData)GetMenuButtonData(ButtonLayer.FaderModesSend, 0, 3);
                umbd.setUserPage(e);
                this.UpdateUserPageButton();

                // Select button
                SelectButtonData.UserPlugSettingsFinder.CurrentUserPage = e;

                // Faders
                UserPlugSettingsFinder.CurrentUserPage = e;
            };

            plugin.SelectButtonPressed += (Object? sender, EventArgs e) =>
            {
                if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                {
                    this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                    ((ModeButtonData)GetButtonData(idxUserSendsPluginsButton)).Activated = false;
                    ((UserModeButtonData)GetButtonData(idxUserSendsUserModeButton)).sendUserPage();
                    this.UpdateAllCommandImages(MenuButtons);
                }
                else if (this.CurrentLayer != ButtonLayer.ChannelPropertiesPlay)
                {
                    this.CurrentLayer = ButtonLayer.ChannelPropertiesPlay;
                    this.UpdateAllCommandImages(MenuButtons);
                }
            };

            plugin.FocusDeviceChanged += (Object? sender, string e) =>
            {
                // Menu buttons
                var pluginName = GetPluginName(e);

                for (var i = 0; i < 2; i++)
                {
                    var bd = this.GetMenuButtonData(ButtonLayer.FaderModesSend, 1, i) as ModeTopCommandButtonData;
                    if (bd == null) continue;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                    bd = this.GetMenuButtonData(ButtonLayer.FaderModesSend, 2, i) as ModeTopCommandButtonData;
                    if (bd == null) continue;
                    bd.setTopDisplay(e);
                    bd.setPluginName(pluginName);
                }
                var ubd = this.GetButtonData(idxUserSendsUserModeButton) as UserModeButtonData;
                if (ubd != null)
                {
                    ubd.resetUserPage();

                    var pf = new PlugSettingsFinder();
                    var deviceEntry = pf.GetPlugParamDeviceEntry(pluginName);
                    if (deviceEntry != null) ubd.setPageNames(deviceEntry.UserPageNames);
                }

                if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                {
                    plugin.EmitSelectModeChanged(SelectButtonMode.User);
                }
                if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.User)
                {
                    this.CommandImageChanged("menu:0");
                    this.CommandImageChanged("menu:1");
                }

                // Select buttons
                SelectButtonData.FocusDeviceName = e;
                SelectButtonData.PluginName = GetPluginName(e);

                // Faders
                this.PluginName = GetPluginName(e);
//                this.UpdateAllAdjustmentImages();
            };

            plugin.AutomationModeChanged += (Object? sender, AutomationMode e) =>
            {
                var ambd = (AutomationModeButtonData)GetButtonData(idxPlayAutomationModeButton);
                ambd.CurrentMode = e;

                if (this.CurrentLayer == ButtonLayer.ChannelPropertiesPlay &&
                   (Int32)this.CurrentPlayLayerMode == idxPlayAutomationModeButton.ModeID)
                {
                    this.CommandImageChanged($"menu:{idxPlayAutomationModeButton.ButtonIndex}");
                }
            };

            plugin.RecPreModeChanged += (Object? sender, RecPreMode rpm) =>
            {
                if (this.CurrentLayer == ButtonLayer.ChannelPropertiesRec &&
                    (this.CurrentRecLayerMode == RecLayerMode.All || this.CurrentRecLayerMode == RecLayerMode.PreModeActivated))
                {
                    this.CommandImageChanged($"menu:{idxRecPreModeButton.ButtonIndex}");
                }
            };

            plugin.FunctionKeyChanged += (Object? sender, FunctionKeyParams fke) =>
            {
                if (fke.KeyID == 12 || fke.KeyID == 13)
                {
                    ((CommandButtonData)GetMenuButtonData(ButtonLayer.FaderModesSend, 1, fke.KeyID - 12)).Name = fke.FunctionName ?? "";
                    if (this.CurrentLayer == ButtonLayer.FaderModesSend && this.CurrentUserSendsLayerMode == UserSendsLayerMode.User)
                    {
                        this.CommandImageChanged($"menu:{fke.KeyID + 2}");
                    }
                }
            };

            plugin.PropertySelectionChanged += (object? sender, ChannelProperty.PropertyType e) =>
            {
                // Menu buttons
                ((PropertyButtonData)GetButtonData(idxPlayMuteSoloButton)).setPropertyType(e);

                // Select buttons
                SelectButtonData.SelectionPropertyType = e;
                this.UpdateAllCommandImages(SelectButtons);
            };

            plugin.UserButtonMenuActivated += (object? sender, UserButtonMenuParams e) =>
            {
                // Menu buttons
                if (e.IsActive)
                {
                    for (var i = 0; i < 6; i++)
                    {
                        Int32 value = 0;

                        if (e.ChannelIndex < 0)
                        {
                            // Channel index not set, assuming user page menu
                            value = i + 1;
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.UserPageMenuActivated;
                        }
                        else if (e.MenuItems != null)
                        {
                            value = (UInt16)(127 / (e.MenuItems.Length - 1) * i);
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.UserMenuActivated;
                        }

                        var ubd = this.GetMenuButtonData(ButtonLayer.FaderModesSend, (Int32)this.CurrentUserSendsLayerMode, i) as UserMenuSelectButtonData;
                        if (ubd != null && e.MenuItems != null)
                        {
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
                    }
                    this.DeactivateUserMenu = false;
                    this.UpdateAllCommandImages(MenuButtons);
                }
                else
                {
                    this.DeactivateUserMenu = true;
                }

                // Select buttons
                if (e.ChannelIndex >= 0)
                {
                    var bd = this.SelectButtonDataDict[e.ChannelIndex];
                    if (bd != null)
                    {
                        bd.UserButtonMenuActive = e.IsActive;
                        this.UpdateAllCommandImages(SelectButtons);
                    }
                }
            };

            ///////////////////////////////////////////////////////////////////////////
            // Channel Select Buttons

            plugin.UserButtonChanged += (object? sender, UserButtonParams e) =>
            {
                var bd = SelectButtonDataDict[e.ChannelIndex];
                if (bd != null) bd.UserButtonActive = e.IsActive();
            };

            plugin.PluginSettingsReloaded += (Object? sender, EventArgs e) =>
            {
                SelectButtonData.UserPlugSettingsFinder.ClearCache();
                this.UpdateAllCommandImages(SelectButtons);
            };

            plugin.SelectModeChanged += (Object? sender, SelectButtonMode e) =>
            {
                // Select buttons
                for (var i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    var bd = this.SelectButtonDataDict[i];
                    if (bd != null) bd.CurrentMode = e;
                }
                this.SelectButtonListenToMidi = false;
                this.UpdateAllCommandImages(SelectButtons);

                // Faders
                this.SelectMode = e;
                Array.Clear(this.CustomSettings);

                this.UpdateAllAdjustmentImages();
            };

            plugin.SelectButtonCustomModeChanged += (Object? sender, SelectButtonCustomParams cp) =>
            {
                // Select buttons
                var bd = this.SelectButtonDataDict[cp.ButtonIndex];
                if (bd != null) bd.SetCustomMode(cp);

                if (cp.MidiCode > 0)
                {
                    this.SelectButtonListenToMidi = true;
                }
                this.UpdateAllCommandImages(SelectButtons);

                // Faders
                this.CustomSettings[cp.ButtonIndex] = new CustomParams
                {
                    FaderBgColor = cp.BgColor,
                    FaderBarColor = cp.BarColor,
                };

                this.UpdateAllAdjustmentImages();
            };

            // Faders
            plugin.FaderModeChanged += (Object? sender, FaderMode e) =>
            {
                this.FaderMode = e;
                this.UpdateAllAdjustmentImages();
            };

            //plugin.ChannelActiveCanged += (Object? sender, ChannelActiveParams e) =>
            //{
            //    FaderIsActive[e.ChannelIndex] = e.IsActive;
            //    this.UpdateAllAdjustmentImages();
            //};

            plugin.UserButtonChanged += (Object? s, UserButtonParams e) =>
            {
                UpdateAllCommandImages(SelectButtons);
                return;
            };

            return result;
        }

        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _)
        {
            return PluginDynamicFolderNavigation.None;
        }

        public override IEnumerable<string> GetButtonPressActionNames(DeviceType deviceType)
        {
            var lst = new List<string>();
            this.MenuButtons.Clear();
            this.SelectButtons.Clear();
            this.FavoritePlugins.Clear();

            // SelectButtons: 0, 3, 4, 7, 8, 11
            // Menu Buttons : 1, 2, 5, 6, 9, 10

            // Commands for buttons
            for (int i = 0; i < 3; i++)
            {
                AddControl("select:" + i, lst, SelectButtons, CreateCommandName);
                AddControl("menu:" + (i * 2), lst, MenuButtons, CreateCommandName);
                AddControl("menu:" + (i * 2 + 1), lst, MenuButtons, CreateCommandName);
                AddControl("select:" + (i + 3), lst, SelectButtons, CreateCommandName);
            }

            foreach (var idx in lst)
            {
                FavoritePlugins.Add(idx.Substring(idx.LastIndexOf('_') + 1), new FavoritePluginPatch());
            }
            return lst;
        }

        public override IEnumerable<string> GetEncoderRotateActionNames(DeviceType deviceType)
        {
            var lst = new List<string>();
            this.Faders.Clear();

            for (int i = 0; i < 6; i++) AddControl("fader:" + i, lst, Faders, CreateAdjustmentName);

            return lst;
        }
        public override IEnumerable<string> GetEncoderPressActionNames(DeviceType deviceType)
        {
            var lst = new List<string>();

            for (int i = 0; i < 6; i++) AddControl("fader:" + i, lst, Faders, CreateCommandName);
            
            return lst;
        }

        public override IEnumerable<string> GetWheelToolNames(DeviceType deviceType)
        {
            var lst = new List<string>();

            for (int i = 0; i < 1; i++) lst.Add(CreateCommandName("wheel:" + i));

            return lst;
        }


        private void AddControl(string name, List<string> globalList, List<string> buttonList, Func<string, string> createFunc)
        {
            globalList.Add(createFunc(name));
            buttonList.Add(name);
        }
        public override BitmapImage GetButtonImage(PluginImageSize imageSize)
        {
            return EmbeddedResources.ReadImage(EmbeddedResources.FindFile("menu_faders_80px.png"));
        }

        public override BitmapImage? GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null) return null;

            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginAddActivated)
            {
                // Show list of favorite plugins across all buttons
                var bbp = new BitmapBuilder(imageSize);

                var patch = FavoritePlugins[actionParameter];
                var pc = ColorConv.Convert(patch.Color);
                bbp.FillRectangle(0, 0, imageSize.GetWidth(), imageSize.GetHeight(), pc);
                var tc = (pc.R + pc.G + pc.B) > 400 ? BitmapColor.Black : BitmapColor.White;
                bbp.DrawText(patch.Name, tc);
                return bbp.ToImage();
            }

            int actionParameterNum = 0;
            int.TryParse(actionParameter.Substring(actionParameter.IndexOf(':') + 1), out actionParameterNum);

            ButtonData? bd = null;
            if (actionParameter.StartsWith("menu"))
            {
                bd = this.GetMenuButtonData(this.CurrentLayer, this.GetCurrentMode(), actionParameterNum);
            }
            else if (actionParameter.StartsWith("select"))
            {
                bd = this.GetSelectButtonData(actionParameterNum);
            }
            else if (actionParameter.StartsWith("wheel"))
            {
                bd = this.GetWheelButtonData(actionParameterNum);
            }

            if (bd != null)
            {
                return bd.getImage(imageSize);
            }

            var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
            return bb.ToImage();
        }

        private ButtonData GetSelectButtonData(int actionParameterNum)
        {
            var bd = this.SelectButtonDataDict[actionParameterNum];
            if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

            // If this is linked to another parameter, check the state of that parameter

            if (bd.CurrentMode == SelectButtonMode.User)
            {
                var deviceEntry = SelectButtonData.UserPlugSettingsFinder.GetPlugParamDeviceEntry(SelectButtonData.PluginName);
                if (deviceEntry == null)
                {
                    // Debug.WriteLine("ChannelSelectButton getCommandImage deviceEntry is null for " + SelectButtonData.PluginName);
                    return bd;
                }

                var linkedParameter = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.Label, 0);
                var linkedParameterUser = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.UserLabel, 0);

                // Debug.WriteLine("ChannelSelectButton getCommandImage channel: " + bd.ChannelIndex + " bd.Label: " + bd.Label + ", linkedParameter: " + linkedParameter +", linkedParameterUser: " + linkedParameterUser);

                if (!linkedParameter.IsNullOrEmpty() || !linkedParameterUser.IsNullOrEmpty())
                {
                    var updateFader = false;

                    foreach (var sbd in this.SelectButtonDataDict.Values)
                    {
                        if (sbd == null) continue;

                        var cd = ((StudioOneMidiPlugin)Plugin).channelData[sbd.ChannelIndex.ToString()];

                        if (sbd.UserLabel == linkedParameterUser)   // user button
                        {
                            bd.UserButtonEnabled = SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.UserLabel, 0) ^ cd.UserValue > 0;
                        }
                        else
                        {
                            bd.UserButtonEnabled = true;
                        }
                        if (linkedParameter != null && sbd.UserLabel == linkedParameter)       // channel value
                        {
                            var linkedStates = SelectButtonData.UserPlugSettingsFinder.GetLinkedStates(deviceEntry, bd.Label, 0);
                            if (!linkedStates.IsNullOrEmpty())
                            {
                                var userMenuItems = SelectButtonData.UserPlugSettingsFinder.GetUserMenuItems(deviceEntry, linkedParameter, 0);
                                if (userMenuItems != null && userMenuItems.Length > 1)
                                {
                                    var menuIndex = (Int32)Math.Round((Double)cd.UserValue / 127 * (userMenuItems.Length - 1));
                                    bd.Enabled = linkedStates != null ? linkedStates.Contains(menuIndex.ToString()) ^ SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.Label, 0)
                                                                       : true;
                                    updateFader = true;
                                }
                            }
                            else
                            {
                                bd.Enabled = SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.Label, 0) ^ cd.UserValue > 0;
                                updateFader = true;
                            }
                        }
                    }
                    if (updateFader && FaderIsActive[bd.ChannelIndex] != bd.Enabled)
                    {
                        FaderIsActive[bd.ChannelIndex] = bd.Enabled;
                        this.AdjustmentImageChanged("fader:" + bd.ChannelIndex);
                    }
                }
                else
                {
                    bd.Enabled = true;
                    bd.UserButtonEnabled = true;
                    FaderIsActive[bd.ChannelIndex] = true;
                }
            }
            return bd;
        }

        private ButtonData? GetWheelButtonData(int actionParameterNum)
        {
            return WheelButtonData[actionParameterNum];
        }

        public override BitmapImage? GetAdjustmentImage(string actionParameter, PluginImageSize imageSize)
        {

            // if (actionParameters == null) return null;
            //if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return null;
            // if (!actionParameters.TryGetString(ControlOrientationSelector, out var controlOrientation)) return null;

            var channelIndex = actionParameter.Substring(actionParameter.IndexOf(':') + 1);

            var bb = new BitmapBuilder(imageSize.GetWidth(), imageSize.GetHeight());

            var cd = ((StudioOneMidiPlugin)this.Plugin).channelData[channelIndex];

            var currentChannel = cd.ChannelID + 1;

            var customParams = cd.ChannelID < this.CustomSettings.Length ? this.CustomSettings[cd.ChannelID] : null;
            bb.FillRectangle(0, 0, bb.Width, bb.Height, customParams != null
                                                            ? customParams.FaderBgColor
                                                            : BitmapColor.Black);

            var deviceEntry = UserPlugSettingsFinder.GetPlugParamDeviceEntry(this.PluginName);

            if (this.SelectMode == SelectButtonMode.FX)
            {
                return bb.ToImage();
            }

            if (UserPlugSettingsFinder.GetLabel(deviceEntry, cd.Label, currentChannel).Length == 0) return bb.ToImage();

            const Int32 sideBarW = 8;
            var sideBarX = bb.Width - sideBarW;
            var volBarX = 0;
            var piW = (bb.Width - 2 * sideBarW) / 2;
            const Int32 piH = 8;

            if (int.Parse(channelIndex) > 2)
            {
                // Right side control orientation
                volBarX = sideBarX;
                sideBarX = 0;
            }

            // Check for selected channel volume & pan
            var isSelectedChannel = cd.ChannelID >= StudioOneMidiPlugin.ChannelCount;
            var isSelectedPan = cd.ChannelID == StudioOneMidiPlugin.ChannelCount + 1;
            var isClick = isSelectedPan ? cd.ValueStr.IsNullOrEmpty()
                                        : this.SelectMode == SelectButtonMode.Send
                                          || this.SelectMode == SelectButtonMode.User ? false
                                                                                      : this.FaderMode == FaderMode.Pan && cd.ValueStr.Contains("dB");
            var isVolume = cd.ChannelID == StudioOneMidiPlugin.ChannelCount
                           || (isSelectedPan
                               ? isClick
                               : this.SelectMode == SelectButtonMode.Send
                                 || this.SelectMode == SelectButtonMode.User
                                 || isClick
                                 ? true
                                 : this.FaderMode == FaderMode.Volume);

            var valueColor = BitmapColor.White;
            var valBarColor = customParams != null
                              ? customParams.FaderBarColor
                              : ColorConv.Convert(UserPlugSettingsFinder.GetBarOnColor(deviceEntry, cd.Label, currentChannel));

            if (this.SelectMode == SelectButtonMode.Select)
            {
                if (cd.Muted || cd.Solo)
                {
                    bb.FillRectangle(
                        sideBarW, piH, bb.Width - 2 * sideBarW, bb.Height - 2 * piH,
                        ChannelProperty.PropertyColor[cd.Muted ? (Int32)ChannelProperty.PropertyType.Mute : (Int32)ChannelProperty.PropertyType.Solo]
                        );
                }
                if (cd.Selected && cd.ChannelID < StudioOneMidiPlugin.ChannelCount)
                {
                    bb.FillRectangle(sideBarX, 0, sideBarW, bb.Height, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Select]);
                }
                if (!isSelectedChannel && cd.Armed)
                {
                    bb.FillRectangle(sideBarW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Arm]);
                }
                if (!isSelectedChannel && cd.Monitor)
                {
                    bb.FillRectangle(sideBarW + piW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Monitor]);
                }
            }


            if (SelectMode == SelectButtonMode.User && cd.ChannelID < FaderIsActive.Length && !FaderIsActive[cd.ChannelID])
            {
                valueColor = ColorConv.Convert(UserPlugSettingsFinder.GetTextOffColor(deviceEntry, cd.Label, currentChannel));
                valBarColor = ColorConv.Convert(UserPlugSettingsFinder.GetOffColor(deviceEntry, cd.Label, currentChannel));
            }

            if (UserPlugSettingsFinder.HideValueBar(deviceEntry, cd.Label, currentChannel)) valBarColor = BitmapColor.Transparent;

            if (isVolume)
            {
                var volBarH = (Int32)Math.Ceiling(cd.Value * bb.Height);
                var volBarY = bb.Height - volBarH;
                if (UserPlugSettingsFinder.GetMode(deviceEntry, cd.Label, currentChannel) == PlugSettingsFinder.PlugParamSetting.PotMode.Symmetric)
                {
                    volBarH = (Int32)(Math.Abs(cd.Value - 0.5) * bb.Height);
                    volBarY = cd.Value < 0.5 ? bb.Height / 2 : bb.Height / 2 - volBarH;
                }
                if (isSelectedChannel && !isSelectedPan)
                {
                    bb.DrawImage(IconVolume, 0, 0);
                }
                bb.FillRectangle(volBarX, volBarY, sideBarW, volBarH, valBarColor);
            }
            else
            {
                var panBarW = (Int32)(Math.Abs(cd.Value - 0.5) * bb.Width);
                var panBarX = cd.Value > 0.5 ? bb.Width / 2 : bb.Width / 2 - panBarW;

                if (isSelectedChannel)
                {
                    bb.DrawImage(IconPan, 0, 0);
                }
                if (!cd.ValueStr.IsNullOrEmpty())
                {
                    bb.FillRectangle(panBarX, 0, panBarW, piH, valBarColor);
                }
            }

            // bb.DrawText(cd.TrackName, 0, 0, bb.Width, bb.Height / 2, null, imageSize == PluginImageSize.Width60 ? 12 : 1);
            // bb.DrawText($"{Math.Round(cd.Value * 100.0f)} %", 0, bb.Height / 2, bb.Width, bb.Height / 2);


            if (isClick)
            {
                bb.DrawImage(EmbeddedResources.ReadImage(EmbeddedResources.FindFile("click_32px.png")), 12, 9);
            }
            else
            {
                // In custom mode limit the number of decimal places to 2. Hard wired for now.
                var maxValuePrecision = customParams != null ? 2
                                                             : UserPlugSettingsFinder.GetMaxValuePrecision(deviceEntry, cd.Label, currentChannel);

                var valStr = maxValuePrecision >= 0 ? Regex.Replace(cd.ValueStr, @"(\d+)([.,]?)(\d{0," + maxValuePrecision + @"})\d*\s?(\D*)", "$1$2$3 $4")
                                                    : cd.ValueStr;

                bb.DrawText(valStr.Replace(' ', '\n'), 0, bb.Height / 4, bb.Width, bb.Height / 2, valueColor);
            }
            return bb.ToImage();
        }


        public override void RunCommand(String actionParameter)
        {
            int actionParameterNum = 0;
            int.TryParse(actionParameter.Substring(actionParameter.IndexOf(':') + 1), out actionParameterNum);

            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginAddActivated)
            {
                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;

                // Insert name of selected plugin into the plugin selection box & return
                // (keyboard input simulation courtesy of SharpHook)
                var patch = FavoritePlugins[actionParameter];

                var simulator = new EventSimulator();
                simulator.SimulateTextEntry(patch.Name);
                simulator.SimulateKeyPress(SharpHook.Native.KeyCode.VcEnter);

                this.UpdateAllCommandImages(SelectButtons);
                this.UpdateAllCommandImages(MenuButtons);
            }
            else if (actionParameter.StartsWith("menu")) RunMenuCommand(actionParameterNum);
            else if (actionParameter.StartsWith("select")) SelectButtonDataDict[actionParameterNum]?.runCommand();
            else if (actionParameter.StartsWith("fader")) RunFaderCommand(actionParameterNum);
            else throw new InvalidOperationException("Unknown action parameter: " + actionParameter);
        }

        private void RunMenuCommand(int actionParameterNum)
        {
            var bd = this.GetMenuButtonData(this.CurrentLayer, this.GetCurrentMode(), actionParameterNum);

            bd?.runCommand();

            if (this.CurrentLayer != ButtonLayer.ViewSelector
                && this.CurrentLayer != this.LastButtonLayer1)
            {
                this.LastButtonLayer2 = this.LastButtonLayer1;
                this.LastButtonLayer1 = this.CurrentLayer;
            }

            SelectButtonMode selectMode;
            var plugin = (StudioOneMidiPlugin)this.Plugin;

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
                            plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            selectMode = ((ModeChannelSelectButtonData)GetButtonData(idxPlaySelButton)).Activated ? SelectButtonMode.Select
                                                                                                                      : SelectButtonMode.Property;
                            plugin.EmitSelectModeChanged(selectMode);
                            plugin.EmitPropertySelectionChanged(((PropertySelectionButtonData)GetButtonData(idxPlayMuteSoloSelectButton)).CurrentType);
                            break;
                        case ButtonLayer.ChannelPropertiesRec:
                            plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                            plugin.EmitPropertySelectionChanged(((PropertySelectionButtonData)GetButtonData(idxRecArmMonitorButton)).CurrentType);
                            break;
                        case ButtonLayer.FaderModesShow:
                            plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                            plugin.EmitPropertySelectionChanged(ChannelProperty.PropertyType.Select);
                            break;
                        case ButtonLayer.FaderModesSend:
                            if (LastUserSendsMode == UserSendsMode.Sends)
                            {
                                selectMode = SelectButtonMode.Send;
                                this.GetButtonData(idxSendButton).runCommand();
                            }
                            else
                            {
                                selectMode = SelectButtonMode.User;
                                this.GetButtonData(idxUserButton).runCommand();
                            }
                            plugin.EmitSelectModeChanged(selectMode);
                            break;
                    }
                    this.UpdateAllCommandImages(MenuButtons);
                    break;
                case ButtonLayer.ChannelPropertiesPlay:
                    if (this.CurrentPlayLayerMode == PlayLayerMode.AutomationActivated)
                    {
                        ((AutomationModeButtonData)GetButtonData(idxPlayAutomationModeButton)).SelectionModeActivated = false;
                        this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                    }
                    else
                    {
                        switch (actionParameterNum)
                        {
                            case 0: // MUTE/SOLO, PROPERTY
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                }
                                break;
                            case 1: // SEL
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                    ((PropertySelectionButtonData)GetButtonData(idxPlayMuteSoloSelectButton)).Activated = false;
                                    this.CurrentPlayLayerMode = PlayLayerMode.ChannelSelect;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    ((PropertySelectionButtonData)GetButtonData(idxPlayMuteSoloSelectButton)).Activated = true;
                                    ((ModeChannelSelectButtonData)GetButtonData(idxPlaySelButton)).Activated = false;
                                    this.CurrentPlayLayerMode = PlayLayerMode.PropertySelect;
                                    plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                                }
                                break;
                            case 2: // ARRANGER / AUTO
                                if (this.CurrentPlayLayerMode == PlayLayerMode.PropertySelect)
                                {
                                    this.CurrentPlayLayerMode = PlayLayerMode.ArrangerActivated;
                                }
                                else if (this.CurrentPlayLayerMode == PlayLayerMode.ChannelSelect)
                                {
                                    if (bd != null)
                                    {
                                        ((AutomationModeButtonData)bd).SelectionModeActivated = true;
                                        this.CurrentPlayLayerMode = PlayLayerMode.AutomationActivated;
                                    }
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
                    this.UpdateAllCommandImages(MenuButtons);
                    break;
                case ButtonLayer.ChannelPropertiesRec:
                    if (this.CurrentRecLayerMode == RecLayerMode.PreModeActivated)
                    {
                        ((RecPreModeButtonData)GetButtonData(idxRecAutomationModeButton)).SelectionModeActivated = false;
                        this.CurrentRecLayerMode = RecLayerMode.All;
                    }
                    else
                    {
                        switch (actionParameterNum)
                        {
                            case 2: // PRE MODE
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    ((RecPreModeButtonData)GetButtonData(idxRecAutomationModeButton)).SelectionModeActivated = true;
                                    this.CurrentRecLayerMode = RecLayerMode.PreModeActivated;
                                }
                                break;
                            case 3: // PANELS
                                if (this.CurrentRecLayerMode == RecLayerMode.All)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.PanelsActivated;
                                    ((ModeButtonData)GetButtonData(idxRecPanelsButton)).Activated = true;
                                }
                                else if (this.CurrentRecLayerMode == RecLayerMode.PanelsActivated)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.All;
                                    ((ModeButtonData)GetButtonData(idxRecPanelsButton)).Activated = false;
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
                                    ((MenuCommandButtonData)GetButtonData(idxRecClickButton)).MenuActivated = true;
                                    plugin.EmitSelectButtonCustomModeChanged(new SelectButtonCustomParams
                                    {
                                        ButtonIndex = 4,
                                        MidiChannel = 14,
                                        MidiCode = 0x58,
                                        IconName = "tempo_tap",
                                        BgColor = ClickBgColor,
                                        BarColor = BitmapColor.Transparent
                                    });
                                    plugin.EmitSelectButtonCustomModeChanged(new SelectButtonCustomParams
                                    {
                                        ButtonIndex = 5,
                                        MidiChannel = 0,
                                        MidiCode = 0x59,
                                        IconName = "click_vol",
                                        BgColor = ClickBgColor,
                                        BarColor = BitmapColor.White
                                    });
                                }
                                else if (this.CurrentRecLayerMode == RecLayerMode.ClickActivated)
                                {
                                    this.CurrentRecLayerMode = RecLayerMode.All;
                                    ((MenuCommandButtonData)GetButtonData(idxRecClickButton)).MenuActivated = false;
                                    plugin.EmitSelectModeChanged(SelectButtonMode.Property);
                                    plugin.SetChannelFaderMode(ChannelFaderMode.Pan);
                                }
                                break;
                        }
                    }
                    this.UpdateAllCommandImages(MenuButtons);
                    break;
                case ButtonLayer.FaderModesShow:
                    switch (actionParameterNum)
                    {
                        case 4: // VIEWS
                            this.CurrentLayer = ButtonLayer.ViewSelector;
                            break;
                        default:
                            for (var i = 0; i <= 5; i++)
                            {
                                if (i != 4) ((CommandButtonData)GetMenuButtonData(ButtonLayer.FaderModesShow, 0, i)).Activated = false;
                            }
                            var cbd = this.GetMenuButtonData(this.CurrentLayer, 0, actionParameterNum) as CommandButtonData;
                            if (cbd != null) cbd.Activated = !cbd.Activated;
                            break;
                    }
                    this.UpdateAllCommandImages(MenuButtons);
                    break;
                case ButtonLayer.FaderModesSend:
                    if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.UserMenuActivated ||
                        this.CurrentUserSendsLayerMode == UserSendsLayerMode.UserPageMenuActivated)
                    {
                        if (this.DeactivateUserMenu)
                        {
                            this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                            this.DeactivateUserMenu = false;
                            this.UpdateAllCommandImages(MenuButtons);
                        }
                        break;
                    }

                    switch (actionParameterNum)
                    {
                        case 2: // PLUGINS
                            if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.User)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.PluginSelectionActivated;
                                plugin.EmitSelectModeChanged(SelectButtonMode.FX);
                            }
                            else if (this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                                plugin.EmitSelectModeChanged(SelectButtonMode.User);
                                ((UserModeButtonData)GetButtonData(idxUserSendsUserModeButton)).sendUserPage();
                            }
                            ((ModeButtonData)GetButtonData(idxUserSendsPluginsButton)).Activated = this.CurrentUserSendsLayerMode == UserSendsLayerMode.PluginSelectionActivated;
                            this.UpdateAllCommandImages(MenuButtons);
                            break;
                        case 3: // USER 1 2 3...
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.User)
                                {
                                    this.CurrentUserSendsLayerMode = UserSendsLayerMode.User;
                                    LastUserSendsMode = UserSendsMode.User;
                                    this.UpdateAllCommandImages(MenuButtons);
                                }
                                plugin.EmitSelectModeChanged(SelectButtonMode.User);
                            }
                            break;
                        case 4: // VIEWS (BACK)
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                this.CurrentLayer = ButtonLayer.ViewSelector;
                                plugin.EmitSelectModeChanged(SelectButtonMode.Select);
                                // (this.GetButtonData(idxUserSendsUserModeButton) as UserModeButtonData).clearActive();
                                this.UpdateAllCommandImages(MenuButtons);
                            }
                            else
                            {
                                // Adding insert, show list of favorite plugins
                                this.CurrentUserSendsLayerMode = UserSendsLayerMode.PluginAddActivated;
                                var favoritePluginsList = new FavoritePluginsList();
                                try
                                {
                                    favoritePluginsList.ReadFromXmlFile();
                                    int i = 0;
                                    foreach(var patch in FavoritePlugins)
                                    {
                                        var xmlEntry = favoritePluginsList[i++];
                                        patch.Value.Name = xmlEntry.Name;
                                        patch.Value.Color = xmlEntry.Color ?? FinderColor.Black;
                                    }
                                }
                                catch (Exception )
                                {
                                    // TODO
                                }
                                this.UpdateAllCommandImages(SelectButtons);
                                this.UpdateAllCommandImages(MenuButtons);
                            }
                            break;
                        case 5: // SENDS
                            if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.PluginSelectionActivated)
                            {
                                if (this.CurrentUserSendsLayerMode != UserSendsLayerMode.Sends)
                                {
                                    this.CurrentUserSendsLayerMode = UserSendsLayerMode.Sends;
                                    LastUserSendsMode = UserSendsMode.Sends;
                                    ((UserModeButtonData)GetButtonData(idxUserSendsUserModeButton)).clearActive();
                                    this.UpdateAllCommandImages(MenuButtons);
                                }
                                plugin.EmitSelectModeChanged(SelectButtonMode.Send);
                            }
                            break;
                    }
                    break;
            }
        }

        public override void ApplyAdjustment(string actionParameter, int diff)
        {
            // int.TryParse(actionParameter.Substring(actionParameter.IndexOf(':') + 1), out actionParameterNum);

            var cd = ((StudioOneMidiPlugin)this.Plugin).channelData[actionParameter.Substring(actionParameter.IndexOf(':') + 1)];

            // ChannelData cd = this.GetChannel(channelIndex);

            var deviceEntry = UserPlugSettingsFinder.GetPlugParamDeviceEntry(this.PluginName);

            var stepDivisions = UserPlugSettingsFinder.GetDialSteps(deviceEntry, cd.Label, cd.ChannelID + 1);
            if (stepDivisions > 50 && ((StudioOneMidiPlugin)this.Plugin).ShiftPressed)
            {
                stepDivisions *= 6;
            }
            cd.Value = Math.Min(1, Math.Max(0, (Single)Math.Round(cd.Value * stepDivisions + diff) / stepDivisions));
            cd.EmitVolumeUpdate();
        }

        private void RunFaderCommand(int actionParameterNum)
        {
            var plugin = (StudioOneMidiPlugin)this.Plugin;
            if (plugin.channelData.TryGetValue(actionParameterNum.ToString(), out var channel))
            {
                channel.EmitValueReset();
            }
        }

        //public override bool ProcessButtonEvent2(string actionParameter, DeviceButtonEvent2 buttonEvent)
        //{
        //    var cd = ((StudioOneMidiPlugin)this.Plugin).channelData[actionParameter.Substring(actionParameter.IndexOf(':') + 1)];

        //    if (buttonEvent.EventType.IsPress())
        //    {
        //        cd.EmitValueReset();
        //    }

        //    return base.ProcessButtonEvent2(actionParameter, buttonEvent);
        //}

        //public override bool ProcessEncoderEvent(string actionParameter, DeviceEncoderEvent encoderEvent)
        //{
        //    return base.ProcessEncoderEvent(actionParameter, encoderEvent);
        //}

        public override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
        {
            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }

        private void UpdateAllCommandImages(List<string> buttonList)
        {
            foreach (var actionName in buttonList)
            {
                this.CommandImageChanged(actionName);
            }
        }

        private void UpdateAllAdjustmentImages()
        {
            foreach (var actionName in Faders)
            {
                this.AdjustmentImageChanged(actionName);
            }
        }

        private void UpdateUserPageButton()
        {
            if (this.CurrentLayer == ButtonLayer.FaderModesSend &&
                (this.CurrentUserSendsLayerMode == UserSendsLayerMode.User ||
                 this.CurrentUserSendsLayerMode == UserSendsLayerMode.Sends))
            {
                this.CommandImageChanged($"menu:{idxUserSendsUserModeButton.ButtonIndex}");
            }
        }

        private Int32 GetCurrentMode()
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

        private void AddButton(ButtonLayer buttonLayer, Int32 modeID, Int32 buttonIndex, ButtonData bd, Boolean isNoteReceiver = false)
        {
            this.MenuButtonLayerDict[buttonLayer].AddModeButtonData(modeID, buttonIndex, bd);
            if (isNoteReceiver)
            {
                var cbd = (CommandButtonData)bd;
                this.NoteReceivers.Add(new NoteReceiverEntry { Note = cbd.Code, Index = new ButtonDataIndex(buttonLayer, modeID, buttonIndex) });
            }
        }

        private ButtonData GetButtonData(ButtonDataIndex bdi) => this.GetMenuButtonData(bdi.ButtonLayerID, bdi.ModeID, bdi.ButtonIndex);

        private ButtonData GetMenuButtonData(ButtonLayer buttonLayer, Int32 modeID, Int32 buttonIndex)
        {
            return this.MenuButtonLayerDict[buttonLayer].ModeDataDict[modeID].ButtonDataList[buttonIndex]
                ?? this.MenuButtonLayerDict[buttonLayer].ModeDataDict[0].ButtonDataList[buttonIndex]
                ?? new DefaultButtonData();
        }

        private class DefaultButtonData : ButtonData
        {
            public override BitmapImage getImage(PluginImageSize imageSize)
            {
                var bb = new BitmapBuilder(imageSize);
                // bb.DrawText("Undefined ButtonData!");
                return bb.ToImage();
            }
            public override void runCommand() { }
        }
    }
}
