namespace Loupedeck.StudioOneMidiPlugin
{
	public class ChannelProperty
	{

		public enum PropertyType
		{
			Select,
            Mute,
			Solo,
			Arm,
            Monitor
		}

		public static BitmapColor[] PropertyColor =
		{
             new BitmapColor(60, 60, 60), // Select
			 new BitmapColor(166, 42, 40), // Mute
			 new BitmapColor(217, 177, 69), // Solo
			 new BitmapColor(242, 88, 88), // Arm
			 new BitmapColor(101, 183, 205), // Monitor
		};

		public static int[] MidiBaseNote = { 24, 16, 8, 0, 120};

		public static string[] PropertyName = { "Select", "Mute", "Solo", "Rec", "Mon" };
		public static string[] PropertyLetter = { "-", "M", "S", "R", "M" };

	}
}
