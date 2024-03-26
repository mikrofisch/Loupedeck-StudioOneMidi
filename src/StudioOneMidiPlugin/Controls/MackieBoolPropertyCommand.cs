namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;

    class MackieSelectedChannelBoolPropertyCommand : PluginDynamicCommand
	{

		StudioOneMidiPlugin plugin;

        private class ButtonData
        {
            public ChannelProperty.BoolType Type = ChannelProperty.BoolType.Select;
            public string Name;
//            public string IconName;

//            public bool Activated = false;

//            public BitmapColor OnColor = new BitmapColor(64, 64, 64);
            public BitmapImage Icon, IconSelMon, IconSelRec;
        }

        private IDictionary<string, ButtonData> buttonData = new Dictionary<string, ButtonData>();

        public MackieSelectedChannelBoolPropertyCommand()
		{
			this.Description = "Control for currently selected channel";

			for (int i = 0; i <= StudioOneMidiPlugin.MackieChannelCount; i++)
			{
                if (i < StudioOneMidiPlugin.MackieChannelCount)
                {
                    AddButton(i, ChannelProperty.BoolType.Select, "Select");
                }

                AddButton(i, ChannelProperty.BoolType.Mute, "Mute");
                AddButton(i, ChannelProperty.BoolType.Solo, "Solo");
                AddButton(i, ChannelProperty.BoolType.Arm, "Arm/Rec", "record");
                AddButton(i, ChannelProperty.BoolType.Monitor, "Monitor", "monitor");
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

            ButtonData bd = this.buttonData[$"{pd.channelIndex}:{ChannelProperty.boolPropertyName[param]}"];

            var bb = new BitmapBuilder(imageSize);

            BitmapColor c = ChannelProperty.boolPropertyColor[param];
            if (cd.BoolProperty[param])
                bb.FillRectangle(0, 0, bb.Width, bb.Height, ChannelProperty.boolPropertyColor[param]);
            else
                bb.FillRectangle(0, 0, bb.Width, bb.Height, new BitmapColor(20, 20, 20));

			const int trackNameH = 24;
            if (bd.Type == ChannelProperty.BoolType.Select)
            {
                int rX = 8;
                int rY = trackNameH+4;
                int rS = 8;
                int rW = (bb.Width-rS)/2-rX;
                int rH = (bb.Height - rY)/2 - rS;
                int rX2 = rX + rW + rS;
                int rY2 = rY + rH + rS;

                bb.FillRectangle(rX - 2, rY2 - 6, 2 * rW + 10, 2, new BitmapColor(40, 40, 40));
                bb.FillRectangle(rX2 - 6, rY - 2, 2, rH * 2 + 10, new BitmapColor(40, 40, 40));

                if (cd.Muted)
                    bb.FillRectangle(rX - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Mute]);
                if (cd.Solo)
                    bb.FillRectangle(rX2 - 2, rY - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Solo]);
                if (cd.Armed)
                    bb.FillRectangle(rX - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Arm]);
                if (cd.Monitor)
                    bb.FillRectangle(rX2 - 2, rY2 - 2, rW + 4, rH + 4, ChannelProperty.boolPropertyColor[(int)ChannelProperty.BoolType.Monitor]);

                bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Mute], rX, rY, rW, rH, null, rH-4);
                bb.DrawText(ChannelProperty.boolPropertyLetter[(int)ChannelProperty.BoolType.Solo], rX2, rY, rW, rH, null, rH-4);
                bb.DrawImage(bd.IconSelRec, rX + rW / 2 - bd.IconSelRec.Width / 2, rY2 + rH / 2 - bd.IconSelRec.Height / 2);
                bb.DrawImage(bd.IconSelMon, rX2 + rW / 2 - bd.IconSelMon.Width / 2, rY2 + rH / 2 - bd.IconSelRec.Height / 2);
            }
            else
            {
                if (bd.Icon != null)
                {
                    bb.DrawImage(bd.Icon, bb.Width / 2 - bd.Icon.Width / 2, trackNameH);
                }
                else
                {
                    bb.DrawText(ChannelProperty.boolPropertyLetter[param], 0, trackNameH, bb.Width, bb.Height - trackNameH, null, 32);
                }
            }
            bb.DrawText(cd.TrackName, 0, 0, bb.Width, trackNameH);

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
                channelIndex = Int32.Parse(dt[0]),
                param        = Int32.Parse(dt[1]),
                channelData  = plugin.mackieChannelData[dt[0]]
            };
		}

		private class ParamData
		{
            public int channelIndex;
            public int param;
			public MackieChannelData channelData;
		}

        private void AddButton(int i, ChannelProperty.BoolType bt, string name, string iconName = null)
        {
            string prefix = $"{i}:";
            string chstr = i == StudioOneMidiPlugin.MackieChannelCount ? " (Selected channel)" : $" (CH {i + 1})";

            ButtonData bd = new ButtonData
            {
                Type = bt,
                Name = prefix + ChannelProperty.boolPropertyName[(int)bt]
            };

            if (iconName != null)
            {
                bd.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{iconName}_52px.png"));
            }

            if (bt == ChannelProperty.BoolType.Select)
            {
                bd.IconSelMon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("monitor_24px.png"));
                bd.IconSelRec = EmbeddedResources.ReadImage(EmbeddedResources.FindFile("record_24px.png"));
            }

            buttonData[bd.Name] = bd;
            AddParameter(prefix + ((int)bd.Type).ToString(), name + chstr, name);
        }


    }
}
