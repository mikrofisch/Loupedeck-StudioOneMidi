namespace Loupedeck.StudioOneMidiPlugin
{
    using System;

    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    public class ChannelData : EventArgs
	{

		public Int32 ChannelID;
		public Single Value = 0;
		public String Label = "";
        public String ValueStr = "";
        public String Description = "";
        public String UserLabel = "";
        public Int32 UserValue = 0;

        public Boolean[] BoolProperty = new Boolean[(Int32)Enum.GetNames(typeof(ChannelProperty.PropertyType)).Length];

		public Boolean IsMasterChannel = false;

		private readonly StudioOneMidiPlugin plugin;


        public Boolean Selected
        {
            get => this.BoolProperty[(Int32)ChannelProperty.PropertyType.Select];
            set
            {
                this.BoolProperty[(Int32)ChannelProperty.PropertyType.Select] = value;
            }
        }

        public Boolean Muted
        {
            get => this.BoolProperty[(Int32)ChannelProperty.PropertyType.Mute];
            set
            {
                this.BoolProperty[(Int32)ChannelProperty.PropertyType.Mute] = value;
            }
        }

        public Boolean Armed
		{
			get => this.BoolProperty[(Int32)ChannelProperty.PropertyType.Arm];
			set
			{
				this.BoolProperty[(Int32)ChannelProperty.PropertyType.Arm] = value;
			}
		}

		public Boolean Solo
		{
			get => this.BoolProperty[(Int32)ChannelProperty.PropertyType.Solo];
			set
			{
				this.BoolProperty[(Int32)ChannelProperty.PropertyType.Solo] = value;
			}
		}

        public Boolean Monitor
        {
            get => this.BoolProperty[(Int32)ChannelProperty.PropertyType.Monitor];
            set
            {
                this.BoolProperty[(Int32)ChannelProperty.PropertyType.Monitor] = value;
            }
        }
        
        public ChannelData(StudioOneMidiPlugin plugin, Int32 channelID)
		{
			this.plugin = plugin;

			this.ChannelID = channelID;
    		this.Label = $"Channel {channelID + 1}";
            if (channelID == StudioOneMidiPlugin.ChannelCount)
            {
                this.Label = "Selected Channel";
            }
		}

		public void EmitVolumeUpdate()
		{
			var e = new PitchBendEvent();
			e.PitchValue = (UInt16)(this.Value * 16383);
			e.Channel = (FourBitNumber)this.ChannelID;
			this.plugin.mackieMidiOut.SendEvent(e);

//			this.plugin.EmitChannelDataChanged();
		}

        public void EmitValueReset()
        {
            this.plugin.SendMidiNote(0, 0x20 + this.ChannelID);
        }

        public void EmitChannelPropertyPress(ChannelProperty.PropertyType type)
		{
			var e = new NoteOnEvent();
			e.NoteNumber = (SevenBitNumber)(ChannelProperty.MidiBaseNote[(Int32)type] + this.ChannelID);
			e.Velocity = (SevenBitNumber)127;
			this.plugin.mackieMidiOut.SendEvent(e);

			var e2 = new NoteOffEvent();
			e2.NoteNumber = e.NoteNumber;
			e2.Velocity = e.Velocity;
			this.plugin.mackieMidiOut.SendEvent(e2);
		}

	}
}
