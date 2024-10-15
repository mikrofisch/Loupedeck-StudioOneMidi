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

    // This class can be used to connect the Loupedeck plugin to an application.

    public class StudioOneMidiApplication : ClientApplication
    {
        public StudioOneMidiApplication()
        {
        }

        // This method can be used to link the plugin to a Windows application.
        protected override String GetProcessName() => "";

        // This method can be used to link the plugin to a macOS application.
        protected override String GetBundleName() => "";

        // This method can be used to check whether the application is installed or not.
        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
