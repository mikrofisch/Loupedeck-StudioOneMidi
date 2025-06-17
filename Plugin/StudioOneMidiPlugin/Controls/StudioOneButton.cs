namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;

    internal class StudioOneButton<B> : PluginDynamicCommand where B : ButtonData
    {
        protected StudioOneMidiPlugin? _plugin;
        protected ConcurrentDictionary<String, B?> _buttonData = new();

        public StudioOneButton() : base()
        {
            this.IsWidget = true;
        }

        protected override bool OnLoad()
        {
            this._plugin = (StudioOneMidiPlugin)base.Plugin;

            foreach (var bd in this._buttonData.Values)
            {
                bd?.OnLoad(this._plugin);
            }
            return base.OnLoad();
        }

        protected void UpdateAllActionImages()
        {
            foreach (var k in this._buttonData.Keys)
            {
                this.ActionImageChanged(k);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (!this._buttonData.ContainsKey(actionParameter)) throw new InvalidOperationException("Uninitialised ButtonData");

            var bd = this._buttonData[actionParameter];
            if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

            return bd.getImage(imageSize);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this._buttonData.ContainsKey(actionParameter))
            {
                var bd = this._buttonData[actionParameter];
                if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

                bd.runCommand();
            }
        }
    }
}
