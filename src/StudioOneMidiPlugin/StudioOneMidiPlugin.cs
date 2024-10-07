namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Navigation;

    using Loupedeck.StudioOneMidiPlugin.Controls;

    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Multimedia;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class StudioOneMidiPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

		public InputDevice midiIn = null, mackieMidiIn = null;
		public OutputDevice midiOut = null, mackieMidiOut = null;

		public const Int32 ChannelCount = 6;

		public IDictionary<string, MackieChannelData> channelData = new Dictionary<string, MackieChannelData>();

        String midiInName, midiOutName, mackieMidiInName, mackieMidiOutName;

        public ChannelFader channelFader;

        public Boolean isConfigWindowOpen = false;

        public event EventHandler ChannelDataChanged;
        public event EventHandler<NoteOnEvent> CommandNoteReceived;
        public event EventHandler<NoteOnEvent> OneWayCommandNoteReceived;
        public event EventHandler<Int32> ActiveUserPagesReceived;
        public event EventHandler SelectButtonPressed;
        public event EventHandler<string> FocusDeviceChanged;
        public event EventHandler<ChannelProperty.PropertyType> PropertySelectionChanged;

        public enum SelectButtonMode
        {
            Select,
            Property,
            Send,
            User
        }
        public event EventHandler<SelectButtonMode> SelectModeChanged;

        public enum FaderMode
        {
            Volume,
            Pan
        }
        public event EventHandler<FaderMode> FaderModeChanged;

        public class FunctionKeyParams
        {
            public int KeyID { get; set; }
            public string FunctionName { get; set; }
        }
        public event EventHandler<FunctionKeyParams> FunctionKeyChanged;

        public const int UserButtonMidiBase = 0x70;
        public class UserButtonParams
        {
            public Int32 channelIndex { get; set; }
            public Int32 userValue { get; set; } = 0;
            public String userLabel;
            public Boolean isActive() => this.userValue > 0 ;
        }
        public event EventHandler<UserButtonParams> UserButtonChanged;

        public class UserButtonMenuParams
        {
            public Int32 ChannelIndex { get; set; } = -1;
            public String[] MenuItems { get; set; }
            public Boolean IsActive { get; set; } = true;
        }
        public event EventHandler<UserButtonMenuParams> UserButtonMenuActivated;

        public const Int32 MaxUserPages = 6;
        private const Int32 UserPageMidiBase = 0x2B;
        private Boolean[] UserModeActivated { get; set; } = new Boolean[MaxUserPages];
        public event EventHandler<Int32> UserPageChanged;

        public enum AutomationMode
        {
            Off = 0,
            Read,
            Touch,
            Latch,
            Write
        }
        public event EventHandler<AutomationMode> AutomationModeChanged;
        public AutomationMode CurrentAutomationMode = AutomationMode.Off;

        public enum RecPreMode
        {
            Off = -1,
            Precount,
            Preroll,
            Autopunch
        }
        public RecPreMode CurrentRecPreMode = RecPreMode.Off;

        public enum ChannelFaderMode
        {
            Send,
            Pan,
            User
        }
        public ChannelFaderMode CurrentChannelFaderMode = ChannelFaderMode.Pan;

        public string MidiInName
        {
			get => this.midiInName;
			set {
				if (this.midiIn != null) {
                    this.midiIn.StopEventsListening();
                    this.midiIn.Dispose();
				}

                this.midiInName = value;
				try {
                    this.midiIn = InputDevice.GetByName(value);
                    this.midiIn.StartEventsListening();
                    this.SetPluginSetting("MidiIn", value, false);
				}
				catch (Exception) {
                    this.midiIn = null;
				}
			}
		}

		public String MidiOutName
        {
			get => this.midiOutName;
			set {
				if (this.midiOut != null) {
                    this.midiOut.Dispose();
				}

                this.midiOutName = value;
				try {
                    this.midiOut = OutputDevice.GetByName(value);
                    this.SetPluginSetting("MidiOut", value, false);
				}
				catch (Exception) {
                    this.midiOut = null;
				}
			}
		}

		public String MackieMidiInName
        {
			get => this.mackieMidiInName;
			set {
				if (this.mackieMidiIn != null) {
                    this.mackieMidiIn.StopEventsListening();
                    this.mackieMidiIn.Dispose();
				}

                this.mackieMidiInName = value;
				try {
                    this.mackieMidiIn = InputDevice.GetByName(value);
                    this.mackieMidiIn.EventReceived += OnMidiEvent;
                    this.mackieMidiIn.StartEventsListening();
                    this.SetPluginSetting("MackieMidiIn", value, false);
				}
				catch (Exception) {
                    this.mackieMidiIn = null;
				}
			}
		}

		public String MackieMidiOutName
        {
			get => this.mackieMidiOutName;
			set {
				if (this.mackieMidiOut != null)
                {
                    this.mackieMidiOut.Dispose();
				}

                this.mackieMidiOutName = value;
				try
                {
                    this.mackieMidiOut = OutputDevice.GetByName(value);
                    this.SetPluginSetting("MackieMidiOut", value, false);
				}
				catch (Exception)
                {
                    this.mackieMidiOut = null;
				}
			}
		}

        private System.Timers.Timer ChannelDataChangeTimer;

        public static String getPluginName(String focusDeviceName)
        {
            var start = focusDeviceName.IndexOf(" - ") + 3;
            var pluginName = "";
            if (start > 2) pluginName = focusDeviceName.Substring(start, focusDeviceName.Length - start);
        
            return pluginName;
        }

        // Initializes a new instance of the plugin class.
        public StudioOneMidiPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);

            // Create the channel data objects:
            // - one object for each bank channel
            // - one object for selected channel volume
            // - one object for selected channel pan
            //
            for (int i = 0; i < ChannelCount + 2; i++)
            {
                this.channelData[i.ToString()] = new MackieChannelData(this, i);
            }

			this.ChannelDataChangeTimer = new System.Timers.Timer(10);
			this.ChannelDataChangeTimer.AutoReset = false;
            this.ChannelDataChangeTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) =>
            {
                ChannelDataChanged.Invoke(this, null);
            };
        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
			this.Info.Icon16x16   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_16px.png"));
			this.Info.Icon32x32   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_32px.png"));
			this.Info.Icon48x48   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_48px.png"));
			this.Info.Icon256x256 = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_96px.png"));

			this.LoadSettings();
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
        }

		public void OpenConfigWindow()
        {
			if (this.isConfigWindowOpen)
				return;

			Thread t = new Thread(() => {
				ConfigWindow w = new ConfigWindow(this);
				w.Closed += (_, _) => this.isConfigWindowOpen = false;
				w.Show();
				System.Windows.Threading.Dispatcher.Run();
			});

			t.SetApartmentState(ApartmentState.STA);
			t.Start();

			this.isConfigWindowOpen = true;
		}

		public void EmitChannelDataChanged() =>
            this.ChannelDataChangeTimer.Start();

        public void EmitSelectedButtonPressed() =>
            this.SelectButtonPressed?.Invoke(this, null);

        public void EmitSelectModeChanged(SelectButtonMode sm) =>
            this.SelectModeChanged?.Invoke(this, sm);
        
        public void EmitFaderModeChanged(FaderMode fm) =>
            this.FaderModeChanged?.Invoke(this, fm);

        public void EmitPropertySelectionChanged(ChannelProperty.PropertyType pt) => 
            this.PropertySelectionChanged?.Invoke(this, pt);

        public void EmitUserButtonMenuActivated(UserButtonMenuParams ubmp) =>
            this.UserButtonMenuActivated?.Invoke(this, ubmp)
