namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System.Collections.Concurrent;

    internal class StudioOneButton<B> : PluginDynamicCommand where B : ButtonData
    {
        protected StudioOneMidiPlugin? plugin;
        protected ConcurrentDictionary<String, B?> buttonData = new ConcurrentDictionary<String, B?>();

        private readonly System.Timers.Timer ActionImageChangedTimer;

        public StudioOneButton() : base()
        {
            this.ActionImageChangedTimer = new System.Timers.Timer(100);
            this.ActionImageChangedTimer.AutoReset = false;
            this.ActionImageChangedTimer.Elapsed += (Object? sender, System.Timers.ElapsedEventArgs e) =>
            {
                // Debug.WriteLine("ActionImageChangeTimer.Elapsed " + this.Name);

                // As of version 6.0.2 of the Loupedeck software ActionImageChanged() requires the
                // actionParameter argument in order to have an effect when used in PluginDynamicCommand.
                //
                foreach (var k in this.buttonData.Keys)
                {
                    this.ActionImageChanged($"{k}");
                }
            };

            this.IsWidget = true;
        }

        protected override bool OnLoad()
        {
            this.plugin = (StudioOneMidiPlugin)base.Plugin;

            foreach (var bd in this.buttonData.Values)
            {
                bd?.OnLoad(this.plugin);
            }
            return base.OnLoad();
        }

        protected void UpdateAllActionImages() => this.ActionImageChangedTimer.Start();

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (!this.buttonData.ContainsKey(actionParameter)) throw new InvalidOperationException("Uninitialised ButtonData");

            var bd = this.buttonData[actionParameter];
            if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

            return bd.getImage(imageSize);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.buttonData.ContainsKey(actionParameter))
            {
                var bd = this.buttonData[actionParameter];
                if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

                bd.runCommand();
            }
        }
    }
}
