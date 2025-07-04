namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Helpers;
    using PluginSettings;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Text.RegularExpressions;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public class ChannelFader : ActionEditorAdjustment
	{
        public static readonly BitmapColor DefaultBarColor = new BitmapColor(60, 192, 232);
		// private StudioOneMidiPlugin plugin = null;

        private const String ChannelSelector = "channelSelector";
        private const String ControlOrientationSelector = "controlOrientationSelector";

        private SelectButtonMode SelectMode = SelectButtonMode.Select;
        private FaderMode FaderMode = FaderMode.Volume;
        private static BitmapImage? IconVolume, IconPan;
        private String PluginName = "";
        private static readonly PlugSettingsFinder UserPlugSettingsFinder = new PlugSettingsFinder(new PlugSettingsFinder.PlugParamSetting
        {
            OnColor = new FinderColorOnColor(ColorConv.Convert(DefaultBarColor)),     // Used for volume bar
            OffColor = new FinderColor(80, 80, 80),         // Used for volume bar
            TextOnColor = FinderColor.White,
            TextOffColor = new FinderColor(80, 80, 80)
        });
        
        private static readonly Boolean[] IsActive = new Boolean[StudioOneMidiPlugin.ChannelCount];

        // Custom settings for value display that is invoked when individual faders
        // get reconfigured on the fly to show specific parameters such as tempo.
        private class CustomParams
        {
            public BitmapColor BgColor = BitmapColor.Black;
            public BitmapColor BarColor = DefaultBarColor;
        }
        private readonly CustomParams[] CustomSettings = new CustomParams[StudioOneMidiPlugin.ChannelCount];

        private readonly System.Timers.Timer ActionImageUpdateTimer;
        private const int _actionImageUpdateTimeout = 20; // milliseconds

        public ChannelFader() : base(hasReset: true)
		{
            this.DisplayName = "Channel Fader";
            this.Description = "Channel fader.\nButton press -> reset to default";
            this.GroupName = "";

            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ChannelSelector, labelText: "Channel:"/*,"Select the fader bank channel"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ControlOrientationSelector, labelText: "Orientation:"/*,"Select the orientation of the channel fader control"*/)
                    .SetRequired()
                );

            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;

            IconVolume ??= EmbeddedResources.ReadImage(EmbeddedResources.FindFile("dial_volume_52px.png"));
            IconPan ??= EmbeddedResources.ReadImage(EmbeddedResources.FindFile("dial_pan_52px.png"));

            for (var i = 0; i < IsActive.Length; i++)
            {
                IsActive[i] = true;
            }

            // Action image update timer
            this.ActionImageUpdateTimer = new System.Timers.Timer(_actionImageUpdateTimeout);
            this.ActionImageUpdateTimer.AutoReset = false;
            this.ActionImageUpdateTimer.Elapsed += (Object? sender, System.Timers.ElapsedEventArgs e) =>
            {
                // Debug.WriteLine("ChannelFader.ActionImageUpdateTimer.Elapsed");
                ActionImageChanged();
            };
        }

        protected override bool OnLoad()
        {
            var plugin = (StudioOneMidiPlugin)base.Plugin;

            plugin.ChannelDataChanged += (s, e) => this.TriggerActionImageUpdateTimer();

            plugin.ChannelValueChanged += (s, e) => this.TriggerActionImageUpdateTimer();

            plugin.SelectModeChanged += (Object? sender, SelectButtonMode e) =>
            {
                this.SelectMode = e;
                Array.Clear(this.CustomSettings);

                this.TriggerActionImageUpdateTimer();
            };

            plugin.SelectButtonCustomModeChanged += (Object? sender, SelectButtonCustomParams cp) =>
            {
                this.CustomSettings[cp.ButtonIndex] = new CustomParams
                {
                    BgColor = cp.BgColor,
                    BarColor = cp.BarColor,
                };

                this.TriggerActionImageUpdateTimer();
            };

            plugin.FaderModeChanged += (Object? sender, FaderMode e) =>
            {
                this.FaderMode = e;
                this.TriggerActionImageUpdateTimer();
            };

            plugin.FocusDeviceChanged += (Object? sender, String e) =>
            {
                this.PluginName = GetPluginName(e);
                this.TriggerActionImageUpdateTimer();
            };

            plugin.ChannelActiveCanged += (Object? sender, ChannelActiveParams e) =>
            {
                IsActive[e.ChannelIndex] = e.IsActive;
                if (e.Update) this.TriggerActionImageUpdateTimer();
            };

            plugin.UserPageChanged += (Object? sender, Int32 e) =>
            {
                UserPlugSettingsFinder.CurrentUserPage = e;
            };

            plugin.PluginSettingsReloaded += (s, e) =>
            {
                UserPlugSettingsFinder.ClearCache();
            };

            return true;
        }

        private void OnActionEditorControlValueChanged(Object? sender, ActionEditorControlValueChangedEventArgs e)
        {
        }

        private void OnActionEditorListboxItemsRequested(Object? sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(ChannelSelector))
            {
                Int32 i;
                for (i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
                {
                    e.AddItem($"{i}", $"Bank Channel {i + 1} Fader", $"Channel {i + 1} of the current bank of 6 channels controlled by the Loupedeck device");
                }
                e.AddItem($"{i}", $"Selected Channel Volume", $"Volume control for the channel currently selected in Studio One");
                e.AddItem($"{i + 1}", $"Selected Channel Pan", $"Pan control for the channel currently selected in Studio One");

            }
            else if (e.ControlName.EqualsNoCase(ControlOrientationSelector))
            {
                e.AddItem("left",  "Left Side", $"Control located on the left side of the Loupedeck device");
                e.AddItem("right", "Right Side", $"Control located on the right side of the Loupedeck device");
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        protected override bool ApplyAdjustment(ActionEditorActionParameters actionParameters, int diff)
		{
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;
            
            ChannelData cd = this.GetChannel(channelIndex);

            var deviceEntry = UserPlugSettingsFinder.GetPlugParamDeviceEntry(this.PluginName);

            var stepDivisions = UserPlugSettingsFinder.GetDialSteps(deviceEntry, cd.Label, cd.ChannelID + 1);
            if (stepDivisions > 50 && ((StudioOneMidiPlugin)this.Plugin).ShiftPressed)
            {
                stepDivisions *= 6;
            }
            cd.Value = Math.Min(1, Math.Max(0, (Single)Math.Round(cd.Value * stepDivisions + diff) / stepDivisions));
			cd.EmitVolumeUpdate();

            return true;
		}
        
        private void TriggerActionImageUpdateTimer()
        {
            if (ActionImageUpdateTimer.Enabled)
            {
                // If the timer is already running, extend the timeout to avoid multiple events
                ActionImageUpdateTimer.Interval = _actionImageUpdateTimeout;
                // Debug.WriteLine($"ChannelFader.ActionImageUpdateTimer reset to {_actionImageUpdateTimeout} ms");
                return;
            }
            ActionImageUpdateTimer.Start();
        }

        protected override BitmapImage? GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {

            if (actionParameters == null) return null;
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return null;
            if (!actionParameters.TryGetString(ControlOrientationSelector, out var controlOrientation)) return null;

            var bb = new BitmapBuilder(imageWidth, imageHeight);

            ChannelData cd = this.GetChannel(channelIndex);
            var currentChannel = cd.ChannelID + 1;

            var customParams = cd.ChannelID < this.CustomSettings.Length ? this.CustomSettings[cd.ChannelID] : null;
            bb.FillRectangle(0, 0, imageWidth, imageHeight, customParams != null
                                                            ? customParams.BgColor
                                                            : BitmapColor.Black);

            var deviceEntry = UserPlugSettingsFinder.GetPlugParamDeviceEntry(this.PluginName);

            if (this.SelectMode == SelectButtonMode.FX)
            {
                return bb.ToImage();
            }

            if (UserPlugSettingsFinder.GetLabel(deviceEntry, cd.Label, currentChannel).Length == 0) return bb.ToImage();

            const Int32 sideBarW = 8;
            var sideBarX = bb.Width - sideBarW;
            var volBarX = 0;
            var piW = (bb.Width - 2* sideBarW)/ 2;
            const Int32 piH = 8;

            if (controlOrientation.Equals("right"))
            {
                volBarX = sideBarX;
                sideBarX = 0;
            }

            // Check for selected channel volume & pan
            var isSelectedChannel = cd.ChannelID >= StudioOneMidiPlugin.ChannelCount;
            var isSelectedPan = cd.ChannelID == StudioOneMidiPlugin.ChannelCount + 1;
            var isClick = isSelectedPan ? cd.ValueStr.IsNullOrEmpty() 
                                        : this.SelectMode == SelectButtonMode.Send
                                          || this.SelectMode == SelectButtonMode.User ? false
                                                                                      : this.FaderMode == FaderMode.Pan && cd.ValueStr.Contains("dB");
            var isVolume = cd.ChannelID == StudioOneMidiPlugin.ChannelCount
                           || (isSelectedPan
                               ? isClick
                               : this.SelectMode == SelectButtonMode.Send
                                 || this.SelectMode == SelectButtonMode.User
                                 || isClick
                                 ? true
                                 : this.FaderMode == FaderMode.Volume);

            var valueColor = BitmapColor.White;
            var valBarColor = customParams != null 
                              ? customParams.BarColor
                              : ColorConv.Convert(UserPlugSettingsFinder.GetBarOnColor(deviceEntry, cd.Label, currentChannel));

            if (this.SelectMode == SelectButtonMode.Select)
            {
                if (cd.Muted || cd.Solo)
                {
                    bb.FillRectangle(
                        sideBarW, piH, bb.Width - 2 * sideBarW, bb.Height - 2 * piH,
                        ChannelProperty.PropertyColor[cd.Muted ? (Int32)ChannelProperty.PropertyType.Mute : (Int32)ChannelProperty.PropertyType.Solo]
                        );
                }
                if (cd.Selected && cd.ChannelID < StudioOneMidiPlugin.ChannelCount)
                {
                    bb.FillRectangle(sideBarX, 0, sideBarW, bb.Height, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Select]);
                }
                if (!isSelectedChannel && cd.Armed)
                {
                    bb.FillRectangle(sideBarW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Arm]);
                }
                if (!isSelectedChannel && cd.Monitor)
                {
                    bb.FillRectangle(sideBarW + piW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(Int32)ChannelProperty.PropertyType.Monitor]);
                }
            }


            if (SelectMode == SelectButtonMode.User && cd.ChannelID < IsActive.Length && !IsActive[cd.ChannelID])
            {
                valueColor = ColorConv.Convert(UserPlugSettingsFinder.GetTextOffColor(deviceEntry, cd.Label, currentChannel));
                valBarColor = ColorConv.Convert(UserPlugSettingsFinder.GetOffColor(deviceEntry, cd.Label, currentChannel));
            }

            if (UserPlugSettingsFinder.HideValueBar(deviceEntry, cd.Label, currentChannel)) valBarColor = BitmapColor.Transparent;

            if (isVolume)
            {
                var volBarH = (Int32)Math.Ceiling(cd.Value * bb.Height);
                var volBarY = bb.Height - volBarH;
                if (UserPlugSettingsFinder.GetMode(deviceEntry, cd.Label, currentChannel) == PlugSettingsFinder.PlugParamSetting.PotMode.Symmetric)
                {
                    volBarH = (Int32)(Math.Abs(cd.Value - 0.5) * bb.Height);
                    volBarY = cd.Value < 0.5 ? bb.Height / 2 : bb.Height / 2 - volBarH;
                }
                if (isSelectedChannel && !isSelectedPan)
                {
                    bb.DrawImage(IconVolume, 0, 0);
                }
                bb.FillRectangle(volBarX, volBarY, sideBarW, volBarH, valBarColor);
            }
            else
            {
                var panBarW = (Int32)(Math.Abs(cd.Value - 0.5) * bb.Width);
                var panBarX = cd.Value > 0.5 ? bb.Width / 2 : bb.Width / 2 - panBarW;

                if (isSelectedChannel)
                {
                    bb.DrawImage(IconPan, 0, 0);
                }
                if (!cd.ValueStr.IsNullOrEmpty())
                {
                    bb.FillRectangle(panBarX, 0, panBarW, piH, valBarColor);
                }
            }

            // bb.DrawText(cd.TrackName, 0, 0, bb.Width, bb.Height / 2, null, imageSize == PluginImageSize.Width60 ? 12 : 1);
            // bb.DrawText($"{Math.Round(cd.Value * 100.0f)} %", 0, bb.Height / 2, bb.Width, bb.Height / 2);


            if (isClick)
            {
                bb.DrawImage(EmbeddedResources.ReadImage(EmbeddedResources.FindFile("click_32px.png")), 12, 9);
            }
            else
            {
                // In custom mode limit the number of decimal places to 2. Hard wired for now.
                var maxValuePrecision = customParams != null ? 2
                                                             : UserPlugSettingsFinder.GetMaxValuePrecision(deviceEntry, cd.Label, currentChannel);

                var valStr = maxValuePrecision >= 0 ? Regex.Replace(cd.ValueStr, @"(\d+)([.,]?)(\d{0," + maxValuePrecision + @"})\d*\s?(\D*)", "$1$2$3 $4")
                                                    : cd.ValueStr;

                bb.DrawText(valStr.Replace(' ', '\n'), 0, bb.Height / 4, bb.Width, bb.Height / 2, valueColor);
            }

            var image = bb.ToImage();

            return image;
		}

		private ChannelData GetChannel(String actionParameter)
		{
			return ((StudioOneMidiPlugin)this.Plugin).channelData[actionParameter];
		}

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
//            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;
//
//            MackieChannelData cd = GetChannel(channelIndex);
//            cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Mute);

            return true;
        }

        // Gets called when the dial is pressed.
        protected override Boolean ProcessButtonEvent2(ActionEditorActionParameters actionParameters, DeviceButtonEvent2 buttonEvent)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex))
                return false;

            ChannelData cd = this.GetChannel(channelIndex);

            if (buttonEvent.EventType.IsPress())
            {
                cd.EmitValueReset();
            }
            else if (buttonEvent.EventType.IsLongPress())
            {
                if (this.SelectMode == SelectButtonMode.User)
                {
                    // Open the user configuration editor
                }
            }

            return base.ProcessButtonEvent2(actionParameters, buttonEvent);
        }

        // This never gets called in the current version of the Loupedeck SDK.
        // 
        // protected override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
        // 
        //	MackieChannelData cd = GetChannel(actionParameter);
        //
        //    if (touchEvent.EventType == DeviceTouchEventType.Tap)
        //    {
        //        cd.EmitBoolPropertyPress(ChannelProperty.BoolType.Select);
        //    }
        //    else if (touchEvent.EventType == DeviceTouchEventType.DoubleTap)
        //    {
        //        cd.EmitBoolPropertyPress(ChannelProperty.BoolType.Arm);
        //    }
        //
        //    return true;
        // }
    }
}
