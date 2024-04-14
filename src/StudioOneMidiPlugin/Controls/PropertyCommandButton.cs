namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using Melanchall.DryWetMidi.Core;

    class PropertyCommandButton : StudioOneButton<PropertyButtonData>
	{

        public PropertyCommandButton()
		{
			this.Description = "Control for currently selected channel";

			for (int i = 0; i <= StudioOneMidiPlugin.ChannelCount; i++)
			{
                if (i < StudioOneMidiPlugin.ChannelCount)
                {
                    this.AddButton(i, ChannelProperty.BoolType.Select, "Select");
                }

                this.AddButton(i, ChannelProperty.BoolType.Mute, "Mute");
                this.AddButton(i, ChannelProperty.BoolType.Solo, "Solo");
                this.AddButton(i, ChannelProperty.BoolType.Arm, "Arm/Rec", "arm");
                this.AddButton(i, ChannelProperty.BoolType.Monitor, "Monitor", "monitor");
            }
        }

		protected override bool OnLoad()
		{
            base.OnLoad();

            this.plugin.MackieNoteReceived += (object sender, NoteOnEvent e) => {
                if (e.NoteNumber >= SelectButtonData.UserButtonMidiBase &&
                    e.NoteNumber <= SelectButtonData.UserButtonMidiBase + StudioOneMidiPlugin.ChannelCount)
                {
                    var bd = this.buttonData[$"{e.NoteNumber - SelectButtonData.UserButtonMidiBase}:{(int)ChannelProperty.BoolType.Select}"] as SelectButtonData;
                    bd.userButtonChanged(e.Velocity > 0);
                    this.EmitActionImageChanged();
                }
            };

            this.plugin.ChannelDataChanged += (object sender, EventArgs e) => {
				this.EmitActionImageChanged();
			};

            this.plugin.SelectModeChanged += (object sender, SelectButtonData.Mode e) =>
            {
                for (int i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    var bd = this.buttonData[$"{i}:{(int)ChannelProperty.BoolType.Select}"] as SelectButtonData;
                    bd.selectModeChanged(e);
                }
                this.EmitActionImageChanged();
            };

			return true;
		}

        private void AddButton(int i, ChannelProperty.BoolType bt, string name, string iconName = null)
        {
            string chstr = i == StudioOneMidiPlugin.ChannelCount ? " (Selected channel)" : $" (CH {i + 1})";

            PropertyButtonData bd;

            if (bt == ChannelProperty.BoolType.Select)
            {
                bd = new SelectButtonData(i, bt);
            }
            else
            {
                bd = new PropertyButtonData(i, bt, PropertyButtonData.TrackNameMode.ShowFull, iconName);
            }

            var idx = $"{i}:{(int)bd.Type}";
            this.buttonData[idx] = bd;
            AddParameter(idx, name + chstr, name);
        }
    }
}
