namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;

    using System;
    using System.Collections.Generic;
    
    class MackieCommand : PluginDynamicCommand
	{
		StudioOneMidiPlugin plugin;

		private class ButtonData
		{
			public int Code;
			public string Name;
			public string IconName;

			public bool Activated = false;

			public BitmapColor OffColor = BitmapColor.Black;
			public BitmapColor OnColor = new BitmapColor(64, 64, 64);
			public BitmapImage Icon, IconOn;
		}

		private IDictionary<string, ButtonData> buttonData = new Dictionary<string, ButtonData>();

		public MackieCommand()
		{
			this.AddButton(new ButtonData
			{
				Code = 94,
				Name = "Play",
				IconName = "play",
				OnColor = new BitmapColor(0, 164, 0),
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
				OnColor = new BitmapColor(128, 0, 0)
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
				IconName = "repeat",
				OnColor = new BitmapColor(0, 57, 148),
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

		protected override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
		{
			if (touchEvent.EventType == DeviceTouchEventType.Press)        HandlePress(actionParameter, true);
			else if (touchEvent.EventType == DeviceTouchEventType.TouchUp) HandlePress(actionParameter, false);

			return base.ProcessTouchEvent(actionParameter, touchEvent);
		}

		protected override bool ProcessButtonEvent2(string actionParameter, DeviceButtonEvent2 buttonEvent)
        {
			if (buttonEvent.EventType.IsButtonPressed()) HandlePress(actionParameter, true);
			else                                         HandlePress(actionParameter, false);

			return base.ProcessButtonEvent2(actionParameter, buttonEvent);
		}

		protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == null)
				return null;

			if (!buttonData.ContainsKey(actionParameter))
				return null;

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

            return bb.ToImage();
		}

		private void HandlePress(string actionParameter, bool pressed)
        {
			if(plugin.mackieMidiOut == null)
            {
				plugin.OpenConfigWindow();
				return;
			}

			int param = Int32.Parse(actionParameter);

			NoteOnEvent e = new NoteOnEvent();
			e.Velocity = (SevenBitNumber)(pressed ? 127 : 0);
			e.NoteNumber = (SevenBitNumber)(param);
			plugin.mackieMidiOut.SendEvent(e);

			ActionImageChanged(actionParameter);
		}

		private void AddButton(ButtonData bd)
        {
            if (bd.IconName != null)
            {
                bd.Icon = EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{bd.IconName}_52px.png"));
                string iconNameOn = $"{bd.IconName}_on_52px.png";
                if (System.IO.File.Exists(iconNameOn))
                {
                    bd.IconOn = EmbeddedResources.ReadImage(EmbeddedResources.FindFile(iconNameOn));
                }
            }

			buttonData[bd.Code.ToString()] = bd;
			AddParameter(bd.Code.ToString(), bd.Name, "Mackie control");
		}

	}
}
