﻿using Loupedeck.StudioOneMidiPlugin.Controls;

namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;
    using System.Threading;
    using Melanchall.DryWetMidi.Core;
    using System.Diagnostics;

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
        private Boolean ListenToMidi = false;

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

            this.plugin.UserButtonChanged += (Object sender, UserButtonParams e) =>
            {
                var bd = this.buttonData[e.channelIndex.ToString()];
                bd.UserButtonActive = e.isActive();
            };
            this.plugin.UserPageChanged += (Object sender, Int32 e) => SelectButtonData.UserPlugSettingsFinder.CurrentUserPage = e;
            this.plugin.FocusDeviceChanged += (Object sender, String e) => SelectButtonData.FocusDeviceName = e;
            this.plugin.ChannelDataChanged += (Object sender, EventArgs e) => 
            {
                this.UpdateAllActionImages();
            };

            this.plugin.SelectModeChanged += (Object sender, SelectButtonMode e) =>
            {
                for (var i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    var bd = this.buttonData[i.ToString()];
                    bd.CurrentMode = e;
                }
                this.ListenToMidi = false;
                this.UpdateAllActionImages();
            };

            this.plugin.SelectButtonCustomModeChanged += (object sender, SelectButtonCustomParams cp) =>
            {
                this.buttonData[cp.ButtonIndex.ToString()].SetCustomMode(cp);
                if (cp.MidiCode > 0)
                {
                    this.ListenToMidi = true;
                }
                this.UpdateAllActionImages();
            };

            this.plugin.PropertySelectionChanged += (object sender, ChannelProperty.PropertyType e) =>
            {
                SelectButtonData.SelectionPropertyType = e;
                this.UpdateAllActionImages();
            };

            this.plugin.FocusDeviceChanged += (object sender, string e) =>
            {
                SelectButtonData.PluginName = getPluginName(e);
            };

            this.plugin.UserButtonMenuActivated += (object sender, UserButtonMenuParams e) =>
            {
                if (e.ChannelIndex >= 0)
                {
                    this.buttonData[e.ChannelIndex.ToString()].UserButtonMenuActive = e.IsActive;
                    this.UpdateAllActionImages();
                }
            };

            this.plugin.CommandNoteReceived += (object sender, NoteOnEvent e) =>
            {
                if (this.ListenToMidi)
                {
                    foreach (KeyValuePair<String, SelectButtonData> bd in this.buttonData)
                    {
                        if (bd.Value.CurrentMode == SelectButtonMode.Custom && bd.Value.CurrentCustomParams.MidiCode == e.NoteNumber)
                        {
                            bd.Value.CustomIsActivated = e.Velocity > 0;
                        }
                    }
                    this.UpdateAllActionImages();
                }
            };

            return true;
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var bd = this.buttonData[actionParameter];
            var sendChannelActiveChange = false;
            if (bd.CurrentMode == SelectButtonMode.User)
            {
                // Check linked parameter dependencies every time channels are updated.
                //
                var deviceEntry = SelectButtonData.UserPlugSettingsFinder.GetPlugParamDeviceEntry(SelectButtonData.PluginName);
                var linkedParameter = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.Label, 0);
                var linkedParameterUser = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.UserLabel, 0);

                // Debug.WriteLine("ChannelSelectButton getCommandImage channel: " + bd.ChannelIndex + " bd.Label: " + bd.Label + ", linkedParameter: " + linkedParameter +", linkedParameterUser: " + linkedParameterUser);

                if (!linkedParameter.IsNullOrEmpty() || !linkedParameterUser.IsNullOrEmpty())
                {
                    foreach (var sbd in this.buttonData.Values)
                    {
                        var cd = this.plugin.channelData[sbd.ChannelIndex.ToString()];

                        if (cd.UserLabel == linkedParameterUser)   // user button
                        {
                            bd.UserButtonEnabled = SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.UserLabel, 0) ^ cd.UserValue > 0;
                        }
                        if (cd.UserLabel == linkedParameter)       // channel value
                        {
                            var linkedStates = SelectButtonData.UserPlugSettingsFinder.GetLinkedStates(deviceEntry, bd.Label, 0);
                            if (!linkedStates.IsNullOrEmpty())
                            {
                                var userMenuItems = SelectButtonData.UserPlugSettingsFinder.GetUserMenuItems(deviceEntry, linkedParameter, 0);
                                if (userMenuItems != null && userMenuItems.Length > 1)
                                {
                                    var menuIndex = (Int32)Math.Round((Double)cd.UserValue / 127 * (userMenuItems.Length - 1));
                                    bd.Enabled = linkedStates.Contains(menuIndex.ToString()) ^ SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.Label, 0);
                                    sendChannelActiveChange = true;
                                }
                            }
                            else
                            {
                                bd.Enabled = SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.Label, 0) ^ cd.UserValue > 0;
                                sendChannelActiveChange = true;
                            }
                        }
                    }
                }
                else
                {
                    bd.Enabled = true;
                    bd.UserButtonEnabled = true;
                    sendChannelActiveChange = true;
                }
            }
            if (sendChannelActiveChange)
            {
                this.plugin.EmitChannelActiveChanged(new ChannelActiveParams { ChannelIndex = bd.ChannelIndex, IsActive = bd.Enabled });
            }

            return bd.getImage(imageSize);
        }

        protected override Boolean ProcessTouchEvent(String actionParameter, DeviceTouchEvent touchEvent)
        {
            if (touchEvent.EventType.IsLongPress())
            {
                if (this.buttonData[actionParameter].CurrentMode == SelectButtonMode.User)
                {
                    ChannelData cd = this.plugin.channelData[actionParameter];

                    this.OpenUserConfigWindow(cd.UserLabel);
                }
                return true;
            }

            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }

        public void OpenUserConfigWindow(String pluginParameter)
        {
            if (this.IsUserConfigWindowOpen)
            {
                return;
            }

            var deviceEntry = SelectButtonData.UserPlugSettingsFinder.GetPlugParamDeviceEntry(SelectButtonData.PluginName);
            var onColor = SelectButtonData.UserPlugSettingsFinder.GetOnColor(deviceEntry, pluginParameter, 0);

            var t = new Thread(() => {
                var w = new UserControlConfig(UserControlConfig.WindowMode.Button,
                                              this.Plugin,
                                              SelectButtonData.UserPlugSettingsFinder,
                                              new UserControlConfigData
                                              {
                                                  PluginName = SelectButtonData.PluginName,
                                                  PluginParameter = pluginParameter,
                                                  ShowCircle = SelectButtonData.UserPlugSettingsFinder.GetShowCircle(deviceEntry, pluginParameter, 0),
                                                  R = onColor.R,
                                                  G = onColor.G,
                                                  B = onColor.B,
                                                  Label = SelectButtonData.UserPlugSettingsFinder.GetLabel(deviceEntry, pluginParameter, 0)
                                              });
                w.Closed += (_, _) =>
                {
                    this.IsUserConfigWindowOpen = false;
                    PlugSettingsFinder.Init(this.Plugin, forceReload: true);
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