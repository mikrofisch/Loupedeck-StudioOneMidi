namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Common;

    // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ChannelSelectButton : StudioOneButton<SelectButtonData>
    {
        public ChannelSelectButton()
        {
            this.DisplayName = "Channel Select Button";
            this.Description = "Button for selecting a channel";

            for (int i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
            {
                var idx = $"{i}";

                this.buttonData[idx] = new SelectButtonData(i);
                AddParameter(idx, $"Select Channel {i+1}", "Channel Selection");
            }
        }

        protected override Boolean OnLoad()
        {
            base.OnLoad();

            this.plugin.UserButtonChanged += (object sender, UserButtonParams e) =>
            {
                this.buttonData[e.channelIndex.ToString()].UserButtonActive = e.isActive;
                this.EmitActionImageChanged();
            };

            this.plugin.ChannelDataChanged += (object sender, EventArgs e) => 
            {
                this.EmitActionImageChanged();
            };

            this.plugin.SelectModeChanged += (object sender, SelectButtonMode e) =>
            {
                for (int i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    this.buttonData[i.ToString()].CurrentMode = e;
                }
                this.EmitActionImageChanged();
            };

            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                SelectButtonData.SelectionPropertyType = e;
                this.EmitActionImageChanged();
            };

            return true;
        }
    }
}