;
        public override void RunCommand(String commandName, String parameter)
        {
		}

		public override void ApplyAdjustment(String adjustmentName, String parameter, Int32 diff)
        {
		}

		private async void LoadSettings()
        {
			// Workaround - 
			await Task.Delay(100);

//			if (TryGetPluginSetting("MidiIn", out midiInName))
//				MidiInName = midiInName;
//
//			if (TryGetPluginSetting("MackieMidiIn", out mackieMidiInName))
//                MackieMidiInName = mackieMidiInName;
            this.MackieMidiInName = "Loupedeck S1 In";

//            if (TryGetPluginSetting("MidiOut", out midiOutName))
//				MidiOutName = midiOutName;

//			if (TryGetPluginSetting("MackieMidiOut", out mackieMidiOutName))
//				MackieMidiOutName = mackieMidiOutName;
            this.MackieMidiOutName = "Loupedeck S1 Out";
        }

        private void OnMidiEvent(object sender, MidiEventReceivedEventArgs args)
        {
            MidiEvent e = args.Event;
            // PitchBend -> faders & user button values
            if (e is PitchBendEvent)
            {
                var pbe = e as PitchBendEvent;

                if (pbe.Channel < ChannelCount + 2)
                {
                    // Faders for 6 channels and vol + pan for selected channel
                    
                    if (!this.channelData.TryGetValue(((Int32)pbe.Channel).ToString(), out MackieChannelData cd))
                    {
                        return;
                    }

                    cd.Value = pbe.PitchValue / 16383.0f;
                    this.EmitChannelDataChanged();
                }
            }
            // Note event -> toggle settings
            else if (e is NoteOnEvent)
            {
                var ce = e as NoteOnEvent;
                ChannelProperty.PropertyType eventType = ChannelProperty.PropertyType.Select;
                var eventTypeFound = false;

                foreach (ChannelProperty.PropertyType bt in Enum.GetValues(typeof(ChannelProperty.PropertyType)))
                {
                    if (ce.NoteNumber >= ChannelProperty.MidiBaseNote[(int)bt] && ce.NoteNumber < (ChannelProperty.MidiBaseNote[(int)bt] + ChannelCount + 1))
                    {
                        eventType = bt;
                        eventTypeFound = true;
                        break;
                    }
                }

                if (eventTypeFound)
                {
                    var channelIndex = ce.NoteNumber - ChannelProperty.MidiBaseNote[(int)eventType];

                    if (!this.channelData.TryGetValue(channelIndex.ToString(), out MackieChannelData cd))
                        return;

                    cd.BoolProperty[(int)eventType] = ce.Velocity > 0;
                    this.EmitChannelDataChanged();
                }
                else if (ce.Channel == 0)
                {
                    if (ce.NoteNumber >= UserButtonMidiBase && ce.NoteNumber < (UserButtonMidiBase + ChannelCount))
                    {
                        // 6 user buttons
                        var ubp = new UserButtonParams();
                        ubp.channelIndex = ce.NoteNumber - UserButtonMidiBase;
                        ubp.userValue = ce.Velocity;
                        if (this.channelData.TryGetValue(ubp.channelIndex.ToString(), out MackieChannelData cd))
                        {
                            cd.UserValue = ubp.userValue;
                            ubp.userLabel = cd.UserLabel;
                        }
                        UserButtonChanged.Invoke(this, ubp);
                    }
                    else if (ce.NoteNumber >= 0x4A && ce.NoteNumber <= 0x4D)
                    {
                        var am = this.CurrentAutomationMode;
                        if (ce.Velocity > 0)
                        {
                            am = (AutomationMode)(ce.NoteNumber - 0x4A + 1);
                        }
                        else if (ce.NoteNumber == 0x4A)
                        {
                            am = AutomationMode.Off;
                        }
                        if (am != this.CurrentAutomationMode)
                        {
                            this.CurrentAutomationMode = am;
                            AutomationModeChanged.Invoke(this, am);
                        }
                    }
                    else if (ce.NoteNumber >= 0x56 && ce.NoteNumber <= 0x58)
                    {
                        var rpm = (RecPreMode)(ce.NoteNumber - 0x56);
                        if (ce.Velocity > 0)
                        {
                            this.CurrentRecPreMode = rpm;
                        }
                        else if (rpm == this.CurrentRecPreMode)
                        {
                            this.CurrentRecPreMode = RecPreMode.Off;
                        }
                        CommandNoteReceived.Invoke(this, ce);
                    }
                    else if (ce.NoteNumber >= UserPageMidiBase && ce.NoteNumber < UserPageMidiBase + MaxUserPages)
                    {
                        // User page changed

                        this.UserModeActivated[ce.NoteNumber - UserPageMidiBase] = ce.Velocity > 0;

                        var userPage = 0;
                        for (var i = 0; i < MaxUserPages; i++)
                        {
                            if (this.UserModeActivated[i])
                            {
                                userPage = i + 1;
                                break;
                            }
                        }

                        UserPageChanged.Invoke(this, userPage);
                    }
                    else
                    {
                        CommandNoteReceived.Invoke(this, ce);
                    }
                }
                else if (ce.Channel >= 14)
                {
                    OneWayCommandNoteReceived.Invoke(this, ce);
                }
            }
            else if (e is ControlChangeEvent)  // controllers -> numeric arguments
            {
                var ce = e as ControlChangeEvent;

                if (ce.ControlNumber == 0x60)
                {
                    ActiveUserPagesReceived.Invoke(this, ce.ControlValue);
                }
            }
            else if (e is NormalSysExEvent)  // SysEx -> text for labels etc.
            {
                var ce = e as NormalSysExEvent;
                if (ce.Data.Length < 5)
                    return;

                // Check if this is a Mackie style SysEx command
                byte[] mackieControlPrefix = { 0x00, 0x00, 0x66 };
                if (!ce.Data.SubArray(0, mackieControlPrefix.Length).SequenceEqual(mackieControlPrefix))
                    return;

                if (ce.Data.Length > 6 && ce.Data[4] == 0x12)
                {
                    byte offset = ce.Data[5];
                    byte[] str = ce.Data.SubArray(6, ce.Data.Length - 7);

                    var receivedString = Encoding.UTF8.GetString(str, 0, str.Length);
                    var displayIndex = offset / 4;

                    if (!this.channelData.TryGetValue(displayIndex.ToString(), out MackieChannelData cd))
                        return;

                    switch (offset % 4)
                    {
                        case 0: // Label
                            cd.Label = receivedString;
                            this.EmitChannelDataChanged();
                            break;
                        case 1: // Value
                            cd.ValueStr = receivedString;
                            this.EmitChannelDataChanged();
                            break;
                        case 2: // Description
                            cd.Description = receivedString;
                            this.EmitChannelDataChanged();
                            break;
                        case 3: // User Button Label
                            cd.UserLabel = receivedString;
                            var ubp = new UserButtonParams();
                            ubp.channelIndex = displayIndex;
                            ubp.userValue = cd.UserValue;
                            ubp.userLabel = cd.UserLabel;
                            UserButtonChanged.Invoke(this, ubp);
                            break;
                    }
                }
                // Focus channel name
                else if (ce.Data.Length > 5 && ce.Data[4] == 0x13)
                {
                    byte[] str = ce.Data.SubArray(5, ce.Data.Length - 6);
                    var receivedString = Encoding.UTF8.GetString(str, 0, str.Length); //.Replace("\0", "");

                    this.FocusDeviceChanged?.Invoke(this, receivedString);
                }
                // Function key name
                else if (ce.Data.Length > 5 && ce.Data[4] == 0x14)
                {
                    byte keyID = ce.Data[5];
                    byte[] str = ce.Data.SubArray(6, ce.Data.Length - 7);
                    var receivedString = Encoding.UTF8.GetString(str, 0, str.Length);

                    var fke = new FunctionKeyParams();
                    fke.KeyID = keyID;
                    fke.FunctionName = receivedString;
                    FunctionKeyChanged.Invoke(this, fke);
                }
            }
        }

        public void SendMidiNote(Int32 midiChannel, Int32 note, Int32 velocity = 127)
        {
            var e = new NoteOnEvent();
            e.Channel = (FourBitNumber)midiChannel;
            e.Velocity = (SevenBitNumber)velocity;
            e.NoteNumber = (SevenBitNumber)note;
            this.mackieMidiOut.SendEvent(e);
        }

        public void SetChannelFaderMode(ChannelFaderMode mode, Int32 userPage = 1)
        {
            switch (mode)
            {
                case ChannelFaderMode.Pan:
                    this.SendMidiNote(0, PanCommandButtonData.Note);
                    break;
                case ChannelFaderMode.Send:
                    this.SendMidiNote(0, SendsCommandButtonData.Note);
                    break;
                case ChannelFaderMode.User:
                    this.SendMidiNote(0, UserPageMidiBase - 1 + userPage);
                    break;
            }
            this.CurrentChannelFaderMode = mode;
        }

        // public override bool TryProcessTouchEvent(string actionName, string actionParameter, DeviceTouchEvent deviceTouchEvent)
        // {
        //    if (actionName == this.channelFader.GetResetActionName())
        //    {
        // return channelFader.TryProcessTouchEvent(actionParameter, deviceTouchEvent);
        //    }
        //
        //   return base.TryProcessTouchEvent(actionName, actionParameter, deviceTouchEvent);
        // }
    }
}
