using Loupedeck.StudioOneMidiPlugin.Controls;

namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;
    using System.Threading;
    using Melanchall.DryWetMidi.Core;

    using PluginSettings;

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

            ((StudioOneMidiPlugin)Plugin).UserButtonChanged += (Object? sender, UserButtonParams e) =>
            {
                var bd = this.buttonData[e.channelIndex.ToString()];
                if (bd != null) bd.UserButtonActive = e.isActive();
            };
            ((StudioOneMidiPlugin)Plugin).UserPageChanged += (Object? sender, Int32 e) => SelectButtonData.UserPlugSettingsFinder.CurrentUserPage = e;
            ((StudioOneMidiPlugin)Plugin).FocusDeviceChanged += (Object? sender, String e) => SelectButtonData.FocusDeviceName = e;
            ((StudioOneMidiPlugin)Plugin).ChannelDataChanged += (Object? sender, EventArgs e) => 
            {
                this.UpdateAllActionImages();
            };

            ((StudioOneMidiPlugin)Plugin).SelectModeChanged += (Object? sender, SelectButtonMode e) =>
            {
                for (var i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    var bd = this.buttonData[i.ToString()];
                    if (bd != null) bd.CurrentMode = e;
                }
                this.ListenToMidi = false;
                this.UpdateAllActionImages();
            };

            ((StudioOneMidiPlugin)Plugin).SelectButtonCustomModeChanged += (Object? sender, SelectButtonCustomParams cp) =>
            {
                var bd = this.buttonData[cp.ButtonIndex.ToString()];
                if (bd != null) bd.SetCustomMode(cp);

                if (cp.MidiCode > 0)
                {
                    this.ListenToMidi = true;
                }
                this.UpdateAllActionImages();
            };

            ((StudioOneMidiPlugin)Plugin).PropertySelectionChanged += (Object? sender, ChannelProperty.PropertyType e) =>
            {
                SelectButtonData.SelectionPropertyType = e;
                this.UpdateAllActionImages();
            };

            ((StudioOneMidiPlugin)Plugin).FocusDeviceChanged += (Object? sender, string e) =>
            {
                SelectButtonData.PluginName = getPluginName(e);
            };

            ((StudioOneMidiPlugin)Plugin).UserButtonMenuActivated += (Object? sender, UserButtonMenuParams e) =>
            {
                if (e.ChannelIndex >= 0)
                {
                    var bd = this.buttonData[e.ChannelIndex.ToString()];
                    if (bd != null)
                    {
                        bd.UserButtonMenuActive = e.IsActive;
                        this.UpdateAllActionImages();
                    }
                }
            };

            ((StudioOneMidiPlugin)Plugin).CommandNoteReceived += (Object? sender, NoteOnEvent e) =>
            {
                if (this.ListenToMidi)
                {
                    foreach (KeyValuePair<String, SelectButtonData?> bd in this.buttonData)
                    {
                        if (bd.Value != null && bd.Value.CurrentMode == SelectButtonMode.Custom && bd.Value.CurrentCustomParams.MidiCode == e.NoteNumber)
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
            if (bd == null) throw new InvalidOperationException("Uninitialised ButtonData");

            var sendChannelActiveChange = false;
            if (bd.CurrentMode == SelectButtonMode.User)
            {
                // Check linked parameter dependencies every time channels are updated.
                //
                var deviceEntry = SelectButtonData.UserPlugSettingsFinder.GetPlugParamDeviceEntry(SelectButtonData.PluginName);
                if (deviceEntry == null)
                {
                    // Debug.WriteLine("ChannelSelectButton getCommandImage deviceEntry is null for " + SelectButtonData.PluginName);
                    return bd.getImage(imageSize);
                }

                var linkedParameter = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.Label, 0);
                var linkedParameterUser = SelectButtonData.UserPlugSettingsFinder.GetLinkedParameter(deviceEntry, bd.UserLabel, 0);

                // Debug.WriteLine("ChannelSelectButton getCommandImage channel: " + bd.ChannelIndex + " bd.Label: " + bd.Label + ", linkedParameter: " + linkedParameter +", linkedParameterUser: " + linkedParameterUser);

                if (!linkedParameter.IsNullOrEmpty() || !linkedParameterUser.IsNullOrEmpty())
                {
                    foreach (var sbd in this.buttonData.Values)
                    {
                        if (sbd == null) continue;

                        var cd = ((StudioOneMidiPlugin)Plugin).channelData[sbd.ChannelIndex.ToString()];

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
                                    bd.Enabled = linkedStates != null ? linkedStates.Contains(menuIndex.ToString()) ^ SelectButtonData.UserPlugSettingsFinder.GetLinkReversed(deviceEntry, bd.Label, 0)
                                                                      : true;
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
                ((StudioOneMidiPlugin)Plugin).EmitChannelActiveChanged(new ChannelActiveParams { ChannelIndex = bd.ChannelIndex, IsActive = bd.Enabled });
            }

            return bd.getImage(imageSize);
        }

        protected override Boolean ProcessTouchEvent(String actionParameter, DeviceTouchEvent touchEvent)
        {
            if (touchEvent.EventType.IsLongPress())
            {
                return true;
            }

            return base.ProcessTouchEvent(actionParameter, touchEvent);
        }

    }

}