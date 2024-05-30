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
            public enum PotMode { Positive, Symmetric };
            public PotMode Mode = PotMode.Positive;

            public BitmapColor OnColor = NoColor;
            public BitmapColor OffColor = NoColor;
            public BitmapColor TextOnColor = NoColor;
            public BitmapColor TextOffColor = NoColor;
            public String IconName, IconNameOn;
            public String Label;
            public String LinkedParameter;

            // For plugin settings
            public const String strTextOnColor = "TextOnColor";
            public const String strLabel = "Label";
            public const String strLinkedParameter = "LinkedParameter";
            public const String strMode = "Mode";
            //public const String[] strModeValue = { "Positive", "Symmetric" };
        }
        private static readonly Dictionary<(String PluginName, String PluginParameter), ColorSettings> ColorDict = new Dictionary<(String, String), ColorSettings>();
        private const String strColorSettingsID = "[cs]";  // for plugin settings

        public ColorSettings DefaultColorSettings = new ColorSettings
        {
            OnColor = BitmapColor.Transparent,
            OffColor = BitmapColor.Transparent,
            TextOnColor = BitmapColor.White,
            TextOffColor = BitmapColor.White
        };

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public ColorFinder() { }

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public ColorFinder(ColorSettings defaultColorSettings)
        {
            this.DefaultColorSettings = defaultColorSettings;
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
        public void Init(Plugin plugin, Boolean forceReload = false)
        {
            if (forceReload)
            {
                ColorDict.Clear();
            }
            if (ColorDict.Count == 0)
            {
                ColorDict.Add(("", "Bypass"), new ColorSettings { OnColor = new BitmapColor(204, 156, 107), IconName = "bypass" });
                ColorDict.Add(("Pro EQ", "Show Controls"), new ProEqTopControlColors(label: "Band Controls"));
                ColorDict.Add(("Pro EQ", "Show Dynamics"), new ProEqTopControlColors(label: "Dynamics"));
                ColorDict.Add(("Pro EQ", "High Quality"), new ProEqTopControlColors());
                ColorDict.Add(("Pro EQ", "View Mode"), new ProEqTopControlColors(label: "Curves"));
                ColorDict.Add(("Pro EQ", "LF-Active"), new ColorSettings { TextOnColor = new BitmapColor(255, 120, 38), Label = "LF Active" });
                ColorDict.Add(("Pro EQ", "MF-Active"), new ColorSettings { TextOnColor = new BitmapColor(107, 224, 44), Label = "MF Active" });
                ColorDict.Add(("Pro EQ", "HF-Active"), new ColorSettings { TextOnColor = new BitmapColor(75, 212, 250), Label = "HF Active" });
                ColorDict.Add(("Pro EQ", "LMF-Active"), new ColorSettings { TextOnColor = new BitmapColor(245, 205, 58), Label = "LMF Active" });
                ColorDict.Add(("Pro EQ", "HMF-Active"), new ColorSettings { TextOnColor = new BitmapColor(70, 183, 130), Label = "HMF Active" });
                ColorDict.Add(("Pro EQ", "LC-Active"), new ColorSettings { TextOnColor = new BitmapColor(255, 74, 61), Label = "LC Active" });
                ColorDict.Add(("Pro EQ", "HC-Active"), new ColorSettings { TextOnColor = new BitmapColor(158, 98, 255), Label = "HC Active" });
                this.addLinked("Pro EQ", "LF-Gain", "LF-Active", label: "LF Gain", mode: ColorSettings.PotMode.Symmetric);
                this.addLinked("Pro EQ", "LF-Frequency", "LF-Active", label: "LF Freq");
                this.addLinked("Pro EQ", "LF-Q", "LF-Active", label: "LF Q");
                this.addLinked("Pro EQ", "MF-Gain", "MF-Active", label: "MF Gain", mode: ColorSettings.PotMode.Symmetric);
                this.addLinked("Pro EQ", "MF-Frequency", "MF-Active", label: "MF Freq");
                this.addLinked("Pro EQ", "MF-Q", "MF-Active", label: "MF Q");
                this.addLinked("Pro EQ", "HF-Gain", "HF-Active", label: "HF Gain", mode: ColorSettings.PotMode.Symmetric);
                this.addLinked("Pro EQ", "HF-Frequency", "HF-Active", label: "HF Freq");
                this.addLinked("Pro EQ", "HF-Q", "HF-Active", label: "HF Q");
                this.addLinked("Pro EQ", "LMF-Gain", "LMF-Active", label: "LMF Gain", mode: ColorSettings.PotMode.Symmetric);
                this.addLinked("Pro EQ", "LMF-Frequency", "LMF-Active", label: "LMF Freq");
                this.addLinked("Pro EQ", "LMF-Q", "LMF-Active", label: "LMF Q");
                this.addLinked("Pro EQ", "HMF-Gain", "HMF-Active", label: "HMF Gain", mode: ColorSettings.PotMode.Symmetric);
                this.addLinked("Pro EQ", "HMF-Frequency", "HMF-Active", label: "HMF Freq");
                this.addLinked("Pro EQ", "HMF-Q", "HMF-Active", label: "HMF Q");
                this.addLinked("Pro EQ", "LC-Frequency", "LC-Active", label: "LC Freq");
                this.addLinked("Pro EQ", "HC-Frequency", "HC-Active", label: "HC Freq");

                var settingsList = plugin.ListPluginSettings();

                foreach (var setting in settingsList)
                {
                    if (setting.StartsWith(strColorSettingsID))
                    {
                        var settingsParsed = setting.Substring(strColorSettingsID.Length).Split('|');
                        if (!ColorDict.TryGetValue((settingsParsed[0], settingsParsed[1]), out var cs))
                        {
                            cs = new ColorSettings { };
                        }
                        if (plugin.TryGetPluginSetting(settingName(settingsParsed[0], settingsParsed[1], settingsParsed[2]), out var val))
                        {
                            switch (settingsParsed[2])
                            {
                                case ColorSettings.strTextOnColor:
                                    cs.TextOnColor = new BitmapColor(Convert.ToByte(val.Substring(0, 2), 16),
                                                                        Convert.ToByte(val.Substring(2, 2), 16),
                                                                        Convert.ToByte(val.Substring(4, 2), 16));
                                    break;
                                case ColorSettings.strLabel:
                                    cs.Label = val;
                                    break;
                                case ColorSettings.strMode:
                                    cs.Mode = val.ParseInt32() == 0 ? ColorSettings.PotMode.Positive : ColorSettings.PotMode.Symmetric;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        private void addLinked(String pluginName, String parameterName, String linkedParameter, String label = null, ColorSettings.PotMode mode = ColorSettings.PotMode.Positive)
        {
            if (label == null) label = parameterName;
            var colorSettings = ColorDict[(pluginName, linkedParameter)];
            ColorDict.Add((pluginName, parameterName), new ColorSettings { Mode = mode,
                                                                           OnColor = colorSettings.OnColor,
                                                                           OffColor = colorSettings.OffColor,
                                                                           TextOnColor = colorSettings.TextOnColor,
                                                                           TextOffColor = colorSettings.TextOffColor,
                                                                           Label = label,
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

        private BitmapColor findColor(BitmapColor settingsColor, BitmapColor defaultColor) => settingsColor == NoColor ? defaultColor : settingsColor;

        public ColorSettings.PotMode getMode(String pluginName, String parameterName) => this.getColorSettings(pluginName, parameterName).Mode;
        public BitmapColor getOnColor(String pluginName, String parameterName) => this.findColor(this.getColorSettings(pluginName, parameterName).OnColor,
                                                                                                 this.DefaultColorSettings.OnColor);
        public BitmapColor getOffColor(String pluginName, String parameterName) => this.findColor(this.getColorSettings(pluginName, parameterName).OffColor,
                                                                                                 this.DefaultColorSettings.OffColor);
        public BitmapColor getTextOnColor(String pluginName, String parameterName) => this.findColor(this.getColorSettings(pluginName, parameterName).TextOnColor,
                                                                                                     this.DefaultColorSettings.TextOnColor);
        public BitmapColor getTextOffColor(String pluginName, String parameterName) => this.findColor(this.getColorSettings(pluginName, parameterName).TextOffColor,
                                                                                                     this.DefaultColorSettings.TextOffColor);
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

        public static String settingName(String pluginName, String parameterName, String setting) => 
            strColorSettingsID + pluginName + "|" + parameterName + "|" + setting;
    }
}
