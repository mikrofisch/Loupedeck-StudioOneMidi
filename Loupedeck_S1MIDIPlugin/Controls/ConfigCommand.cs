using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Loupedeck.Loupedeck_S1MIDIPlugin.Controls
{
	class ConfigCommand : PluginDynamicCommand
	{

		public ConfigCommand() : base("S1 MIDI Settings", "Open S1 MIDI settings window", "Control") {

		}
		protected override void RunCommand(string actionParameter) {
			(base.Plugin as Loupedeck_S1MIDIPlugin).OpenConfigWindow();
		}

	}
}
