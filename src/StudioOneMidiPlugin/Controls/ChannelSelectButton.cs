namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;
    using System.Threading;

    // Button for channel selection functions. These are used in the left and
    // right columns of the channel modes keypad. Different selection modes allow
    // the setting of channel states such as selection, mute, solo, as well
    // as user defined functions for plugin control.
    //
    // Based on the source code for the official Loupedeck OBS Studio plugin
    // (https://github.com/Loupedeck-open-source/Loupedeck-ObsStudio-OpenPlugin)
    //
    internal class ChannelSelectButton : StudioOneButton<SelectButtonData>
    {
        private Boolean IsUserConfigWindowOpen = false;

        public ChannelSelectButton()
        {
            this.DisplayName = "Channel Select Button";
            this.Description = "Button for channel functions, works together with the modes keypad";

            for (var i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
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
                var bd = this.buttonData[e.channelIndex.ToString()];
                bd.UserButtonActive = e.isActive();
                bd.UserLabel = e.userLabel;

                foreach (var sbd in this.buttonData.Values)
                {
                    if (SelectButtonData.UserColorFinder.getLinkedParameter(SelectButtonData.PluginName, sbd.UserLabel) == e.userLabel)
                    {
                        sbd.UserButtonEnabled = SelectButtonData.UserColorFinder.getLinkReversed(SelectButtonData.PluginName, sbd.UserLabel) ? !e.isActive() : e.isActive();
                    }
                }
                this.EmitActionImageChanged();
            };
            this.plugin.UserPageChanged += (object sender, Int32 e) => SelectButtonData.UserColorFinder.CurrentUserPage = e;

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

            this.plugin.UserButtonMenuActivated += (object sender, UserButtonMenuParams e) =>
            {
                if (e.ChannelIndex >= 0)
                {
                    this.buttonData[e.ChannelIndex.ToString()].UserButtonMenuActive = e.IsActive;
                    this.EmitActionImageChanged();
                }
            };

            return true;
        }

        protected override Boolean ProcessTouchEvent(String actionParameter, DeviceTouchEvent touchEvent)
        {
            if (touchEvent.EventType.IsLongPress())
            {
                if (this.buttonData[actionParameter].CurrentMode == SelectButtonMode.User)
                {
                    MackieChannelData cd = this.plugin.channelData[actionParameter];

                    this.OpenUserConfigWindow(cd.UserLabel);
                }
                return true;
            }

            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }

        public void OpenUserConfigWindow(String pluginParameter)
        {
            if (this.IsUserConfigWindowOpen)
                return;

            var onColor = SelectButtonData.UserColorFinder.getOnColor(SelectButtonData.PluginName, pluginParameter);

            var t = new Thread(() => {
                var w = new UserControlConfig(UserControlConfig.WindowMode.Button,
                                              this.Plugin,
                                              SelectButtonData.UserColorFinder,
                                              new UserControlConfigData
                                              {
                                                  PluginName = SelectButtonData.PluginName,
                                                  PluginParameter = pluginParameter,
                                                  ShowCircle = SelectButtonData.UserColorFinder.getShowCircle(SelectButtonData.PluginName, pluginParameter),
                                                  R = onColor.R,
                                                  G = onColor.G,
                                                  B = onColor.B,
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