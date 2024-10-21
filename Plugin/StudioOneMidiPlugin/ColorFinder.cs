namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Xml.Serialization;
    using System.ComponentModel;
    using System.Windows.Controls;


    // BitmapColor objects that have not been explicitly assigned to a
    // color are automatically replaced by the currently defined default color.
    // Since it is not possible to have a BitmapColor object that is not assigned
    // to a color (BitmapColor.NoColor evaluates to the same values as BitmapColor.White) and
    // it cannot be set to null, we define a new class that can be null.
    //
    [JsonConverter(typeof(FinderColorConverter))]
    [XmlRoot("FinderColor")]
    public class FinderColor : IXmlSerializable
    {
        public BitmapColor Color { get; set; }

        public FinderColor() { }
        public FinderColor(BitmapColor b)
        {
            this.Color = b;
        }
        public FinderColor(Byte r, Byte g, Byte b)
        {
            this.Color = new BitmapColor(r, g, b);
        }

        public static implicit operator BitmapColor(FinderColor f) => f != null ? f.Color : new BitmapColor();
        public static explicit operator FinderColor(BitmapColor b) => new FinderColor(b);

        public static FinderColor Transparent => new FinderColor(BitmapColor.Transparent);
        public static FinderColor White => new FinderColor(BitmapColor.White);
        public static FinderColor Black => new FinderColor(BitmapColor.Black);

        #region IXmlSerializable members

        public System.Xml.Schema.XmlSchema GetSchema() => null;

        public void ReadXml(System.Xml.XmlReader reader)
        {
            var str = reader.ReadString();
            reader.ReadEndElement();
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            if (this.Color != null)
            {
                var str = $"rgb({this.Color.R},{this.Color.G},{this.Color.B})";
                str = "murks";

                writer.WriteString(str);
                writer.WriteEndElement();
            }
        }

        #endregion
    }
    public class FinderColorConverter : JsonConverter<FinderColor>
    {
        public override FinderColor Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options) =>
                FinderColor.White;

        public override void Write(
            Utf8JsonWriter writer,
            FinderColor color,
            JsonSerializerOptions options) =>
                writer.WriteStringValue($"{color.Color.R},{color.Color.G},{color.Color.B}");
    }

    public class ColorFinder
    {
        public static readonly BitmapColor NoColor = new BitmapColor(-1, -1, -1);
        [XmlInclude(typeof(S1TopControlColors))]
        public class ColorSettings
        {
            public enum PotMode { Positive, Symmetric };
            public PotMode Mode { get; set; } = PotMode.Positive;
            [DefaultValueAttribute(false)]
            public Boolean HideValueBar { get; set; } = false;
            [DefaultValueAttribute(false)]
            public Boolean ShowUserButtonCircle { get; set; } = false;
            [DefaultValueAttribute(true)]
            public Boolean PaintLabelBg { get; set; } = true;

            public FinderColor OnColor { get; set; }
            public Int32 OnTransparency { get; set; } = 80;
            public FinderColor OffColor { get; set; }
            //            [XmlElement, DefaultValue(null)]
            public FinderColor TextOnColor { get; set; }
            public FinderColor TextOffColor { get; set; }
            public FinderColor BarOnColor { get; set; }
            public String IconName { get; set; }
            public String IconNameOn { get; set; }
            public String Label { get; set; }
            public String LabelOn { get; set; }
            public String LinkedParameter { get; set; }
            [DefaultValueAttribute(false)]
            public Boolean LinkReversed { get; set; } = false;
            [DefaultValueAttribute(100)]
            public Int32 DialSteps { get; set; } = 100;               // Number of steps for a mode dial

            public String[] MenuItems;                  // Items for user button menu

            // For plugin settings
            public const String strOnColor = "OnColor";
            public const String strLabel = "Label";
            public const String strLabelOn = "LabelOn";
            public const String strLinkedParameter = "LinkedParameter";
            public const String strMode = "Mode";
            public const String strShowCircle = "ShowCircle";
            public const String strPaintLabelBg = "PaintLabelBg";
            //public const String[] strModeValue = { "Positive", "Symmetric" };
        }
        private static readonly Dictionary<(String PluginName, String PluginParameter), ColorSettings> ColorDict = new Dictionary<(String, String), ColorSettings>();
        private const String strColorSettingsID = "[cs]";  // for plugin settings

        private String LastPluginName, LastPluginParameter;
        private ColorSettings LastColorSettings;

        public Int32 CurrentUserPage = 0;              // For tracking the current user page position
        public Int32 CurrentChannel = 0;

        const String ConfigFileName = "AudioPluginConfig.xml";

        public class ConfigEntry
        {
            public String key1;
            public String key2;
            public ColorSettings colorSettings;
        }

        public ColorSettings DefaultColorSettings { get; private set; } = new ColorSettings
        {
            OnColor = FinderColor.Transparent,
            OffColor = FinderColor.Transparent,
            TextOnColor = FinderColor.White,
            TextOffColor = FinderColor.White
        };

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public ColorFinder() { }

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public ColorFinder(ColorSettings defaultColorSettings)
        {
            this.DefaultColorSettings = defaultColorSettings;
        }

        public class S1TopControlColors : ColorSettings
        {
            public S1TopControlColors() { }
            public S1TopControlColors(String label = null)
            {
                this.OnColor = new FinderColor(54, 84, 122);
                this.OffColor = new FinderColor(27, 34, 37);
                this.TextOffColor = new FinderColor(58, 117, 195);
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
                this.InitColorDict();

                //                var ConfigFilePath = Path.Combine(Directory.GetParent(Application.LocalUserAppDataPath.TrimEnd(Path.DirectorySeparatorChar)).FullName, ConfigFileName);
                //
                //                var serializer = new XmlSerializer(typeof(ConfigEntry));
                //                TextWriter writer = new StreamWriter(ConfigFilePath);
                //
                //                foreach (KeyValuePair<(String, String), ColorSettings> entry in ColorDict)
                //                {
                //                    serializer.Serialize(writer, new ConfigEntry { key1 = entry.Key.Item1,
                //                                                                   key2 = entry.Key.Item2,
                //                                                                   colorSettings = entry.Value });
                //                }
                //
                //                writer.Close();

                // var options = new JsonSerializerOptions { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault };
                // foreach (KeyValuePair<(String, String), ColorSettings> entry in ColorDict)
                // {
                //    var jsonString = "{ \"key1\": \"" + entry.Key.Item1 + "\",\n";
                //    jsonString += "  \"key2\": \"" + entry.Key.Item2 + "\",\n";
                //    jsonString += "  \"colorSettings\": " + JsonSerializer.Serialize(entry.Value, options);
                //    jsonString += "}";
                //
                //    Debug.WriteLine(jsonString);
                // }

                // Read Loupedeck plugin settings

                var settingsList = plugin.ListPluginSettings();

                foreach (var setting in settingsList)
                {
                    if (setting.StartsWith(strColorSettingsID))
                    {
                        var settingsParsed = setting.Substring(strColorSettingsID.Length).Split('|');
                        if (!ColorDict.TryGetValue((settingsParsed[0], settingsParsed[1]), out var cs))
                        {
                            cs = new ColorSettings { };
                            ColorDict.Add((settingsParsed[0], settingsParsed[1]), cs);
                        }
                        if (plugin.TryGetPluginSetting(settingName(settingsParsed[0], settingsParsed[1], settingsParsed[2]), out var val))
                        {
                            switch (settingsParsed[2])
                            {
                                case ColorSettings.strOnColor:
                                    cs.OnColor = new FinderColor(Convert.ToByte(val.Substring(0, 2), 16),
                                                                 Convert.ToByte(val.Substring(2, 2), 16),
                                                                 Convert.ToByte(val.Substring(4, 2), 16));
                                    break;
                                case ColorSettings.strLabel:
                                    cs.Label = val;
                                    break;
                                case ColorSettings.strLinkedParameter:
                                    cs.LinkedParameter = val;
                                    break;
                                case ColorSettings.strMode:
                                    cs.Mode = val.ParseInt32() == 0 ? ColorSettings.PotMode.Positive : ColorSettings.PotMode.Symmetric;
                                    break;
                                case ColorSettings.strShowCircle:
                                    cs.ShowUserButtonCircle = val.ParseInt32() == 1 ? true : false;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        private void addLinked(String pluginName, String parameterName, String linkedParameter,
                               String label = null,
                               ColorSettings.PotMode mode = ColorSettings.PotMode.Positive,
                               Boolean linkReversed = false,
                               FinderColor onColor = null,
                               Int32 onTransparency = 80,
                               FinderColor textOnColor = null,
                               FinderColor offColor = null,
                               FinderColor textOffColor = null,
                               String[] menuItems = null)
        {
            if (label == null) label = parameterName;
            var colorSettings = ColorDict[(pluginName, linkedParameter)];
            ColorDict.Add((pluginName, parameterName), new ColorSettings { Mode = mode,
                OnColor = onColor ?? colorSettings.OnColor,
                OnTransparency = onTransparency,
                OffColor = offColor ?? colorSettings.OffColor,
                TextOnColor = textOnColor ?? colorSettings.TextOnColor,
                TextOffColor = textOffColor ?? colorSettings.TextOffColor,
                Label = label,
                LinkedParameter = linkedParameter,
                LinkReversed = linkReversed,
                MenuItems = menuItems
            });
        }

        private ColorSettings saveLastSettings(ColorSettings colorSettings)
        {
            this.LastColorSettings = colorSettings;
            return colorSettings;
        }
        public ColorSettings getColorSettings(String pluginName, String parameterName, Boolean isUser)
        {
            if (pluginName == null || parameterName == null) return this.DefaultColorSettings;
            if (this.LastColorSettings != null && pluginName == this.LastPluginName && parameterName == this.LastPluginParameter) return this.LastColorSettings;

            this.LastPluginName = pluginName;
            this.LastPluginParameter = parameterName;

            var userPagePos = $"{this.CurrentUserPage}:{this.CurrentChannel}" + (isUser ? "U" : "");

            if (ColorDict.TryGetValue((pluginName, userPagePos), out var colorSettings) ||
                ColorDict.TryGetValue((pluginName, parameterName), out colorSettings) ||
                ColorDict.TryGetValue((pluginName, ""), out colorSettings) ||
                ColorDict.TryGetValue(("", parameterName), out colorSettings))
            {
                return this.saveLastSettings(colorSettings);
            }

            // Try partial match of plugin name.
            var partialMatchKeys = ColorDict.Keys.Where(currentKey => pluginName.StartsWith(currentKey.PluginName) && currentKey.PluginParameter == parameterName);
            if (partialMatchKeys.Count() > 0 && ColorDict.TryGetValue(partialMatchKeys.First(), out colorSettings))
            {
                return this.saveLastSettings(colorSettings);
            }

            partialMatchKeys = ColorDict.Keys.Where(currentKey => pluginName.Contains(currentKey.PluginName) && currentKey.PluginParameter == "");
            if (partialMatchKeys.Count() > 0 && ColorDict.TryGetValue(partialMatchKeys.First(), out colorSettings))
            {
                return this.saveLastSettings(colorSettings);
            }


            return this.saveLastSettings(this.DefaultColorSettings);
        }

        private BitmapColor findColor(FinderColor settingsColor, BitmapColor defaultColor) => settingsColor ?? defaultColor;

        public ColorSettings.PotMode getMode(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).Mode;
        public Boolean getShowCircle(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).ShowUserButtonCircle;
        public Boolean getPaintLabelBg(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).PaintLabelBg;

        public BitmapColor getOnColor(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.getColorSettings(pluginName, parameterName, isUser);
            return cs != null ? new BitmapColor(cs.OnColor, cs.OnTransparency)
                              : new BitmapColor(this.DefaultColorSettings.OnColor, this.DefaultColorSettings.OnTransparency);
        }
        public BitmapColor getBarOnColor(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.getColorSettings(pluginName, parameterName, isUser);
            return cs.BarOnColor ?? this.findColor(cs.OnColor, this.DefaultColorSettings.OnColor);
        }
        public BitmapColor getOffColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.getColorSettings(pluginName, parameterName, isUser).OffColor,
                                                                                                  this.DefaultColorSettings.OffColor);
        public BitmapColor getTextOnColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.getColorSettings(pluginName, parameterName, isUser).TextOnColor,
                                                                                                     this.DefaultColorSettings.TextOnColor);
        public BitmapColor getTextOffColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.getColorSettings(pluginName, parameterName, isUser).TextOffColor,
                                                                                                      this.DefaultColorSettings.TextOffColor);
        public String getLabel(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).Label ?? parameterName;
        public String getLabelOn(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.getColorSettings(pluginName, parameterName, isUser);
            return cs.LabelOn ?? cs.Label ?? parameterName;
        }
        public String getLabelShort(String pluginName, String parameterName, Boolean isUser = false) => stripLabel(this.getLabel(pluginName, parameterName, isUser));
        public String getLabelOnShort(String pluginName, String parameterName, Boolean isUser = false) => stripLabel(this.getLabelOn(pluginName, parameterName, isUser));
        public static String stripLabel(String label)
        {
            if (label.Length <= 12) return label;
            return Regex.Replace(label, "(?<!^)[aeiou](?!$)", "");
        }
        public BitmapImage getIcon(String pluginName, String parameterName)
        {
            var colorSettings = this.getColorSettings(pluginName, parameterName, false);
            if (colorSettings.IconName != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconName}_52px.png"));
            }
            return null;
        }

        public BitmapImage getIconOn(String pluginName, String parameterName)
        {
            var colorSettings = this.getColorSettings(pluginName, parameterName, false);
            if (colorSettings.IconNameOn != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconNameOn}_52px.png"));
            }
            return null;
        }
        public String getLinkedParameter(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).LinkedParameter;
        public Boolean getLinkReversed(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).LinkReversed;
        public Boolean hideValueBar(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).HideValueBar;
        public Boolean showUserButtonCircle(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).ShowUserButtonCircle;
        public Int32 getDialSteps(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).DialSteps;
        public String[] getMenuItems(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).MenuItems;
        public Boolean hasMenu(String pluginName, String parameterName, Boolean isUser = false) => this.getColorSettings(pluginName, parameterName, isUser).MenuItems != null;
        public static String settingName(String pluginName, String parameterName, String setting) => strColorSettingsID + pluginName + "|" + parameterName + "|" + setting;

        private void InitColorDict()
        {
            ColorDict.Add(("", "Bypass"), new ColorSettings { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });
            ColorDict.Add(("", "Global Bypass"), new ColorSettings { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });

            ColorDict.Add(("Pro EQ", "Show Controls"), new S1TopControlColors(label: "Band Controls"));
            ColorDict.Add(("Pro EQ", "Show Dynamics"), new S1TopControlColors(label: "Dynamics"));
            ColorDict.Add(("Pro EQ", "High Quality"), new S1TopControlColors());
            ColorDict.Add(("Pro EQ", "View Mode"), new S1TopControlColors(label: "Curves"));
            ColorDict.Add(("Pro EQ", "LF-Active"), new ColorSettings { OnColor = new FinderColor(255, 120, 38), Label = "LF", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "MF-Active"), new ColorSettings { OnColor = new FinderColor(107, 224, 44), Label = "MF", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "HF-Active"), new ColorSettings { OnColor = new FinderColor(75, 212, 250), Label = "HF", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "LMF-Active"), new ColorSettings { OnColor = new FinderColor(245, 205, 58), Label = "LMF", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "HMF-Active"), new ColorSettings { OnColor = new FinderColor(70, 183, 130), Label = "HMF", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "LC-Active"), new ColorSettings { OnColor = new FinderColor(255, 74, 61), Label = "LC", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "HC-Active"), new ColorSettings { OnColor = new FinderColor(158, 98, 255), Label = "HC", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "LLC-Active"), new ColorSettings { OnColor = FinderColor.White, Label = "LLC", ShowUserButtonCircle = true });
            ColorDict.Add(("Pro EQ", "Global Gain"), new ColorSettings { OnColor = new FinderColor(200, 200, 200), Label = "Gain", Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Pro EQ", "Auto Gain"), new ColorSettings { Label = "Auto" });
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
            ColorDict.Add(("Pro EQ", "LF-Solo"), new ColorSettings { OnColor = new FinderColor(224, 182, 69), Label = "LF Solo" });
            ColorDict.Add(("Pro EQ", "MF-Solo"), new ColorSettings { OnColor = new FinderColor(224, 182, 69), Label = "MF Solo" });
            ColorDict.Add(("Pro EQ", "HF-Solo"), new ColorSettings { OnColor = new FinderColor(224, 182, 69), Label = "HF Solo" });
            ColorDict.Add(("Pro EQ", "LMF-Solo"), new ColorSettings { OnColor = new FinderColor(224, 182, 69), Label = "LMF Solo" });
            ColorDict.Add(("Pro EQ", "HMF-Solo"), new ColorSettings { OnColor = new FinderColor(224, 182, 69), Label = "HMF Solo" });

            ColorDict.Add(("Fat Channel", "Hi Pass Filter"), new ColorSettings { Label = "Hi Pass" });
            ColorDict.Add(("Fat Channel", "Gate On"), new ColorSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Gate ON" });
            ColorDict.Add(("Fat Channel", "Range"), new ColorSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Expander", LinkReversed = true });
            ColorDict.Add(("Fat Channel", "Expander"), new ColorSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Fat Channel", "Key Listen"), new ColorSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Fat Channel", "Compressor On"), new ColorSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Cmpr ON" });
            ColorDict.Add(("Fat Channel", "Attack"), new ColorSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto", LinkReversed = true });
            ColorDict.Add(("Fat Channel", "Release"), new ColorSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto", LinkReversed = true });
            ColorDict.Add(("Fat Channel", "Auto"), new ColorSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Fat Channel", "Peak Reduction"), new ColorSettings { Label = "Pk Reductn" });
            ColorDict.Add(("Fat Channel", "EQ On"), new ColorSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "EQ ON" });
            ColorDict.Add(("Fat Channel", "Low On"), new ColorSettings { OnColor = new FinderColor(241, 84, 220), Label = "LF", ShowUserButtonCircle = true });
            ColorDict.Add(("Fat Channel", "Low-Mid On"), new ColorSettings { OnColor = new FinderColor(89, 236, 236), Label = "LMF", ShowUserButtonCircle = true });
            ColorDict.Add(("Fat Channel", "Hi-Mid On"), new ColorSettings { OnColor = new FinderColor(241, 178, 84), Label = "HMF", ShowUserButtonCircle = true });
            ColorDict.Add(("Fat Channel", "High On"), new ColorSettings { OnColor = new FinderColor(122, 240, 79), Label = "HF", ShowUserButtonCircle = true });
            this.addLinked("Fat Channel", "Low Gain", "Low On", label: "LF Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("Fat Channel", "Low Freq", "Low On", label: "LF Freq");
            this.addLinked("Fat Channel", "Low Q", "Low On", label: "LMF Q");
            this.addLinked("Fat Channel", "Low-Mid Gain", "Low-Mid On", label: "LMF Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("Fat Channel", "Low-Mid Freq", "Low-Mid On", label: "LMF Freq");
            this.addLinked("Fat Channel", "Low-Mid Q", "Low-Mid On", label: "LMF Q");
            this.addLinked("Fat Channel", "Hi-Mid Gain", "Hi-Mid On", label: "HMF Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("Fat Channel", "Hi-Mid Freq", "Hi-Mid On", label: "HMF Freq");
            this.addLinked("Fat Channel", "Hi-Mid Q", "Hi-Mid On", label: "HMF Q");
            this.addLinked("Fat Channel", "High Gain", "High On", label: "HF Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("Fat Channel", "High Freq", "High On", label: "HF Freq");
            this.addLinked("Fat Channel", "High Q", "High On", label: "HF Q");
            ColorDict.Add(("Fat Channel", "Low Boost"), new ColorSettings { OnColor = new FinderColor(241, 84, 220) });
            ColorDict.Add(("Fat Channel", "Low Atten"), new ColorSettings { OnColor = new FinderColor(241, 84, 220) });
            ColorDict.Add(("Fat Channel", "Low Frequency"), new ColorSettings { Label = "LF Freq", OnColor = new FinderColor(241, 84, 220), DialSteps = 3 });
            ColorDict.Add(("Fat Channel", "High Boost"), new ColorSettings { OnColor = new FinderColor(122, 240, 79) });
            ColorDict.Add(("Fat Channel", "High Atten"), new ColorSettings { OnColor = new FinderColor(122, 240, 79) });
            ColorDict.Add(("Fat Channel", "High Bandwidth"), new ColorSettings { Label = "Bandwidth", OnColor = new FinderColor(122, 240, 79) });
            ColorDict.Add(("Fat Channel", "Attenuation Select"), new ColorSettings { Label = "Atten Sel", OnColor = new FinderColor(122, 240, 79), DialSteps = 2 });
            ColorDict.Add(("Fat Channel", "Limiter On"), new ColorSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Limiter ON" });

            ColorDict.Add(("Compressor", "LookAhead"), new S1TopControlColors());
            ColorDict.Add(("Compressor", "Link Channels"), new S1TopControlColors(label: "CH Link"));
            ColorDict.Add(("Compressor", "Attack"), new ColorSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto Speed", LinkReversed = true });
            ColorDict.Add(("Compressor", "Release"), new ColorSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto Speed", LinkReversed = true });
            ColorDict.Add(("Compressor", "Auto Speed"), new ColorSettings { Label = "Auto" });
            ColorDict.Add(("Compressor", "Adaptive Speed"), new ColorSettings { Label = "Adaptive" });
            ColorDict.Add(("Compressor", "Gain"), new ColorSettings { Label = "Makeup", OffColor = FinderColor.Transparent, LinkedParameter = "Auto Gain", LinkReversed = true });
            ColorDict.Add(("Compressor", "Auto Gain"), new ColorSettings { Label = "Auto" });
            ColorDict.Add(("Compressor", "Sidechain LC-Freq"), new ColorSettings { Label = "Side LC", OffColor = FinderColor.Transparent, LinkedParameter = "Sidechain Filter" });
            ColorDict.Add(("Compressor", "Sidechain HC-Freq"), new ColorSettings { Label = "Side HC", OffColor = FinderColor.Transparent, LinkedParameter = "Sidechain Filter" });
            ColorDict.Add(("Compressor", "Sidechain Filter"), new ColorSettings { Label = "Filter" });
            ColorDict.Add(("Compressor", "Sidechain Listen"), new ColorSettings { Label = "Listen" });
            ColorDict.Add(("Compressor", "Swap Frequencies"), new ColorSettings { Label = "Swap" });

            ColorDict.Add(("Limiter", "Mode "), new ColorSettings { Label = "A", LabelOn = "B", OnColor = new FinderColor(40, 40, 40), OffColor = new FinderColor(40, 40, 40),
                                                                   TextOnColor = new FinderColor(171, 197, 226), TextOffColor = new FinderColor(171, 197, 226) });
            ColorDict.Add(("Limiter", "True Peak Limiting"), new S1TopControlColors(label: "True Peak"));
            this.addLinked("Limiter", "SoftClipper", "True Peak Limiting", label: " Soft Clip", linkReversed: true);
            ColorDict.Add(("Limiter", "Attack"), new ColorSettings { DialSteps = 2, HideValueBar = true } );

            ColorDict.Add(("Flanger", ""), new ColorSettings { OnColor = new FinderColor(238, 204, 103) });
            ColorDict.Add(("Flanger", "Feedback"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Flanger", "LFO Sync"), new ColorSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Flanger", "Depth"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });

            ColorDict.Add(("Phaser", ""), new ColorSettings { OnColor = new FinderColor(238, 204, 103) });
            ColorDict.Add(("Phaser", "Center Frequency"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Label = "Center" });
            ColorDict.Add(("Phaser", "Sweep Range"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Label = "Range" });
            ColorDict.Add(("Phaser", "Stereo Spread"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Label = "Spread" });
            ColorDict.Add(("Phaser", "Depth"), new ColorSettings { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });
            ColorDict.Add(("Phaser", "LFO Sync"), new ColorSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Phaser", "Log. Sweep"), new ColorSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Phaser", "Soft"), new ColorSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });

            ColorDict.Add(("Alpine Desk", "Boost"), new ColorSettings { DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("Alpine Desk", "Preamp On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            ColorDict.Add(("Alpine Desk", "Noise On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            ColorDict.Add(("Alpine Desk", "Noise Gate On"), new ColorSettings { Label = "Noise Gate", OnColor = new FinderColor(0, 154, 144) });
            ColorDict.Add(("Alpine Desk", "Crosstalk"), new ColorSettings { OnColor = new FinderColor(253, 202, 0) });
            ColorDict.Add(("Alpine Desk", "Crosstalk On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            ColorDict.Add(("Alpine Desk", "Transformer"), new ColorSettings { OnColor = new FinderColor(224, 22, 36), DialSteps = 1, HideValueBar = true });
            ColorDict.Add(("Alpine Desk", "Master"), new ColorSettings { Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Alpine Desk", "Compensation"), new ColorSettings { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(0, 154, 144), OffColor = new FinderColor(0, 154, 144), TextOffColor = FinderColor.White });
            ColorDict.Add(("Alpine Desk", "Character Enhancer"), new ColorSettings { Label = "Character" });
            ColorDict.Add(("Alpine Desk", "Economy"), new ColorSettings { Label = "Eco", OnColor = new FinderColor(0, 154, 144) });

            ColorDict.Add(("Brit Console", "Boost"), new ColorSettings { DialSteps = 2, OnColor = new FinderColor(43, 128, 157), HideValueBar = true });
            ColorDict.Add(("Brit Console", "Drive"), new ColorSettings { OnColor = new FinderColor(43, 128, 157) });
            ColorDict.Add(("Brit Console", "Preamp On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            ColorDict.Add(("Brit Console", "Noise"), new ColorSettings { OnColor = new FinderColor(43, 128, 157) });
            ColorDict.Add(("Brit Console", "Noise On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            ColorDict.Add(("Brit Console", "Noise Gate On"), new ColorSettings { Label = "Gate", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            ColorDict.Add(("Brit Console", "Crosstalk"), new ColorSettings { OnColor = new FinderColor(43, 128, 157) });
            ColorDict.Add(("Brit Console", "Crosstalk On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            ColorDict.Add(("Brit Console", "Style"), new ColorSettings { OnColor = new FinderColor(202, 74, 68), DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("Brit Console", "Harmonics"), new ColorSettings { OnColor = new FinderColor(202, 74, 68) });
            ColorDict.Add(("Brit Console", "Compensation"), new ColorSettings { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            ColorDict.Add(("Brit Console", "Character Enhancer"), new ColorSettings { Label = "Character", OnColor = new FinderColor(43, 128, 157) });
            ColorDict.Add(("Brit Console", "Master"), new ColorSettings { OnColor = new FinderColor(43, 128, 157), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Brit Console", "Economy"), new ColorSettings { Label = "Eco", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });

            ColorDict.Add(("CTC-1", "Boost"), new ColorSettings { OnColor = new FinderColor(244, 104, 26) });
            ColorDict.Add(("CTC-1", "Preamp On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            ColorDict.Add(("CTC-1", "Noise"), new ColorSettings { DialSteps = 4 });
            ColorDict.Add(("CTC-1", "Noise On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            ColorDict.Add(("CTC-1", "Noise Gate On"), new ColorSettings { Label = "Gate", OnColor = new FinderColor(244, 104, 26) });
            ColorDict.Add(("CTC-1", "Preamp Type"), new ColorSettings { Label = "Type", DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("CTC-1", "Crosstalk On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            ColorDict.Add(("CTC-1", "Compensation"), new ColorSettings { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(69, 125, 159), OffColor = new FinderColor(69, 125, 159), TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            ColorDict.Add(("CTC-1", "Character Enhancer"), new ColorSettings { Label = "Character" });
            ColorDict.Add(("CTC-1", "Master"), new ColorSettings { Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CTC-1", "Economy"), new ColorSettings { Label = "Eco", OnColor = new FinderColor(69, 125, 159) });

            ColorDict.Add(("Porta Casstte", "Boost"), new ColorSettings { OnColor = new FinderColor(251, 0, 3) });
            ColorDict.Add(("Porta Cassette", "Drive"), new ColorSettings { OnColor = new FinderColor(226, 226, 226) });
            ColorDict.Add(("Porta Cassette", "Preamp On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            ColorDict.Add(("Porta Cassette", "Noise"), new ColorSettings { OnColor = new FinderColor(226, 226, 226) });
            ColorDict.Add(("Porta Cassette", "Noise On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            ColorDict.Add(("Porta Cassette", "Noise Gate On"), new ColorSettings { Label = "Gate", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            ColorDict.Add(("Porta Cassette", "Crosstalk"), new ColorSettings { OnColor = new FinderColor(226, 226, 226) });
            ColorDict.Add(("Porta Cassette", "Crosstalk On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            ColorDict.Add(("Porta Cassette", "Pitch"), new ColorSettings { OnColor = new FinderColor(144, 153, 153), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Porta Cassette", "Compensation"), new ColorSettings { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            ColorDict.Add(("Porta Cassette", "Character Enhancer"), new ColorSettings { Label = "Character", OnColor = new FinderColor(226, 226, 226) });
            ColorDict.Add(("Porta Cassette", "Master"), new ColorSettings { OnColor = new FinderColor(226, 226, 226), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Porta Cassette", "Economy"), new ColorSettings { Label = "Eco" });

            ColorDict.Add(("Console Shaper", "Preamp On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            ColorDict.Add(("Console Shaper", "Noise"), new ColorSettings { DialSteps = 4 });
            ColorDict.Add(("Console Shaper", "Noise On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            ColorDict.Add(("Console Shaper", "Crosstalk On"), new ColorSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });

            // Waves

            ColorDict.Add(("SSLGChannel", "HP Frq"), new ColorSettings { OnColor = new FinderColor(220, 216, 207) });
            ColorDict.Add(("SSLGChannel", "LP Frq"), new ColorSettings { OnColor = new FinderColor(220, 216, 207) });
            ColorDict.Add(("SSLGChannel", "FilterSplit"), new ColorSettings { OnColor = new FinderColor(204, 191, 46), Label = "SPLIT" });
            ColorDict.Add(("SSLGChannel", "HF Gain"), new ColorSettings { OnColor = new FinderColor(177, 53, 63), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "HF Frq"), new ColorSettings { OnColor = new FinderColor(177, 53, 63) });
            ColorDict.Add(("SSLGChannel", "HMF X3"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Label = "HMFx3" });
            ColorDict.Add(("SSLGChannel", "LF Gain"), new ColorSettings { OnColor = new FinderColor(180, 180, 180), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "LF Frq"), new ColorSettings { OnColor = new FinderColor(180, 180, 180) });
            ColorDict.Add(("SSLGChannel", "LMF div3"), new ColorSettings { OnColor = new FinderColor(22, 97, 120), Label = "LMF/3" });
            ColorDict.Add(("SSLGChannel", "HMF Gain"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "HMF Frq"), new ColorSettings { OnColor = new FinderColor(27, 92, 64) });
            ColorDict.Add(("SSLGChannel", "HMF Q"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "LMF Gain"), new ColorSettings { OnColor = new FinderColor(22, 97, 120), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "LMF Frq"), new ColorSettings { OnColor = new FinderColor(22, 97, 120) });
            ColorDict.Add(("SSLGChannel", "LMF Q"), new ColorSettings { OnColor = new FinderColor(22, 97, 120), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("SSLGChannel", "EQBypass"), new ColorSettings { OnColor = new FinderColor(226, 61, 80), Label = "EQ BYP" });
            ColorDict.Add(("SSLGChannel", "EQDynamic"), new ColorSettings { OnColor = new FinderColor(241, 171, 53), Label = "FLT DYN SC" });
            ColorDict.Add(("SSLGChannel", "CompRatio"), new ColorSettings { OnColor = new FinderColor(220, 216, 207), Label = "C RATIO" });
            ColorDict.Add(("SSLGChannel", "CompThresh"), new ColorSettings { OnColor = new FinderColor(220, 216, 207), Label = "C THRESH" });
            ColorDict.Add(("SSLGChannel", "CompRelease"), new ColorSettings { OnColor = new FinderColor(220, 216, 207), Label = "C RELEASE" });
            ColorDict.Add(("SSLGChannel", "CompFast"), new ColorSettings { Label = "F.ATK" });
            ColorDict.Add(("SSLGChannel", "ExpRange"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Label = "E RANGE" });
            ColorDict.Add(("SSLGChannel", "ExpThresh"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Label = "E THRESH" });
            ColorDict.Add(("SSLGChannel", "ExpRelease"), new ColorSettings { OnColor = new FinderColor(27, 92, 64), Label = "E RELEASE" });
            ColorDict.Add(("SSLGChannel", "ExpAttack"), new ColorSettings { Label = "F.ATK" });
            ColorDict.Add(("SSLGChannel", "ExpGate"), new ColorSettings { Label = "GATE" });
            ColorDict.Add(("SSLGChannel", "DynamicBypass"), new ColorSettings { OnColor = new FinderColor(226, 61, 80), Label = "DYN BYP" });
            ColorDict.Add(("SSLGChannel", "DynaminCHOut"), new ColorSettings { OnColor = new FinderColor(241, 171, 53), Label = "DYN CH OUT" });
            ColorDict.Add(("SSLGChannel", "VUInOut"), new ColorSettings { OnColor = new FinderColor(241, 171, 53), Label = "VU OUT" });

            ColorDict.Add(("RCompressor", "Threshold"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("RCompressor", "Ratio"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("RCompressor", "Attack"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("RCompressor", "Release"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("RCompressor", "Gain"), new ColorSettings { OnColor = new FinderColor(243, 132, 1), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("RCompressor", "Trim"), new ColorSettings { Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("RCompressor", "ARC / Manual"), new ColorSettings { Label = "ARC", LabelOn = "Manual", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            ColorDict.Add(("RCompressor", "Electro / Opto"), new ColorSettings { Label = "Electro", LabelOn = "Opto", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            ColorDict.Add(("RCompressor", "Warm / Smooth"), new ColorSettings { Label = "Warm", LabelOn = "Smooth", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });

            ColorDict.Add(("RBass", "Orig. In-Out"), new ColorSettings { Label = "ORIG IN", OffColor = new FinderColor(230, 230, 230), TextOnColor = FinderColor.Black  });
            ColorDict.Add(("RBass", "Intensity"), new ColorSettings { OnColor = new FinderColor(243, 132, 1), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("RBass", "Frequency"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("RBass", "Out Gain"), new ColorSettings { Label = "Gain", OnColor = new FinderColor(243, 132, 1) });

            ColorDict.Add(("REQ", "Band1 On/Off"), new ColorSettings { Label = "Band 1", OnColor = new FinderColor(196, 116, 100), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band1 Gain", "Band1 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band1 Frq", "Band1 On/Off", label: "Freq");
            this.addLinked("REQ", "Band1 Q", "Band1 On/Off", label: "Q");
            this.addLinked("REQ", "Band1 Type", "Band1 On/Off", label: "", menuItems: ["!Bell", "!Low-Shelf", "!Hi-Pass", "!Low-RShelv"]);
            ColorDict.Add(("REQ", "Band2 On/Off"), new ColorSettings { Label = "Band 2", OnColor = new FinderColor(175, 173, 29), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band2 Gain", "Band2 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band2 Frq", "Band2 On/Off", label: "Freq");
            this.addLinked("REQ", "Band2 Q", "Band2 On/Off", label: "Q");
            this.addLinked("REQ", "Band2 Type", "Band2 On/Off", label: "", menuItems: ["!Bell", "!Low-Shelf"]);
            ColorDict.Add(("REQ", "Band3 On/Off"), new ColorSettings { Label = "Band 3", OnColor = new FinderColor(57, 181, 74), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band3 Gain", "Band3 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band3 Frq", "Band3 On/Off", label: "Freq");
            this.addLinked("REQ", "Band3 Q", "Band3 On/Off", label: "Q");
            this.addLinked("REQ", "Band3 Type", "Band3 On/Off", label: "", menuItems: ["!Bell", "!Low-Shelf"]);
            ColorDict.Add(("REQ", "Band4 On/Off"), new ColorSettings { Label = "Band 4", OnColor = new FinderColor(56, 149, 203), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band4 Gain", "Band4 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band4 Frq", "Band4 On/Off", label: "Freq");
            this.addLinked("REQ", "Band4 Q", "Band4 On/Off", label: "Q");
            this.addLinked("REQ", "Band4 Type", "Band4 On/Off", label: "", menuItems: ["!Bell", "!Hi-Shelf"]);
            ColorDict.Add(("REQ", "Band5 On/Off"), new ColorSettings { Label = "Band 5", OnColor = new FinderColor(130, 41, 141), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band5 Gain", "Band5 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band5 Frq", "Band5 On/Off", label: "Freq");
            this.addLinked("REQ", "Band5 Q", "Band5 On/Off", label: "Q");
            this.addLinked("REQ", "Band5 Type", "Band5 On/Off", label: "", menuItems: ["!Bell", "!Hi-Shelf"]);
            ColorDict.Add(("REQ", "Band6 On/Off"), new ColorSettings { Label = "Band 6", OnColor = new FinderColor(199, 48, 105), TextOnColor = FinderColor.Black });
            this.addLinked("REQ", "Band6 Gain", "Band6 On/Off", label: "Gain", mode: ColorSettings.PotMode.Symmetric);
            this.addLinked("REQ", "Band6 Frq", "Band6 On/Off", label: "Freq");
            this.addLinked("REQ", "Band6 Q", "Band6 On/Off", label: "Q");
            this.addLinked("REQ", "Band6 Type", "Band6 On/Off", label: "", menuItems: ["!Bell", "!Hi-Shelf", "!Low-Pass", "!Hi-RShelv"]);
            ColorDict.Add(("REQ", "Fader left Out"), new ColorSettings { Label = "Output", OnColor = new FinderColor(242, 101, 34) });
            ColorDict.Add(("REQ", "Gain-L (link)"), new ColorSettings { Label = "Out L", OnColor = new FinderColor(242, 101, 34) });
            ColorDict.Add(("REQ", "Gain-R"), new ColorSettings { Label = "Out R", OnColor = new FinderColor(242, 101, 34) });

            ColorDict.Add(("RVerb", ""), new ColorSettings { OnColor = new FinderColor(244, 134, 2), TextOnColor = FinderColor.Black });
            ColorDict.Add(("RVerb", "Dmp Low-F Ratio"), new ColorSettings { Label = "Dmp Lo Rto", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "Dmp Low-F Freq"), new ColorSettings { Label = "Dmp Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "Dmp Hi-F Ratio"), new ColorSettings { Label = "Dmp Hi Rto", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "Dmp Hi-F Freq"), new ColorSettings { Label = "Dmp Hi Frq", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "EQ Low-F Gain"), new ColorSettings { Label = "EQ Lo Gn", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "EQ Low-F Freq"), new ColorSettings { Label = "EQ Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "EQ Hi-F Gain"), new ColorSettings { Label = "EQ Hi Gn", OnColor = new FinderColor(74, 149, 155) });
            ColorDict.Add(("RVerb", "EQ Hi-F Freq"), new ColorSettings { Label = "EQ Hi Frq", OnColor = new FinderColor(74, 149, 155) });


            ColorDict.Add(("L1 limiter", "Threshold"), new ColorSettings { OnColor = new FinderColor(243, 132, 1) });
            ColorDict.Add(("L1 limiter", "Ceiling"), new ColorSettings { OnColor = new FinderColor(255, 172, 66) });
            ColorDict.Add(("L1 limiter", "Release"), new ColorSettings { OnColor = new FinderColor(54, 206, 206) });
            ColorDict.Add(("L1 limiter", "Auto Release"), new ColorSettings { Label = "AUTO", OnColor = new FinderColor(54, 206, 206) });

            ColorDict.Add(("PuigTec EQP1A", "OnOff"), new ColorSettings { Label = "IN", OnColor = new FinderColor(203, 53, 53) });
            ColorDict.Add(("PuigTec EQP1A", "LowBoost"), new ColorSettings { Label = "Low Boost", OnColor = new FinderColor(96, 116, 115) });
            ColorDict.Add(("PuigTec EQP1A", "LowAtten"), new ColorSettings { Label = "Low Atten", OnColor = new FinderColor(96, 116, 115) });
            ColorDict.Add(("PuigTec EQP1A", "HiBoost"), new ColorSettings { Label = "High Boost", OnColor = new FinderColor(96, 116, 115) });
            ColorDict.Add(("PuigTec EQP1A", "HiAtten"), new ColorSettings { Label = "High Atten", OnColor = new FinderColor(96, 116, 115) });
            ColorDict.Add(("PuigTec EQP1A", "LowFrequency"), new ColorSettings { Label = "Low Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 3 });
            ColorDict.Add(("PuigTec EQP1A", "HiFrequency"), new ColorSettings { Label = "High Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 6 });
            ColorDict.Add(("PuigTec EQP1A", "Bandwidth"), new ColorSettings { Label = "Bandwidth", OnColor = new FinderColor(96, 116, 115) });
            ColorDict.Add(("PuigTec EQP1A", "AttenSelect"), new ColorSettings { Label = "Atten Sel", OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            ColorDict.Add(("PuigTec EQP1A", "Mains"), new ColorSettings { OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            ColorDict.Add(("PuigTec EQP1A", "Gain"), new ColorSettings { OnColor = new FinderColor(96, 116, 115), Mode = ColorSettings.PotMode.Symmetric });

            ColorDict.Add(("Smack Attack", "Attack"), new ColorSettings { OnColor = new FinderColor(9, 217, 179), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Smack Attack", "AttackSensitivity"), new ColorSettings { Label = "Sensitivity", OnColor = new FinderColor(9, 217, 179) });
            ColorDict.Add(("Smack Attack", "AttackDuration"), new ColorSettings { Label = "Duration", OnColor = new FinderColor(9, 217, 179) });
            ColorDict.Add(("Smack Attack", "AttackShape"), new ColorSettings { Label = "", OnColor = new FinderColor(30, 30, 30), MenuItems = ["!sm_Needle", "!sm_Nail", "!sm_BluntA"], DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("Smack Attack", "Sustain"), new ColorSettings { OnColor = new FinderColor(230, 172, 5), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("Smack Attack", "SustainSensitivity"), new ColorSettings { Label = "Sensitivity", OnColor = new FinderColor(230, 172, 5) });
            ColorDict.Add(("Smack Attack", "SustainDuration"), new ColorSettings { Label = "Duration", OnColor = new FinderColor(230, 172, 5) });
            ColorDict.Add(("Smack Attack", "SustainShape"), new ColorSettings { Label = "", OnColor = new FinderColor(30, 30, 30), MenuItems = ["!sm_Linear", "!sm_Nonlinear", "!sm_BluntS"], DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("Smack Attack", "Guard"), new ColorSettings { TextOnColor = new FinderColor(0, 198, 250), MenuItems = ["Off", "Clip", "Limit"], DialSteps = 2, HideValueBar = true });
            ColorDict.Add(("Smack Attack", "Mix"), new ColorSettings { OnColor = new FinderColor(0, 198, 250) });
            ColorDict.Add(("Smack Attack", "Output"), new ColorSettings { OnColor = new FinderColor(0, 198, 250), Mode = ColorSettings.PotMode.Symmetric });

            ColorDict.Add(("Brauer Motion", "Loupedeck User Pages"), new ColorSettings { MenuItems = ["MAIN", "PNR 1", "PNR 2", "T/D 1", "T/D 2", "MIX"] });
            var path1Color = new FinderColor(139, 195, 74);
            var path2Color = new FinderColor(230, 74, 25);
            var bgColor = new FinderColor(12, 80, 124);
            var buttonBgColor = new FinderColor(3, 18, 31);
            var textColor = new FinderColor(105, 133, 157);
            var checkOnColor = new FinderColor(7, 152, 202);
            ColorDict.Add(("Brauer Motion", "Mute 1"), new ColorSettings { Label = "MUTE 1", OnColor = path1Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Mute 2"), new ColorSettings { Label = "MUTE 2", OnColor = path2Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Path 1 A Marker"), new ColorSettings { Label = "A", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Path 1 B Marker"), new ColorSettings { Label = "B", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Path 1 Start Marker"), new ColorSettings { Label = "START", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Path 2 A Marker"), new ColorSettings { Label = "A", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Path 2 B Marker"), new ColorSettings { Label = "B", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Path 2 Start Marker"), new ColorSettings { Label = "START", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Panner 1 Mode"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = path1Color, MenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
            ColorDict.Add(("Brauer Motion", "Panner 2 Mode"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = path2Color, MenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
            ColorDict.Add(("Brauer Motion", "Link"), new ColorSettings { Label = "LINK", OnColor = buttonBgColor, TextOnColor = new FinderColor(0, 192, 255), OffColor = buttonBgColor, TextOffColor = new FinderColor(60, 60, 60) });
            ColorDict.Add(("Brauer Motion", "Path 1"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
            ColorDict.Add(("Brauer Motion", "Modulator 1"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
            ColorDict.Add(("Brauer Motion", "Reverse 1"), new ColorSettings { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Mod Delay On/Off 1"), new ColorSettings { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Speed 1"), new ColorSettings { Label = "SPEED 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Offset 1"), new ColorSettings { Label = "OFFSET 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Depth 1"), new ColorSettings { Label = "DEPTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Width 1"), new ColorSettings { Label = "WIDTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Pre Delay 1"), new ColorSettings { Label = "PRE DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Mod Delay 1"), new ColorSettings { Label = "MOD DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Path 2"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
            ColorDict.Add(("Brauer Motion", "Modulator 2"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
            ColorDict.Add(("Brauer Motion", "Reverse 2"), new ColorSettings { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Mod Delay On/Off 2"), new ColorSettings { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Speed 2"), new ColorSettings { Label = "SPEED 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Offset 2"), new ColorSettings { Label = "OFFSET 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Depth 2"), new ColorSettings { Label = "DEPTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Width 2"), new ColorSettings { Label = "WIDTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Pre Delay 2"), new ColorSettings { Label = "PRE DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Mod Delay 2"), new ColorSettings { Label = "MOD DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Trigger Mode 1"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
            ColorDict.Add(("Brauer Motion", "Trigger A to B 1"), new ColorSettings { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
            ColorDict.Add(("Brauer Motion", "Trigger Sensitivity 1"), new ColorSettings { Label = "SENS 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Trigger HP 1"), new ColorSettings { Label = "HOLD 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Dynamics 1"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
            ColorDict.Add(("Brauer Motion", "Drive 1"), new ColorSettings { Label = "DRIVE 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Ratio 1"), new ColorSettings { Label = "RATIO 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Dynamics HP 1"), new ColorSettings { Label = "HP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Dynamics LP 1"), new ColorSettings { Label = "LP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Trigger Mode 2"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
            ColorDict.Add(("Brauer Motion", "Trigger A to B 2"), new ColorSettings { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
            ColorDict.Add(("Brauer Motion", "Trigger Sensitivity 2"), new ColorSettings { Label = "SENS 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Trigger HP 2"), new ColorSettings { Label = "HOLD 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Dynamics 2"), new ColorSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), MenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
            ColorDict.Add(("Brauer Motion", "Drive 2"), new ColorSettings { Label = "DRIVE 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Ratio 2"), new ColorSettings { Label = "RATIO 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Dynamics HP 2"), new ColorSettings { Label = "HP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Dynamics LP 2"), new ColorSettings { Label = "LP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Panner 1 Level"), new ColorSettings { Label = "PANNER 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            ColorDict.Add(("Brauer Motion", "Panner 2 Level"), new ColorSettings { Label = "PANNER 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            ColorDict.Add(("Brauer Motion", "Input"), new ColorSettings { Label = "INPUT", OnColor = bgColor, TextOnColor = textColor });
            ColorDict.Add(("Brauer Motion", "Output"), new ColorSettings { Label = "OUTPUT", OnColor = bgColor, TextOnColor = textColor });
            ColorDict.Add(("Brauer Motion", "Mix"), new ColorSettings { Label = "MIX", OnColor = bgColor, TextOnColor = textColor });
            ColorDict.Add(("Brauer Motion", "Start/Stop 1"), new ColorSettings { Label = "START 1", LabelOn = "STOP 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Start/Stop 2"), new ColorSettings { Label = "START 2", LabelOn = "STOP 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Ex-SC 1"), new ColorSettings { Label = "EXT SC 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Ex-SC 2"), new ColorSettings { Label = "EXT SC 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
            ColorDict.Add(("Brauer Motion", "Auto Reset"), new ColorSettings { Label = "AUTO RESET", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });

            var mixColor = new FinderColor(254, 251, 248);
            var mainCtrlColor = new FinderColor(52, 139, 125);
            var typeColor = new FinderColor(90, 92, 88);
            var delayButtonColor = new FinderColor(38, 39, 37);
            var optionsOffBgColor = new FinderColor(100, 99, 95);
            ColorDict.Add(("Abbey Road Chambers", "Input Level"), new ColorSettings { Label = "INPUT", OnColor = mixColor });
            ColorDict.Add(("Abbey Road Chambers", "Output"), new ColorSettings { Label = "OUTPUT", OnColor = mixColor });
            ColorDict.Add(("Abbey Road Chambers", "Reverb Mix"), new ColorSettings { Label = "REVERB", OnColor = mixColor });
            ColorDict.Add(("Abbey Road Chambers", "Dry/Wet"), new ColorSettings { Label = "DRY/WET", OnColor = mixColor });
            ColorDict.Add(("Abbey Road Chambers", "Reverb Time X"), new ColorSettings { Label = "TIME X", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "RS106 Top Cut"), new ColorSettings { Label = "TOP CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "RS106 Bass Cut"), new ColorSettings { Label = "BASS CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "RS127 Gain"), new ColorSettings { Label = "GAIN", Mode = ColorSettings.PotMode.Symmetric, OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "RS127 Freq"), new ColorSettings { Label = "FREQ", OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White, DialSteps = 2 });
            ColorDict.Add(("Abbey Road Chambers", "Reverb Type"), new ColorSettings { Label = "", OnColor = mixColor, TextOnColor = FinderColor.Black, MenuItems = ["CHMBR 2", "MIRROR", "STONE"] });
            this.addLinked("Abbey Road Chambers", "Mic", "Reverb Type", label: "M", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, menuItems: ["KM53", "MK2H"]);
            ColorDict.Add(("Abbey Road Chambers", "Mic Position"), new ColorSettings { Label = "P", OnColor = typeColor, TextOnColor = FinderColor.White, MenuItems = ["WALL", "CLASSIC", "ROOM"] });
            this.addLinked("Abbey Road Chambers", "Speaker", "Reverb Type", label: "S", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, menuItems: ["ALTEC", "B&W"]);
            this.addLinked("Abbey Road Chambers", "Speaker Facing", "Reverb Type", label: "F", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, menuItems: ["ROOM", "WALL"]);
            ColorDict.Add(("Abbey Road Chambers", "Filters To Chamber On/Off"), new ColorSettings { Label = "FILTERS", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "ARChambers On/Off"), new ColorSettings { Label = "STEED", OnColor = optionsOffBgColor, TextOnColor = FinderColor.White, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "Feedback"), new ColorSettings { Label = "FEEDBACK", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Top Cut FB"), new ColorSettings { Label = "TOP CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Mid FB"), new ColorSettings { Label = "MID", Mode = ColorSettings.PotMode.Symmetric, OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Bass Cut FB"), new ColorSettings { Label = "BASS CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Drive On/Off"), new ColorSettings { Label = "OFF", LabelOn = "ON", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "Drive"), new ColorSettings { Label = "DRIVE", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Delay Mod"), new ColorSettings { Label = "MOD", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Delay Time"), new ColorSettings { Label = "DELAY L", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Delay Time R"), new ColorSettings { Label = "DELAY R", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            ColorDict.Add(("Abbey Road Chambers", "Delay Link"), new ColorSettings { Label = "LINK", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });
            ColorDict.Add(("Abbey Road Chambers", "Delay Sync On/Off"), new ColorSettings { Label = "SYNC", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });

            var hybridLineColor = new FinderColor(220, 148, 49);
            var hybridButtonOnColor = new FinderColor(142, 137, 116);
            var hybridButtonOffColor = new FinderColor(215, 209, 186);
            var hybridButtonTextOnColor = new FinderColor(247, 230, 25);
            var hybridButtonTextOffColor = FinderColor.Black;
            ColorDict.Add(("H-Delay", "Sync"), new ColorSettings { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, MenuItems = ["BPM", "HOST", "MS"] });
            this.addLinked("H-Delay", "Delay BPM", "Sync", label: "DELAY", linkReversed: true, onColor: hybridLineColor, onTransparency: 255, textOnColor: FinderColor.Black, offColor: new FinderColor(50, 50, 50));
            this.addLinked("H-Delay", "Delay Sec", "Sync", label: "DELAY", onColor: hybridLineColor, onTransparency: 255, textOnColor: FinderColor.Black, offColor: new FinderColor(50, 50, 50));
            ColorDict.Add(("H-Delay", "Feedback"), new ColorSettings { Label = "FEEDBACK", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "Mix"), new ColorSettings { Label = "DRY/WET", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "Output"), new ColorSettings { Label = "OUTPUT", Mode = ColorSettings.PotMode.Symmetric, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "Analog"), new ColorSettings { Label = "ANALOG", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black, DialSteps = 4 });
            ColorDict.Add(("H-Delay", "Phase L"), new ColorSettings { Label = "PHASE L", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            ColorDict.Add(("H-Delay", "Phase R"), new ColorSettings { Label = "PHASE R", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            ColorDict.Add(("H-Delay", "PingPong"), new ColorSettings { Label = "PING PONG", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            ColorDict.Add(("H-Delay", "LoFi"), new ColorSettings { Label = "LoFi", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            ColorDict.Add(("H-Delay", "Depth"), new ColorSettings { Label = "DEPTH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "Rate"), new ColorSettings { Label = "RATE", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "HiPass"), new ColorSettings { Label = "HIPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            ColorDict.Add(("H-Delay", "LoPass"), new ColorSettings { Label = "LOPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });


            ColorDict.Add(("Sibilance", "Monitor"), new ColorSettings { OnColor = new FinderColor(0, 195, 230) });
            ColorDict.Add(("Sibilance", "Lookahead"), new ColorSettings { OnColor = new FinderColor(0, 195, 230) });

            ColorDict.Add(("MondoMod", ""), new ColorSettings { OnColor = new FinderColor(102, 255, 51) });
            ColorDict.Add(("MondoMod", "AM On/Off"), new ColorSettings { Label = "AM", LabelOn = "AM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            ColorDict.Add(("MondoMod", "FM On/Off"), new ColorSettings { Label = "FM", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            ColorDict.Add(("MondoMod", "Pan On/Off"), new ColorSettings { Label = "Pan", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            ColorDict.Add(("MondoMod", "Sync On/Off"), new ColorSettings { Label = "Manual", LabelOn = "Auto", OnColor = new FinderColor(181, 214, 165), TextOnColor = FinderColor.Black });
            ColorDict.Add(("MondoMod", "Waveform"), new ColorSettings { OnColor = new FinderColor(102, 255, 51), DialSteps = 4, HideValueBar = true });

            ColorDict.Add(("LoAir", "LoAir"), new ColorSettings { Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("LoAir", "Lo"), new ColorSettings { Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("LoAir", "Align"), new ColorSettings { OnColor = new FinderColor(206, 175, 43), TextOnColor = FinderColor.Black });

            ColorDict.Add(("CLA Unplugged", "Bass Color"), new ColorSettings { Label = "", MenuItems = [ "OFF", "SUB", "LOWER", "UPPER" ] });
            ColorDict.Add(("CLA Unplugged", "Bass"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Treble Color"), new ColorSettings { Label = "", MenuItems = ["OFF", "BITE", "TOP", "ROOF"] });
            ColorDict.Add(("CLA Unplugged", "Treble"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Compress"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Compress Color"), new ColorSettings { Label = "", MenuItems = ["OFF", "PUSH", "SPANK", "WALL"] });
            ColorDict.Add(("CLA Unplugged", "Reverb 1"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Reverb 1 Color"), new ColorSettings { Label = "", MenuItems = ["OFF", "ROOM", "HALL", "CHAMBER"] });
            ColorDict.Add(("CLA Unplugged", "Reverb 2"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Reverb 2 Color"), new ColorSettings { Label = "", MenuItems = ["OFF", "TIGHT", "LARGE", "CANYON"] });
            ColorDict.Add(("CLA Unplugged", "Delay"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Delay Color"), new ColorSettings { Label = "", MenuItems = ["OFF", "SLAP", "EIGHT", "QUARTER"] });
            ColorDict.Add(("CLA Unplugged", "Sensitivity"), new ColorSettings { Label = "Input Sens", OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "Output"), new ColorSettings { OnColor = new FinderColor(210, 209, 96), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("CLA Unplugged", "PreDelay 1"), new ColorSettings { Label = "Pre Rvrb 1", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            ColorDict.Add(("CLA Unplugged", "PreDelay 2"), new ColorSettings { Label = "Pre Rvrb 2", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            ColorDict.Add(("CLA Unplugged", "PreDelay 1 On"), new ColorSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            ColorDict.Add(("CLA Unplugged", "PreDelay 2 On"), new ColorSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            ColorDict.Add(("CLA Unplugged", "Direct"), new ColorSettings { OnColor = new FinderColor(80, 80, 80), OffColor = new FinderColor(240, 228, 87),
                                                                           TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });

            ColorDict.Add(("CLA-76", "Revision"), new ColorSettings { Label = "Bluey", LabelOn = "Blacky", OffColor = new FinderColor(62, 141, 180), TextOffColor = FinderColor.White, 
                                                                                                           OnColor = FinderColor.Black, TextOnColor = FinderColor.White });
            ColorDict.Add(("CLA-76", "Ratio"), new ColorSettings { MenuItems = ["20", "12", "8", "4", "ALL"] });
            ColorDict.Add(("CLA-76", "Analog"), new ColorSettings { Label = "A", MenuItems = ["50Hz", "60Hz", "Off"], TextOnColor = new FinderColor(254, 246, 212) });
            ColorDict.Add(("CLA-76", "Meter"), new ColorSettings { MenuItems = ["GR", "IN", "OUT"] });
            ColorDict.Add(("CLA-76", "Comp Off"), new ColorSettings { OnColor = new FinderColor(162, 38, 38), TextOnColor = FinderColor.White });

            // Analog Obsession

            ColorDict.Add(("Rare", "Bypass"), new ColorSettings { Label = "IN", OnColor = new FinderColor(191, 0, 22) });
            ColorDict.Add(("Rare", "Low Boost"), new ColorSettings { Label = "Low Boost", OnColor = new FinderColor(93, 161, 183) });
            ColorDict.Add(("Rare", "Low Atten"), new ColorSettings { Label = "Low Atten", OnColor = new FinderColor(93, 161, 183) });
            ColorDict.Add(("Rare", "High Boost"), new ColorSettings { Label = "High Boost", OnColor = new FinderColor(93, 161, 183) });
            ColorDict.Add(("Rare", "High Atten"), new ColorSettings { Label = "High Atten", OnColor = new FinderColor(93, 161, 183) });
            ColorDict.Add(("Rare", "Low Frequency"), new ColorSettings { Label = "Low Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 3 });
            ColorDict.Add(("Rare", "High Freqency"), new ColorSettings { Label = "High Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 6 });
            ColorDict.Add(("Rare", "High Bandwidth"), new ColorSettings { Label = "Bandwidth", OnColor = new FinderColor(93, 161, 183) });
            ColorDict.Add(("Rare", "High Atten Freqency"), new ColorSettings { Label = "Atten Sel", OnColor = new FinderColor(93, 161, 183), DialSteps = 2 });

            ColorDict.Add(("LALA", "Bypass"), new ColorSettings { Label = "OFF", LabelOn = "ON", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0), OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "Gain"), new ColorSettings { Label = "GAIN", OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "Peak Reduction"), new ColorSettings { Label = "REDUCTION", OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "Mode"), new ColorSettings { Label = "LIMIT", LabelOn = "COMP", TextOnColor = new FinderColor(0, 0, 0), 
                                                                                                   TextOffColor = new FinderColor(0, 0, 0),
                                                                                                   OnColor = new FinderColor(185, 182, 163),
                                                                                                   OffColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "1:3"), new ColorSettings { Label = "MIX", OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "2:1"), new ColorSettings { Label = "HPF", OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "MF"), new ColorSettings { OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "MG"), new ColorSettings { OnColor = new FinderColor(185, 182, 163), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("LALA", "HF"), new ColorSettings { OnColor = new FinderColor(185, 182, 163) });
            ColorDict.Add(("LALA", "External Sidechain"), new ColorSettings { Label = "SIDECHAIN", OnColor = new FinderColor(185, 182, 163) });

            ColorDict.Add(("FETish", ""), new ColorSettings { OnColor = new FinderColor(24, 86, 119) });
            ColorDict.Add(("FETish", "Bypass"), new ColorSettings { Label = "IN", OnColor = new FinderColor(24, 86, 119) });
            ColorDict.Add(("FETish", "Input"), new ColorSettings { Label = "INPUT", OnColor = new FinderColor(186, 175, 176) });
            ColorDict.Add(("FETish", "Output"), new ColorSettings { Label = "OUTPUT", OnColor = new FinderColor(186, 175, 176) });
            ColorDict.Add(("FETish", "Ratio"), new ColorSettings { OnColor = new FinderColor(186, 175, 176), DialSteps = 16 });
            ColorDict.Add(("FETish", "Sidechain"), new ColorSettings { Label = "EXT", OnColor = new FinderColor(24, 86, 119) });
            ColorDict.Add(("FETish", "Mid Frequency"), new ColorSettings { Label = "MF", OnColor = new FinderColor(24, 86, 119) });
            ColorDict.Add(("FETish", "Mid Gain"), new ColorSettings { Label = "MG", OnColor = new FinderColor(24, 86, 119), Mode = ColorSettings.PotMode.Symmetric });

            ColorDict.Add(("dBComp", ""), new ColorSettings { OnColor = new FinderColor(105, 99, 94) });
            ColorDict.Add(("dBComp", "Output Gain"), new ColorSettings { Label = "Output", OnColor = new FinderColor(105, 99, 94) });
            ColorDict.Add(("dBComp", "1:4U"), new ColorSettings { Label = "EXT SC", OnColor = new FinderColor(208, 207, 203), TextOnColor = FinderColor.Black });

            ColorDict.Add(("BUSTERse", "Bypass"), new ColorSettings { Label = "MAIN", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BUSTERse", "Turbo"), new ColorSettings { Label = "TURBO", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BUSTERse", "XFormer"), new ColorSettings { Label = "XFORMER", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BUSTERse", "Threshold"), new ColorSettings { Label = "THRESH", OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "Attack Time"), new ColorSettings { Label = "ATTACK", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            ColorDict.Add(("BUSTERse", "Ratio"), new ColorSettings { Label = "RATIO", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            ColorDict.Add(("BUSTERse", "Make-Up Gain"), new ColorSettings { Label = "MAKE-UP", OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "Release Time"), new ColorSettings { Label = "RELEASE", OnColor = new FinderColor(174, 164, 167), DialSteps = 4 });
            ColorDict.Add(("BUSTERse", "Compressor Mix"), new ColorSettings { Label = "MIX", OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "External Sidechain"), new ColorSettings { Label = "EXT", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BUSTERse", "HF"), new ColorSettings { OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "Mid Gain"), new ColorSettings { Label = "MID", OnColor = new FinderColor(174, 164, 167), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("BUSTERse", "HPF"), new ColorSettings { OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "Boost"), new ColorSettings { Label = "TR BOOST", OnColor = new FinderColor(174, 164, 167) });
            ColorDict.Add(("BUSTERse", "Transient Tilt"), new ColorSettings { Label = "TR TILT", OnColor = new FinderColor(174, 164, 167), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("BUSTERse", "Transient Mix"), new ColorSettings { Label = "TR MIX", OnColor = new FinderColor(174, 164, 167) });

            ColorDict.Add(("BritChannel", ""), new ColorSettings { OnColor = new FinderColor(141, 134, 137), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("BritChannel", "Bypass"), new ColorSettings { Label = "IN", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BritChannel", "Mic Pre"), new ColorSettings { Label = "MIC", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            ColorDict.Add(("BritChannel", "Mid Freq"), new ColorSettings { OnColor = new FinderColor(141, 134, 137), DialSteps = 6 });
            ColorDict.Add(("BritChannel", "Low Freq"), new ColorSettings { OnColor = new FinderColor(141, 134, 137), DialSteps = 4 });
            ColorDict.Add(("BritChannel", "HighPass"), new ColorSettings { Label = "High Pass", OnColor = new FinderColor(49, 81, 119), DialSteps = 4 });
            ColorDict.Add(("BritChannel", "Preamp Gain"), new ColorSettings { Label = "PRE GAIN", OnColor = new FinderColor(160, 53, 50), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("BritChannel", "Output Trim"), new ColorSettings { Label = "OUT TRIM", OnColor = new FinderColor(124, 117, 115), Mode = ColorSettings.PotMode.Symmetric });

            // Acon Digital

            ColorDict.Add(("Acon Digital Equalize 2", "Gain-bandwidth link"), new ColorSettings { Label = "Link", OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 1"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 1"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 1"), new ColorSettings { OnColor = new FinderColor(221, 125, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 1"), new ColorSettings { OnColor = new FinderColor(221, 125, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 1"), new ColorSettings { Label = "Filter 1", OnColor = new FinderColor(221, 125, 125), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 1"), new ColorSettings { Label = "Bandwidth 1", OnColor = new FinderColor(221, 125, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 1"), new ColorSettings { OnColor = new FinderColor(221, 125, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 1"), new ColorSettings { OnColor = new FinderColor(221, 125, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 2"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 2"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 2"), new ColorSettings { OnColor = new FinderColor(204, 133, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 2"), new ColorSettings { OnColor = new FinderColor(204, 133, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 2"), new ColorSettings { Label = "Filter 2", OnColor = new FinderColor(204, 133, 61), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 2"), new ColorSettings { Label = "Bandwidth 2", OnColor = new FinderColor(204, 133, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 2"), new ColorSettings { OnColor = new FinderColor(204, 133, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 2"), new ColorSettings { OnColor = new FinderColor(204, 133, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 3"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 3"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 3"), new ColorSettings { OnColor = new FinderColor(204, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 3"), new ColorSettings { OnColor = new FinderColor(204, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 3"), new ColorSettings { Label = "Filter 3", OnColor = new FinderColor(204, 204, 61), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 3"), new ColorSettings { Label = "Bandwidth 3", OnColor = new FinderColor(204, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 3"), new ColorSettings { OnColor = new FinderColor(204, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 3"), new ColorSettings { OnColor = new FinderColor(204, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 4"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 4"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 4"), new ColorSettings { OnColor = new FinderColor(61, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 4"), new ColorSettings { OnColor = new FinderColor(61, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 4"), new ColorSettings { Label = "Filter 4", OnColor = new FinderColor(61, 204, 61), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 4"), new ColorSettings { Label = "Bandwidth 4", OnColor = new FinderColor(61, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 4"), new ColorSettings { OnColor = new FinderColor(61, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 4"), new ColorSettings { OnColor = new FinderColor(61, 204, 61) });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 5"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 5"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 5"), new ColorSettings { OnColor = new FinderColor(61, 204, 133) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 5"), new ColorSettings { OnColor = new FinderColor(61, 204, 133) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 5"), new ColorSettings { Label = "Filter 5", OnColor = new FinderColor(61, 204, 133), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 5"), new ColorSettings { Label = "Bandwidth 5", OnColor = new FinderColor(61, 204, 133) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 5"), new ColorSettings { OnColor = new FinderColor(61, 204, 133) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 5"), new ColorSettings { OnColor = new FinderColor(61, 204, 133) });
            ColorDict.Add(("Acon Digital Equalize 2", "Solo 6"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Bypass 6"), new ColorSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Equalize 2", "Frequency 6"), new ColorSettings { OnColor = new FinderColor(173, 221, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Gain 6"), new ColorSettings { OnColor = new FinderColor(173, 221, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Filter type 6"), new ColorSettings { Label = "Filter 6", OnColor = new FinderColor(173, 221, 125), DialSteps = 8, HideValueBar = true });
            ColorDict.Add(("Acon Digital Equalize 2", "Band width 6"), new ColorSettings { Label = "Bandwidth 6 ", OnColor = new FinderColor(173, 221, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Slope 6"), new ColorSettings { OnColor = new FinderColor(173, 221, 125) });
            ColorDict.Add(("Acon Digital Equalize 2", "Resonance 6"), new ColorSettings { OnColor = new FinderColor(173, 221, 125) });

            ColorDict.Add(("Acon Digital Verberate 2", "Dry Mute"), new ColorSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Verberate 2", "Reverb Mute"), new ColorSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Verberate 2", "ER Mute"), new ColorSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Verberate 2", "Freeze"), new ColorSettings { OnColor = new FinderColor(230, 173, 43), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Acon Digital Verberate 2", "Stereo Spread"), new ColorSettings { Label = "Spread" });
            ColorDict.Add(("Acon Digital Verberate 2", "EarlyReflectionsType"), new ColorSettings { Label = "ER Type", DialSteps = 14, HideValueBar = true });
            ColorDict.Add(("Acon Digital Verberate 2", "Algorithm"), new ColorSettings { Label = "Vivid", LabelOn = "Legacy", TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            ColorDict.Add(("Acon Digital Verberate 2", "Decay High Cut Enable"), new ColorSettings { Label = "Decay HC", OnColor = new FinderColor(221, 85, 255) });
            this.addLinked("Acon Digital Verberate 2", "Decay High Cut Frequency", "Decay High Cut Enable", label: "Freq");
            this.addLinked("Acon Digital Verberate 2", "Decay High Cut Slope", "Decay High Cut Enable", label: "Slope");
            ColorDict.Add(("Acon Digital Verberate 2", "EQ High Cut Enable"), new ColorSettings { Label = "EQ HC", OnColor = new FinderColor(221, 85, 255) });
            this.addLinked("Acon Digital Verberate 2", "EQ High Cut Frequency", "EQ High Cut Enable", label: "Freq");
            this.addLinked("Acon Digital Verberate 2", "EQ High Cut Slope", "EQ High Cut Enable", label: "Slope");


            // AXP

            ColorDict.Add(("AXP SoftAmp PSA", "Enable"), new ColorSettings { Label = "ENABLE" });
            ColorDict.Add(("AXP SoftAmp PSA", "Preamp"), new ColorSettings { Label = "PRE-AMP", OnColor = new FinderColor(200, 200, 200) });
            ColorDict.Add(("AXP SoftAmp PSA", "Asymm"), new ColorSettings { Label = "ASYMM", OnColor = new FinderColor(237, 244, 1), TextOnColor = FinderColor.Black });
            ColorDict.Add(("AXP SoftAmp PSA", "Buzz"), new ColorSettings { Label = "BUZZ", OnColor = new FinderColor(200, 200, 200), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("AXP SoftAmp PSA", "Punch"), new ColorSettings { Label = "PUNCH", OnColor = new FinderColor(200, 200, 200), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("AXP SoftAmp PSA", "Crunch"), new ColorSettings { Label = "CRUNCH", OnColor = new FinderColor(200, 200, 200) });
            ColorDict.Add(("AXP SoftAmp PSA", "SoftClip"), new ColorSettings { Label = "SOFT CLIP", OnColor = new FinderColor(234, 105, 30), TextOnColor = FinderColor.Black });
            ColorDict.Add(("AXP SoftAmp PSA", "Drive"), new ColorSettings { Label = "DRIVE", OnColor = new FinderColor(200, 200, 200) });
            ColorDict.Add(("AXP SoftAmp PSA", "Level"), new ColorSettings { Label = "LEVEL", OnColor = new FinderColor(200, 200, 200) });
            ColorDict.Add(("AXP SoftAmp PSA", "Limiter"), new ColorSettings { Label = "LIMITER", OnColor = new FinderColor(237, 0, 0), TextOnColor = FinderColor.Black });
            ColorDict.Add(("AXP SoftAmp PSA", "Low"), new ColorSettings { Label = "LOW", OnColor = new FinderColor(200, 200, 200), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("AXP SoftAmp PSA", "High"), new ColorSettings { Label = "HIGH", OnColor = new FinderColor(200, 200, 200), Mode = ColorSettings.PotMode.Symmetric });
            ColorDict.Add(("AXP SoftAmp PSA", "SpkReso"), new ColorSettings { Label = "SHAPE", OnColor = new FinderColor(120, 120, 120) });
            ColorDict.Add(("AXP SoftAmp PSA", "SpkRoll"), new ColorSettings { Label = "ROLL-OFF", OnColor = new FinderColor(120, 120, 120) });
            ColorDict.Add(("AXP SoftAmp PSA", "PSI_En"), new ColorSettings { Label = "PSI DNS", OnColor = new FinderColor(10, 178, 255), TextOnColor = FinderColor.Black });
            this.addLinked("AXP SoftAmp PSA", "PSI_Thr", "PSI_En", label: "THRESHOLD");
            ColorDict.Add(("AXP SoftAmp PSA", "OS_Enab"), new ColorSettings { Label = "SQUEEZO", OnColor = new FinderColor(209, 155, 104), TextOnColor = FinderColor.Black });
            this.addLinked("AXP SoftAmp PSA", "OS_Gain", "OS_Enab", label: "GAIN");
            this.addLinked("AXP SoftAmp PSA", "OS_Bias", "OS_Enab", label: "BIAS");
            this.addLinked("AXP SoftAmp PSA", "OS_Level", "OS_Enab", label: "LEVEL");

            // Izotope

            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B1 Attack"), new ColorSettings { Label = "1: Attack", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B1 Sustain"), new ColorSettings { Label = "1: Sustain", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B1 Bypass"), new ColorSettings { Label = "Bypass", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B2 Attack"), new ColorSettings { Label = "2: Attack", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B2 Sustain"), new ColorSettings { Label = "2: Sustain", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B2 Bypass"), new ColorSettings { Label = "Bypass", OnColor = new FinderColor(63, 191, 173), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B3 Attack"), new ColorSettings { Label = "3: Attack", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B3 Sustain"), new ColorSettings { Label = "3: Sustain", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS B3 Bypass"), new ColorSettings { Label = "Bypass", OnColor = new FinderColor(196, 232, 107), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "Global Input Gain"), new ColorSettings { Label = "In" });
            ColorDict.Add(("Neutron 4 Transient Shaper", "Global Output Gain"), new ColorSettings { Label = "Out" });
            ColorDict.Add(("Neutron 4 Transient Shaper", "Sum to Mono"), new ColorSettings { Label = "Mono", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "Swap Channels"), new ColorSettings { Label = "Swap", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "Invert Phase"), new ColorSettings { OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            ColorDict.Add(("Neutron 4 Transient Shaper", "TS Global Mix"), new ColorSettings { Label = "Mix", OnColor = new FinderColor(255, 96, 28) });

            ColorDict.Add(("Trash", "B2 Trash Drive"), new ColorSettings { Label = "Drive", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Tilt Gain"), new ColorSettings { Label = "Tilt", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Tilt Frequency"), new ColorSettings { Label = "Frequency", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Mix"), new ColorSettings { Label = "Mix", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Blend X"), new ColorSettings { Label = "X", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Blend Y"), new ColorSettings { Label = "Y", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Top Left Style"), new ColorSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Top Right Style"), new ColorSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Bottom Left Style"), new ColorSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "B2 Trash Bottom Right Style"), new ColorSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = ColorSettings.PotMode.Symmetric, PaintLabelBg = false });
            ColorDict.Add(("Trash", "Global Input Gain"), new ColorSettings { Label = "IN" });
            ColorDict.Add(("Trash", "Global Output Gain"), new ColorSettings { Label = "OUT" });
            ColorDict.Add(("Trash", "Auto Gain Enabled"), new ColorSettings { Label = "Auto Gain" });
            ColorDict.Add(("Trash", "Limiter Enabled"), new ColorSettings { Label = "Limiter" });


            // Tokio Dawn Labs
            ColorDict.Add(("TDR Kotelnikov", ""), new ColorSettings { OnColor = new FinderColor(42, 75, 124) });
            ColorDict.Add(("TDR Kotelnikov", "SC Stereo Diff"), new ColorSettings { Label = "Stereo Diff", OnColor = new FinderColor(42, 75, 124) });
        }
    }
}
