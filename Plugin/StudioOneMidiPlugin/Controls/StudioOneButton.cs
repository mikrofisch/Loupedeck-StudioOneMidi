namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System.Collections.Generic;
    using System.Diagnostics;

    internal class StudioOneButton<B> : PluginDynamicCommand where B : ButtonData
    {
        protected StudioOneMidiPlugin plugin;
        protected IDictionary<string, B> buttonData = new Dictionary<string, B>();

        private System.Timers.Timer ActionImageChangedTimer;

        public StudioOneButton()
        {
            this.ActionImageChangedTimer = new System.Timers.Timer(10);
            this.ActionImageChangedTimer.AutoReset = false;
            this.ActionImageChangedTimer.Elapsed += (Object sender, System.Timers.ElapsedEventArgs e) =>
            {
                // Debug.WriteLine("ActionImageChangeTimer.Elapsed " + this.Name);

                // As of version 6.0.2 of the Loupedeck software ActionImageChanged() requires the
                // actionParameter argument in order to have an effect when used in PluginDynamicCommand.
                // Iterating through 6 buttons for now (assuming that EmitActionImageChanged() is called when
                // all buttons on display should be updated - we have 6 ChannelSelectButtons and
                // 6 ChannelModesKeypad buttons).
                //
                for (var i = 0; i < 6; i++)
                {
                    this.ActionImageChanged($"{i}");
                }
            };

        }

        protected override bool OnLoad()
        {
            this.plugin = base.Plugin as StudioOneMidiPlugin;

            foreach (var bd in this.buttonData.Values)
            {
                bd.OnLoad(this.plugin);
            }
            return base.OnLoad();
        }

        protected void EmitActionImageChanged()
        {
            this.ActionImageChangedTimer.Start();
        }

        protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
            if (actionParameter == null) return null;
            if (!this.buttonData.ContainsKey(actionParameter)) return null;

            return this.buttonData[actionParameter].getImage(imageSize);
        }

        protected override void RunCommand(string actionParameter)
        {
            if (!this.buttonData.ContainsKey(actionParameter)) return;

            this.buttonData[actionParameter].runCommand();
        }
    }
}
