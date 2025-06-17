namespace Loupedeck.StudioOneMidiPlugin
{
    using Loupedeck.StudioOneMidiPlugin.Controls;
    using Loupedeck.StudioOneMidiPlugin.Helpers;
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Multimedia;
    using PluginSettings;
    using SharpHook;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class StudioOneMidiPlugin : Plugin
    {
        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

		public InputDevice? ConfigMidiIn = null, S1MidiIn = null;
		public OutputDevice? ConfigMidiOut = null, S1MidiOut = null;

		public const Int32 ChannelCount = 6;

        public ConcurrentDictionary<String, ChannelData> channelData = new ConcurrentDictionary<String, ChannelData>();

        public event EventHandler? ChannelDataChanged;
        public event EventHandler? ChannelValueChanged;
        public event EventHandler<NoteOnEvent>? CommandNoteReceived;
        public event EventHandler<NoteOnEvent>? OneWayCommandNoteReceived;
        public event EventHandler<Int32>? ActiveUserPagesReceived;
        public event EventHandler? SelectButtonPressed;
        public event EventHandler<String>? FocusDeviceChanged;
        public event EventHandler<ChannelProperty.PropertyType>? PropertySelectionChanged;
        public event EventHandler? PluginSettingsReloaded; // Triggered when the plugin settings are reloaded from XML file

        // Keyboard modifier flags
        Task? KeyHookTask;
        public Boolean ShiftPressed { get; private set; } = false;
        public Boolean ControlPressed { get; private set; } = false;

        public enum SelectButtonMode
        {
            Select,
            Property,
            Send,
            User,
            FX,
            Custom
        }
        public event EventHandler<SelectButtonMode>? SelectModeChanged;

        public class SelectButtonCustomParams
        {
            public Int32 ButtonIndex = -1;
            public Int32 MidiChannel = 0;
            public Int32 MidiCode = 0;
            public String Label = "";
            public String IconName = "";
            public BitmapColor BgColor = BitmapColor.Black;
            public BitmapColor BarColor = ChannelFader.DefaultBarColor;
        }
        public event EventHandler<SelectButtonCustomParams>? SelectButtonCustomModeChanged;

        public enum FaderMode
        {
            Volume,
            Pan
        }
        public event EventHandler<FaderMode>? FaderModeChanged;

        public class FunctionKeyParams
        {
            public int KeyID { get; set; }
            public string? FunctionName { get; set; }
        }
        public event EventHandler<FunctionKeyParams>? FunctionKeyChanged;

        public const int UserButtonMidiBase = 0x70;
        public class UserButtonParams
        {
            public Int32 channelIndex { get; set; }
            public Int32 userValue { get; set; } = 0;
            public String? userLabel;
            public Boolean isActive() => this.userValue > 0 ;
        }
        public event EventHandler<UserButtonParams>? UserButtonChanged;

        public class UserButtonMenuParams
        {
            public Int32 ChannelIndex { get; set; } = -1;
            public String[]? MenuItems { get; set; }
            public Boolean IsActive { get; set; } = true;
        }
        public event EventHandler<UserButtonMenuParams>? UserButtonMenuActivated;

        public const Int32 MaxUserPages = 6;
        private const Int32 UserPageMidiBase = 0x2B;
        private Boolean[] UserModeActivated { get; set; } = new Boolean[MaxUserPages];
        public event EventHandler<Int32>? UserPageChanged;
        private Int32 CurrentUserPage = 0;

        public class ChannelActiveParams
        {
            public Int32 ChannelIndex { get; set; }
            public Boolean IsActive { get; set; } = true;
        }
        public event EventHandler<ChannelActiveParams>? ChannelActiveCanged; 

        public enum AutomationMode
        {
            Off = 0,
            Read,
            Touch,
            Latch,
            Write
        }
        public event EventHandler<AutomationMode>? AutomationModeChanged;
        public AutomationMode CurrentAutomationMode = AutomationMode.Off;

        public enum RecPreMode
        {
            Off = -1,
            Precount,
            Preroll,
            Autopunch
        }
        public RecPreMode CurrentRecPreMode = RecPreMode.Off;
        public event EventHandler<RecPreMode>? RecPreModeChanged;

        public enum ChannelFaderMode
        {
            Send,
            Pan,
            User
        }
        public ChannelFaderMode CurrentChannelFaderMode = ChannelFaderMode.Pan;

        string configMidiInName = "";
        public String ConfigMidiInName
        {
			get => this.configMidiInName;
			set {
                if (this.ConfigMidiIn != null)
                {
                    this.ConfigMidiIn.StopEventsListening();
                    this.ConfigMidiIn.Dispose();
				}

                this.configMidiInName = value;
				try
                {
                    this.ConfigMidiIn = InputDevice.GetByName(value);
                    this.ConfigMidiIn.EventReceived += OnConfigMidiEvent;
                    this.ConfigMidiIn.StartEventsListening();
                    this.SetPluginSetting("ConfigMidiIn", value, false);
				}
				catch (Exception)
                {
                    this.ConfigMidiIn = null;
				}
			}
		}

        string configMidiOutName = "";
        public String ConfigMidiOutName
        {
			get => this.configMidiOutName;
			set {
				if (this.ConfigMidiOut != null)
                {
                    this.ConfigMidiOut.Dispose();
				}

                this.configMidiOutName = value;
				try {
                    this.ConfigMidiOut = OutputDevice.GetByName(value);
                    this.SetPluginSetting("ConfigMidiOut", value, false);
				}
				catch (Exception)
                {
                    this.ConfigMidiOut = null;
				}
			}
		}

        string s1MidiInName = "";
        public String S1MidiInName
        {
			get => this.s1MidiInName;
			set {
				if (this.S1MidiIn != null)
                {
                    this.S1MidiIn.StopEventsListening();
                    this.S1MidiIn.Dispose();
				}

                this.s1MidiInName = value;
				try
                {
                    this.S1MidiIn = InputDevice.GetByName(value);
                    this.S1MidiIn.EventReceived += OnS1MidiEvent;
                    this.S1MidiIn.StartEventsListening();
                    // this.SetPluginSetting("LoupedeckMidiIn", value, false);
				}
				catch (Exception)
                {
                    this.S1MidiIn = null;
				}
			}
		}

        string s1MidiOutName = "";
        public String S1MidiOutName
        {
			get => this.s1MidiOutName;
			set {
				if (this.S1MidiOut != null)
                {
                    this.S1MidiOut.Dispose();
				}

                this.s1MidiOutName = value;
				try
                {
                    this.S1MidiOut = OutputDevice.GetByName(value);
				}
				catch (Exception)
                {
                    this.S1MidiOut = null;
				}
			}
		}

        public static String GetPluginName(String focusDeviceName)
        {
            var start = focusDeviceName.IndexOf(" - ") + 3;
            var pluginName = "";
            if (start > 2) pluginName = focusDeviceName.Substring(start, focusDeviceName.Length - start);
        
            return pluginName;
        }

        // Timers used to delay the sending of channel data change events to
        // to consolidate multipe successive changes into a single event.
        // Just in case, a semaphore is used as a lock to ensure channel
        // description and value changes are sent successively.
        private SemaphoreSlim ChannelDataChangeLock = new SemaphoreSlim(1, 1);
        private readonly System.Timers.Timer ChannelDataChangeTimer, ChannelValueChangeTimer;
        private const int _channelDataChangeTimeout = 30; // milliseconds
        private const int _channelValueChangeTimeout = 10; // milliseconds

        private DialStepsDetector _dialStepsDetector;

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
                this.channelData[i.ToString()] = new ChannelData(this, i);
            }

			this.ChannelDataChangeTimer = new System.Timers.Timer(_channelDataChangeTimeout);
			this.ChannelDataChangeTimer.AutoReset = false;
            this.ChannelDataChangeTimer.Elapsed += (Object? sender, System.Timers.ElapsedEventArgs e) =>
            {
                ChannelDataChangeLock.Wait();
                ChannelDataChanged?.Invoke(this, EventArgs.Empty);
                ChannelDataChangeLock.Release();
            };
            this.ChannelValueChangeTimer = new System.Timers.Timer(_channelValueChangeTimeout);
            this.ChannelValueChangeTimer.AutoReset = false;
            this.ChannelValueChangeTimer.Elapsed += (Object? sender, System.Timers.ElapsedEventArgs e) =>
            {
                ChannelDataChangeLock.Wait();
                ChannelValueChanged?.Invoke(this, EventArgs.Empty);
                ChannelDataChangeLock.Release();
            };

            this._dialStepsDetector = new DialStepsDetector(this);
        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
			this.Info.Icon16x16   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_16px.png"));
			this.Info.Icon32x32   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_32px.png"));
			this.Info.Icon48x48   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_48px.png"));
			this.Info.Icon256x256 = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("plugin_icon_s1_96px.png"));

            var keyHook = new TaskPoolGlobalHook(globalHookType: GlobalHookType.Keyboard);

            keyHook.KeyPressed += (Object? sender, KeyboardHookEventArgs k) => this.SetKeyboardFlags(k.Data.KeyCode, true);
            keyHook.KeyReleased += (Object? sender, KeyboardHookEventArgs k) => this.SetKeyboardFlags(k.Data.KeyCode, false);

            this.KeyHookTask = keyHook.RunAsync();

			this.LoadSettings();
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
        }

        private void SetKeyboardFlags(SharpHook.Native.KeyCode code, Boolean keyPressed)
        {
            switch (code)
            {
                case SharpHook.Native.KeyCode.VcLeftShift:
                case SharpHook.Native.KeyCode.VcRightShift:
                    this.ShiftPressed = keyPressed;
                    break;
                case SharpHook.Native.KeyCode.VcLeftControl:
                case SharpHook.Native.KeyCode.VcRightControl:
                    this.ControlPressed = keyPressed;
                    break;
            }
        }

        public void EmitChannelDataChanged() => ResetTimerTimeout(this.ChannelDataChangeTimer, _channelDataChangeTimeout);
        public void EmitChannelValueChanged() => ResetTimerTimeout(this.ChannelValueChangeTimer, _channelValueChangeTimeout);
        // public void EmitChannelDataChanged(int channelIndex) => this.ChannelDataChanged?.Invoke(this, channelIndex);
        // public void EmitChannelValueChanged(int channelIndex) => this.ChannelValueChanged?.Invoke(this, channelIndex);
        private void ResetTimerTimeout(System.Timers.Timer timer, int timeout)
        {
            if (timer.Enabled)
            {
                // If the timer is already running, extend the timeout to avoid multiple events
                timer.Interval = timeout;
                // Debug.WriteLine($"Timer reset to {timeout} ms");
                return;
            }
            timer.Start();
        }

        public void EmitSelectedButtonPressed() => this.SelectButtonPressed?.Invoke(this, EventArgs.Empty);

        public void EmitSelectModeChanged(SelectButtonMode sm) => this.SelectModeChanged?.Invoke(this, sm);

        public void EmitSelectButtonCustomModeChanged(SelectButtonCustomParams cp) => this.SelectButtonCustomModeChanged?.Invoke(this, cp);
        
        public void EmitFaderModeChanged(FaderMode fm) => this.FaderModeChanged?.Invoke(this, fm);

        public void EmitPropertySelectionChanged(ChannelProperty.PropertyType pt) => this.PropertySelectionChanged?.Invoke(this, pt);

        public void EmitUserButtonMenuActivated(UserButtonMenuParams ubmp) => this.UserButtonMenuActivated?.Invoke(this, ubmp);

        public void EmitChannelActiveChanged(ChannelActiveParams cap) => this.ChannelActiveCanged?.Invoke(this, cap);

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
//			if (TryGetPluginSetting("LoupedeckMidiIn", out loupedeckMidiInName))
//                LoupedeckMidiInName = loupedeckMidiInName;
            this.S1MidiInName = "Loupedeck S1 Out";

