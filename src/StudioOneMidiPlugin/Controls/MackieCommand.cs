namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Shapes;

    class MackieCommand : PluginDynamicCommand
	{
		StudioOneMidiPlugin plugin;

		private IDictionary<string, CommandButtonData> buttonData = new Dictionary<string, CommandButtonData>();

		public MackieCommand()
		{
            this.AddButton(new CommandButtonData(0x5E, 0x5D, "Play", "play"));   // 1st click - play, 2nd click - stop
            this.AddButton(new CommandButtonData(0x5D, "Stop", "stop"));
            this.AddButton(new CommandButtonData(0x5F, "Record", "record"));
            this.AddButton(new CommandButtonData(0x5C, "Fast forward", "fast_forward"));
            this.AddButton(new CommandButtonData(0x5B, "Rewind", "rewind"));
            this.AddButton(new CommandButtonData(0x56, "Loop", "loop"));
            this.AddButton(new CommandButtonData(0x2E, "Fader Bank Left", "faderBankLeft"));
            this.AddButton(new CommandButtonData(0x2F, "Fader Bank Right", "faderBankRight"));
            this.AddButton(new CommandButtonData(0x30, "Fader Channel Left", "faderChannelLeft"));
            this.AddButton(new CommandButtonData(0x31, "Fader Channel Right", "faderChannelRight"));
            this.AddButton(new CommandButtonData(0x20, "TRACK"));
            this.AddButton(new CommandButtonData(0x29, "SEND"));
            this.AddButton(new CommandButtonData(0x2A, "VOL/PAN"));
            this.AddButton(new CommandButtonData(0x33, "GLOBAL", new BitmapColor(60, 60, 20)));
            this.AddButton(new CommandButtonData(0x40, "AUDIO"));
            this.AddButton(new CommandButtonData(0x42, "FX"));
            this.AddButton(new CommandButtonData(0x43, "BUS"));
            this.AddButton(new CommandButtonData(0x44, "OUT"));
            this.AddButton(new FlipPanVolCommandButtonData(0x32));
            this.AddButton(new CommandButtonData(0x2B, "PLUGIN"));
        }

        protected override bool OnLoad()
		{
			plugin = base.Plugin as StudioOneMidiPlugin;
			plugin.MackieNoteReceived += this.OnNoteReceived;

            foreach (var bd in this.buttonData.Values)
            {
                bd.OnLoad(plugin);
            }

            return base.OnLoad();
		}

		protected void OnNoteReceived(object sender, NoteOnEvent e)
		{
			string param = e.NoteNumber.ToString();

            if (!this.buttonData.ContainsKey(param))
            {
                return;
            }

			CommandButtonData bd = this.buttonData[param];
			bd.Activated = e.Velocity > 0;
			this.ActionImageChanged(param);
		}


        protected override void RunCommand(string actionParameter)
        {
            if (!buttonData.ContainsKey(actionParameter))
                return;

            buttonData[actionParameter].runCommand();
        }

 		protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == null) return null;
			if (!buttonData.ContainsKey(actionParameter)) return null;

			return buttonData[actionParameter].getImage(imageSize);
		}

		private void AddButton(CommandButtonData bd)
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
			AddParameter(bd.Code.ToString(), bd.Name, "Control");
		}

	}
}
