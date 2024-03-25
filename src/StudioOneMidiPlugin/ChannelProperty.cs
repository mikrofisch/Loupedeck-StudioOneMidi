using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Loupedeck.StudioOneMidiPlugin
{
	public class ChannelProperty
	{

		public enum BoolType
		{
			Select,
            Mute,
			Solo,
			Arm,
            Monitor
		}

		public static BitmapColor[] boolPropertyColor =
		{
             new BitmapColor(60, 60, 60), // Select
			 new BitmapColor(166, 42, 40), // Mute
			 new BitmapColor(217, 177, 69), // Solo
			 new BitmapColor(242, 88, 88), // Arm
			 new BitmapColor(101, 183, 205), // Monitor
		};

		public static int[] boolPropertyMackieNote = { 24, 16, 8, 0, 120};

		public static string[] boolPropertyName = { "Select", "Mute", "Solo", "Rec", "Mon" };
		public static string[] boolPropertyLetter = { "-", "M", "S", "R", "M" };

	}
}
