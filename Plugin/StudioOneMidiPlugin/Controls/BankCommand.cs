namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    class ConfigCommand : PluginDynamicCommand
	{
		public ConfigCommand() : base("Studio One MIDI Settings", "Open Studio One MIDI settings window", "Control")
		{

		}
		protected override void RunCommand(string actionParameter)
		{
			(base.Plugin as StudioOneMidiPlugin).OpenConfigWindow();
		}
	}
}
