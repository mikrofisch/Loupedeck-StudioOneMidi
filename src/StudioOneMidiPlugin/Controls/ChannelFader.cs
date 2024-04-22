namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using static Loupedeck.StudioOneMidiPlugin.StudioOneMidiPlugin;

    public class ChannelFader : ActionEditorAdjustment
	{
		private StudioOneMidiPlugin plugin = null;

        private const String ChannelSelector = "channelSelector";
        private const String ControlOrientationSelector = "controlOrientationSelector";

        private SelectButtonMode selectMode = SelectButtonMode.Select;

		public ChannelFader() : base(true)
		{

            this.DisplayName = "Channel Fader";
            this.Description = "Channel fader.\nButton press -> Mute\nScreen touch -> Select\nScreen double tap -> Arm/rec";
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
			plugin = base.Plugin as StudioOneMidiPlugin;
			plugin.channelFader = this;

			plugin.ChannelDataChanged += (object sender, EventArgs e) => {
				ActionImageChanged();
			};

            plugin.SelectModeChanged += (object sender, SelectButtonMode e) =>
            {
                this.selectMode = e;
                ActionImageChanged();
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
            
			MackieChannelData cd = this.GetChannel(channelIndex);

			var bb = new BitmapBuilder(imageWidth, imageHeight);

            int sideBarW = 8;
            int piW = (bb.Width - 2* sideBarW)/ 2;
            int piH = 8;

            if (this.selectMode == SelectButtonMode.Select)
            {
                if (cd.Muted || cd.Solo)
                {
                    bb.FillRectangle(
                        0, 0, bb.Width, bb.Height,
                        ChannelProperty.PropertyColor[cd.Muted ? (int)ChannelProperty.PropertyType.Mute : (int)ChannelProperty.PropertyType.Solo]
                        );
                }
                if (cd.Selected && cd.ChannelID < StudioOneMidiPlugin.ChannelCount)
                {
                    bb.FillRectangle(0, 0, sideBarW, bb.Height, ChannelProperty.PropertyColor[(int)ChannelProperty.PropertyType.Select]);
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
            //			bb.DrawText(cd.TrackName, 0, 0, bb.Width, bb.Height / 2, null, imageSize == PluginImageSize.Width60 ? 12 : 1);
            //            bb.DrawText($"{Math.Round(cd.Value * 100.0f)} %", 0, bb.Height / 2, bb.Width, bb.Height / 2);
            bb.DrawText(cd.ValueStr, 0, bb.Height / 4, bb.Width, bb.Height / 2);
//            bb.DrawText(cd.Value.ToString(), 0, bb.Height / 4, bb.Width, bb.Height / 2);
            return bb.ToImage();
		}

		private MackieChannelData GetChannel(string actionParameter)
		{
			return plugin.mackieChannelData[actionParameter];
		}

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (!actionParameters.TryGetString(ChannelSelector, out var channelIndex)) return false;

            MackieChannelData cd = GetChannel(channelIndex);
            cd.EmitChannelPropertyPress(ChannelProperty.PropertyType.Mute);

            return true;
        }

            // This never gets called in the current version of the Loupedeck SDK.
            // 
            // protected override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
            // {
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

            // This gets called when the dial is pressed, but does not react to the
            // corresponding touch screen area at all. Could be used to catch a long press
            // or double click on the dial.
            //
            // protected override bool ProcessButtonEvent2(string actionParameter, DeviceButtonEvent2 buttonEvent)
            // {
            //    MackieChannelData cd = GetChannel(actionParameter);
            //    if (buttonEvent.EventType.IsButtonPressed())
            //        cd.EmitBoolPropertyPress(ChannelProperty.BoolType.Select);
            //
            //    return base.ProcessButtonEvent2(actionParameter, buttonEvent);
            // }

        }
    }
