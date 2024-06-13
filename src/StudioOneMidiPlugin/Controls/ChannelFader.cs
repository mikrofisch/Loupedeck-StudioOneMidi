namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Threading;
    using System.Windows.Forms;

    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public class ChannelFader : ActionEditorAdjustment
	{
		// private StudioOneMidiPlugin plugin = null;

        private const String ChannelSelector = "channelSelector";
        private const String ControlOrientationSelector = "controlOrientationSelector";

        private SelectButtonMode SelectMode = SelectButtonMode.Select;
        private FaderMode FaderMode = FaderMode.Volume;
        private static BitmapImage IconVolume, IconPan;
        private String PluginName;
        private static readonly ColorFinder UserColorFinder = new ColorFinder(new ColorFinder.ColorSettings
        {
            OnColor =  new FinderColor(60, 192, 232),      // Used for volume bar
            OffColor = new FinderColor(80, 80, 80)         // Used for volume bar
        });
        private static readonly UserButtonParams[] UserButtonInfo = new UserButtonParams[StudioOneMidiPlugin.ChannelCount];

        private Boolean IsUserConfigWindowOpen = false;

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
        }

        protected override bool OnLoad()
        {
            var plugin = base.Plugin as StudioOneMidiPlugin;
            plugin.channelFader = this;
            UserColorFinder.Init( plugin );

            plugin.ChannelDataChanged += (object sender, EventArgs e) => {
                this.ActionImageChanged();
            };

            plugin.SelectModeChanged += (object sender, SelectButtonMode e) =>
            {
                this.SelectMode = e;
                this.ActionImageChanged();
            };

            plugin.FaderModeChanged += (object sender, FaderMode e) =>
            {
                this.FaderMode = e;
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

            var stepDivisions = 100;
            if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                stepDivisions = 600;
            }
            cd.Value = Math.Min(1, Math.Max(0, (float)Math.Round(cd.Value * stepDivisions + diff) / stepDivisions));
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
            var isSelectedChannel = false;
            if (cd.ChannelID == StudioOneMidiPlugin.ChannelCount)
            {
                this.FaderMode = FaderMode.Volume;
                isSelectedChannel = true;
            }
            else if (cd.ChannelID == StudioOneMidiPlugin.ChannelCount + 1)
            {
                this.FaderMode = FaderMode.Pan;
                isSelectedChannel = true;
            }
            if (this.SelectMode == SelectButtonMode.Select)
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
                if (!isSelectedChannel && cd.Armed)
                {
                    bb.FillRectangle(sideBarW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Arm]);
                }
                if (!isSelectedChannel && cd.Monitor)
                {
                    bb.FillRectangle(sideBarW + piW, bb.Height - piH, piW, piH, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Monitor]);
                }
            }

            var ValueColor = BitmapColor.White;
            if (this.FaderMode == FaderMode.Volume)
            {
                var volBarColor = UserColorFinder.getOnColor(this.PluginName, cd.Label);

                var linkedParameter = UserColorFinder.getLinkedParameter(this.PluginName, cd.Label);
                if (linkedParameter != null)
                {
                    var isActive = false;
                    foreach (UserButtonParams ubp in UserButtonInfo)
                    {
                        if (ubp != null && ubp.userLabel == linkedParameter)
                        {
                            isActive = ubp.isActive;
                            break;
                        }
                    }

                    if (isActive == UserColorFinder.getLinkReversed(this.PluginName, cd.Label))
                    {
                        ValueColor = new BitmapColor(70, 70, 70);
                        volBarColor = UserColorFinder.getOffColor(this.PluginName, cd.Label);
                    }
                }
                var volBarH = (Int32)Math.Ceiling(cd.Value * bb.Height);
                var volBarY = bb.Height - volBarH;
                if (UserColorFinder.getMode(this.PluginName, cd.Label) == ColorFinder.ColorSettings.PotMode.Symmetric)
                {
                    volBarH = (Int32)(Math.Abs(cd.Value - 0.5) * bb.Height);
                    volBarY = cd.Value < 0.5 ? bb.Height / 2 : bb.Height / 2 - volBarH;
                }
                if (isSelectedChannel)
                {
                    bb.DrawImage(IconVolume, 0, 0);
                }
                bb.FillRectangle(volBarX, volBarY, sideBarW, volBarH, volBarColor);
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
                    bb.FillRectangle(panBarX, 0, panBarW, piH, new BitmapColor(60, 192, 232));
                }
            }

            // bb.DrawText(cd.TrackName, 0, 0, bb.Width, bb.Height / 2, null, imageSize == PluginImageSize.Width60 ? 12 : 1);
            // bb.DrawText($"{Math.Round(cd.Value * 100.0f)} %", 0, bb.Height / 2, bb.Width, bb.Height / 2);
            bb.DrawText(cd.ValueStr, 0, bb.Height / 4, bb.Width, bb.Height / 2, ValueColor);
            return bb.ToImage();
		}

		private MackieChannelData GetChannel(String actionParameter)
		{
			return (this.Plugin as StudioOneMidiPlugin).channelData[actionParameter];
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
                if (this.SelectMode == SelectButtonMode.User)
                {
                    this.OpenUserConfigWindow(cd.Label);
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

        public void OpenUserConfigWindow(String pluginParameter)
        {
            if (this.IsUserConfigWindowOpen)
                return;

            var volBarColor = UserColorFinder.getOnColor(this.PluginName, pluginParameter);

            var t = new Thread(() => {
                var w = new UserControlConfig(UserControlConfig.WindowMode.Dial,
                                              this.Plugin,
                                              UserColorFinder,
                                              new UserControlConfigData { PluginName = this.PluginName,
                                                                          PluginParameter = pluginParameter,
                                                                          Mode = UserColorFinder.getMode(this.PluginName, pluginParameter),
                                                                          R = volBarColor.R,
                                                                          G = volBarColor.G,
                                                                          B = volBarColor.B,
                                                                          LinkedParameter = UserColorFinder.getLinkedParameter(this.PluginName, pluginParameter),
                                                                          Label = UserColorFinder.getLabel(this.PluginName, pluginParameter) } );
                w.Closed += (_, _) =>
                {
                    this.IsUserConfigWindowOpen = false;
                    UserColorFinder.Init(this.Plugin, forceReload: true);
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
