﻿namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Core;

    class CommandButton : StudioOneButton<CommandButtonData>
	{
		public CommandButton()
		{
            this.AddButton(new CommandButtonData(0x5E, 0x5D, "Play", "play"), "Transport");   // 1st click - play, 2nd click - stop
            this.AddButton(new CommandButtonData(0x5D, "Stop", "stop"), "Transport");
            this.AddButton(new CommandButtonData(0x5F, "Record", "record"), "Transport");
            this.AddButton(new CommandButtonData(0x5C, "Fast forward", "fast_forward"), "Transport");
            this.AddButton(new CommandButtonData(0x5B, "Rewind", "rewind"), "Transport");
            this.AddButton(new CommandButtonData(0x55, "Loop", "loop"), "Transport");
            this.AddButton(new CommandButtonData(0x59, "Click", "click"), "Settings");
            this.AddButton(new OneWayCommandButtonData(0x00, "Console"), "View");
            this.AddButton(new OneWayCommandButtonData(0x01, "Browser"), "View");
            this.AddButton(new OneWayCommandButtonData(0x02, "Editor"), "View");
            this.AddButton(new OneWayCommandButtonData(0x03, "Fullscreen"), "View");
            this.AddButton(new OneWayCommandButtonData(0x04, "Inspector"), "View");
            this.AddButton(new OneWayCommandButtonData(0x05, "Record Panel", "rec_panel"), "View");
            this.AddButton(new OneWayCommandButtonData(0x06, "Track List"), "View");
            this.AddButton(new OneWayCommandButtonData(0x07, "Previous Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(0x08, "Next Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData(0x09, "Show Groups", "show_groups"), "View");
            this.AddButton(new OneWayCommandButtonData(0x0A, "Floating Window"), "View");
            this.AddButton(new OneWayCommandButtonData(0x10, "Show Inputs", "show_inputs"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x11, "Show Track"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x12, "Channel Editor", "channel_editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x13, "Instrument Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x14, "Open Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x15, "Add Insert"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x16, "Add Send"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x17, "Add Bus Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x18, "Add FX Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x19, "Global Mute"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x1A, "Global Solo"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x1B, "Next Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x1C, "Previous Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(0x20, "Suspend Group"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x21, "Suspend All Groups"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x22, "Group 1"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x23, "Group 2"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x24, "Group 3"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x25, "Group 4"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x26, "Group 5"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x27, "Group 6"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x28, "Group 7"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x29, "Group 8"), "Group");
            this.AddButton(new OneWayCommandButtonData(0x30, "Previous Layer", "layer_up"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x31, "Next Layer", "layer_down"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x32, "Add Layer", "layer_add"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x33, "Remove Layer", "layer_remove"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x34, "Expand Layers", "layers_expand"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x35, "Rename Layer"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x36, "Group Selected Tracks"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x37, "Dissolve Group"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x38, "Show Envelopes"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x39, "Show in Console"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3A, "Expand Folder Track"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3B, "Collapse All Tracks"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3C, "Add Tracks"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3D, "Add Automation Track"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3E, "Add Bus for Selected Channels"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x3F, "Arm All Audio Tracks"), "Track");
            this.AddButton(new OneWayCommandButtonData(0x40, "Return to Zero", "return_to_zero"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x41, "Return to Start on Stop"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x42, "Enable Play Start Marker"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x43, "Set Play Start Marker", "playmarker"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x44, "Loop Selection"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x45, "Loop Selection Snapped"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x46, "Play from Loop Start"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x47, "Loop Follows Selection"), "Transport");
            this.AddButton(new OneWayCommandButtonData(0x48, "Tap Tempo"), "Transport");
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