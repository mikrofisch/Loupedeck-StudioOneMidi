namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;

    class CommandButton : StudioOneButton<CommandButtonData>
	{
		public CommandButton()
		{
            this.AddButton(new CommandButtonData(0x5E, 0x5D, "Play", "play"), "Transport");   // 1st click - play, 2nd click - stop
            this.AddButton(new CommandButtonData(0x5D, "Stop", "stop"), "Transport");
            this.AddButton(new CommandButtonData(0x5F, "Record", "record"), "Transport");
            this.AddButton(new CommandButtonData(0x5C, "Fast forward", "fast_forward"), "Transport");
            this.AddButton(new CommandButtonData(0x5B, "Rewind", "rewind"), "Transport");
            this.AddButton(new CommandButtonData(0x56, "Loop", "loop"), "Transport");
            this.AddButton(new CommandButtonData(0x2E, "Fader Bank Left", "faderBankLeft"));
            this.AddButton(new CommandButtonData(0x2F, "Fader Bank Right", "faderBankRight"));
            this.AddButton(new CommandButtonData(0x30, "Fader Channel Left", "faderChannelLeft"));
            this.AddButton(new CommandButtonData(0x31, "Fader Channel Right", "faderChannelRight"));
            this.AddButton(new CommandButtonData(0x20, "TRACK"));
            this.AddButton(new CommandButtonData(0x29, "SEND"));
            this.AddButton(new CommandButtonData(0x2A, "VOL/PAN"));
            this.AddButton(new CommandButtonData(0x33, "GLOBAL", new BitmapColor(60, 60, 20), BitmapColor.White));
            this.AddButton(new CommandButtonData(0x40, "AUDIO"));
            this.AddButton(new CommandButtonData(0x42, "FX"));
            this.AddButton(new CommandButtonData(0x43, "BUS"));
            this.AddButton(new CommandButtonData(0x44, "OUT"));
            this.AddButton(new FlipPanVolCommandButtonData(0x32));
            this.AddButton(new CommandButtonData(0x2B, "PLUGIN"));
            this.AddButton(new OneWayCommandButtonData(0x00, "Console"), "View");
            this.AddButton(new OneWayCommandButtonData(0x01, "Browser"), "View");
            this.AddButton(new OneWayCommandButtonData(0x02, "Editor"), "View");
            this.AddButton(new OneWayCommandButtonData(0x03, "Fullscreen"), "View");
            this.AddButton(new OneWayCommandButtonData(0x04, "Inspector"), "View");
            this.AddButton(new OneWayCommandButtonData(0x05, "Record Panel"), "View");
            this.AddButton(new OneWayCommandButtonData(0x06, "Track List"), "View");
            this.AddButton(new OneWayCommandButtonData(0x07, "Previous Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(0x08, "Next Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(0x09, "Show Groups"), "View");
            this.AddButton(new OneWayCommandButtonData(0x0A, "Floating Window"), "View");
            this.AddButton(new OneWayCommandButtonData(0x0B, "Show Inputs"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x0C, "Show Track"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x0D, "Channel Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x0E, "Instrument Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x0F, "Open Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x10, "Add Insert"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x11, "Add Send"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x12, "Add Bus Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x13, "Add FX Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x14, "Global Mute"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x15, "Global Solo"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x16, "Next Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x17, "Prevous Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x18, "Suspend Group"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x19, "Suspend All Groups"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1A, "Group 1"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1B, "Group 2"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1C, "Group 3"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1D, "Group 4"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1E, "Group 5"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x1F, "Group 6"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x20, "Group 7"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x21, "Group 8"), "Group");
        }

        protected override bool OnLoad()
		{
            base.OnLoad();

            this.plugin.Ch0NoteReceived += (object sender, NoteOnEvent e) =>
            {
                string idx = $"{e.Channel}:{e.NoteNumber}";

                if (!this.buttonData.ContainsKey(idx)) return;

                var bd = this.buttonData[idx];
                bd.Activated = e.Velocity > 0;
                this.ActionImageChanged(idx);
            };

            return true;
		}

        private void AddButton(CommandButtonData bd, string parameterGroup = "Control")
        {
            string idx = $"{bd.midiChannel}:{bd.Code}"; 

			buttonData[idx] = bd;
			AddParameter(idx, bd.Name, parameterGroup);
		}
	}
}
