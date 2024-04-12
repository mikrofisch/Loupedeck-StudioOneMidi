namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class StudioOneButton<B> : PluginDynamicCommand where B : ButtonData
    {
        protected StudioOneMidiPlugin plugin;

        protected IDictionary<string, B> buttonData = new Dictionary<string, B>();

        protected override bool OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            foreach (var bd in this.buttonData.Values)
            {
                bd.OnLoad(this.plugin);
            }
            return base.OnLoad();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null) return null;
            if (!this.buttonData.ContainsKey(actionParameter)) return null;

            Debug.WriteLine("StudioOneButton.GetCommandImage: " + actionParameter);

            return this.buttonData[actionParameter].getImage(imageSize);
        }

        protected override void RunCommand(string actionParameter)
        {
            if (!this.buttonData.ContainsKey(actionParameter)) return;

            this.buttonData[actionParameter].runCommand();
        }
    }
}
