using PluginSettings;


namespace Loupedeck.StudioOneMidiPlugin.Helpers
{
    internal class ColorConv
    {
        public static BitmapColor Convert(FinderColor? color)
        {
            if (color == null)
            {
                return BitmapColor.Transparent; // Default color if null
            }
            return new BitmapColor(color.R, color.G, color.B, color.A);
        }
        public static FinderColor Convert(BitmapColor color)
        {
            return new FinderColor(color.R, color.G, color.B, color.A);
        }
    }
}