//            if (TryGetPluginSetting("MidiOut", out midiOutName))
//				MidiOutName = midiOutName;

//			if (TryGetPluginSetting("LoupedeckMidiOut", out loupedeckMidiOutName))
//				LoupedeckMidiOutName = loupedeckMidiOutName;
            this.S1MidiOutName = "Loupedeck S1 In";

            this.ConfigMidiInName = "Loupedeck Config In";
            this.ConfigMidiOutName = "Loupedeck Config Out";

            PlugSettingsFinder.Init();
        }

        private void OnConfigMidiEvent(object? sender, MidiEventReceivedEventArgs args)
        {
            MidiEvent e = args.Event;
            if (e is NoteOnEvent)
            {
                var ce = (NoteOnEvent)e;
                if (ce.Channel == 15 && ce.Velocity > 0)
                {
                    switch (ce.NoteNumber)
                    {
                        case 0x10:      // Reload plugin settings from XML file
                            PlugSettingsFinder.ReadDictFromXmlConfig();
                            LabelImageLoader.ClearCache();
                            PluginSettingsReloaded?.Invoke(this, EventArgs.Empty);
                            break;
                        case 0x11:      // Loopback test
                            if (this.ConfigMidiOut != null)
                            {
                                this.ConfigMidiOut.SendEvent(new NoteOnEvent
                                { 
                                    Channel = (FourBitNumber)15, 
                                    NoteNumber = (SevenBitNumber)0x11, 
                                    Velocity = (SevenBitNumber)127 
                                });
                            }
                            break;
                        case 0x12:      // Activate dial steps detector
                            _dialStepsDetector.Activate();
                            break;
                    }
                }
            }
        }

        private void OnS1MidiEvent(object? sender, MidiEventReceivedEventArgs args)
        {
            MidiEvent e = args.Event;

            // PitchBend -> faders & user button values
            if (e is PitchBendEvent)
            {
                var pbe = (PitchBendEvent)e;

                if (pbe.Channel < ChannelCount + 2)
                {
                    // Faders for 6 channels and vol + pan for selected channel
                    
                    if (!this.channelData.TryGetValue(((Int32)pbe.Channel).ToString(), out ChannelData? cd))
                    {
                        return;
                    }

                    cd.Value = pbe.PitchValue / 16383.0f;

                    this.EmitChannelValueChanged();
                }
            }
            // Note event -> toggle settings
            else if (e is NoteOnEvent)
            {
                var ce = (NoteOnEvent)e;
                ChannelProperty.PropertyType eventType = ChannelProperty.PropertyType.Select;
                var eventTypeFound = false;

                foreach (ChannelProperty.PropertyType bt in Enum.GetValues(typeof(ChannelProperty.PropertyType)))
                {
                    if (ce.NoteNumber >= ChannelProperty.MidiBaseNote[(Int32)bt] && ce.NoteNumber < (ChannelProperty.MidiBaseNote[(Int32)bt] + ChannelCount + 1))
                    {
                        eventType = bt;
                        eventTypeFound = true;
                        break;
                    }
                }

                if (eventTypeFound)
                {
                    var channelIndex = ce.NoteNumber - ChannelProperty.MidiBaseNote[(Int32)eventType];

                    if (!this.channelData.TryGetValue(channelIndex.ToString(), out ChannelData? cd))
                        return;

                    cd.BoolProperty[(Int32)eventType] = ce.Velocity > 0;

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
                        if (this.channelData.TryGetValue(ubp.channelIndex.ToString(), out ChannelData? cd))
                        {
                            if (cd.UserValue != ubp.userValue)
                            {
                                cd.UserValue = ubp.userValue;
                                this.EmitChannelDataChanged();
                            }
                            ubp.userLabel = cd.UserLabel;
                        }
                        UserButtonChanged?.Invoke(this, ubp);
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
                            AutomationModeChanged?.Invoke(this, am);
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
                        CommandNoteReceived?.Invoke(this, ce);
                        this.RecPreModeChanged?.Invoke(this, this.CurrentRecPreMode);
                    }
                    else if (ce.NoteNumber >= UserPageMidiBase && ce.NoteNumber < UserPageMidiBase + MaxUserPages)
                    {
                        // User page changed

                        this.UserModeActivated[ce.NoteNumber - UserPageMidiBase] = ce.Velocity > 0;

                        var userPage = 0;
                        for (var i = MaxUserPages - 1; i >= 0; i--)
                        {
                            if (this.UserModeActivated[i])
                            {
                                userPage = i + 1;
                                break;
                            }
                        }

                        if (userPage != this.CurrentUserPage)
                        {
                            this.CurrentUserPage = userPage;
                            UserPageChanged?.Invoke(this, userPage);
                        }
                    }
                    else
                    {
                        CommandNoteReceived?.Invoke(this, ce);
                    }
                }
                else if (ce.Channel >= 14)
                {
                    OneWayCommandNoteReceived?.Invoke(this, ce);
                }
            }
            else if (e is ControlChangeEvent)  // controllers -> numeric arguments
            {
                var ce = (ControlChangeEvent)e;

                if (ce.ControlNumber == 0x60)
                {
                    ActiveUserPagesReceived?.Invoke(this, ce.ControlValue);
                }
            }
            else if (e is NormalSysExEvent)  // SysEx -> text for labels etc.
            {
                var ce = (NormalSysExEvent)e;
                if (ce.Data.Length < 5)
                    return;

                // Check if this is a SysEx command we know about
                byte[] mackieControlPrefix = { 0x00, 0x00, 0x66 };
                if (!ce.Data.SubArray(0, mackieControlPrefix.Length).SequenceEqual(mackieControlPrefix))
                    return;

                if (ce.Data.Length > 6 && ce.Data[4] == 0x12)
                {
                    byte offset = ce.Data[5];
                    byte[] str = ce.Data.SubArray(6, ce.Data.Length - 7);

                    var receivedString = Encoding.UTF8.GetString(str, 0, str.Length);
                    var channelIndex = offset / 4;

                    if (!this.channelData.TryGetValue(channelIndex.ToString(), out ChannelData? cd))
                        return;

                    switch (offset % 4)
                    {
                        case 0: // Label
                            cd.Label = receivedString;
                            this.EmitChannelDataChanged();
                            break;
                        case 1: // Value
                            cd.ValueStr = receivedString;
                            this.EmitChannelValueChanged();
                            break;
                        case 2: // Description
                            cd.Description = receivedString;
                            this.EmitChannelDataChanged();
                            break;
                        case 3: // User Button Label
                            cd.UserLabel = receivedString;
                            var ubp = new UserButtonParams();
                            ubp.channelIndex = channelIndex;
                            ubp.userValue = cd.UserValue;
                            ubp.userLabel = cd.UserLabel;
                            UserButtonChanged?.Invoke(this, ubp);
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
                    FunctionKeyChanged?.Invoke(this, fke);
                }
            }
        }

        public void SendMidiNote(Int32 midiChannel, Int32 note, Int32 velocity = 127)
        {
            if (this.S1MidiOut == null) throw new NullReferenceException("LoupedeckMidiOut is not initialized.");

            var e = new NoteOnEvent();
            e.Channel = (FourBitNumber)midiChannel;
            e.Velocity = (SevenBitNumber)velocity;
            e.NoteNumber = (SevenBitNumber)note;
            this.S1MidiOut.SendEvent(e);
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
