namespace Loupedeck.StudioOneMidiPlugin.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class ColorFinder
    {
        private class ColorSettings
        {
            public BitmapColor OnColor = BitmapColor.Transparent;
            public BitmapColor OffColor = BitmapColor.Transparent;
            public BitmapColor TextOnColor = BitmapColor.White;
            public BitmapColor TextOffColor = BitmapColor.White;
            public String IconNameOff, IconNameOn;
        }
        private Dictionary<(String, String), ColorSettings> ColorDict = new Dictionary<(String, String), ColorSettings>();

        public ColorFinder()
        {
            this.ColorDict.Add(("#any", "Bypass"), new ColorSettings { OnColor = new BitmapColor(204, 156, 107) });
        }

        BitmapColor getOnColor(String pluginName, String parameterName)
        {
            if (this.ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings))
            {
                return colorSettings.OnColor;
            }
            return BitmapColor.Transparent;
        }

        BitmapColor getOffColor(String pluginName, String parameterName)
        {
            if (this.ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings))
            {
                return colorSettings.OffColor;
            }
            return BitmapColor.Transparent;
        }

        BitmapColor getTextOnColor(String pluginName, String parameterName)
        {
            if (this.ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings))
            {
                return colorSettings.TextOnColor;
            }
            return BitmapColor.White;
        }
        BitmapColor getTextOffColor(String pluginName, String parameterName)
        {
            if (this.ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings))
            {
                return colorSettings.TextOffColor;
            }
            return BitmapColor.White;
        }

        BitmapImage getIconOff(String pluginName, String parameterName)
        {
            return null;
        }
        BitmapImage getIconOn(String pluginName, String parameterName)
        {
            return null;
        }
    }
}
