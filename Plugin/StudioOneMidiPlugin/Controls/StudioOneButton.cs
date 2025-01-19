namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System.Collections.Generic;

    internal class StudioOneButton<B> : PluginDynamicCommand where B : ButtonData
    {
        protected StudioOneMidiPlugin plugin;
        protected Dictionary<String, B> buttonData = new Dictionary<String, B>();

        private readonly System.Timers.Timer ActionImageChangedTimer;

        public StudioOneButton()
        {
            this.ActionImageChangedTimer = new System.Timers.Timer(100);
            this.ActionImageChangedTimer.AutoReset = false;
            this.ActionImageChangedTimer.Elapsed += (Object sender, System.Timers.ElapsedEventArgs e) =>
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

        }

        protected override bool OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            foreach (var bd in this.buttonData.Values)
            {
                bd?.OnLoad(this.plugin);
            }
            return base.OnLoad();
        }

        protected void UpdateAllActionImages() => this.ActionImageChangedTimer.Start();

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null) return null;
            if (!this.buttonData.ContainsKey(actionParameter)) return null;

            return this.buttonData[actionParameter].getImage(imageSize);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.buttonData.ContainsKey(actionParameter))
            {
                this.buttonData[actionParameter].runCommand();
            }
        }
    }
}
