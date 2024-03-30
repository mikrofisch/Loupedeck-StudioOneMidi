namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

		public const int MackieChannelCount = 6;

		public IDictionary<string, MackieChannelData> mackieChannelData = new Dictionary<string, MackieChannelData>();

        string midiInName, midiOutName, mackieMidiInName, mackieMidiOutName;

        public MackieFader mackieFader;

        public bool isConfigWindowOpen = false;
        
        public event EventHandler MackieDataChanged;
		public event EventHandler<NoteOnEvent> MackieNoteReceived;

        public event EventHandler SelectButtonPressed;
        public event EventHandler<bool> SendModeChanged;

        private System.Timers.Timer mackieDataChangeTimer;
        public bool sendMode;

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
			for (int i = 0; i <= MackieChannelCount; i++)
				mackieChannelData[i.ToString()] = new MackieChannelData(this, i);

			mackieDataChangeTimer = new System.Timers.Timer(10);
			mackieDataChangeTimer.AutoReset = false;
			mackieDataChangeTimer.Elapsed += (object sender, System.Timers.ElapsedEventArgs e) => {
				MackieDataChanged.Invoke(this, null);
			};
        }

        // This method is called when the plugin is loaded during the Loupedeck service start-up.
        public override void Load()
        {
			this.Info.Icon16x16   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("midi_connector_male_16px.png"));
			this.Info.Icon32x32   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("midi_connector_male_32px.png"));
			this.Info.Icon48x48   = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("midi_connector_male_48px.png"));
			this.Info.Icon256x256 = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("midi_connector_male_96px.png"));

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

		public void EmitMackieChannelDataChanged(MackieChannelData cd)
        {
            mackieDataChangeTimer.Start();
		}

        public void EmitSelectedButtonPressed()
        {
            this.SelectButtonPressed?.Invoke(this, null);
        }

        public void EmitSendModeChanged(bool sm)
        {
            this.SendModeChanged?.Invoke(this, sm );
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

			if (TryGetPluginSetting("MidiIn", out midiInName))
				MidiInName = midiInName;

//			if (TryGetPluginSetting("MackieMidiIn", out mackieMidiInName))
//                MackieMidiInName = mackieMidiInName;
            MackieMidiInName = "Loupedeck S1 In";

            if (TryGetPluginSetting("MidiOut", out midiOutName))
				MidiOutName = midiOutName;

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
				EmitMackieChannelDataChanged(cd);
			}

			// Note event -> solo/mute/...
			else if (e is NoteOnEvent)
            {
                var ce = e as NoteOnEvent;
                ChannelProperty.BoolType eventType = ChannelProperty.BoolType.Select;
                bool eventTypeFound = false;

                foreach (ChannelProperty.BoolType bt in Enum.GetValues(typeof(ChannelProperty.BoolType)))
                {
                    if (ce.NoteNumber >= ChannelProperty.boolPropertyMackieNote[(int)bt] && ce.NoteNumber < (ChannelProperty.boolPropertyMackieNote[(int)bt] + MackieChannelCount +1))
                    {
                        eventType = bt;
                        eventTypeFound = true;
                        break;
                    }
                }

                if (eventTypeFound)
                {
                    var channelIndex = ce.NoteNumber - ChannelProperty.boolPropertyMackieNote[(int)eventType];

                    if (!mackieChannelData.TryGetValue(channelIndex.ToString(), out MackieChannelData cd))
                        return;

                    cd.BoolProperty[(int)eventType] = ce.Velocity > 0;
                    EmitMackieChannelDataChanged(cd);
                }
                else
                {
                    MackieNoteReceived.Invoke(this, ce);
                }
            }
            else if (e is NormalSysExEvent)
            {
				var ce = e as NormalSysExEvent;
				if (ce.Data.Length < 5) return;

				// Check if this is mackie control command
				byte[] mackieControlPrefix = { 0x00, 0x00, 0x66 };
				if (!ce.Data.SubArray(0, mackieControlPrefix.Length).SequenceEqual(mackieControlPrefix)) return;

				// LCD command
				if (ce.Data.Length > 6 && ce.Data[4] == 0x12)
                {
					byte offset = ce.Data[5];
					byte[] str = ce.Data.SubArray(6, ce.Data.Length - 7);

                    var receivedString = Encoding.UTF8.GetString(str, 0, str.Length); //.Replace("\0", "");
                    var displayIndex = offset / 3;

                    if (!mackieChannelData.TryGetValue(displayIndex.ToString(), out MackieChannelData cd))
                        return;

                    switch (offset % 3)
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
                    }

                    EmitMackieChannelDataChanged(cd);
				}
			}
		}

		public override bool TryProcessTouchEvent(string actionName, string actionParameter, DeviceTouchEvent deviceTouchEvent)
        {
            if (actionName == mackieFader.GetResetActionName())
            {
                return mackieFader.TryProcessTouchEvent(actionParameter, deviceTouchEvent);
            }

			return base.TryProcessTouchEvent(actionName, actionParameter, deviceTouchEvent);
		}

    }
}
