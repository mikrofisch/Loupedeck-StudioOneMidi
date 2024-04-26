namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading.Tasks;

    using Melanchall.DryWetMidi.Core;

    // Buttons to select the property to control when a ChannelPropertyButton
    // is set to 'Multi' mode.
    //
    internal class PropertySelectionButton : StudioOneButton<PropertySelectionButtonData>
    {
        public PropertySelectionButton()
        {
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                           true));
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Solo,
                                                           false));
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                           true,
                                                           "record"));
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Monitor,
                                                           false,
                                                           "monitor"));
        }

        protected override bool OnLoad()
        {
            base.OnLoad();

            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                this.buttonData[ChannelProperty.PropertyType.Mute.ToString()].IsActive = e == ChannelProperty.PropertyType.Mute;
                this.buttonData[ChannelProperty.PropertyType.Solo.ToString()].IsActive = e == ChannelProperty.PropertyType.Solo;
                this.buttonData[ChannelProperty.PropertyType.Arm.ToString()].IsActive = e == ChannelProperty.PropertyType.Arm;
                this.buttonData[ChannelProperty.PropertyType.Monitor.ToString()].IsActive = e == ChannelProperty.PropertyType.Monitor;

                this.EmitActionImageChanged();
            };

            return true;
        }

        private void AddButton(PropertySelectionButtonData bd)
        {
            String name = ChannelProperty.PropertyName[(int)bd.Type];
            String idx = bd.Type.ToString();

            this.buttonData[idx] = bd;
            AddParameter(idx, ChannelProperty.PropertyName[(int)bd.Type], "Modes");
        }
    }
}
