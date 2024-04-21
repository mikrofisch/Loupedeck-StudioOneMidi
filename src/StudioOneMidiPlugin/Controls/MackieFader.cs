namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    
    public class MackieFader : PluginDynamicAdjustment
	{
		private StudioOneMidiPlugin plugin = null;
        private SelectButtonData.Mode selectMode = SelectButtonData.Mode.Select;

		public MackieFader() : base(true)
		{
			this.Description = "Channel fader.\nButton press -> Mute\nScreen touch -> Select\nScreen double tap -> Arm/rec";

			for (int i = 0; i < StudioOneMidiPlugin.ChannelCount; i++)
			{
				AddParameter(i.ToString(), $"Fader (CH {i + 1})", "Faders");
			}
			AddParameter(StudioOneMidiPlugin.ChannelCount.ToString(), $"Fader (Selected Channel)", "Faders");
		}

		protected override bool OnLoad()
		{
			plugin = base.Plugin as StudioOneMidiPlugin;
			plugin.mackieFader = this;

			plugin.ChannelDataChanged += (object sender, EventArgs e) => {
				ActionImageChanged();
			};

            plugin.SelectModeChanged += (object sender, SelectButtonData.Mode e) =>
            {
                this.selectMode = e;
                ActionImageChanged();
            };
            
            return true;
		}

		protected override void ApplyAdjustment(string actionParameter, int diff)
		{
			if (plugin.mackieMidiOut == null)
			{
				plugin.OpenConfigWindow();
				return;
			}

			MackieChannelData cd = GetChannel(actionParameter);

			cd.Value = Math.Min(1, Math.Max(0, (float)Math.Round(cd.Value * 100 + diff) / 100));
			cd.EmitVolumeUpdate();
		}

		protected override void RunCommand(string actionParameter)
		{
			MackieChannelData cd = GetChannel(actionParameter);
			cd.EmitBoolPropertyPress(ChannelProperty.PropertyType.Mute);
		}

		protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
		{
			if (actionParameter == null) return null;

			MackieChannelData cd = GetChannel(actionParameter);

			var bb = new BitmapBuilder(imageSize);

            int sideBarW = 8;
            int piW = (bb.Width - 2* sideBarW)/ 2;
            int piH = 8;

            if (this.selectMode == SelectButtonData.Mode.Select)
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
