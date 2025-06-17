namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;


    // Buttons to select the property to control when a ChannelPropertyButton
    // is set to 'Multi' mode.
    //
    internal class PropertySelectionButton : StudioOneButton<PropertySelectionButtonData>
    {
        public PropertySelectionButton()
        {
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Mute,
                                                           ChannelProperty.PropertyType.Solo,
                                                           "select-mute", "select-solo", "select-mute-solo"));
            this.AddButton(new PropertySelectionButtonData(ChannelProperty.PropertyType.Arm,
                                                           ChannelProperty.PropertyType.Monitor,
                                                           "select-arm", "select-monitor", "select-arm-monitor"));
        }

        protected override bool OnLoad()
        {
            base.OnLoad();

            ((StudioOneMidiPlugin)base.Plugin).PropertySelectionChanged += (object? sender, ChannelProperty.PropertyType e) =>
            {
                this.UpdateAllActionImages();
            };

            return true;
        }

        //protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        //{
        //    var bd = this.buttonData[actionParameter];
        //    if ((bd.Type == ChannelProperty.PropertyType.Mute || bd.Type == ChannelProperty.PropertyType.Solo) &&
        //        !this.buttonData[ChannelProperty.PropertyType.Mute.ToString()].IsActive &&
        //        !this.buttonData[ChannelProperty.PropertyType.Solo.ToString()].IsActive)
        //        this.SetActive(ChannelProperty.PropertyType.Mute);
        //    if ((bd.Type == ChannelProperty.PropertyType.Arm || bd.Type == ChannelProperty.PropertyType.Monitor) &&
        //        !this.buttonData[ChannelProperty.PropertyType.Arm.ToString()].IsActive &&
        //        !this.buttonData[ChannelProperty.PropertyType.Monitor.ToString()].IsActive)
        //        this.SetActive(ChannelProperty.PropertyType.Arm);

        //    return base.GetCommandImage(actionParameter, imageSize);
        //}

        //private void SetActive(ChannelProperty.PropertyType propertyType)
        //{
        //    this.buttonData[ChannelProperty.PropertyType.Mute.ToString()].IsActive = propertyType == ChannelProperty.PropertyType.Mute;
        //    this.buttonData[ChannelProperty.PropertyType.Solo.ToString()].IsActive = propertyType == ChannelProperty.PropertyType.Solo;
        //    this.buttonData[ChannelProperty.PropertyType.Arm.ToString()].IsActive = propertyType == ChannelProperty.PropertyType.Arm;
        //    this.buttonData[ChannelProperty.PropertyType.Monitor.ToString()].IsActive = propertyType == ChannelProperty.PropertyType.Monitor;
        //}

        private void AddButton(PropertySelectionButtonData bd)
        {
            // String name = ChannelProperty.PropertyName[(int)bd.Type];
            var idx = bd.TypeA.ToString();

            this._buttonData[idx] = bd;
            this.AddParameter(idx, $"{ChannelProperty.PropertyName[(Int32)bd.TypeA]}/{ChannelProperty.PropertyName[(int)bd.TypeB]}", "Modes");
        }
    }
}
