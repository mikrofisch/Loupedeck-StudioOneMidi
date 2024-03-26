namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Shapes;

    class MackieCommand : PluginDynamicCommand
	{
		StudioOneMidiPlugin plugin;

		private class ButtonData
		{
			public int Code;
            public int CodeOn = 0;              // alternative code to send when activated
			public string Name;
			public string IconName;

			public bool Activated = false;

			public BitmapColor OffColor = BitmapColor.Black;
			public BitmapColor OnColor  = BitmapColor.Black;
			public BitmapImage Icon, IconOn;
		}

		private IDictionary<string, ButtonData> buttonData = new Dictionary<string, ButtonData>();

		public MackieCommand()
		{
			this.AddButton(new ButtonData
			{
				Code = 94,
                CodeOn = 93,        // 1st click - play, 2nd click - stop
				Name = "Play",
				IconName = "play",
				// OnColor = new BitmapColor(0, 164, 0),
			});
			this.AddButton(new ButtonData
			{
				Code = 93,
				Name = "Stop",
				IconName = "stop"
			});
			this.AddButton(new ButtonData
			{
				Code = 95,
				Name = "Record",
				IconName = "record",
				// OnColor = new BitmapColor(128, 0, 0)
			}); ;
			this.AddButton(new ButtonData
			{
				Code = 92,
				Name = "Fast forward",
				IconName = "fast_forward"
			});
			this.AddButton(new ButtonData
			{
				Code = 91,
				Name = "Rewind",
				IconName = "rewind"
			});

            this.AddButton(new ButtonData
            {
                Code = 86,
                Name = "Loop",
                IconName = "loop",
                // OnColor = new BitmapColor(0, 57, 148),
            });
            this.AddButton(new ButtonData
            {
                Code = 0x2E,
                Name = "Fader Bank Left",
                IconName = "faderBankLeft",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x2F,
                Name = "Fader Bank Right",
                IconName = "faderBankRight",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x30,
                Name = "Fader Channel Left",
                IconName = "faderChannelLeft",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x31,
                Name = "Fader Channel Right",
                IconName = "faderChannelRight",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x28,
                Name = "TRACK",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x29,
                Name = "SEND",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x2A,
                Name = "VOL/PAN",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x33,
                Name = "GLOBAL",
                OnColor = new BitmapColor(60, 60, 20)
            });
            this.AddButton(new ButtonData
            {
                Code = 0x40,
                Name = "AUDIO",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x42,
                Name = "FX",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x43,
                Name = "BUS",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x44,
                Name = "OUT",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x2A,
                Name = "VOL/PAN",
            });
            this.AddButton(new ButtonData
            {
                Code = 0x32,
                Name = "Flip Volume/Pan",
            });
        }

        protected override bool OnLoad()
		{
			plugin = base.Plugin as StudioOneMidiPlugin;
			plugin.MackieNoteReceived += this.OnMackieNoteReceived;

			return base.OnLoad();
		}

		protected void OnMackieNoteReceived(object sender, NoteOnEvent e)
		{
			string param = e.NoteNumber.ToString();

            if (!this.buttonData.ContainsKey(param))
            {
                return;
            }

			ButtonData bd = this.buttonData[param];
			bd.Activated = e.Velocity > 0;
			this.ActionImageChanged(param);
		}


        protected override void RunCommand(string actionParameter)
        {
            if (!buttonData.ContainsKey(actionParameter))
                return;

            ButtonData bd = buttonData[actionParameter];
            int param = (SevenBitNumber)(bd.Code);
            if (bd.Activated && (bd.CodeOn > 0))
            {
                param = (SevenBitNumber)(bd.CodeOn);
            }

            // int param = Int32.Parse(actionParameter);

            NoteOnEvent e = new NoteOnEvent();
            e.Velocity = (SevenBitNumber)(127);
            e.NoteNumber = (SevenBitNumber)(param);
            plugin.mackieMidiOut.SendEvent(e);
        }

 		protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == null) return null;
			if (!buttonData.ContainsKey(actionParameter)) return null;

			ButtonData bd = buttonData[actionParameter];

			var bb = new BitmapBuilder(imageSize);
            bb.FillRectangle(0, 0, bb.Width, bb.Height, bd.Activated ? bd.OnColor : bd.OffColor);

            if (bd.Activated && bd.IconOn != null)
            {
                bb.DrawImage(bd.IconOn);
            }
            else if (bd.Icon != null)
            {
                bb.DrawImage(bd.Icon);
            }
            else if (bd.Code == 0x32)  // Flip Pan/Vol
            {
                BitmapColor cRectOn  = BitmapColor.White;
                BitmapColor cTextOn  = BitmapColor.Black;
                BitmapColor cRectOff = new BitmapColor(50, 50, 50);
                BitmapColor cTextOff = new BitmapColor(160, 160, 160);

                int rY = 16;
                int rS = 8;
                int rW = bb.Width - 24;
                int rH = (bb.Height - 2 * rY - rS) / 2;
                int rX = (bb.Width - rW) / 2;

                bb.FillRectangle(rX, rY, rW, rH, bd.Activated ? cRectOff : cRectOn);
                bb.DrawText("VOL", rX, rY, rW, rH, bd.Activated ? cTextOff : cTextOn, rH - 6);

                bb.FillRectangle(rX, rY + rH + rS, rW, rH, bd.Activated ? cRectOn : cRectOff);
                bb.DrawText("PAN", rX, rY + rH + rS, rW, rH, bd.Activated ? cTextOn : cTextOff, rH - 6);
            }
            else
            {
                bb.DrawText(bd.Name, 0, 0, bb.Width, bb.Height, null, 16);
            }

            return bb.ToImage();
		}

		private void AddButton(ButtonData bd)
        {
            if (bd.IconName != null)
            {
                bd.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{bd.IconName}_52px.png"));
                string iconResOn = EmbeddedResources.FindFile($"{bd.IconName}_on_52px.png");
                if (iconResOn != null)
                {
                    bd.IconOn = EmbeddedResources.ReadImage(iconResOn);
                }
            }

			buttonData[bd.Code.ToString()] = bd;
			AddParameter(bd.Code.ToString(), bd.Name, "Control");
		}

	}
}
