namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;
    using System.Threading;

    // This defines 
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ChannelSelectButton : StudioOneButton<SelectButtonData>
    {
        private Boolean IsUserConfigWindowOpen = false;

        public ChannelSelectButton()
        {
            this.DisplayName = "Channel Select Button";
            this.Description = "Button for selecting a channel";

            for (int i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
            {
                var idx = $"{i}";

                this.buttonData[idx] = new SelectButtonData(i);
                this.AddParameter(idx, $"Select Channel {i+1}", "Channel Selection");
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

            this.plugin.FocusDeviceChanged += (object sender, string e) =>
            {
                SelectButtonData.PluginName = getPluginName(e);
                this.EmitActionImageChanged();
            };
            
            return true;
        }

        protected override Boolean ProcessButtonEvent2(string actionParameter, DeviceButtonEvent2 buttonEvent)
        {
            if (buttonEvent.EventType.IsLongPress())
            {
                if (this.buttonData[actionParameter].CurrentMode == SelectButtonMode.User)
                {
                    MackieChannelData cd = this.plugin.channelData[actionParameter];

                    this.OpenUserConfigWindow(cd.Label);
                }
            }


            return base.ProcessButtonEvent2(actionParameter, buttonEvent);
        }
        public void OpenUserConfigWindow(String pluginParameter)
        {
            if (this.IsUserConfigWindowOpen)
                return;

            var volBarColor = SelectButtonData.UserColorFinder.getTextOnColor(SelectButtonData.PluginName, pluginParameter);

            var t = new Thread(() => {
                var w = new UserControlConfig(this.Plugin,
                                          SelectButtonData.UserColorFinder,
                                          new UserControlConfigData
                                          {
                                              PluginName = SelectButtonData.PluginName,
                                              PluginParameter = pluginParameter,
                                              Mode = SelectButtonData.UserColorFinder.getMode(SelectButtonData.PluginName, pluginParameter),
                                              R = volBarColor.R,
                                              G = volBarColor.G,
                                              B = volBarColor.B,
                                              Label = SelectButtonData.UserColorFinder.getLabel(SelectButtonData.PluginName, pluginParameter)
                                          });
                w.Closed += (_, _) =>
                {
                    this.IsUserConfigWindowOpen = false;
                    SelectButtonData.UserColorFinder.Init(this.Plugin, forceReload: true);
                    (this.Plugin as StudioOneMidiPlugin).EmitChannelDataChanged();
                };
                w.Show();
                System.Windows.Threading.Dispatcher.Run();
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            this.IsUserConfigWindowOpen = true;
        }

    }

}