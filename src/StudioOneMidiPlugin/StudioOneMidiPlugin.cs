namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Eventing.Reader;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;

    using Loupedeck.StudioOneMidiPlugin.Controls;

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

		public const int ChannelCount = 6;

		public IDictionary<string, MackieChannelData> mackieChannelData = new Dictionary<string, MackieChannelData>();

        string midiInName, midiOutName, mackieMidiInName, mackieMidiOutName;

        public ChannelFader channelFader;

        public bool isConfigWindowOpen = false;

        public event EventHandler ChannelDataChanged;
        public event EventHandler<NoteOnEvent> Ch0NoteReceived;
        public event EventHandler<NoteOnEvent> Ch1NoteReceived;
        public event EventHandler SelectButtonPressed;
        public event EventHandler<string> FocusDeviceChanged;

        public enum SelectButtonMode
        {
            Select,
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

        public const int UserButtonMidiBase = 0x6C;
        public class UserButtonParams
        {
            public int channelIndex { get; set; }
            public bool isActive { get; set; }
        }
        public event EventHandler<UserButtonParams> UserButtonChanged;

        private System.Timers.Timer ChannelDataChangeTimer;

        public string MidiInName
        {
			get => midiInName;
			set {
				if (midiIn != null) {
					midiIn.StopEventsListening();
					midiIn.Dispose();
				}

				midiInName = value;
				try {
					midiIn = InputDevice.GetByName(value);
					midiIn.StartEventsListening();
					SetPluginSetting("MidiIn", value, false);
				}
				catch (Exception) {
					midiIn = null;
				}
			}
		}

		public string MidiOutName
        {
			get => midiOutName;
			set {
				if (midiOut != null) {
					midiOut.Dispose();
				}

				midiOutName = value;
				try {
					midiOut = OutputDevice.GetByName(value);
					SetPluginSetting("MidiOut", value, false);
				}
				catch (Exception) {
					midiOut = null;
				}
			}
		}

		public string MackieMidiInName
        {
			get => mackieMidiInName;
			set {
				if (mackieMidiIn != null) {
					mackieMidiIn.StopEventsListening();
					mackieMidiIn.Dispose();
				}

				mackieMidiInName = value;
				try {
					mackieMidiIn = InputDevice.GetByName(value);
					mackieMidiIn.EventReceived += OnMackieMidiEvent;
					mackieMidiIn.StartEventsListening();
					SetPluginSetting("MackieMidiIn", value, false);
				}
				catch (Exception) {
					mackieMidiIn = null;
				}
			}
		}

		public string MackieMidiOutName
        {
			get => mackieMidiOutName;
			set {
				if (mackieMidiOut != null)
                {
					mackieMidiOut.Dispose();
				}

				mackieMidiOutName = value;
				try
                {
					mackieMidiOut = OutputDevice.GetByName(value);
					SetPluginSetting("MackieMidiOut", value, false);
				}
				catch (Exception)
                {
					mackieMidiOut = null;
				}
			}
		}

        // Initializes a new instance of the plugin class.
        public StudioOneMidiPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);

            // Create the channel data objects (one object for each bank channel, plus one for the selected channel).
            for (int i = 0; i <= ChannelCount; i++)
            {
                mackieChannelData[i.ToString()] = new MackieChannelData(this, i);
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

			LoadSettings();
        }

        // This method is called when the plugin is unloaded during the Loupedeck service shutdown.
        public override void Unload()
        {
        }

		public void OpenConfigWindow()
        {
			if (isConfigWindowOpen)
				return;

			Thread t = new Thread(() => {
				ConfigWindow w = new ConfigWindow(this);
				w.Closed += (_, _) => isConfigWindowOpen = false;
				w.Show();
				System.Windows.Threading.Dispatcher.Run();
			});

			t.SetApartmentState(ApartmentState.STA);
			t.Start();

			isConfigWindowOpen = true;
		}

		public void EmitChannelDataChanged()
        {
            ChannelDataChangeTimer.Start();
		}

        public void EmitSelectedButtonPressed()
        {
            this.SelectButtonPressed?.Invoke(this, null);
        }

        public void EmitSelectModeChanged(SelectButtonMode sm)
        {
            this.SelectModeChanged?.Invoke(this, sm);
        }
        
        public void EmitFaderModeChanged(FaderMode fm)
        {
            this.FaderModeChanged?.Invoke(this, fm);
        }

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
            MackieMidiInName = "Loupedeck S1 In";

//            if (TryGetPluginSetting("MidiOut", out midiOutName))
//				MidiOutName = midiOutName;

//			if (TryGetPluginSetting("MackieMidiOut", out mackieMidiOutName))
//				MackieMidiOutName = mackieMidiOutName;
            MackieMidiOutName = "Loupedeck S1 Out";
        }

        private void OnMackieMidiEvent(object sender, MidiEventReceivedEventArgs args)
        {
            MidiEvent e = args.Event;
            // PitchBend -> volume
            if (e is PitchBendEvent)
            {
                if (!mackieChannelData.TryGetValue(((int)(e as ChannelEvent).Channel).ToString(), out MackieChannelData cd))
                    return;

                var ce = e as PitchBendEvent;
                cd.Value = ce.PitchValue / 16383.0f;
                this.EmitChannelDataChanged();
            }
            // Note event -> solo/mute/...
            else if (e is NoteOnEvent)
            {
                var ce = e as NoteOnEvent;
                ChannelProperty.PropertyType eventType = ChannelProperty.PropertyType.Select;
                bool eventTypeFound = false;

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

                    if (!mackieChannelData.TryGetValue(channelIndex.ToString(), out MackieChannelData cd))
                        return;

                    cd.BoolProperty[(int)eventType] = ce.Velocity > 0;
                    this.EmitChannelDataChanged();
                }
                else if (ce.Channel == 0)
                {
                    if (ce.NoteNumber >= UserButtonMidiBase && ce.NoteNumber < (UserButtonMidiBase + ChannelCount))
                    {
                        var ubp = new UserButtonParams();
                        ubp.channelIndex = ce.NoteNumber - UserButtonMidiBase;
                        ubp.isActive = ce.Velocity > 0;
                        UserButtonChanged.Invoke(this, ubp);
                    }
                    else
                    {
                        Ch0NoteReceived.Invoke(this, ce);
                    }
                }
                else if (ce.Channel == 1)
                {
                    Ch1NoteReceived.Invoke(this, ce);
                }
            }
            else if (e is NormalSysExEvent)
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

                    if (!mackieChannelData.TryGetValue(displayIndex.ToString(), out MackieChannelData cd))
                        return;

                    switch (offset % 4)
                    {
                        case 0: // Label
                            cd.Label = receivedString;
                            break;
                        case 1: // Value
                            cd.ValueStr = receivedString;
                            break;
                        case 2: // Description
                            cd.Description = receivedString;
                            break;
                        case 3: // User Button Label
                            cd.UserLabel = receivedString;
                            break;
                    }

                    this.EmitChannelDataChanged();
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

		public override bool TryProcessTouchEvent(string actionName, string actionParameter, DeviceTouchEvent deviceTouchEvent)
        {
            if (actionName == channelFader.GetResetActionName())
            {
                // return channelFader.TryProcessTouchEvent(actionParameter, deviceTouchEvent);
            }

			return base.TryProcessTouchEvent(actionName, actionParameter, deviceTouchEvent);
		}
    }
}
