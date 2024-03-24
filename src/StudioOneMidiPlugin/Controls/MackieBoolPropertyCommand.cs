namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    class MackieSelectedChannelBoolPropertyCommand : PluginDynamicCommand
	{

		StudioOneMidiPlugin plugin;

		public MackieSelectedChannelBoolPropertyCommand()
		{
			this.Description = "Control for currently selected channel";

			for (int i = 0; i <= StudioOneMidiPlugin.MackieChannelCount; i++)
			{
				string prefix = $"{i}:";
				string chstr = i == StudioOneMidiPlugin.MackieChannelCount ? " (Selected channel)" : $" (CH {i + 1})";

                if (i < StudioOneMidiPlugin.MackieChannelCount)
                {
                    AddParameter(prefix + ((int)ChannelProperty.BoolType.Select).ToString(), "Select" + chstr, "Select");
                }
                AddParameter(prefix + ((int)ChannelProperty.BoolType.Mute).ToString(), "Mute" + chstr, "Mute");
				AddParameter(prefix + ((int)ChannelProperty.BoolType.Solo).ToString(), "Solo " + chstr, "Solo");
                AddParameter(prefix + ((int)ChannelProperty.BoolType.Arm).ToString(), "Arm/Rec" + chstr, "Arm/Rec");
                AddParameter(prefix + ((int)ChannelProperty.BoolType.Monitor).ToString(), "Monitor" + chstr, "Monitor");
            }
        }

		protected override bool OnLoad()
		{
			plugin = base.Plugin as StudioOneMidiPlugin;

			plugin.MackieDataChanged += (object sender, EventArgs a) => {
				ActionImageChanged();
			};

			return true;
		}

		protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
		{
			if (actionParameter == null) return null;

			ParamData pd = GetParamData(actionParameter);
			MackieChannelData cd = pd.channelData;
			int param = pd.param;

			var bb = new BitmapBuilder(imageSize);

			BitmapColor c = ChannelProperty.boolPropertyColor[param];
			bb.FillRectangle(0, 0, bb.Width, bb.Height, BitmapColor.Black);
			bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(c.R, c.G, c.B, cd.BoolProperty[param] ? 255 : 32));

			const int trackNameH = 24;
			bb.DrawText(cd.TrackName, 0, 0, bb.Width, trackNameH);
			bb.DrawText(ChannelProperty.boolPropertyLetter[param], 0, trackNameH, bb.Width, bb.Height - trackNameH, null, 32);

			return bb.ToImage();
		}

		protected override void RunCommand(string actionParameter)
		{
			if (plugin.mackieMidiOut == null)
			{
				plugin.OpenConfigWindow();
				return;
			}

			ParamData pd = GetParamData(actionParameter);
			MackieChannelData cd = pd.channelData;
			int param = pd.param;

			// if (cd.IsMasterChannel) return;

			cd.EmitBoolPropertyPress((ChannelProperty.BoolType)param);
		}

		private ParamData GetParamData(string actionParameter)
		{
			var dt = actionParameter.Split(':');
			return new ParamData
			{
				param = Int32.Parse(dt[1]),
                channelData = plugin.mackieChannelData[dt[0]]
//                channelData = (dt[0] == StudioOneMidiPlugin.MackieChannelCount.ToString()) ? plugin.MackieSelectedChannel : plugin.mackieChannelData[dt[0]]
            };
		}

		private class ParamData
		{
			public int param;
			public MackieChannelData channelData;
		}

	}
}
