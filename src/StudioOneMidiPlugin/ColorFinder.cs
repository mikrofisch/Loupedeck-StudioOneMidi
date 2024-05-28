namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;


    public class ColorFinder
    {
        public static readonly BitmapColor NoColor = new BitmapColor(-1, -1, -1);
        public class ColorSettings
        {
            public BitmapColor OnColor = NoColor;
            public BitmapColor OffColor = NoColor;
            public BitmapColor TextOnColor = NoColor;
            public BitmapColor TextOffColor = NoColor;
            public String IconName, IconNameOn;
            public String Label;
            public String LinkedParameter;
        }
        private static readonly Dictionary<(String, String), ColorSettings> ColorDict = new Dictionary<(String, String), ColorSettings>();
        public ColorSettings DefaultColorSettings = new ColorSettings
        {
            OnColor = BitmapColor.Transparent,
            OffColor = BitmapColor.Transparent,
            TextOnColor = BitmapColor.White,
            TextOffColor = BitmapColor.White
        };

        public ColorFinder()
        {
            this.InitColorDict();
        }

        public ColorFinder(ColorSettings defaultColorSettings)
        {
            this.DefaultColorSettings = defaultColorSettings;
            this.InitColorDict();
        }

        internal class ProEqTopControlColors : ColorSettings
        {
            public ProEqTopControlColors(String label = null)
            {
                this.OnColor = new BitmapColor(54, 84, 122);
                this.OffColor = new BitmapColor(27, 34, 37);
                this.TextOffColor = new BitmapColor(58, 117, 195);
                this.Label = label;
            }
        }
        private void InitColorDict()
        {
            if (ColorDict.Count == 0)
            {
                ColorDict.Add(("", "Bypass"), new ColorSettings { OnColor = new BitmapColor(204, 156, 107), IconName = "bypass" });
                ColorDict.Add(("Pro EQ", "Show Controls"), new ProEqTopControlColors(label: "Band Controls"));
                ColorDict.Add(("Pro EQ", "Show Dynamics"), new ProEqTopControlColors(label: "Dynamics"));
                ColorDict.Add(("Pro EQ", "High Quality"),  new ProEqTopControlColors());
                ColorDict.Add(("Pro EQ", "View Mode"),     new ProEqTopControlColors(label: "Curves"));
                ColorDict.Add(("Pro EQ", "LF-Active"),  new ColorSettings { TextOnColor = new BitmapColor(255, 120,  38) });
                ColorDict.Add(("Pro EQ", "MF-Active"),  new ColorSettings { TextOnColor = new BitmapColor(107, 224,  44) });
                ColorDict.Add(("Pro EQ", "HF-Active"),  new ColorSettings { TextOnColor = new BitmapColor( 75, 212, 250) });
                ColorDict.Add(("Pro EQ", "LMF-Active"), new ColorSettings { TextOnColor = new BitmapColor(245, 205,  58) });
                ColorDict.Add(("Pro EQ", "HMF-Active"), new ColorSettings { TextOnColor = new BitmapColor( 70, 183, 130) });
                ColorDict.Add(("Pro EQ", "LC-Active"), new ColorSettings { TextOnColor = new BitmapColor(255, 74, 61) });
                ColorDict.Add(("Pro EQ", "HC-Active"),  new ColorSettings { TextOnColor = new BitmapColor(158,  98, 255) });
                this.addLinked("Pro EQ", "LF-Gain", "LF-Active");
                this.addLinked("Pro EQ", "MF-Gain", "MF-Active");
                this.addLinked("Pro EQ", "HF-Gain", "HF-Active");
                this.addLinked("Pro EQ", "LMF-Gain", "LMF-Active");
                this.addLinked("Pro EQ", "HMF-Gain", "HMF-Active");
                this.addLinked("Pro EQ", "LC-Frequency", "LC-Active");
                this.addLinked("Pro EQ", "HC-Frequency", "HC-Active");
            }
        }
        private void addLinked(String pluginName, String parameterName, String linkedParameter)
        {
            var colorSettings = ColorDict[(pluginName, linkedParameter)];
            ColorDict.Add((pluginName, parameterName), new ColorSettings { OnColor = colorSettings.OnColor,
                                                                           OffColor = colorSettings.OffColor,
                                                                           TextOnColor = colorSettings.TextOnColor,
                                                                           TextOffColor = colorSettings.TextOffColor,
                                                                           Label = colorSettings.Label,
                                                                           LinkedParameter = linkedParameter
                                                                         });
        }
        public ColorSettings getColorSettings(String pluginName, String parameterName)
        {
            if (ColorDict.TryGetValue((pluginName, parameterName), out var colorSettings) ||
                ColorDict.TryGetValue(("", parameterName), out colorSettings))
            {
                return colorSettings;
            }
            return this.DefaultColorSettings;
        }

        private BitmapColor findOnColor(ColorSettings colorSettings) => colorSettings.OnColor == NoColor ? this.DefaultColorSettings.OnColor : colorSettings.OnColor;
        private BitmapColor findOffColor(ColorSettings colorSettings) => colorSettings.OffColor == NoColor ? this.DefaultColorSettings.OffColor : colorSettings.OffColor;
        private BitmapColor findTextOnColor(ColorSettings colorSettings) => colorSettings.TextOnColor == NoColor ? this.DefaultColorSettings.TextOnColor : colorSettings.TextOnColor;
        private BitmapColor findTextOffColor(ColorSettings colorSettings) => colorSettings.TextOffColor == NoColor ? this.DefaultColorSettings.TextOffColor : colorSettings.TextOffColor;

        public BitmapColor getOnColor(String pluginName, String parameterName) => this.findOnColor(this.getColorSettings(pluginName, parameterName));
        public BitmapColor getOffColor(String pluginName, String parameterName) => this.findOffColor(this.getColorSettings(pluginName, parameterName));
        public BitmapColor getTextOnColor(String pluginName, String parameterName) => this.findTextOnColor(this.getColorSettings(pluginName, parameterName));
        public BitmapColor getTextOffColor(String pluginName, String parameterName) => this.findTextOffColor(this.getColorSettings(pluginName, parameterName));
        public String getLabel(String pluginName, String parameterName)
        {
            var label = this.getColorSettings(pluginName, parameterName).Label;
            if (label == null) label = parameterName;
            return label;
        }
        public String getLabelShort(String pluginName, String parameterName) => stripLabel(this.getLabel(pluginName, parameterName));
        public static String stripLabel(String label)
        {
            if (label.Length <= 10) return label;
            return Regex.Replace(label, "(?<!^)[aeiou](?!$)", "");
        }
        public BitmapImage getIcon(String pluginName, String parameterName)
        {
            var colorSettings = this.getColorSettings(pluginName, parameterName);
            if (colorSettings.IconName != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconName}_52px.png"));
            }
            return null;
        }

        public BitmapImage getIconOn(String pluginName, String parameterName)
        {
            var colorSettings = this.getColorSettings(pluginName, parameterName);
            if (colorSettings.IconNameOn != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconNameOn}_52px.png"));
            }
            return null;
        }
        public String getLinkedParameter(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).LinkedParameter;
    }
}
