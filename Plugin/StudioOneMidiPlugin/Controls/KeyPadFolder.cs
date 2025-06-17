
namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using Melanchall.DryWetMidi.Common;
    using Melanchall.DryWetMidi.Core;
    using Melanchall.DryWetMidi.Multimedia;

    using System;
    using System.Collections.Generic;
    using System.Windows.Navigation;

    class KeyPadFolder : PluginDynamicFolder
	{
		static string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
        static BitmapColor[] NoteColors = 
        {
            new BitmapColor(0x10, 0x10, 0xFF), // C
            new BitmapColor(0x00, 0x00, 0x00), // C#
            new BitmapColor(0xFF, 0xFF, 0xFF), // D
            new BitmapColor(0x00, 0x00, 0x00), // D#
            new BitmapColor(0xFF, 0xFF, 0xFF), // E
            new BitmapColor(0xFF, 0xFF, 0xFF), // F
            new BitmapColor(0x00, 0x00, 0x00), // F#
            new BitmapColor(0xFF, 0xFF, 0xFF), // G
            new BitmapColor(0x00, 0x00, 0x00), // G#
            new BitmapColor(0xFF, 0xFF, 0xFF), // A
            new BitmapColor(0x00, 0x00, 0x00), // A#
            new BitmapColor(0xFF, 0xFF, 0xFF)  // B
        };

        private OutputDevice? MidiOut = null;
        FourBitNumber MidiChannel = (FourBitNumber)0;
        string midiOutName = "";
        public String MidiOutName
        {
            get => this.midiOutName;
            set
            {
                if (this.MidiOut != null)
                {
                    this.MidiOut.Dispose();
                }

                this.midiOutName = value;
                try
                {
                    this.MidiOut = OutputDevice.GetByName(value);
                    //this.SetPluginSetting("ConfigMidiOut", value, false);
                }
                catch (Exception)
                {
                    this.MidiOut = null;
                }
            }
        }

        class PadLayout
		{
			public string? Name;

			public delegate string NoteNameT(CommandParams p);
			public delegate int NoteNumberT(CommandParams p);
            public delegate BitmapColor NoteColorT(CommandParams p);

            public NoteNameT NoteName;
			public NoteNumberT NoteNumber = (CommandParams) => 0;
            public NoteColorT NoteColor;

            public PadLayout()
            {
                NoteName = (CommandParams p) =>
                {
                    int id = NoteNumber(p);
                    return NoteNames[id % NoteNames.Length] + ((id / NoteNames.Length) - 2).ToString();
                };
                NoteColor = (CommandParams p) =>
                {
                    return NoteColors[NoteNumber(p) % NoteColors.Length];
                };
            }
		}

		IList<PadLayout> layouts;
		PadLayout currentLayout;
		int currentLayoutIx = 0;

		int transpose = 0;

		class Adjustment
		{
			public string Name = "";

			public int value = 0;

			public delegate void AdjustT(int delta);
			public delegate void ReleaseT();

			public AdjustT? Adjust;
			public ReleaseT? Release;
		}
		IList<Adjustment> adjustments;
		Adjustment currentHorizontalAdjustment, currentVerticalAdjustment;
		int currentHorizontalAdjustmentIx = 0, currentVerticalAdjustmentIx = 1;

		public KeyPadFolder()
		{
			this.DisplayName = "KeyPad";
			this.GroupName = "KeyPad";

			layouts = new List<PadLayout>();
			adjustments = new List<Adjustment>();


			const int C1MidiCode = 36;

			{
				PadLayout lt = new PadLayout();
				lt.Name = "Halft";
				lt.NoteNumber = (CommandParams p) => C1MidiCode + p.ix + transpose;
				layouts.Add(lt);
			}

			{
				int[] nums = { 0, 2, 4, 5, 7, 9, 11 };

				PadLayout lt = new PadLayout();
				lt.Name = "Hepta";
				lt.NoteNumber = (CommandParams p) => C1MidiCode + nums[p.ix % nums.Length] + (p.ix / nums.Length) * 12 + transpose;
				layouts.Add(lt);
			}

			{
				int[] nums = { 0, 2, 4, 7, 9 };

				PadLayout lt = new PadLayout();
				lt.Name = "Penta";
				lt.NoteNumber = (CommandParams p) => C1MidiCode + nums[p.ix % nums.Length] + (p.ix / nums.Length) * 12 + transpose;
				layouts.Add(lt);
			}

			{
				Adjustment adj = new Adjustment();
				adj.Name = "Mod";
				adj.Adjust = (int delta) =>
                {
					adj.value += delta;

					SendMidiEvent(new ControlChangeEvent { ControlNumber = (SevenBitNumber)1,
                                                           ControlValue = (SevenBitNumber)Math.Max(0, Math.Min(adj.value * 10, 127)) });
				};
				adjustments.Add(adj);
			}

			{
				Adjustment adj = new Adjustment();
				adj.Name = "Pitch Bend";
				adj.Adjust = (int delta) =>
                {
                    adj.value += delta;
					SendMidiEvent(new PitchBendEvent { PitchValue = (ushort)Math.Max(0, Math.Min(8192 + adj.value * 500, 16383)) });
				};

				// Reset the pitch bend after release
				adj.Release = () =>
                {
					adj.value = 0;

					var e = new PitchBendEvent();
					e.PitchValue = 8192;
					SendMidiEvent(e);
				};
				adjustments.Add(adj);
			}

			{
				Adjustment adj = new Adjustment();
				adj.Name = "None";
				adjustments.Add(adj);
			}

			currentLayout = layouts[currentLayoutIx];
			currentHorizontalAdjustment = adjustments[currentHorizontalAdjustmentIx];
			currentVerticalAdjustment = adjustments[currentVerticalAdjustmentIx];
		}

		public override bool Load()
		{
			var result = base.Load();

            MidiOutName = "Loupedeck KeyPad";

			return result;
		}

        public override PluginDynamicFolderNavigation GetNavigationArea(DeviceType _)
        {
            return PluginDynamicFolderNavigation.None;
        }

        public override IEnumerable<string> GetButtonPressActionNames(DeviceType deviceType)
		{
			var lst = new List<string>();

			// Commands for buttons
			for (int i = 0; i < 12; i++)
			{
				lst.Add(CreateCommandName(i.ToString()));
			}
			return lst;
		}

		public override IEnumerable<string> GetEncoderRotateActionNames(DeviceType deviceType)
        {
			var lst = new List<string>();

			lst.Add(CreateAdjustmentName("layout"));
			lst.Add(CreateAdjustmentName("hAdj"));
			lst.Add(CreateAdjustmentName("vAdj"));
			lst.Add(CreateAdjustmentName("trans"));

			return lst;
		}

		public override string? GetAdjustmentDisplayName(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == "layout")     return "Grid\n" + currentLayout.Name;
			else if (actionParameter == "hAdj")  return "<>\n" + currentHorizontalAdjustment.Name;
			else if (actionParameter == "vAdj")  return "/\\/\n" + currentVerticalAdjustment.Name;
			else if (actionParameter == "trans") return "Trans\n" + (transpose > 0 ? "+" : "") + transpose.ToString();

			return null;
		}
		public override string? GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == null) return null;

			return currentLayout.NoteName(CommandParams.parse(actionParameter));
		}

		public override BitmapImage? GetCommandImage(string actionParameter, PluginImageSize imageSize)
        {
			if (actionParameter == null) return null;

			var bb = new BitmapBuilder(imageSize);
            var bgColor = currentLayout.NoteColor(CommandParams.parse(actionParameter));
            bb.FillRectangle(0, 0, bb.Width, bb.Height, bgColor);
            bb.DrawText(GetCommandDisplayName(actionParameter, imageSize), bgColor.A + bgColor.G + bgColor.B < 600 ? BitmapColor.White : BitmapColor.Black);
			return bb.ToImage();
		}

		class TouchData
		{
			public int originX, originY;
		}
		IDictionary<int, TouchData> touchData = new Dictionary<int, TouchData>();

		public override bool ProcessTouchEvent(string actionParameter, DeviceTouchEvent touchEvent)
        {
			if (actionParameter == null)
				return false;

			var p = CommandParams.parse(actionParameter);
			int noteNumber = currentLayout.NoteNumber(p);
			int ix = p.ix;

			if (touchEvent.EventType == DeviceTouchEventType.Press && !touchData.ContainsKey(ix))
            {
				var td = new TouchData
				{
					originX = touchEvent.X,
					originY = touchEvent.Y,
				};
				touchData.Add(ix, td);

                SendMidiEvent(new NoteOnEvent { Velocity = (SevenBitNumber)127, NoteNumber = (SevenBitNumber)noteNumber });

                return true;
			}
			else if (touchEvent.EventType == DeviceTouchEventType.TouchUp && touchData.ContainsKey(ix))
            {
				touchData.Remove(ix);

				SendMidiEvent(new NoteOffEvent { Velocity = (SevenBitNumber)127, NoteNumber = (SevenBitNumber)noteNumber });

				if (currentHorizontalAdjustment.Release != null)
					currentHorizontalAdjustment.Release();

				if (currentVerticalAdjustment.Release != null)
					currentVerticalAdjustment.Release();

				return true;
			}
			else if (touchData.ContainsKey(ix))
            {
				if (touchEvent.DeltaX != 0 && currentHorizontalAdjustment.Adjust != null)
					currentHorizontalAdjustment.Adjust(touchEvent.DeltaX);

				if (touchEvent.DeltaY != 0 && currentVerticalAdjustment.Adjust != null)
					currentVerticalAdjustment.Adjust(-touchEvent.DeltaY);

				return true;
			}

			return base.ProcessTouchEvent(actionParameter, touchEvent);
		}

		static int mod(int a, int b)
        {
			int r = a % b;
			if (r < 0)
				r += b;

			return r;
		}

		public override bool ProcessEncoderEvent(string actionParameter, DeviceEncoderEvent encoderEvent)
        {
			if (actionParameter == "layout")
            {
				currentLayoutIx = mod(currentLayoutIx + encoderEvent.Clicks, layouts.Count);
				currentLayout = layouts[currentLayoutIx];
				CommandImageChanged(null);
				AdjustmentImageChanged("layout");
			}
			else if (actionParameter == "hAdj")
            {
				currentHorizontalAdjustmentIx = mod(currentHorizontalAdjustmentIx + encoderEvent.Clicks, adjustments.Count);
				currentHorizontalAdjustment = adjustments[currentHorizontalAdjustmentIx];
				AdjustmentImageChanged("hAdj");
			}
			else if (actionParameter == "vAdj")
            {
				currentVerticalAdjustmentIx = mod(currentVerticalAdjustmentIx + encoderEvent.Clicks, adjustments.Count);
				currentVerticalAdjustment = adjustments[currentVerticalAdjustmentIx];
				AdjustmentImageChanged("vAdj");
			}
			else if(actionParameter == "trans")
            {
				transpose += encoderEvent.Clicks;
				transpose = Math.Min(12 * 4, Math.Max(-12 * 2, transpose));
				CommandImageChanged(null);
				AdjustmentImageChanged("trans");
			}

			return base.ProcessEncoderEvent(actionParameter, encoderEvent);
		}

		private class CommandParams
		{
			// Position on the loupedeck grid
			public int x, y;

			// General index of the param
			public int ix;

			public static CommandParams parse(string actionParameter)
            {
				int ix = Int32.Parse(actionParameter);
				return new CommandParams { x = ix % 4, y = ix / 4, ix = ix };
			}
		}

        private void SendMidiEvent(ChannelEvent e)
        {   
            if (MidiOut != null)
            {
                e.Channel = MidiChannel;
                MidiOut.SendEvent(e);
            }
        }
	}
}
    