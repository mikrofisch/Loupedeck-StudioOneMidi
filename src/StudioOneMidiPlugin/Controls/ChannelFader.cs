namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Threading;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public class ChannelFader : ActionEditorAdjustment
	{
		// private StudioOneMidiPlugin plugin = null;

        private const String ChannelSelector = "channelSelector";
        private const String ControlOrientationSelector = "controlOrientationSelector";

        private SelectButtonMode selectMode = SelectButtonMode.Select;
        private FaderMode faderMode = FaderMode.Volume;
        private String PluginName;
        private static readonly ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings
        {
            OnColor = BitmapColor.Transparent,
            OffColor = BitmapColor.Transparent,
            TextOnColor = new BitmapColor(60, 192, 232),  // Used for volume bar
            TextOffColor = new BitmapColor(80, 80, 80)    // Used for volume bar
        });
        private static UserButtonParams[] UserButtonInfo = new UserButtonParams[StudioOneMidiPlugin.ChannelCount];

        private Boolean IsUserPotConfigWindowOpen = false;

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
        }

        protected override bool OnLoad()
        {
            StudioOneMidiPlugin plugin = base.Plugin as StudioOneMidiPlugin;
            plugin.channelFader = this;
            UserColorFinder.Init( plugin );

            plugin.ChannelDataChanged += (object sender, EventArgs e) => {
                this.ActionImageChanged();
            };

            plugin.SelectModeChanged += (object sender, SelectButtonMode e) =>
            {
                this.selectMode = e;
                this.ActionImageChanged();
            };

            plugin.FaderModeChanged += (object sender, FaderMode e) =>
            {
                this.faderMode = e;
                this.ActionImageChanged();
            };

            plugin.FocusDeviceChanged += (object sender, string e) =>
            {
                this.PluginName = getPluginName(e);
                this.ActionImageChanged();
            };

            plugin.UserButtonChanged += (object sender, UserButtonParams e) =>
            {
                UserButtonInfo[e.channelIndex] = e;
                this.ActionImageChanged();
            };

            return true;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
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

            MackieChannelData cd = this.GetChannel(channelIndex);

			cd.Value = Math.Min(1, Math.Max(0, (float)Math.Round(cd.Value * 100 + diff) / 100));
			cd.EmitVolumeUpdate();
            return true;
		}

		protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return null;
            if (!actionParameters.TryGetString(ControlOrientationSelector, out var controlOrientation)) return null;

            MackieChannelData cd = this.GetChannel(channelIndex);

			var bb = new BitmapBuilder(imageWidth, imageHeight);

            const int sideBarW = 8;
            int sideBarX = bb.Width - sideBarW;
            int volBarX = 0;
            int piW = (bb.Width - 2* sideBarW)/ 2;
            const int piH = 8;

            if (controlOrientation.Equals("right"))
            {
                volBarX = sideBarX;
                sideBarX = 0;
            }

            // Check for selected channel volume & pan
            if (cd.ChannelID == StudioOneMidiPlugin.ChannelCount)          this.faderMode = FaderMode.Volume;
            else if (cd.ChannelID == StudioOneMidiPlugin.ChannelCount + 1) this.faderMode = FaderMode.Pan;

            if (this.selectMode == SelectButtonMode.Select)
            {
                if (cd.Muted || cd.Solo)
                {
                    bb.FillRectangle(
                        sideBarW, piH, bb.Width - 2 * sideBarW, bb.Height - 2 * piH,
                        ChannelProperty.PropertyColor[cd.Muted ? (int)ChannelProperty.PropertyType.Mute : (int)ChannelProperty.PropertyType.Solo]
                        );
                }
                if (cd.Selected && cd.ChannelID < StudioOneMidiPlugin.ChannelCount)
                {
                    bb.FillRectangle(sideBarX, 0, sideBarW, bb.Height, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Select]);
                }
                if (cd.Armed)
                {
                    bb.FillRectangle(sideBarW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Arm]);
                }
                if (cd.Monitor)
                {
                    bb.FillRectangle(sideBarW + piW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Monitor]);
                }
            }
            if (this.faderMode == FaderMode.Volume)
            {
                var linkedParameter = UserColorFinder.getLinkedParameter(this.PluginName, cd.Label);
                var isActive = false;
                foreach (UserButtonParams ubp in UserButtonInfo)
                {
                    if (ubp != null && ubp.userLabel == linkedParameter)
                    {
                        isActive = ubp.isActive;
                        break;
                    }
                }

                var volBarColor = UserColorFinder.getTextOnColor(this.PluginName, cd.Label);
                if (!linkedParameter.IsNullOrEmpty() && !isActive)
                {
                    volBarColor = UserColorFinder.getTextOffColor(this.PluginName, cd.Label);
                }

                int volBarH = (int)Math.Ceiling(cd.Value * bb.Height);
                int volBarY = bb.Height - volBarH;
                if (UserColorFinder.getMode(this.PluginName, cd.Label) == ColorFinder.ColorSettings.PotMode.Symmetric)
                {
                    volBarH = (int)(Math.Abs(cd.Value - 0.5) * bb.Height);
                    volBarY = cd.Value < 0.5 ? bb.Height / 2 : bb.Height / 2 - volBarH;
                }
                bb.FillRectangle(volBarX, volBarY, sideBarW, volBarH, volBarColor);
            }
            else
            {
                int panBarW = (int)(Math.Abs(cd.Value - 0.5) * bb.Width);
                int panBarX = cd.Value > 0.5 ? bb.Width / 2 : bb.Width / 2 - panBarW;

                bb.FillRectangle(panBarX, 0, panBarW, piH, new BitmapColor(60, 192, 232));
            }

            //			bb.DrawText(cd.TrackName, 0, 0, bb.Width, bb.Height / 2, null, imageSize == PluginImageSize.Width60 ? 12 : 1);
            //            bb.DrawText($"{Math.Round(cd.Value * 100.0f)} %", 0, bb.Height / 2, bb.Width, bb.Height / 2);
            bb.DrawText(cd.ValueStr, 0, bb.Height / 4, bb.Width, bb.Height / 2);
