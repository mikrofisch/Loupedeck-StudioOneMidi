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
            this.ColorDict.Add(("", "Bypass"), new ColorSettings { OnColor = new BitmapColor(204, 156, 107) });
        }

        private ColorSettings getColorSettings(String pluginName, String parameterName)
        {
            if (this.ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings) ||
                this.ColorDict.TryGetValue(("", parameterName), out colorSettings))
            {
                return colorSettings;
            }
            return new ColorSettings();
        }

        public BitmapColor getOnColor(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).OnColor;
        public BitmapColor getOffColor(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).OffColor;
        public BitmapColor getTextOnColor(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).TextOnColor;
        public BitmapColor getTextOffColor(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).TextOffColor;

        
        public BitmapImage getIconOff(String pluginName, String parameterName)
        {
            return null;
        }
        public BitmapImage getIconOn(String pluginName, String parameterName)
        {
            return null;
        }
    }
}
