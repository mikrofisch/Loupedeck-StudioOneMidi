namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using Melanchall.DryWetMidi.Core;
    class MackieSelectedChannelBoolPropertyCommand : PluginDynamicCommand
	{

		StudioOneMidiPlugin plugin;

        private IDictionary<string, PropertyButtonData> buttonData = new Dictionary<string, PropertyButtonData>();

        public MackieSelectedChannelBoolPropertyCommand()
		{
			this.Description = "Control for currently selected channel";

			for (int i = 0; i <= StudioOneMidiPlugin.MackieChannelCount; i++)
			{
                if (i < StudioOneMidiPlugin.MackieChannelCount)
                {
                    this.AddButton(i, ChannelProperty.BoolType.Select, "Select");
                }

                this.AddButton(i, ChannelProperty.BoolType.Mute, "Mute");
                this.AddButton(i, ChannelProperty.BoolType.Solo, "Solo");
                this.AddButton(i, ChannelProperty.BoolType.Arm, "Arm/Rec", "record");
                this.AddButton(i, ChannelProperty.BoolType.Monitor, "Monitor", "monitor");
            }
        }

		protected override bool OnLoad()
		{
			plugin = base.Plugin as StudioOneMidiPlugin;

            foreach (var bd in this.buttonData.Values)
            {
                bd.OnLoad(plugin);
            }

            plugin.MackieNoteReceived += (object sender, NoteOnEvent e) => {
                if (e.NoteNumber >= SelectButtonData.UserButtonMidiBase &&
                    e.NoteNumber <= SelectButtonData.UserButtonMidiBase + StudioOneMidiPlugin.MackieChannelCount)
                {
                    var bd = this.buttonData[$"{e.NoteNumber - SelectButtonData.UserButtonMidiBase}:{(int)ChannelProperty.BoolType.Select}"] as SelectButtonData;
                    bd.userButtonChanged(e.Velocity > 0);
                    ActionImageChanged();
                }
            };

            plugin.MackieDataChanged += (object sender, EventArgs e) => {
				ActionImageChanged();
			};

            plugin.SelectModeChanged += (object sender, SelectButtonData.Mode e) =>
            {
                for (int i = 0; i < StudioOneMidiPlugin.MackieChannelCount; i++)
                {
                    var bd = this.buttonData[$"{i}:{(int)ChannelProperty.BoolType.Select}"] as SelectButtonData;
                    bd.selectModeChanged(e);
                }
                ActionImageChanged();
            };

			return true;
		}

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null) return null;

            return this.buttonData[actionParameter].getImage(imageSize);
		}

		protected override void RunCommand(string actionParameter)
		{
            //			if (plugin.mackieMidiOut == null)
            //			{
            //				plugin.OpenConfigWindow();
            //				return;
            //			}

            this.buttonData[actionParameter].runCommand();
		}

        private void AddButton(int i, ChannelProperty.BoolType bt, string name, string iconName = null)
        {
            string chstr = i == StudioOneMidiPlugin.MackieChannelCount ? " (Selected channel)" : $" (CH {i + 1})";

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
            buttonData[idx] = bd;
            AddParameter(idx, name + chstr, name);
        }


    }
}
