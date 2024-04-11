namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;

    class MackieCommand : LoupedeckButton<CommandButtonData>
	{
		public MackieCommand()
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
            this.AddButton(new OneWayCommandButtonData( 0, "Console"), "View");
            this.AddButton(new OneWayCommandButtonData( 1, "Browser"), "View");
            this.AddButton(new OneWayCommandButtonData( 2, "Editor"), "View");
            this.AddButton(new OneWayCommandButtonData( 3, "Fullscreen"), "View");
            this.AddButton(new OneWayCommandButtonData( 4, "Inspector"), "View");
            this.AddButton(new OneWayCommandButtonData( 5, "Record Panel"), "View");
            this.AddButton(new OneWayCommandButtonData( 6, "Track List"), "View");
            this.AddButton(new OneWayCommandButtonData( 7, "Previous Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData( 8, "Next Perspective"), "View");
            this.AddButton(new OneWayCommandButtonData( 9, "Show Groups"), "View");
            this.AddButton(new OneWayCommandButtonData(10, "Floating Window"), "View");
            this.AddButton(new OneWayCommandButtonData(11, "Show Inputs"), "Console");
            this.AddButton(new OneWayCommandButtonData(12, "Show Track"), "Console");
            this.AddButton(new OneWayCommandButtonData(13, "Channel Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(14, "Instrument Editor"), "Console");
            this.AddButton(new OneWayCommandButtonData(15, "Open Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(16, "Add Insert"), "Console");
            this.AddButton(new OneWayCommandButtonData(17, "Add Send"), "Console");
            this.AddButton(new OneWayCommandButtonData(18, "Add Bus Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(19, "Add FX Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(20, "Global Mute"), "Console");
            this.AddButton(new OneWayCommandButtonData(21, "Global Solo"), "Console");
            this.AddButton(new OneWayCommandButtonData(22, "Next Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(23, "Prevous Channel"), "Console");
            this.AddButton(new OneWayCommandButtonData(24, "Suspend Group"), "Group");
            this.AddButton(new OneWayCommandButtonData(25, "Suspend All Groups"), "Group");
            this.AddButton(new OneWayCommandButtonData(26, "Group 1"), "Group");
            this.AddButton(new OneWayCommandButtonData(27, "Group 2"), "Group");
            this.AddButton(new OneWayCommandButtonData(28, "Group 3"), "Group");
            this.AddButton(new OneWayCommandButtonData(29, "Group 4"), "Group");
            this.AddButton(new OneWayCommandButtonData(30, "Group 5"), "Group");
            this.AddButton(new OneWayCommandButtonData(31, "Group 6"), "Group");
            this.AddButton(new OneWayCommandButtonData(32, "Group 7"), "Group");
            this.AddButton(new OneWayCommandButtonData(33, "Group 8"), "Group");
        }

        protected override bool OnLoad()
		{
            base.OnLoad();

            this.plugin.MackieNoteReceived += (object sender, NoteOnEvent e) =>
            {
                string param = e.NoteNumber.ToString();

                if (!this.buttonData.ContainsKey(param)) return;

                var bd = this.buttonData[param];
                bd.Activated = e.Velocity > 0;
                this.ActionImageChanged(param);
            };

            return true;
		}

        private void AddButton(CommandButtonData bd, string parameterGroup = "Control")
        {
            if (bd.IconName != null)
            {
                bd.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{bd.IconName}_52px.png"));
                string iconResOn = EmbeddedResources.FindFile($"{bd.IconName}_on_52px.png");
                if (iconResOn != null)
                {
                    bd.IconOn = EmbeddedResources.ReadImage(iconResOn);
                }
            }

			buttonData[bd.Code.ToString()] = bd;
			AddParameter(bd.Code.ToString(), bd.Name, parameterGroup);
		}
	}
}