//            bb.DrawText(cd.Value.ToString(), 0, bb.Height / 4, bb.Width, bb.Height / 2);
            return bb.ToImage();
		}

		private MackieChannelData GetChannel(string actionParameter)
		{
			return (this.Plugin as StudioOneMidiPlugin).mackieChannelData[actionParameter];
		}

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
//            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;
//
//            MackieChannelData cd = GetChannel(channelIndex);
//            cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Mute);

            return true;
        }

        // Gets called when the dial is pressed. Assuming this is how you implement
        // the value reset command.
        protected override Boolean ProcessButtonEvent2(ActionEditorActionParameters actionParameters, DeviceButtonEvent2 buttonEvent)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex))
                return false;

            MackieChannelData cd = this.GetChannel(channelIndex);

            if (buttonEvent.EventType.IsPress())
            {
                cd.EmitValueReset();
            }
            else if (buttonEvent.EventType.IsLongPress())
            {
                if (this.selectMode == SelectButtonMode.User)
                {
                    this.OpenUserPotConfigWindow(cd.Label);
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

        public void OpenUserPotConfigWindow(String pluginParameter)
        {
            if (this.IsUserPotConfigWindowOpen)
                return;

            var volBarColor = UserColorFinder.getTextOnColor(this.PluginName, pluginParameter);

            var t = new Thread(() => {
                var w = new UserPotConfig(this.Plugin,
                                          new UserPotConfigData { PluginName = this.PluginName,
                                                                  PluginParameter = pluginParameter,
                                                                  Mode = UserColorFinder.getMode(this.PluginName, pluginParameter),
                                                                  R = volBarColor.R,
                                                                  G = volBarColor.G,
                                                                  B = volBarColor.B,
                                                                  Label = UserColorFinder.getLabel(this.PluginName, pluginParameter) } );
                w.Closed += (_, _) =>
                {
                    this.IsUserPotConfigWindowOpen = false;
                    UserColorFinder.Init(this.Plugin, forceReload: true);
//                    this.ActionImageChanged();
                    (this.Plugin as StudioOneMidiPlugin).EmitChannelDataChanged();
                };
                w.Show();
                System.Windows.Threading.Dispatcher.Run();
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            this.IsUserPotConfigWindowOpen = true;
        }
    }
}
