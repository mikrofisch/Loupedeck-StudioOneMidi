namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Serialization;
    using System.ComponentModel;
    using System.Diagnostics;


    // BitmapColor objects that have not been explicitly assigned to a
    // color are automatically replaced by the currently defined default color.
    // Since it is not possible to have a BitmapColor object that is not assigned
    // to a color (BitmapColor.NoColor evaluates to the same values as BitmapColor.White) and
    // it cannot be set to null, we define a new class that can be null.
    //
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

    public class PlugSettingsFinder
    {
        public const Int32 DefaultOnTransparency = 80;

        public static readonly BitmapColor NoColor = new BitmapColor(-1, -1, -1);
        [XmlInclude(typeof(S1TopControlColors))]
        public class PlugParamSettings
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
            public Int32 OnTransparency { get; set; } = DefaultOnTransparency;
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
            public String LinkedStates { get; set; }                // Comma separated list of indices for which the linked parameter is active
                                                                    // if the linked parameter has multipe states
            [DefaultValueAttribute(100)]
            public Int32 DialSteps { get; set; } = 100;             // Number of steps for a mode dial

            public String[] UserMenuItems;                              // Items for user button menu

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

        private class PlugParamDeviceEntry
        {
            public Dictionary<String, FinderColor> Colors = [];
            public Dictionary<String, PlugParamSettings> ParamSettings = [];
        }

//        private static readonly Dictionary<(String PluginName, String PluginParameter), PlugParamSettings> PlugParamDict = [];
        private static readonly Dictionary<String, PlugParamDeviceEntry> PlugParamDict = [];
        private const String strPlugParamSettingsID = "[ps]";  // for plugin settings

        private String LastPluginName, LastPluginParameter;
        private PlugParamSettings LastParamSettings;

        public Int32 CurrentUserPage = 0;              // For tracking the current user page position
        public Int32 CurrentChannel = 0;

        const String ConfigFileName = "AudioPluginConfig.xml";

        public class ConfigEntry
        {
            public String key1;
            public String key2;
            public PlugParamSettings colorSettings;
        }

        public PlugParamSettings DefaultPlugParamSettings { get; private set; } = new PlugParamSettings
        {
            OnColor = FinderColor.Transparent,
            OffColor = FinderColor.Transparent,
            TextOnColor = FinderColor.White,
            TextOffColor = FinderColor.White
        };

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public PlugSettingsFinder() { }

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public PlugSettingsFinder(PlugParamSettings defaultPlugParamSettings)
        {
            this.DefaultPlugParamSettings = defaultPlugParamSettings;
        }

        public class S1TopControlColors : PlugParamSettings
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

        public class XmlConfig
        {
            public ConfigEntry[] Entries = new ConfigEntry[10];
        }

        public static void Init(Plugin plugin, Boolean forceReload = false)
        {
            if (forceReload)
            {
                PlugParamDict.Clear();
            }
            if (PlugParamDict.Count == 0)
            {
                InitColorDict();

                var configFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StudioOneMidiPlugin");
                if (!Directory.Exists(configFolderPath))
                {
                    Directory.CreateDirectory(configFolderPath);
                }
                var configFilePath = System.IO.Path.Combine(configFolderPath, ConfigFileName);

                XmlConfig blub = new XmlConfig();

                var serializer = new XmlSerializer(typeof(XmlConfig));
                TextWriter writer = new StreamWriter(configFilePath);

                serializer.Serialize(writer, blub);

//                foreach (KeyValuePair<(String, String), ColorSettings> entry in ColorDict)
//                {
//                    serializer.Serialize(writer, new ConfigEntry
//                    {
//                        key1 = entry.Key.Item1,
//                        key2 = entry.Key.Item2,
//                        colorSettings = entry.Value
//                    });
//                }

                writer.Close();

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
                    if (setting.StartsWith(strPlugParamSettingsID))
                    {
                        var settingsParsed = setting.Substring(strPlugParamSettingsID.Length).Split('|');
                        if (!PlugParamDict.TryGetValue(settingsParsed[0], out var pe))
                        {
                            pe = new PlugParamDeviceEntry { };
                            PlugParamDict.Add(settingsParsed[0], pe);
                        }
                        if (!pe.ParamSettings.TryGetValue(settingsParsed[1], out var ps))
                        {
                            ps = new PlugParamSettings { };
                            pe.ParamSettings.Add(settingsParsed[0], ps);
                        }


                        if (plugin.TryGetPluginSetting(settingName(settingsParsed[0], settingsParsed[1], settingsParsed[2]), out var val))
                        {
                            switch (settingsParsed[2])
                            {
                                case PlugParamSettings.strOnColor:
                                    ps.OnColor = new FinderColor(Convert.ToByte(val.Substring(0, 2), 16),
                                                                 Convert.ToByte(val.Substring(2, 2), 16),
                                                                 Convert.ToByte(val.Substring(4, 2), 16));
                                    break;
                                case PlugParamSettings.strLabel:
                                    ps.Label = val;
                                    break;
                                case PlugParamSettings.strLinkedParameter:
                                    ps.LinkedParameter = val;
                                    break;
                                case PlugParamSettings.strMode:
                                    ps.Mode = val.ParseInt32() == 0 ? PlugParamSettings.PotMode.Positive : PlugParamSettings.PotMode.Symmetric;
                                    break;
                                case PlugParamSettings.strShowCircle:
                                    ps.ShowUserButtonCircle = val.ParseInt32() == 1 ? true : false;
                                    break;
                            }
                        }
                    }
                }
            }
        }

        private PlugParamSettings SaveLastSettings(PlugParamSettings paramSettings)
        {
            this.LastParamSettings = paramSettings;
            return paramSettings;
        }
        public PlugParamSettings GetParamSettings(String pluginName, String parameterName, Boolean isUser)
        {
            if (pluginName == null || parameterName == null) return this.DefaultPlugParamSettings;
//            if (this.LastParamSettings != null && pluginName == this.LastPluginName && parameterName == this.LastPluginParameter) return this.LastParamSettings;

//            if (!PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
//            {
//                return this.DefaultPlugParamSettings;
//            }

            this.LastPluginName = pluginName;
            this.LastPluginParameter = parameterName;

            var userPagePos = $"{this.CurrentUserPage}:{this.CurrentChannel}" + (isUser ? "U" : "");
            PlugParamSettings paramSettings;

            // Find device entry.

            if (!PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
            {
                // No full match, try partial match
                var partialMatchKeys = PlugParamDict.Keys.Where(key => key != "" && pluginName.StartsWith(key));
                if (partialMatchKeys.Any())
                {
                    PlugParamDict.TryGetValue(partialMatchKeys.First(), out deviceEntry);
                }
            }

            if (deviceEntry != null &&
                (deviceEntry.ParamSettings.TryGetValue(userPagePos, out paramSettings) ||
                deviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings) ||
                deviceEntry.ParamSettings.TryGetValue("", out paramSettings)))
            {
                if (isUser)
                {
                    Debug.WriteLine("Match 1: " + userPagePos + ", " + paramSettings.Label);
                }
                return this.SaveLastSettings(paramSettings);
            }

            if (PlugParamDict.TryGetValue("", out deviceEntry) &&
                deviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings))
            {
                if (isUser)
                {
                    Debug.WriteLine("Match 2: " + userPagePos + ", " + paramSettings.Label);
                }
                return this.SaveLastSettings(paramSettings);
            }

            // Try partial match of plugin name.
            //            var partialMatchKeys = PlugParamDict.Keys.Where(currentKey => pluginName.StartsWith(currentKey.PluginName) && currentKey.PluginParameter == parameterName);
            //            if (partialMatchKeys.Count() > 0 && PlugParamDict.TryGetValue(partialMatchKeys.First(), out paramSettings))
            //            {
            //                return this.saveLastSettings(paramSettings);
            //            }
            //            partialMatchKeys = PlugParamDict.Keys.Where(currentKey => pluginName.Contains(currentKey.PluginName) && currentKey.PluginParameter == "");
            //            if (partialMatchKeys.Count() > 0 && PlugParamDict.TryGetValue(partialMatchKeys.First(), out paramSettings))
            //            {
            //                return this.saveLastSettings(paramSettings);
            //            }

            //            var partialMatchKeys = PlugParamDict.Keys.Where(key => key != "" && pluginName.StartsWith(key));
            //            if (PlugParamDict.TryGetValue(partialMatchKeys.First(), out deviceEntry) &&
            //                (deviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings) ||
            //                 deviceEntry.ParamSettings.TryGetValue("", out paramSettings)))
            //            {
            //                return this.SaveLastSettings(paramSettings);
            //            }

            if (isUser)
            {
                Debug.WriteLine("Default: " + userPagePos + ", " + DefaultPlugParamSettings.Label);
            }
            return this.SaveLastSettings(this.DefaultPlugParamSettings);
        }

        private BitmapColor findColor(FinderColor settingsColor, BitmapColor defaultColor) => settingsColor ?? defaultColor;

        public PlugParamSettings.PotMode getMode(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).Mode;
        public Boolean getShowCircle(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).ShowUserButtonCircle;
        public Boolean getPaintLabelBg(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).PaintLabelBg;

        public BitmapColor getOnColor(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.GetParamSettings(pluginName, parameterName, isUser);
            return cs != null ? new BitmapColor(cs.OnColor, isUser ? 255 : cs.OnTransparency)
                              : new BitmapColor(this.DefaultPlugParamSettings.OnColor, isUser ? 255 : this.DefaultPlugParamSettings.OnTransparency);
        }
        public BitmapColor getBarOnColor(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.GetParamSettings(pluginName, parameterName, isUser);
            return cs.BarOnColor ?? this.findColor(cs.OnColor, this.DefaultPlugParamSettings.OnColor);
        }
        public BitmapColor getOffColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.GetParamSettings(pluginName, parameterName, isUser).OffColor,
                                                                                                  this.DefaultPlugParamSettings.OffColor);
        public BitmapColor getTextOnColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.GetParamSettings(pluginName, parameterName, isUser).TextOnColor,
                                                                                                     this.DefaultPlugParamSettings.TextOnColor);
        public BitmapColor getTextOffColor(String pluginName, String parameterName, Boolean isUser = false) => this.findColor(this.GetParamSettings(pluginName, parameterName, isUser).TextOffColor,
                                                                                                      this.DefaultPlugParamSettings.TextOffColor);
        public String getLabel(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).Label ?? parameterName;
        public String getLabelOn(String pluginName, String parameterName, Boolean isUser = false)
        {
            var cs = this.GetParamSettings(pluginName, parameterName, isUser);
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
            var colorSettings = this.GetParamSettings(pluginName, parameterName, false);
            if (colorSettings.IconName != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconName}_52px.png"));
            }
            return null;
        }

        public BitmapImage getIconOn(String pluginName, String parameterName)
        {
            var colorSettings = this.GetParamSettings(pluginName, parameterName, false);
            if (colorSettings.IconNameOn != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconNameOn}_52px.png"));
            }
            return null;
        }
        public String getLinkedParameter(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).LinkedParameter;
        public Boolean getLinkReversed(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).LinkReversed;
        public String getLinkedStates(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).LinkedStates;
        public Boolean hideValueBar(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).HideValueBar;
        public Boolean showUserButtonCircle(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).ShowUserButtonCircle;
        public Int32 getDialSteps(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).DialSteps;
        public String[] getUserMenuItems(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).UserMenuItems;
        public Boolean hasMenu(String pluginName, String parameterName, Boolean isUser = false) => this.GetParamSettings(pluginName, parameterName, isUser).UserMenuItems != null;
        public static String settingName(String pluginName, String parameterName, String setting) => strPlugParamSettingsID + pluginName + "|" + parameterName + "|" + setting;

        private static void AddParamSetting(String pluginName, String parameterName, PlugParamSettings setting)
        {
            if (!PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
            {
                deviceEntry = new PlugParamDeviceEntry();
                PlugParamDict.Add(pluginName, deviceEntry);
            }
            deviceEntry.ParamSettings.Add(parameterName, setting);
        }
        private static void AddLinked(String pluginName, String parameterName, String linkedParameter,
                                      String label = null,
                                      PlugParamSettings.PotMode mode = PlugParamSettings.PotMode.Positive,
                                      Boolean linkReversed = false,
                                      String linkedStates = "",
                                      FinderColor onColor = null,
                                      Int32 onTransparency = DefaultOnTransparency,
                                      FinderColor textOnColor = null,
                                      FinderColor offColor = null,
                                      FinderColor textOffColor = null,
                                      String[] userMenuItems = null)
        {
            if (label == null)
                label = parameterName;
            var paramSettings = PlugParamDict[pluginName].ParamSettings[linkedParameter];
            AddParamSetting(pluginName, parameterName, new PlugParamSettings
            {
                Mode = mode,
                OnColor = onColor ?? paramSettings.OnColor,
                OnTransparency = onTransparency,
                OffColor = offColor ?? paramSettings.OffColor,
                TextOnColor = textOnColor ?? paramSettings.TextOnColor,
                TextOffColor = textOffColor ?? paramSettings.TextOffColor,
                Label = label,
                LinkedParameter = linkedParameter,
                LinkReversed = linkReversed,
                LinkedStates = linkedStates,
                UserMenuItems = userMenuItems
            });
        }

        private static void InitColorDict()
        {
            AddParamSetting("", "Bypass", new PlugParamSettings { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });
            AddParamSetting("", "Global Bypass", new PlugParamSettings { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });

            AddParamSetting("Pro EQ", "Show Controls", new S1TopControlColors(label: "Band Controls"));
            AddParamSetting("Pro EQ", "Show Dynamics", new S1TopControlColors(label: "Dynamics"));
            AddParamSetting("Pro EQ", "High Quality", new S1TopControlColors());
            AddParamSetting("Pro EQ", "View Mode", new S1TopControlColors(label: "Curves"));
            AddParamSetting("Pro EQ", "LF-Active", new PlugParamSettings { OnColor = new FinderColor(255, 120, 38), Label = "LF", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "MF-Active", new PlugParamSettings { OnColor = new FinderColor(107, 224, 44), Label = "MF", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "HF-Active", new PlugParamSettings { OnColor = new FinderColor(75, 212, 250), Label = "HF", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "LMF-Active", new PlugParamSettings { OnColor = new FinderColor(245, 205, 58), Label = "LMF", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "HMF-Active", new PlugParamSettings { OnColor = new FinderColor(70, 183, 130), Label = "HMF", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "LC-Active", new PlugParamSettings { OnColor = new FinderColor(255, 74, 61), Label = "LC", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "HC-Active", new PlugParamSettings { OnColor = new FinderColor(158, 98, 255), Label = "HC", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "LLC-Active", new PlugParamSettings { OnColor = FinderColor.White, Label = "LLC", ShowUserButtonCircle = true });
            AddParamSetting("Pro EQ", "Global Gain", new PlugParamSettings { OnColor = new FinderColor(200, 200, 200), Label = "Gain", Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Pro EQ", "Auto Gain", new PlugParamSettings { Label = "Auto" });
            AddLinked("Pro EQ", "LF-Gain", "LF-Active", label: "LF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Pro EQ", "LF-Frequency", "LF-Active", label: "LF Freq");
            AddLinked("Pro EQ", "LF-Q", "LF-Active", label: "LF Q");
            AddLinked("Pro EQ", "MF-Gain", "MF-Active", label: "MF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Pro EQ", "MF-Frequency", "MF-Active", label: "MF Freq");
            AddLinked("Pro EQ", "MF-Q", "MF-Active", label: "MF Q");
            AddLinked("Pro EQ", "HF-Gain", "HF-Active", label: "HF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Pro EQ", "HF-Frequency", "HF-Active", label: "HF Freq");
            AddLinked("Pro EQ", "HF-Q", "HF-Active", label: "HF Q");
            AddLinked("Pro EQ", "LMF-Gain", "LMF-Active", label: "LMF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Pro EQ", "LMF-Frequency", "LMF-Active", label: "LMF Freq");
            AddLinked("Pro EQ", "LMF-Q", "LMF-Active", label: "LMF Q");
            AddLinked("Pro EQ", "HMF-Gain", "HMF-Active", label: "HMF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Pro EQ", "HMF-Frequency", "HMF-Active", label: "HMF Freq");
            AddLinked("Pro EQ", "HMF-Q", "HMF-Active", label: "HMF Q");
            AddLinked("Pro EQ", "LC-Frequency", "LC-Active", label: "LC Freq");
            AddLinked("Pro EQ", "HC-Frequency", "HC-Active", label: "HC Freq");
            AddParamSetting("Pro EQ", "LF-Solo", new PlugParamSettings { OnColor = new FinderColor(224, 182, 69), Label = "LF Solo" });
            AddParamSetting("Pro EQ", "MF-Solo", new PlugParamSettings { OnColor = new FinderColor(224, 182, 69), Label = "MF Solo" });
            AddParamSetting("Pro EQ", "HF-Solo", new PlugParamSettings { OnColor = new FinderColor(224, 182, 69), Label = "HF Solo" });
            AddParamSetting("Pro EQ", "LMF-Solo", new PlugParamSettings { OnColor = new FinderColor(224, 182, 69), Label = "LMF Solo" });
            AddParamSetting("Pro EQ", "HMF-Solo", new PlugParamSettings { OnColor = new FinderColor(224, 182, 69), Label = "HMF Solo" });

            AddParamSetting("Fat Channel", "Loupedeck User Pages", new PlugParamSettings { UserMenuItems = ["Gate", "Comp", "EQ 1", "EQ 2", "Limiter"] });
            AddParamSetting("Fat Channel", "Hi Pass Filter", new PlugParamSettings { Label = "Hi Pass" });
            AddParamSetting("Fat Channel", "Gate On", new PlugParamSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Gate ON" });
            AddParamSetting("Fat Channel", "Range", new PlugParamSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Expander", LinkReversed = true });
            AddParamSetting("Fat Channel", "Expander", new PlugParamSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            AddParamSetting("Fat Channel", "Key Listen", new PlugParamSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            AddParamSetting("Fat Channel", "Compressor On", new PlugParamSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Cmpr ON" });
            AddParamSetting("Fat Channel", "Attack", new PlugParamSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto", LinkReversed = true });
            AddParamSetting("Fat Channel", "Release", new PlugParamSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto", LinkReversed = true });
            AddParamSetting("Fat Channel", "Auto", new PlugParamSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            AddParamSetting("Fat Channel", "Soft Knee", new PlugParamSettings { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            AddParamSetting("Fat Channel", "Peak Reduction", new PlugParamSettings { Label = "Pk Reductn" });
            AddParamSetting("Fat Channel", "EQ On", new PlugParamSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "EQ ON" });
            AddParamSetting("Fat Channel", "Low On", new PlugParamSettings { OnColor = new FinderColor(241, 84, 220), Label = "LF", ShowUserButtonCircle = true });
            AddParamSetting("Fat Channel", "Low-Mid On", new PlugParamSettings { OnColor = new FinderColor(89, 236, 236), Label = "LMF", ShowUserButtonCircle = true });
            AddParamSetting("Fat Channel", "Hi-Mid On", new PlugParamSettings { OnColor = new FinderColor(241, 178, 84), Label = "HMF", ShowUserButtonCircle = true });
            AddParamSetting("Fat Channel", "High On", new PlugParamSettings { OnColor = new FinderColor(122, 240, 79), Label = "HF", ShowUserButtonCircle = true });
            AddLinked("Fat Channel", "Low Gain", "Low On", label: "LF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Fat Channel", "Low Freq", "Low On", label: "LF Freq");
            AddLinked("Fat Channel", "Low Q", "Low On", label: "LMF Q");
            AddLinked("Fat Channel", "Low-Mid Gain", "Low-Mid On", label: "LMF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Fat Channel", "Low-Mid Freq", "Low-Mid On", label: "LMF Freq");
            AddLinked("Fat Channel", "Low-Mid Q", "Low-Mid On", label: "LMF Q");
            AddLinked("Fat Channel", "Hi-Mid Gain", "Hi-Mid On", label: "HMF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Fat Channel", "Hi-Mid Freq", "Hi-Mid On", label: "HMF Freq");
            AddLinked("Fat Channel", "Hi-Mid Q", "Hi-Mid On", label: "HMF Q");
            AddLinked("Fat Channel", "High Gain", "High On", label: "HF Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("Fat Channel", "High Freq", "High On", label: "HF Freq");
            AddLinked("Fat Channel", "High Q", "High On", label: "HF Q");
            AddParamSetting("Fat Channel", "Low Boost", new PlugParamSettings { OnColor = new FinderColor(241, 84, 220) });
            AddParamSetting("Fat Channel", "Low Atten", new PlugParamSettings { OnColor = new FinderColor(241, 84, 220) });
            AddParamSetting("Fat Channel", "Low Frequency", new PlugParamSettings { Label = "LF Freq", OnColor = new FinderColor(241, 84, 220), DialSteps = 3 });
            AddParamSetting("Fat Channel", "High Boost", new PlugParamSettings { OnColor = new FinderColor(122, 240, 79) });
            AddParamSetting("Fat Channel", "High Atten", new PlugParamSettings { OnColor = new FinderColor(122, 240, 79) });
            AddParamSetting("Fat Channel", "High Bandwidth", new PlugParamSettings { Label = "Bandwidth", OnColor = new FinderColor(122, 240, 79) });
            AddParamSetting("Fat Channel", "Attenuation Select", new PlugParamSettings { Label = "Atten Sel", OnColor = new FinderColor(122, 240, 79), DialSteps = 2 });
            AddParamSetting("Fat Channel", "Limiter On", new PlugParamSettings { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Limiter ON" });

            AddParamSetting("Compressor", "LookAhead", new S1TopControlColors());
            AddParamSetting("Compressor", "Link Channels", new S1TopControlColors(label: "CH Link"));
            AddParamSetting("Compressor", "Attack", new PlugParamSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto Speed", LinkReversed = true });
            AddParamSetting("Compressor", "Release", new PlugParamSettings { OffColor = FinderColor.Transparent, LinkedParameter = "Auto Speed", LinkReversed = true });
            AddParamSetting("Compressor", "Auto Speed", new PlugParamSettings { Label = "Auto" });
            AddParamSetting("Compressor", "Adaptive Speed", new PlugParamSettings { Label = "Adaptive" });
            AddParamSetting("Compressor", "Gain", new PlugParamSettings { Label = "Makeup", OffColor = FinderColor.Transparent, LinkedParameter = "Auto Gain", LinkReversed = true });
            AddParamSetting("Compressor", "Auto Gain", new PlugParamSettings { Label = "Auto" });
            AddParamSetting("Compressor", "Sidechain LC-Freq", new PlugParamSettings { Label = "Side LC", OffColor = FinderColor.Transparent, LinkedParameter = "Sidechain Filter" });
            AddParamSetting("Compressor", "Sidechain HC-Freq", new PlugParamSettings { Label = "Side HC", OffColor = FinderColor.Transparent, LinkedParameter = "Sidechain Filter" });
            AddParamSetting("Compressor", "Sidechain Filter", new PlugParamSettings { Label = "Filter" });
            AddParamSetting("Compressor", "Sidechain Listen", new PlugParamSettings { Label = "Listen" });
            AddParamSetting("Compressor", "Swap Frequencies", new PlugParamSettings { Label = "Swap" });

            AddParamSetting("Limiter", "Mode ", new PlugParamSettings { Label = "A", LabelOn = "B", OnColor = new FinderColor(40, 40, 40), OffColor = new FinderColor(40, 40, 40),
                                                                             TextOnColor = new FinderColor(171, 197, 226), TextOffColor = new FinderColor(171, 197, 226) });
            AddParamSetting("Limiter", "True Peak Limiting", new S1TopControlColors(label: "True Peak"));
            AddLinked("Limiter", "SoftClipper", "True Peak Limiting", label: " Soft Clip", linkReversed: true);
            AddParamSetting("Limiter", "Attack", new PlugParamSettings { DialSteps = 2, HideValueBar = true } );

            AddParamSetting("Flanger", "", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103) });
            AddParamSetting("Flanger", "Feedback", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Flanger", "LFO Sync", new PlugParamSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            AddParamSetting("Flanger", "Depth", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });

            AddParamSetting("Phaser", "", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103) });
            AddParamSetting("Phaser", "Center Frequency", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Label = "Center" });
            AddParamSetting("Phaser", "Sweep Range", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Label = "Range" });
            AddParamSetting("Phaser", "Stereo Spread", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Label = "Spread" });
            AddParamSetting("Phaser", "Depth", new PlugParamSettings { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });
            AddParamSetting("Phaser", "LFO Sync", new PlugParamSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            AddParamSetting("Phaser", "Log. Sweep", new PlugParamSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            AddParamSetting("Phaser", "Soft", new PlugParamSettings { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });

            var analogDelayButtonOnColor = new FinderColor(255, 59, 58);
            var analogDelayButtonOffColor = new FinderColor(84, 18, 18);
            AddParamSetting("Analog Delay", "Delay Beats", new PlugParamSettings { LinkedParameter = "Delay Sync", Label = "TIME", DialSteps = 40, OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(25, 28, 55), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Delay Time", new PlugParamSettings { LinkedParameter = "Delay Sync", LinkReversed = true, Label = "TIME", OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(25, 28, 55), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Delay Sync", new PlugParamSettings { Label = "SYNC", OnColor = analogDelayButtonOnColor, TextOnColor = FinderColor.White, OffColor = analogDelayButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Feedback Level", new PlugParamSettings { Label = "FEEDBACK", OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(46, 50, 84), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Feedback Boost", new PlugParamSettings { Label = "BOOST", OnColor = analogDelayButtonOnColor, TextOnColor = FinderColor.White, OffColor = analogDelayButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "LFO Speed", new PlugParamSettings { LinkedParameter = "LFO Sync", LinkReversed = true, Label = "SPEED", OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White, OffColor = new FinderColor(26, 46, 29), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "LFO Beats", new PlugParamSettings { LinkedParameter = "LFO Sync", Label = "SPEED", OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White, OffColor = new FinderColor(26, 46, 29), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "LFO Width", new PlugParamSettings { Label = "AMOUNT", Mode = PlugParamSettings.PotMode.Symmetric, OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "LFO Sync", new PlugParamSettings { Label = "SYNC", OnColor = analogDelayButtonOnColor, TextOnColor = FinderColor.White, OffColor = analogDelayButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "LFO Waveform", new PlugParamSettings { Label = "", OnColor = new FinderColor(30, 51, 33), UserMenuItems = ["!ad_Triangle", "!ad_Sine", "!ad_Sawtooth", "!ad_Square"] });
            AddParamSetting("Analog Delay", "Low Cut", new PlugParamSettings { Label = "LOW CUT", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "High Cut", new PlugParamSettings { Label = "HI CUT", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "Saturation", new PlugParamSettings { Label = "DRIVE", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "Delay Speed", new PlugParamSettings { Label = "FACTOR", OnColor = new FinderColor(178, 103, 32), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "Delay Inertia", new PlugParamSettings { Label = "INERTIA", OnColor = new FinderColor(178, 103, 32), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "Feedback Width", new PlugParamSettings { Label = "WIDTH", OnColor = new FinderColor(195, 81, 35), TextOnColor = FinderColor.White });
            AddParamSetting("Analog Delay", "Ping-Pong Swap", new PlugParamSettings { Label = "SWAP", OnColor = analogDelayButtonOnColor, TextOnColor = FinderColor.White, OffColor = analogDelayButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Ping-Pong Mode", new PlugParamSettings { Label = "PP", UserMenuItems = ["OFF", "SUM", "2-CH"], OnColor = new FinderColor(35, 23, 17), TextOnColor = FinderColor.White, OffColor = new FinderColor(35, 23, 17), TextOffColor = FinderColor.Black });
            AddParamSetting("Analog Delay", "Mix", new PlugParamSettings { Label = "DRY/WET", OnColor = new FinderColor(213, 68, 68), TextOnColor = FinderColor.White });

            AddParamSetting("Alpine Desk", "Boost", new PlugParamSettings { DialSteps = 2, HideValueBar = true });
            AddParamSetting("Alpine Desk", "Preamp On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            AddParamSetting("Alpine Desk", "Noise On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            AddParamSetting("Alpine Desk", "Noise Gate On", new PlugParamSettings { Label = "Noise Gate", OnColor = new FinderColor(0, 154, 144) });
            AddParamSetting("Alpine Desk", "Crosstalk", new PlugParamSettings { OnColor = new FinderColor(253, 202, 0) });
            AddParamSetting("Alpine Desk", "Crosstalk On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            AddParamSetting("Alpine Desk", "Transformer", new PlugParamSettings { OnColor = new FinderColor(224, 22, 36), DialSteps = 1, HideValueBar = true });
            AddParamSetting("Alpine Desk", "Master", new PlugParamSettings { Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Alpine Desk", "Compensation", new PlugParamSettings { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(0, 154, 144), OffColor = new FinderColor(0, 154, 144), TextOffColor = FinderColor.White });
            AddParamSetting("Alpine Desk", "Character Enhancer", new PlugParamSettings { Label = "Character" });
            AddParamSetting("Alpine Desk", "Economy", new PlugParamSettings { Label = "Eco", OnColor = new FinderColor(0, 154, 144) });

            AddParamSetting("Brit Console", "Boost", new PlugParamSettings { DialSteps = 2, OnColor = new FinderColor(43, 128, 157), HideValueBar = true });
            AddParamSetting("Brit Console", "Drive", new PlugParamSettings { OnColor = new FinderColor(43, 128, 157) });
            AddParamSetting("Brit Console", "Preamp On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            AddParamSetting("Brit Console", "Noise", new PlugParamSettings { OnColor = new FinderColor(43, 128, 157) });
            AddParamSetting("Brit Console", "Noise On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            AddParamSetting("Brit Console", "Noise Gate On", new PlugParamSettings { Label = "Gate", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            AddParamSetting("Brit Console", "Crosstalk", new PlugParamSettings { OnColor = new FinderColor(43, 128, 157) });
            AddParamSetting("Brit Console", "Crosstalk On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            AddParamSetting("Brit Console", "Style", new PlugParamSettings { OnColor = new FinderColor(202, 74, 68), DialSteps = 2, HideValueBar = true });
            AddParamSetting("Brit Console", "Harmonics", new PlugParamSettings { OnColor = new FinderColor(202, 74, 68) });
            AddParamSetting("Brit Console", "Compensation", new PlugParamSettings { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            AddParamSetting("Brit Console", "Character Enhancer", new PlugParamSettings { Label = "Character", OnColor = new FinderColor(43, 128, 157) });
            AddParamSetting("Brit Console", "Master", new PlugParamSettings { OnColor = new FinderColor(43, 128, 157), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Brit Console", "Economy", new PlugParamSettings { Label = "Eco", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });

            AddParamSetting("CTC-1", "Boost", new PlugParamSettings { OnColor = new FinderColor(244, 104, 26) });
            AddParamSetting("CTC-1", "Preamp On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            AddParamSetting("CTC-1", "Noise", new PlugParamSettings { DialSteps = 4 });
            AddParamSetting("CTC-1", "Noise On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            AddParamSetting("CTC-1", "Noise Gate On", new PlugParamSettings { Label = "Gate", OnColor = new FinderColor(244, 104, 26) });
            AddParamSetting("CTC-1", "Preamp Type", new PlugParamSettings { Label = "Type", DialSteps = 2, HideValueBar = true });
            AddParamSetting("CTC-1", "Crosstalk On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            AddParamSetting("CTC-1", "Compensation", new PlugParamSettings { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(69, 125, 159), OffColor = new FinderColor(69, 125, 159), TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            AddParamSetting("CTC-1", "Character Enhancer", new PlugParamSettings { Label = "Character" });
            AddParamSetting("CTC-1", "Master", new PlugParamSettings { Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CTC-1", "Economy", new PlugParamSettings { Label = "Eco", OnColor = new FinderColor(69, 125, 159) });

            AddParamSetting("Porta Casstte", "Boost", new PlugParamSettings { OnColor = new FinderColor(251, 0, 3) });
            AddParamSetting("Porta Cassette", "Drive", new PlugParamSettings { OnColor = new FinderColor(226, 226, 226) });
            AddParamSetting("Porta Cassette", "Preamp On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            AddParamSetting("Porta Cassette", "Noise", new PlugParamSettings { OnColor = new FinderColor(226, 226, 226) });
            AddParamSetting("Porta Cassette", "Noise On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            AddParamSetting("Porta Cassette", "Noise Gate On", new PlugParamSettings { Label = "Gate", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            AddParamSetting("Porta Cassette", "Crosstalk", new PlugParamSettings { OnColor = new FinderColor(226, 226, 226) });
            AddParamSetting("Porta Cassette", "Crosstalk On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            AddParamSetting("Porta Cassette", "Pitch", new PlugParamSettings { OnColor = new FinderColor(144, 153, 153), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Porta Cassette", "Compensation", new PlugParamSettings { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            AddParamSetting("Porta Cassette", "Character Enhancer", new PlugParamSettings { Label = "Character", OnColor = new FinderColor(226, 226, 226) });
            AddParamSetting("Porta Cassette", "Master", new PlugParamSettings { OnColor = new FinderColor(226, 226, 226), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Porta Cassette", "Economy", new PlugParamSettings { Label = "Eco" });

            AddParamSetting("Console Shaper", "Preamp On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            AddParamSetting("Console Shaper", "Noise", new PlugParamSettings { DialSteps = 4 });
            AddParamSetting("Console Shaper", "Noise On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            AddParamSetting("Console Shaper", "Crosstalk On", new PlugParamSettings { Label = "ON", OnColor = new FinderColor(114, 167, 204) });

            // brainworx

            var bxSslOnTransparency = 180;
            var bxSslLedRedColor = new FinderColor(255, 48, 24);
            var bxSslLedGreenColor = new FinderColor(95, 255, 48);
            var bxSslLedOffColor = new FinderColor(50, 50, 50);
            var bxSslWhite = new FinderColor(206, 206, 206);
            var bxSslRed = new FinderColor(184, 59, 55);
            var bxSslGreen = new FinderColor(73, 109, 70);
            var bxSslBlue = new FinderColor(70, 121, 162);
            var bxSslYellow = new FinderColor(255, 240, 29);
            var bxSslBlack = new FinderColor(53, 56, 56);
            var bxSslBrown = new FinderColor(110, 81, 69);
            var bxSslButtonColor = new FinderColor(198, 200, 195);
            var bxSslButtonOffColor = new FinderColor(138, 140, 138);
            AddParamSetting("bx_console SSL 4000 E", "Loupedeck User Pages", new PlugParamSettings { UserMenuItems = ["EQ 1", "EQ 2", "DYN", "DN/MX"  ] });
            AddParamSetting("bx_console SSL 4000 E", "EQ On/Off", new PlugParamSettings { Label = "EQ", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "Dyn On/Off", new PlugParamSettings { Label = "DYN", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "HPF Frequency", new PlugParamSettings { Label = "HP Frq", LinkedParameter = "HPF On/Off", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "HPF On/Off", new PlugParamSettings { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "LPF Frequency", new PlugParamSettings { Label = "LP Frq", LinkedParameter = "LPF On/Off", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LPF On/Off", new PlugParamSettings { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "FLT Position", new PlugParamSettings { Label = "DYN SC", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Gain", new PlugParamSettings { Label = "HF Gain", OnColor = bxSslRed, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Frequency", new PlugParamSettings { Label = "HF Freq", OnColor = bxSslRed, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Bell", new PlugParamSettings { Label = "SHELF", LabelOn = "BELL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Gain", new PlugParamSettings { Label = "LF Gain", LinkedParameter = "EQ Type", OnColor = bxSslBrown, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Frequency", new PlugParamSettings { Label = "LF Freq", LinkedParameter = "EQ Type", OnColor = bxSslBrown, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Bell", new PlugParamSettings { Label = "SHELF", LabelOn = "BELL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "EQ Type", new PlugParamSettings { Label = "EQ TYPE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Mid Gain", new PlugParamSettings { Label = "HMF Gain", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Mid Frequency", new PlugParamSettings { Label = "HMF Freq", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "EQ High Mid Q", new PlugParamSettings { Label = "HMF Q", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Mid Gain", new PlugParamSettings { Label = "LMF Gain", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Mid Frequency", new PlugParamSettings { Label = "LMF Freq", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 E", "EQ Low Mid Q", new PlugParamSettings { Label = "LMF Q", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 E", "LC Ratio", new PlugParamSettings { Label = "LC RATIO", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LC Threshold", new PlugParamSettings { Label = "LC THRES", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LC Release", new PlugParamSettings { Label = "LC REL", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LC Attack", new PlugParamSettings { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "LC Link", new PlugParamSettings { Label = "LINK", OnColor = bxSslYellow, TextOnColor = FinderColor.Black, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "GE Range", new PlugParamSettings { Label = "GE RANGE", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "GE Threshold", new PlugParamSettings { Label = "GE THRES", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "GE Release", new PlugParamSettings { Label = "GE REL", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 E", "GE Attack", new PlugParamSettings { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "GE Mode", new PlugParamSettings { Label = "EXP", LabelOn = "GATE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "GE Invert", new PlugParamSettings { Label = "NORM", LabelOn = "INV", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "GE Threshold Range", new PlugParamSettings { Label = "-30 dB", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "LC Highpass", new PlugParamSettings { Label = "LC HPF", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LC 2nd Thresh Level", new PlugParamSettings { Label = "LC REL2", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "LC Mix", new PlugParamSettings { Label = "LC MIX", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 E", "In Gain", new PlugParamSettings { Label = "IN GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "Virtual Gain", new PlugParamSettings { Label = "V GAIN", OnColor = bxSslRed, OnTransparency = 180 });
            AddParamSetting("bx_console SSL 4000 E", "Out Gain", new PlugParamSettings { Label = "OUT GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "Phase", new PlugParamSettings { Label = "PHASE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = new FinderColor(100, 100, 100), TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "Stereo Mode", new PlugParamSettings { Label = "ANALOG", LabelOn = "DIGITAL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslYellow, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 E", "EQ Position", new PlugParamSettings { Label = "", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, UserMenuItems = ["PRE DYN", "DYN SC", "POST DYN"] });
            AddParamSetting("bx_console SSL 4000 E", "Dyn Key", new PlugParamSettings { Label = "D 2 KEY", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });

            var bxSslCyan = new FinderColor(54, 146, 124);
            var bxSslMagenta = new FinderColor(197, 80, 148);
            var bxSslOrange = new FinderColor(218, 109, 44);
            AddParamSetting("bx_console SSL 4000 G", "Loupedeck User Pages", new PlugParamSettings { UserMenuItems = ["EQ 1", "EQ 2", "DYN", "DN/MX"] });
            AddParamSetting("bx_console SSL 4000 G", "EQ On/Off", new PlugParamSettings { Label = "EQ", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "Dyn On/Off", new PlugParamSettings { Label = "DYN", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "HPF Frequency", new PlugParamSettings { Label = "HP Frq", LinkedParameter = "HPF On/Off", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "HPF On/Off", new PlugParamSettings { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "LPF Frequency", new PlugParamSettings { Label = "LP Frq", LinkedParameter = "LPF On/Off", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LPF On/Off", new PlugParamSettings { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "FLT Position", new PlugParamSettings { Label = "DYN SC", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Gain", new PlugParamSettings { Label = "HF Gain", LinkedParameter = "EQ Type", OnColor = bxSslMagenta, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslRed, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Frequency", new PlugParamSettings { Label = "HF Freq", LinkedParameter = "EQ Type", OnColor = bxSslMagenta, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslRed, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Bell", new PlugParamSettings { Label = "SHELF", LabelOn = "BELL", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Gain", new PlugParamSettings { Label = "LF Gain", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslOrange, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Frequency", new PlugParamSettings { Label = "LF Freq", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslOrange, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Bell", new PlugParamSettings { Label = "SHELF", LabelOn = "BELL", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ Type", new PlugParamSettings { Label = "EQ TYPE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Mid Gain", new PlugParamSettings { Label = "HMF Gain", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Mid Frequency", new PlugParamSettings { Label = "HMF Freq", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Mid Q", new PlugParamSettings { Label = "HMF Q", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "EQ High Mid x3", new PlugParamSettings { Label = "x3", LinkedParameter = "EQ Type", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Mid Gain", new PlugParamSettings { Label = "LMF Gain", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Mid Frequency", new PlugParamSettings { Label = "LMF Freq", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Mid Q", new PlugParamSettings { Label = "LMF Q", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
            AddParamSetting("bx_console SSL 4000 G", "EQ Low Mid /3", new PlugParamSettings { Label = "/3", LinkedParameter = "EQ Type", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "LC Ratio", new PlugParamSettings { Label = "LC RATIO", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LC Threshold", new PlugParamSettings { Label = "LC THRES", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LC Release", new PlugParamSettings { Label = "LC REL", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LC Attack", new PlugParamSettings { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "LC Link", new PlugParamSettings { Label = "LINK", OnColor = bxSslYellow, TextOnColor = FinderColor.Black, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "GE Range", new PlugParamSettings { Label = "GE RANGE", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "GE Threshold", new PlugParamSettings { Label = "GE THRES", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "GE Release", new PlugParamSettings { Label = "GE REL", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
            AddParamSetting("bx_console SSL 4000 G", "GE Attack", new PlugParamSettings { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "GE Mode", new PlugParamSettings { Label = "EXP", LabelOn = "GATE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "GE Invert", new PlugParamSettings { Label = "NORM", LabelOn = "INV", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "GE Threshold Range", new PlugParamSettings { Label = "-30 dB", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "LC Highpass", new PlugParamSettings { Label = "LC HPF", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LC 2nd Thresh Level", new PlugParamSettings { Label = "LC REL2", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "LC Mix", new PlugParamSettings { Label = "LC MIX", OnColor = bxSslWhite });
            AddParamSetting("bx_console SSL 4000 G", "In Gain", new PlugParamSettings { Label = "IN GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "Virtual Gain", new PlugParamSettings { Label = "V GAIN", OnColor = bxSslMagenta, OnTransparency = 180 });
            AddParamSetting("bx_console SSL 4000 G", "Out Gain", new PlugParamSettings { Label = "OUT GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "Phase", new PlugParamSettings { Label = "PHASE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = new FinderColor(100, 100, 100), TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "Stereo Mode", new PlugParamSettings { Label = "ANALOG", LabelOn = "DIGITAL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslYellow, TextOffColor = FinderColor.Black });
            AddParamSetting("bx_console SSL 4000 G", "EQ Position", new PlugParamSettings { Label = "", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, UserMenuItems = ["PRE DYN", "DYN SC", "POST DYN"] });
            AddParamSetting("bx_console SSL 4000 G", "Dyn Key", new PlugParamSettings { Label = "D 2 KEY", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });


            // Waves

            AddParamSetting("SSLGChannel", "HP Frq", new PlugParamSettings { OnColor = new FinderColor(220, 216, 207) });
            AddParamSetting("SSLGChannel", "LP Frq", new PlugParamSettings { OnColor = new FinderColor(220, 216, 207) });
            AddParamSetting("SSLGChannel", "FilterSplit", new PlugParamSettings { OnColor = new FinderColor(204, 191, 46), Label = "SPLIT" });
            AddParamSetting("SSLGChannel", "HF Gain", new PlugParamSettings { OnColor = new FinderColor(177, 53, 63), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "HF Frq", new PlugParamSettings { OnColor = new FinderColor(177, 53, 63) });
            AddParamSetting("SSLGChannel", "HMF X3", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Label = "HMFx3" });
            AddParamSetting("SSLGChannel", "LF Gain", new PlugParamSettings { OnColor = new FinderColor(180, 180, 180), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "LF Frq", new PlugParamSettings { OnColor = new FinderColor(180, 180, 180) });
            AddParamSetting("SSLGChannel", "LMF div3", new PlugParamSettings { OnColor = new FinderColor(22, 97, 120), Label = "LMF/3" });
            AddParamSetting("SSLGChannel", "HMF Gain", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "HMF Frq", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64) });
            AddParamSetting("SSLGChannel", "HMF Q", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "LMF Gain", new PlugParamSettings { OnColor = new FinderColor(22, 97, 120), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "LMF Frq", new PlugParamSettings { OnColor = new FinderColor(22, 97, 120) });
            AddParamSetting("SSLGChannel", "LMF Q", new PlugParamSettings { OnColor = new FinderColor(22, 97, 120), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("SSLGChannel", "EQBypass", new PlugParamSettings { OnColor = new FinderColor(226, 61, 80), Label = "EQ BYP" });
            AddParamSetting("SSLGChannel", "EQDynamic", new PlugParamSettings { OnColor = new FinderColor(241, 171, 53), Label = "FLT DYN SC" });
            AddParamSetting("SSLGChannel", "CompRatio", new PlugParamSettings { OnColor = new FinderColor(220, 216, 207), Label = "C RATIO" });
            AddParamSetting("SSLGChannel", "CompThresh", new PlugParamSettings { OnColor = new FinderColor(220, 216, 207), Label = "C THRESH" });
            AddParamSetting("SSLGChannel", "CompRelease", new PlugParamSettings { OnColor = new FinderColor(220, 216, 207), Label = "C RELEASE" });
            AddParamSetting("SSLGChannel", "CompFast", new PlugParamSettings { Label = "F.ATK" });
            AddParamSetting("SSLGChannel", "ExpRange", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Label = "E RANGE" });
            AddParamSetting("SSLGChannel", "ExpThresh", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Label = "E THRESH" });
            AddParamSetting("SSLGChannel", "ExpRelease", new PlugParamSettings { OnColor = new FinderColor(27, 92, 64), Label = "E RELEASE" });
            AddParamSetting("SSLGChannel", "ExpAttack", new PlugParamSettings { Label = "F.ATK" });
            AddParamSetting("SSLGChannel", "ExpGate", new PlugParamSettings { Label = "GATE" });
            AddParamSetting("SSLGChannel", "DynamicBypass", new PlugParamSettings { OnColor = new FinderColor(226, 61, 80), Label = "DYN BYP" });
            AddParamSetting("SSLGChannel", "DynaminCHOut", new PlugParamSettings { OnColor = new FinderColor(241, 171, 53), Label = "DYN CH OUT" });
            AddParamSetting("SSLGChannel", "VUInOut", new PlugParamSettings { OnColor = new FinderColor(241, 171, 53), Label = "VU OUT" });

            AddParamSetting("RCompressor", "Threshold", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("RCompressor", "Ratio", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("RCompressor", "Attack", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("RCompressor", "Release", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("RCompressor", "Gain", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("RCompressor", "Trim", new PlugParamSettings { Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("RCompressor", "ARC / Manual", new PlugParamSettings { Label = "ARC", LabelOn = "Manual", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            AddParamSetting("RCompressor", "Electro / Opto", new PlugParamSettings { Label = "Electro", LabelOn = "Opto", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            AddParamSetting("RCompressor", "Warm / Smooth", new PlugParamSettings { Label = "Warm", LabelOn = "Smooth", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });

            AddParamSetting("RBass", "Orig. In-Out", new PlugParamSettings { Label = "ORIG IN", OffColor = new FinderColor(230, 230, 230), TextOnColor = FinderColor.Black  });
            AddParamSetting("RBass", "Intensity", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("RBass", "Frequency", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("RBass", "Out Gain", new PlugParamSettings { Label = "Gain", OnColor = new FinderColor(243, 132, 1) });

            AddParamSetting("REQ", "Band1 On/Off", new PlugParamSettings { Label = "Band 1", OnColor = new FinderColor(196, 116, 100), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band1 Gain", "Band1 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band1 Frq", "Band1 On/Off", label: "Freq");
            AddLinked("REQ", "Band1 Q", "Band1 On/Off", label: "Q");
            AddLinked("REQ", "Band1 Type", "Band1 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf", "!Hi-Pass", "!Low-RShelv"]);
            AddParamSetting("REQ", "Band2 On/Off", new PlugParamSettings { Label = "Band 2", OnColor = new FinderColor(175, 173, 29), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band2 Gain", "Band2 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band2 Frq", "Band2 On/Off", label: "Freq");
            AddLinked("REQ", "Band2 Q", "Band2 On/Off", label: "Q");
            AddLinked("REQ", "Band2 Type", "Band2 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf"]);
            AddParamSetting("REQ", "Band3 On/Off", new PlugParamSettings { Label = "Band 3", OnColor = new FinderColor(57, 181, 74), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band3 Gain", "Band3 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band3 Frq", "Band3 On/Off", label: "Freq");
            AddLinked("REQ", "Band3 Q", "Band3 On/Off", label: "Q");
            AddLinked("REQ", "Band3 Type", "Band3 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf"]);
            AddParamSetting("REQ", "Band4 On/Off", new PlugParamSettings { Label = "Band 4", OnColor = new FinderColor(56, 149, 203), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band4 Gain", "Band4 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band4 Frq", "Band4 On/Off", label: "Freq");
            AddLinked("REQ", "Band4 Q", "Band4 On/Off", label: "Q");
            AddLinked("REQ", "Band4 Type", "Band4 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf"]);
            AddParamSetting("REQ", "Band5 On/Off", new PlugParamSettings { Label = "Band 5", OnColor = new FinderColor(130, 41, 141), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band5 Gain", "Band5 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band5 Frq", "Band5 On/Off", label: "Freq");
            AddLinked("REQ", "Band5 Q", "Band5 On/Off", label: "Q");
            AddLinked("REQ", "Band5 Type", "Band5 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf"]);
            AddParamSetting("REQ", "Band6 On/Off", new PlugParamSettings { Label = "Band 6", OnColor = new FinderColor(199, 48, 105), TextOnColor = FinderColor.Black });
            AddLinked("REQ", "Band6 Gain", "Band6 On/Off", label: "Gain", mode: PlugParamSettings.PotMode.Symmetric);
            AddLinked("REQ", "Band6 Frq", "Band6 On/Off", label: "Freq");
            AddLinked("REQ", "Band6 Q", "Band6 On/Off", label: "Q");
            AddLinked("REQ", "Band6 Type", "Band6 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf", "!Low-Pass", "!Hi-RShelv"]);
            AddParamSetting("REQ", "Fader left Out", new PlugParamSettings { Label = "Output", OnColor = new FinderColor(242, 101, 34) });
            AddParamSetting("REQ", "Gain-L (link)", new PlugParamSettings { Label = "Out L", OnColor = new FinderColor(242, 101, 34) });
            AddParamSetting("REQ", "Gain-R", new PlugParamSettings { Label = "Out R", OnColor = new FinderColor(242, 101, 34) });

            AddParamSetting("RVerb", "", new PlugParamSettings { OnColor = new FinderColor(244, 134, 2), TextOnColor = FinderColor.Black });
            AddParamSetting("RVerb", "Dmp Low-F Ratio", new PlugParamSettings { Label = "Dmp Lo Rto", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "Dmp Low-F Freq", new PlugParamSettings { Label = "Dmp Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "Dmp Hi-F Ratio", new PlugParamSettings { Label = "Dmp Hi Rto", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "Dmp Hi-F Freq", new PlugParamSettings { Label = "Dmp Hi Frq", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "EQ Low-F Gain", new PlugParamSettings { Label = "EQ Lo Gn", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "EQ Low-F Freq", new PlugParamSettings { Label = "EQ Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "EQ Hi-F Gain", new PlugParamSettings { Label = "EQ Hi Gn", OnColor = new FinderColor(74, 149, 155) });
            AddParamSetting("RVerb", "EQ Hi-F Freq", new PlugParamSettings { Label = "EQ Hi Frq", OnColor = new FinderColor(74, 149, 155) });


            AddParamSetting("L1 limiter", "Threshold", new PlugParamSettings { OnColor = new FinderColor(243, 132, 1) });
            AddParamSetting("L1 limiter", "Ceiling", new PlugParamSettings { OnColor = new FinderColor(255, 172, 66) });
            AddParamSetting("L1 limiter", "Release", new PlugParamSettings { OnColor = new FinderColor(54, 206, 206) });
            AddParamSetting("L1 limiter", "Auto Release", new PlugParamSettings { Label = "AUTO", OnColor = new FinderColor(54, 206, 206) });

            AddParamSetting("PuigTec EQP1A", "OnOff", new PlugParamSettings { Label = "IN", OnColor = new FinderColor(203, 53, 53) });
            AddParamSetting("PuigTec EQP1A", "LowBoost", new PlugParamSettings { Label = "Low Boost", OnColor = new FinderColor(96, 116, 115) });
            AddParamSetting("PuigTec EQP1A", "LowAtten", new PlugParamSettings { Label = "Low Atten", OnColor = new FinderColor(96, 116, 115) });
            AddParamSetting("PuigTec EQP1A", "HiBoost", new PlugParamSettings { Label = "High Boost", OnColor = new FinderColor(96, 116, 115) });
            AddParamSetting("PuigTec EQP1A", "HiAtten", new PlugParamSettings { Label = "High Atten", OnColor = new FinderColor(96, 116, 115) });
            AddParamSetting("PuigTec EQP1A", "LowFrequency", new PlugParamSettings { Label = "Low Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 3 });
            AddParamSetting("PuigTec EQP1A", "HiFrequency", new PlugParamSettings { Label = "High Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 6 });
            AddParamSetting("PuigTec EQP1A", "Bandwidth", new PlugParamSettings { Label = "Bandwidth", OnColor = new FinderColor(96, 116, 115) });
            AddParamSetting("PuigTec EQP1A", "AttenSelect", new PlugParamSettings { Label = "Atten Sel", OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            AddParamSetting("PuigTec EQP1A", "Mains", new PlugParamSettings { OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            AddParamSetting("PuigTec EQP1A", "Gain", new PlugParamSettings { OnColor = new FinderColor(96, 116, 115), Mode = PlugParamSettings.PotMode.Symmetric });

            AddParamSetting("Smack Attack", "Attack", new PlugParamSettings { OnColor = new FinderColor(9, 217, 179), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Smack Attack", "AttackSensitivity", new PlugParamSettings { Label = "Sensitivity", OnColor = new FinderColor(9, 217, 179) });
            AddParamSetting("Smack Attack", "AttackDuration", new PlugParamSettings { Label = "Duration", OnColor = new FinderColor(9, 217, 179) });
            AddParamSetting("Smack Attack", "AttackShape", new PlugParamSettings { Label = "", OnColor = new FinderColor(30, 30, 30), UserMenuItems = ["!sm_Needle", "!sm_Nail", "!sm_BluntA"], DialSteps = 2, HideValueBar = true });
            AddParamSetting("Smack Attack", "Sustain", new PlugParamSettings { OnColor = new FinderColor(230, 172, 5), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("Smack Attack", "SustainSensitivity", new PlugParamSettings { Label = "Sensitivity", OnColor = new FinderColor(230, 172, 5) });
            AddParamSetting("Smack Attack", "SustainDuration", new PlugParamSettings { Label = "Duration", OnColor = new FinderColor(230, 172, 5) });
            AddParamSetting("Smack Attack", "SustainShape", new PlugParamSettings { Label = "", OnColor = new FinderColor(30, 30, 30), UserMenuItems = ["!sm_Linear", "!sm_Nonlinear", "!sm_BluntS"], DialSteps = 2, HideValueBar = true });
            AddParamSetting("Smack Attack", "Guard", new PlugParamSettings { TextOnColor = new FinderColor(0, 198, 250), UserMenuItems = ["Off", "Clip", "Limit"], DialSteps = 2, HideValueBar = true });
            AddParamSetting("Smack Attack", "Mix", new PlugParamSettings { OnColor = new FinderColor(0, 198, 250) });
            AddParamSetting("Smack Attack", "Output", new PlugParamSettings { OnColor = new FinderColor(0, 198, 250), Mode = PlugParamSettings.PotMode.Symmetric });

            AddParamSetting("Brauer Motion", "Loupedeck User Pages", new PlugParamSettings { UserMenuItems = ["MAIN", "PNR 1", "PNR 2", "T/D 1", "T/D 2", "MIX"] });
            var path1Color = new FinderColor(139, 195, 74);
            var path2Color = new FinderColor(230, 74, 25);
            var bgColor = new FinderColor(12, 80, 124);
            var buttonBgColor = new FinderColor(3, 18, 31);
            var textColor = new FinderColor(105, 133, 157);
            var checkOnColor = new FinderColor(7, 152, 202);
            AddParamSetting("Brauer Motion", "Mute 1", new PlugParamSettings { Label = "MUTE 1", OnColor = path1Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path1Color });
            AddParamSetting("Brauer Motion", "Mute 2", new PlugParamSettings { Label = "MUTE 2", OnColor = path2Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path2Color });
            AddParamSetting("Brauer Motion", "Path 1 A Marker", new PlugParamSettings { Label = "A", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Path 1 B Marker", new PlugParamSettings { Label = "B", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Path 1 Start Marker", new PlugParamSettings { Label = "START", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Path 2 A Marker", new PlugParamSettings { Label = "A", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Path 2 B Marker", new PlugParamSettings { Label = "B", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Path 2 Start Marker", new PlugParamSettings { Label = "START", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Panner 1 Mode", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = path1Color, UserMenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
            AddParamSetting("Brauer Motion", "Panner 2 Mode", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = path2Color, UserMenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
            AddParamSetting("Brauer Motion", "Link", new PlugParamSettings { Label = "LINK", OnColor = buttonBgColor, TextOnColor = new FinderColor(0, 192, 255), OffColor = buttonBgColor, TextOffColor = new FinderColor(60, 60, 60) });
            AddParamSetting("Brauer Motion", "Path 1", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
            AddParamSetting("Brauer Motion", "Modulator 1", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
            AddParamSetting("Brauer Motion", "Reverse 1", new PlugParamSettings { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Mod Delay On/Off 1", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Speed 1", new PlugParamSettings { Label = "SPEED 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Offset 1", new PlugParamSettings { Label = "OFFSET 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Depth 1", new PlugParamSettings { Label = "DEPTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Width 1", new PlugParamSettings { Label = "WIDTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Pre Delay 1", new PlugParamSettings { Label = "PRE DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Mod Delay 1", new PlugParamSettings { Label = "MOD DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Path 2", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
            AddParamSetting("Brauer Motion", "Modulator 2", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
            AddParamSetting("Brauer Motion", "Reverse 2", new PlugParamSettings { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Mod Delay On/Off 2", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Speed 2", new PlugParamSettings { Label = "SPEED 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Offset 2", new PlugParamSettings { Label = "OFFSET 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Depth 2", new PlugParamSettings { Label = "DEPTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Width 2", new PlugParamSettings { Label = "WIDTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Pre Delay 2", new PlugParamSettings { Label = "PRE DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Mod Delay 2", new PlugParamSettings { Label = "MOD DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Trigger Mode 1", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
            AddParamSetting("Brauer Motion", "Trigger A to B 1", new PlugParamSettings { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
            AddParamSetting("Brauer Motion", "Trigger Sensitivity 1", new PlugParamSettings { Label = "SENS 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Trigger HP 1", new PlugParamSettings { Label = "HOLD 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Dynamics 1", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
            AddParamSetting("Brauer Motion", "Drive 1", new PlugParamSettings { Label = "DRIVE 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Ratio 1", new PlugParamSettings { Label = "RATIO 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Dynamics HP 1", new PlugParamSettings { Label = "HP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Dynamics LP 1", new PlugParamSettings { Label = "LP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Trigger Mode 2", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
            AddParamSetting("Brauer Motion", "Trigger A to B 2", new PlugParamSettings { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
            AddParamSetting("Brauer Motion", "Trigger Sensitivity 2", new PlugParamSettings { Label = "SENS 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Trigger HP 2", new PlugParamSettings { Label = "HOLD 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Dynamics 2", new PlugParamSettings { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
            AddParamSetting("Brauer Motion", "Drive 2", new PlugParamSettings { Label = "DRIVE 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Ratio 2", new PlugParamSettings { Label = "RATIO 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Dynamics HP 2", new PlugParamSettings { Label = "HP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Dynamics LP 2", new PlugParamSettings { Label = "LP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Panner 1 Level", new PlugParamSettings { Label = "PANNER 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
            AddParamSetting("Brauer Motion", "Panner 2 Level", new PlugParamSettings { Label = "PANNER 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
            AddParamSetting("Brauer Motion", "Input", new PlugParamSettings { Label = "INPUT", OnColor = bgColor, TextOnColor = textColor });
            AddParamSetting("Brauer Motion", "Output", new PlugParamSettings { Label = "OUTPUT", OnColor = bgColor, TextOnColor = textColor });
            AddParamSetting("Brauer Motion", "Mix", new PlugParamSettings { Label = "MIX", OnColor = bgColor, TextOnColor = textColor });
            AddParamSetting("Brauer Motion", "Start/Stop 1", new PlugParamSettings { Label = "START 1", LabelOn = "STOP 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Start/Stop 2", new PlugParamSettings { Label = "START 2", LabelOn = "STOP 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Ex-SC 1", new PlugParamSettings { Label = "EXT SC 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Ex-SC 2", new PlugParamSettings { Label = "EXT SC 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
            AddParamSetting("Brauer Motion", "Auto Reset", new PlugParamSettings { Label = "AUTO RESET", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });

            var mixColor = new FinderColor(254, 251, 248);
            var mainCtrlColor = new FinderColor(52, 139, 125);
            var typeColor = new FinderColor(90, 92, 88);
            var delayButtonColor = new FinderColor(38, 39, 37);
            var optionsOffBgColor = new FinderColor(100, 99, 95);
            AddParamSetting("Abbey Road Chambers", "Input Level", new PlugParamSettings { Label = "INPUT", OnColor = mixColor });
            AddParamSetting("Abbey Road Chambers", "Output", new PlugParamSettings { Label = "OUTPUT", OnColor = mixColor });
            AddParamSetting("Abbey Road Chambers", "Reverb Mix", new PlugParamSettings { Label = "REVERB", OnColor = mixColor });
            AddParamSetting("Abbey Road Chambers", "Dry/Wet", new PlugParamSettings { Label = "DRY/WET", OnColor = mixColor });
            AddParamSetting("Abbey Road Chambers", "Reverb Time X", new PlugParamSettings { Label = "TIME X", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "RS106 Top Cut", new PlugParamSettings { Label = "TOP CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "RS106 Bass Cut", new PlugParamSettings { Label = "BASS CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "RS127 Gain", new PlugParamSettings { Label = "GAIN", Mode = PlugParamSettings.PotMode.Symmetric, OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "RS127 Freq", new PlugParamSettings { Label = "FREQ", OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White, DialSteps = 2 });
            AddParamSetting("Abbey Road Chambers", "Reverb Type", new PlugParamSettings { Label = "", OnColor = mixColor, TextOnColor = FinderColor.Black, UserMenuItems = ["CHMBR 2", "MIRROR", "STONE"] });
            AddLinked("Abbey Road Chambers", "Mic", "Reverb Type", label: "M", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["KM53", "MK2H"]);
            AddParamSetting("Abbey Road Chambers", "Mic Position", new PlugParamSettings { Label = "P", OnColor = typeColor, TextOnColor = FinderColor.White, UserMenuItems = ["WALL", "CLASSIC", "ROOM"] });
            AddLinked("Abbey Road Chambers", "Speaker", "Reverb Type", label: "S", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["ALTEC", "B&W"]);
            AddLinked("Abbey Road Chambers", "Speaker Facing", "Reverb Type", label: "F", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["ROOM", "WALL"]);
            AddParamSetting("Abbey Road Chambers", "Filters To Chamber On/Off", new PlugParamSettings { Label = "FILTERS", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "ARChambers On/Off", new PlugParamSettings { Label = "STEED", OnColor = optionsOffBgColor, TextOnColor = FinderColor.White, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "Feedback", new PlugParamSettings { Label = "FEEDBACK", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Top Cut FB", new PlugParamSettings { Label = "TOP CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Mid FB", new PlugParamSettings { Label = "MID", Mode = PlugParamSettings.PotMode.Symmetric, OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Bass Cut FB", new PlugParamSettings { Label = "BASS CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Drive On/Off", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "Drive", new PlugParamSettings { Label = "DRIVE", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Delay Mod", new PlugParamSettings { Label = "MOD", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Delay Time", new PlugParamSettings { Label = "DELAY L", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Delay Time R", new PlugParamSettings { Label = "DELAY R", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
            AddParamSetting("Abbey Road Chambers", "Delay Link", new PlugParamSettings { Label = "LINK", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });
            AddParamSetting("Abbey Road Chambers", "Delay Sync On/Off", new PlugParamSettings { Label = "SYNC", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });

            var hybridLineColor = new FinderColor(220, 148, 49);
            var hybridButtonOnColor = new FinderColor(142, 137, 116);
            var hybridButtonOffColor = new FinderColor(215, 209, 186);
            var hybridButtonTextOnColor = new FinderColor(247, 230, 25);
            var hybridButtonTextOffColor = FinderColor.Black;
            AddParamSetting("H-Delay", "Sync", new PlugParamSettings { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["BPM", "HOST", "MS"] });
            AddParamSetting("H-Delay", "Delay BPM", new PlugParamSettings { Label = "DELAY", LinkedParameter = "Sync", LinkedStates = "0,1", DialSteps = 19, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Delay Sec", new PlugParamSettings { Label = "DELAY", LinkedParameter = "Sync", LinkedStates = "2", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Feedback", new PlugParamSettings { Label = "FEEDBACK", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Mix", new PlugParamSettings { Label = "DRY/WET", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Output", new PlugParamSettings { Label = "OUTPUT", Mode = PlugParamSettings.PotMode.Symmetric, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Analog", new PlugParamSettings { Label = "ANALOG", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black, DialSteps = 4 });
            AddParamSetting("H-Delay", "Phase L", new PlugParamSettings { Label = "PHASE L", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            AddParamSetting("H-Delay", "Phase R", new PlugParamSettings { Label = "PHASE R", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            AddParamSetting("H-Delay", "PingPong", new PlugParamSettings { Label = "PINGPONG", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            AddParamSetting("H-Delay", "LoFi", new PlugParamSettings { Label = "LoFi", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            AddParamSetting("H-Delay", "Depth", new PlugParamSettings { Label = "DEPTH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "Rate", new PlugParamSettings { Label = "RATE", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "HiPass", new PlugParamSettings { Label = "HIPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Delay", "LoPass", new PlugParamSettings { Label = "LOPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });

            AddParamSetting("H-Comp", "Threshold", new PlugParamSettings { Label = "THRESH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Meter Select", new PlugParamSettings { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["IN", "GR", "OUT"] });
            AddParamSetting("H-Comp", "Punch", new PlugParamSettings { Label = "PUNCH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Ratio", new PlugParamSettings { Label = "RATIO", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Attack", new PlugParamSettings { Label = "ATTACK", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Limiter", new PlugParamSettings { Label = "LIMITER", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
            AddParamSetting("H-Comp", "Sync", new PlugParamSettings { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["BPM", "HOST", "MS"] });
            AddParamSetting("H-Comp", "Release", new PlugParamSettings { LinkedParameter = "Sync", Label = "RELEASE", LinkedStates = "2", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "ReleaseBPM", new PlugParamSettings { LinkedParameter = "Sync", Label = "RELEASE", LinkedStates = "0,1", DialSteps = 19, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Mix", new PlugParamSettings { Label = "DRY/WET", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Output", new PlugParamSettings { Label = "OUTPUT", Mode = PlugParamSettings.PotMode.Symmetric, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
            AddParamSetting("H-Comp", "Analog", new PlugParamSettings { Label = "ANALOG", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black, DialSteps = 4 });


            AddParamSetting("Sibilance", "Monitor", new PlugParamSettings { OnColor = new FinderColor(0, 195, 230) });
            AddParamSetting("Sibilance", "Lookahead", new PlugParamSettings { OnColor = new FinderColor(0, 195, 230) });

            AddParamSetting("MondoMod", "", new PlugParamSettings { OnColor = new FinderColor(102, 255, 51) });
            AddParamSetting("MondoMod", "AM On/Off", new PlugParamSettings { Label = "AM", LabelOn = "AM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            AddParamSetting("MondoMod", "FM On/Off", new PlugParamSettings { Label = "FM", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            AddParamSetting("MondoMod", "Pan On/Off", new PlugParamSettings { Label = "Pan", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            AddParamSetting("MondoMod", "Sync On/Off", new PlugParamSettings { Label = "Manual", LabelOn = "Auto", OnColor = new FinderColor(181, 214, 165), TextOnColor = FinderColor.Black });
            AddParamSetting("MondoMod", "Waveform", new PlugParamSettings { OnColor = new FinderColor(102, 255, 51), DialSteps = 4, HideValueBar = true });

            AddParamSetting("LoAir", "LoAir", new PlugParamSettings { Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("LoAir", "Lo", new PlugParamSettings { Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("LoAir", "Align", new PlugParamSettings { OnColor = new FinderColor(206, 175, 43), TextOnColor = FinderColor.Black });

            AddParamSetting("CLA Unplugged", "Bass Color", new PlugParamSettings { Label = "", UserMenuItems = [ "OFF", "SUB", "LOWER", "UPPER" ] });
            AddParamSetting("CLA Unplugged", "Bass", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Treble Color", new PlugParamSettings { Label = "", UserMenuItems = ["OFF", "BITE", "TOP", "ROOF"] });
            AddParamSetting("CLA Unplugged", "Treble", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Compress", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Compress Color", new PlugParamSettings { Label = "", UserMenuItems = ["OFF", "PUSH", "SPANK", "WALL"] });
            AddParamSetting("CLA Unplugged", "Reverb 1", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Reverb 1 Color", new PlugParamSettings { Label = "", UserMenuItems = ["OFF", "ROOM", "HALL", "CHAMBER"] });
            AddParamSetting("CLA Unplugged", "Reverb 2", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Reverb 2 Color", new PlugParamSettings { Label = "", UserMenuItems = ["OFF", "TIGHT", "LARGE", "CANYON"] });
            AddParamSetting("CLA Unplugged", "Delay", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Delay Color", new PlugParamSettings { Label = "", UserMenuItems = ["OFF", "SLAP", "EIGHT", "QUARTER"] });
            AddParamSetting("CLA Unplugged", "Sensitivity", new PlugParamSettings { Label = "Input Sens", OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "Output", new PlugParamSettings { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("CLA Unplugged", "PreDelay 1", new PlugParamSettings { Label = "Pre Rvrb 1", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            AddParamSetting("CLA Unplugged", "PreDelay 2", new PlugParamSettings { Label = "Pre Rvrb 2", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            AddParamSetting("CLA Unplugged", "PreDelay 1 On", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            AddParamSetting("CLA Unplugged", "PreDelay 2 On", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            AddParamSetting("CLA Unplugged", "Direct", new PlugParamSettings { OnColor = new FinderColor(80, 80, 80), OffColor = new FinderColor(240, 228, 87),
                                                                           TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });

            AddParamSetting("CLA-76", "Revision", new PlugParamSettings { Label = "Bluey", LabelOn = "Blacky", OffColor = new FinderColor(62, 141, 180), TextOffColor = FinderColor.White, 
                                                                                                           OnColor = FinderColor.Black, TextOnColor = FinderColor.White });
            AddParamSetting("CLA-76", "Ratio", new PlugParamSettings { UserMenuItems = ["20", "12", "8", "4", "ALL"] });
            AddParamSetting("CLA-76", "Analog", new PlugParamSettings { Label = "A", UserMenuItems = ["50Hz", "60Hz", "Off"], TextOnColor = new FinderColor(254, 246, 212) });
            AddParamSetting("CLA-76", "Meter", new PlugParamSettings { UserMenuItems = ["GR", "IN", "OUT"] });
            AddParamSetting("CLA-76", "Comp Off", new PlugParamSettings { OnColor = new FinderColor(162, 38, 38), TextOnColor = FinderColor.White });

            // Black Rooster Audio

            {
                var barOnColor = new FinderColor(242, 202, 75);
                var knobOnColor = new FinderColor(210, 204, 182);
                AddParamSetting("VLA-2A", "Power", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                AddParamSetting("VLA-2A", "Mode", new PlugParamSettings { Label = "COMPRESS", LabelOn = "LIMIT", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                AddParamSetting("VLA-2A", "ExSidech", new PlugParamSettings { Label = "EXT SC OFF", LabelOn = "EXT SC ON", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                AddParamSetting("VLA-2A", "CellSel", new PlugParamSettings { Label = "CEL", UserMenuItems = ["A", "B", "C"] });
                AddParamSetting("VLA-2A", "VULevel", new PlugParamSettings { Label = "VU", UserMenuItems = ["IN", "GR", "OUT"] });
                AddParamSetting("VLA-2A", "Gain", new PlugParamSettings { Label = "GAIN", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("VLA-2A", "PeakRedc", new PlugParamSettings { Label = "PK REDCT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("VLA-2A", "Emphasis", new PlugParamSettings { Label = "EMPHASIS", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("VLA-2A", "Makeup", new PlugParamSettings { Label = "MAKEUP", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("VLA-2A", "Mix", new PlugParamSettings { Label = "MIX", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
            }
            {
                var barOnColor = FinderColor.White;
                AddParamSetting("VLA-3A", "Power", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                AddParamSetting("VLA-3A", "Mode", new PlugParamSettings { Label = "COMPRESS", LabelOn = "LIMIT", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                AddParamSetting("VLA-3A", "VULevel", new PlugParamSettings { Label = "VU", UserMenuItems = ["IN", "GR", "OUT"] });
                AddParamSetting("VLA-3A", "Gain", new PlugParamSettings { Label = "GAIN", BarOnColor = barOnColor });
                AddParamSetting("VLA-3A", "PeakRedc", new PlugParamSettings { Label = "PK REDCT", BarOnColor = barOnColor });
            }
            {
                var barOnColor = new FinderColor(255, 161, 75);
                var knobOnColor = new FinderColor(199, 183, 160);
                AddParamSetting("RO-140", "Power", new PlugParamSettings { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                AddParamSetting("RO-140", "Material", new PlugParamSettings { Label = "", UserMenuItems = ["STEEL", "ALUMINUM", "BRONZE", "SILVER", "GOLD", "TITANIUM"] });
                AddParamSetting("RO-140", "Mode", new PlugParamSettings { Label = "", UserMenuItems = ["MONO", "MONO>ST", "STEREO" ] });
                AddParamSetting("RO-140", "Low", new PlugParamSettings { Label = "LOW", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Mid", new PlugParamSettings { Label = "MID", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "High", new PlugParamSettings { Label = "HIGH", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Damper", new PlugParamSettings { Label = "DAMPER", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = new FinderColor(200, 155, 127), OnTransparency = 255, DialSteps = 9 });
                AddParamSetting("RO-140", "PreDelay", new PlugParamSettings { Label = "PRE/DELAY", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Size", new PlugParamSettings { Label = "SIZE", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "BassCut", new PlugParamSettings { Label = "BASS CUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Mix", new PlugParamSettings { Label = "MIX", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Input", new PlugParamSettings { Label = "INPUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                AddParamSetting("RO-140", "Output", new PlugParamSettings { Label = "OUTPUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
            }

            // Analog Obsession

            AddParamSetting("Rare", "Bypass", new PlugParamSettings { Label = "IN", OnColor = new FinderColor(191, 0, 22) });
            AddParamSetting("Rare", "Low Boost", new PlugParamSettings { Label = "Low Boost", OnColor = new FinderColor(93, 161, 183) });
            AddParamSetting("Rare", "Low Atten", new PlugParamSettings { Label = "Low Atten", OnColor = new FinderColor(93, 161, 183) });
            AddParamSetting("Rare", "High Boost", new PlugParamSettings { Label = "High Boost", OnColor = new FinderColor(93, 161, 183) });
            AddParamSetting("Rare", "High Atten", new PlugParamSettings { Label = "High Atten", OnColor = new FinderColor(93, 161, 183) });
            AddParamSetting("Rare", "Low Frequency", new PlugParamSettings { Label = "Low Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 3 });
            AddParamSetting("Rare", "High Freqency", new PlugParamSettings { Label = "High Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 6 });
            AddParamSetting("Rare", "High Bandwidth", new PlugParamSettings { Label = "Bandwidth", OnColor = new FinderColor(93, 161, 183) });
            AddParamSetting("Rare", "High Atten Freqency", new PlugParamSettings { Label = "Atten Sel", OnColor = new FinderColor(93, 161, 183), DialSteps = 2 });

            AddParamSetting("LALA", "Bypass", new PlugParamSettings { Label = "OFF", LabelOn = "ON", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0), OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "Gain", new PlugParamSettings { Label = "GAIN", OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "Peak Reduction", new PlugParamSettings { Label = "REDUCTION", OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "Mode", new PlugParamSettings { Label = "LIMIT", LabelOn = "COMP", TextOnColor = new FinderColor(0, 0, 0), 
                                                                                                   TextOffColor = new FinderColor(0, 0, 0),
                                                                                                   OnColor = new FinderColor(185, 182, 163),
                                                                                                   OffColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "1:3", new PlugParamSettings { Label = "MIX", OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "2:1", new PlugParamSettings { Label = "HPF", OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "MF", new PlugParamSettings { OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "MG", new PlugParamSettings { OnColor = new FinderColor(185, 182, 163), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("LALA", "HF", new PlugParamSettings { OnColor = new FinderColor(185, 182, 163) });
            AddParamSetting("LALA", "External Sidechain", new PlugParamSettings { Label = "SIDECHAIN", OnColor = new FinderColor(185, 182, 163) });

            AddParamSetting("FETish", "", new PlugParamSettings { OnColor = new FinderColor(24, 86, 119) });
            AddParamSetting("FETish", "Bypass", new PlugParamSettings { Label = "IN", OnColor = new FinderColor(24, 86, 119) });
            AddParamSetting("FETish", "Input", new PlugParamSettings { Label = "INPUT", OnColor = new FinderColor(186, 175, 176) });
            AddParamSetting("FETish", "Output", new PlugParamSettings { Label = "OUTPUT", OnColor = new FinderColor(186, 175, 176) });
            AddParamSetting("FETish", "Ratio", new PlugParamSettings { OnColor = new FinderColor(186, 175, 176), DialSteps = 16 });
            AddParamSetting("FETish", "Sidechain", new PlugParamSettings { Label = "EXT", OnColor = new FinderColor(24, 86, 119) });
            AddParamSetting("FETish", "Mid Frequency", new PlugParamSettings { Label = "MF", OnColor = new FinderColor(24, 86, 119) });
            AddParamSetting("FETish", "Mid Gain", new PlugParamSettings { Label = "MG", OnColor = new FinderColor(24, 86, 119), Mode = PlugParamSettings.PotMode.Symmetric });

            AddParamSetting("dBComp", "", new PlugParamSettings { OnColor = new FinderColor(105, 99, 94) });
            AddParamSetting("dBComp", "Output Gain", new PlugParamSettings { Label = "Output", OnColor = new FinderColor(105, 99, 94) });
            AddParamSetting("dBComp", "1:4U", new PlugParamSettings { Label = "EXT SC", OnColor = new FinderColor(208, 207, 203), TextOnColor = FinderColor.Black });

            AddParamSetting("BUSTERse", "Bypass", new PlugParamSettings { Label = "MAIN", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            AddParamSetting("BUSTERse", "Turbo", new PlugParamSettings { Label = "TURBO", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            AddParamSetting("BUSTERse", "XFormer", new PlugParamSettings { Label = "XFORMER", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            AddParamSetting("BUSTERse", "Threshold", new PlugParamSettings { Label = "THRESH", OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "Attack Time", new PlugParamSettings { Label = "ATTACK", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            AddParamSetting("BUSTERse", "Ratio", new PlugParamSettings { Label = "RATIO", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            AddParamSetting("BUSTERse", "Make-Up Gain", new PlugParamSettings { Label = "MAKE-UP", OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "Release Time", new PlugParamSettings { Label = "RELEASE", OnColor = new FinderColor(174, 164, 167), DialSteps = 4 });
            AddParamSetting("BUSTERse", "Compressor Mix", new PlugParamSettings { Label = "MIX", OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "External Sidechain", new PlugParamSettings { Label = "EXT", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            AddParamSetting("BUSTERse", "HF", new PlugParamSettings { OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "Mid Gain", new PlugParamSettings { Label = "MID", OnColor = new FinderColor(174, 164, 167), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("BUSTERse", "HPF", new PlugParamSettings { OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "Boost", new PlugParamSettings { Label = "TR BOOST", OnColor = new FinderColor(174, 164, 167) });
            AddParamSetting("BUSTERse", "Transient Tilt", new PlugParamSettings { Label = "TR TILT", OnColor = new FinderColor(174, 164, 167), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("BUSTERse", "Transient Mix", new PlugParamSettings { Label = "TR MIX", OnColor = new FinderColor(174, 164, 167) });

            AddParamSetting("BritChannel", "", new PlugParamSettings { OnColor = new FinderColor(141, 134, 137), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("BritChannel", "Bypass", new PlugParamSettings { Label = "IN", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            AddParamSetting("BritChannel", "Mic Pre", new PlugParamSettings { Label = "MIC", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            AddParamSetting("BritChannel", "Mid Freq", new PlugParamSettings { OnColor = new FinderColor(141, 134, 137), DialSteps = 6 });
            AddParamSetting("BritChannel", "Low Freq", new PlugParamSettings { OnColor = new FinderColor(141, 134, 137), DialSteps = 4 });
            AddParamSetting("BritChannel", "HighPass", new PlugParamSettings { Label = "High Pass", OnColor = new FinderColor(49, 81, 119), DialSteps = 4 });
            AddParamSetting("BritChannel", "Preamp Gain", new PlugParamSettings { Label = "PRE GAIN", OnColor = new FinderColor(160, 53, 50), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("BritChannel", "Output Trim", new PlugParamSettings { Label = "OUT TRIM", OnColor = new FinderColor(124, 117, 115), Mode = PlugParamSettings.PotMode.Symmetric });

            // Acon Digital

            AddParamSetting("Acon Digital Equalize 2", "Gain-bandwidth link", new PlugParamSettings { Label = "Link", OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Solo 1", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 1", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 1", new PlugParamSettings { OnColor = new FinderColor(221, 125, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 1", new PlugParamSettings { OnColor = new FinderColor(221, 125, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 1", new PlugParamSettings { Label = "Filter 1", OnColor = new FinderColor(221, 125, 125), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 1", new PlugParamSettings { Label = "Bandwidth 1", OnColor = new FinderColor(221, 125, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 1", new PlugParamSettings { OnColor = new FinderColor(221, 125, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 1", new PlugParamSettings { OnColor = new FinderColor(221, 125, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Solo 2", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 2", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 2", new PlugParamSettings { OnColor = new FinderColor(204, 133, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 2", new PlugParamSettings { OnColor = new FinderColor(204, 133, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 2", new PlugParamSettings { Label = "Filter 2", OnColor = new FinderColor(204, 133, 61), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 2", new PlugParamSettings { Label = "Bandwidth 2", OnColor = new FinderColor(204, 133, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 2", new PlugParamSettings { OnColor = new FinderColor(204, 133, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 2", new PlugParamSettings { OnColor = new FinderColor(204, 133, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Solo 3", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 3", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 3", new PlugParamSettings { OnColor = new FinderColor(204, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 3", new PlugParamSettings { OnColor = new FinderColor(204, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 3", new PlugParamSettings { Label = "Filter 3", OnColor = new FinderColor(204, 204, 61), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 3", new PlugParamSettings { Label = "Bandwidth 3", OnColor = new FinderColor(204, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 3", new PlugParamSettings { OnColor = new FinderColor(204, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 3", new PlugParamSettings { OnColor = new FinderColor(204, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Solo 4", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 4", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 4", new PlugParamSettings { OnColor = new FinderColor(61, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 4", new PlugParamSettings { OnColor = new FinderColor(61, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 4", new PlugParamSettings { Label = "Filter 4", OnColor = new FinderColor(61, 204, 61), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 4", new PlugParamSettings { Label = "Bandwidth 4", OnColor = new FinderColor(61, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 4", new PlugParamSettings { OnColor = new FinderColor(61, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 4", new PlugParamSettings { OnColor = new FinderColor(61, 204, 61) });
            AddParamSetting("Acon Digital Equalize 2", "Solo 5", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 5", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 5", new PlugParamSettings { OnColor = new FinderColor(61, 204, 133) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 5", new PlugParamSettings { OnColor = new FinderColor(61, 204, 133) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 5", new PlugParamSettings { Label = "Filter 5", OnColor = new FinderColor(61, 204, 133), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 5", new PlugParamSettings { Label = "Bandwidth 5", OnColor = new FinderColor(61, 204, 133) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 5", new PlugParamSettings { OnColor = new FinderColor(61, 204, 133) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 5", new PlugParamSettings { OnColor = new FinderColor(61, 204, 133) });
            AddParamSetting("Acon Digital Equalize 2", "Solo 6", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Bypass 6", new PlugParamSettings { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Equalize 2", "Frequency 6", new PlugParamSettings { OnColor = new FinderColor(173, 221, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Gain 6", new PlugParamSettings { OnColor = new FinderColor(173, 221, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Filter type 6", new PlugParamSettings { Label = "Filter 6", OnColor = new FinderColor(173, 221, 125), DialSteps = 8, HideValueBar = true });
            AddParamSetting("Acon Digital Equalize 2", "Band width 6", new PlugParamSettings { Label = "Bandwidth 6 ", OnColor = new FinderColor(173, 221, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Slope 6", new PlugParamSettings { OnColor = new FinderColor(173, 221, 125) });
            AddParamSetting("Acon Digital Equalize 2", "Resonance 6", new PlugParamSettings { OnColor = new FinderColor(173, 221, 125) });

            AddParamSetting("Acon Digital Verberate 2", "Dry Mute", new PlugParamSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Verberate 2", "Reverb Mute", new PlugParamSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Verberate 2", "ER Mute", new PlugParamSettings { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Verberate 2", "Freeze", new PlugParamSettings { OnColor = new FinderColor(230, 173, 43), TextOnColor = FinderColor.Black });
            AddParamSetting("Acon Digital Verberate 2", "Stereo Spread", new PlugParamSettings { Label = "Spread" });
            AddParamSetting("Acon Digital Verberate 2", "EarlyReflectionsType", new PlugParamSettings { Label = "ER Type", DialSteps = 14, HideValueBar = true });
            AddParamSetting("Acon Digital Verberate 2", "Algorithm", new PlugParamSettings { Label = "Vivid", LabelOn = "Legacy", TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            AddParamSetting("Acon Digital Verberate 2", "Decay High Cut Enable", new PlugParamSettings { Label = "Decay HC", OnColor = new FinderColor(221, 85, 255) });
            AddLinked("Acon Digital Verberate 2", "Decay High Cut Frequency", "Decay High Cut Enable", label: "Freq");
            AddLinked("Acon Digital Verberate 2", "Decay High Cut Slope", "Decay High Cut Enable", label: "Slope");
            AddParamSetting("Acon Digital Verberate 2", "EQ High Cut Enable", new PlugParamSettings { Label = "EQ HC", OnColor = new FinderColor(221, 85, 255) });
            AddLinked("Acon Digital Verberate 2", "EQ High Cut Frequency", "EQ High Cut Enable", label: "Freq");
            AddLinked("Acon Digital Verberate 2", "EQ High Cut Slope", "EQ High Cut Enable", label: "Slope");


            // AXP

            AddParamSetting("AXP SoftAmp PSA", "Enable", new PlugParamSettings { Label = "ENABLE" });
            AddParamSetting("AXP SoftAmp PSA", "Preamp", new PlugParamSettings { Label = "PRE-AMP", OnColor = new FinderColor(200, 200, 200) });
            AddParamSetting("AXP SoftAmp PSA", "Asymm", new PlugParamSettings { Label = "ASYMM", OnColor = new FinderColor(237, 244, 1), TextOnColor = FinderColor.Black });
            AddParamSetting("AXP SoftAmp PSA", "Buzz", new PlugParamSettings { Label = "BUZZ", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("AXP SoftAmp PSA", "Punch", new PlugParamSettings { Label = "PUNCH", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("AXP SoftAmp PSA", "Crunch", new PlugParamSettings { Label = "CRUNCH", OnColor = new FinderColor(200, 200, 200) });
            AddParamSetting("AXP SoftAmp PSA", "SoftClip", new PlugParamSettings { Label = "SOFT CLIP", OnColor = new FinderColor(234, 105, 30), TextOnColor = FinderColor.Black });
            AddParamSetting("AXP SoftAmp PSA", "Drive", new PlugParamSettings { Label = "DRIVE", OnColor = new FinderColor(200, 200, 200) });
            AddParamSetting("AXP SoftAmp PSA", "Level", new PlugParamSettings { Label = "LEVEL", OnColor = new FinderColor(200, 200, 200) });
            AddParamSetting("AXP SoftAmp PSA", "Limiter", new PlugParamSettings { Label = "LIMITER", OnColor = new FinderColor(237, 0, 0), TextOnColor = FinderColor.Black });
            AddParamSetting("AXP SoftAmp PSA", "Low", new PlugParamSettings { Label = "LOW", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("AXP SoftAmp PSA", "High", new PlugParamSettings { Label = "HIGH", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSettings.PotMode.Symmetric });
            AddParamSetting("AXP SoftAmp PSA", "SpkReso", new PlugParamSettings { Label = "SHAPE", OnColor = new FinderColor(120, 120, 120) });
            AddParamSetting("AXP SoftAmp PSA", "SpkRoll", new PlugParamSettings { Label = "ROLL-OFF", OnColor = new FinderColor(120, 120, 120) });
            AddParamSetting("AXP SoftAmp PSA", "PSI_En", new PlugParamSettings { Label = "PSI DNS", OnColor = new FinderColor(10, 178, 255), TextOnColor = FinderColor.Black });
            AddLinked("AXP SoftAmp PSA", "PSI_Thr", "PSI_En", label: "THRESHOLD");
            AddParamSetting("AXP SoftAmp PSA", "OS_Enab", new PlugParamSettings { Label = "SQUEEZO", OnColor = new FinderColor(209, 155, 104), TextOnColor = FinderColor.Black });
            AddLinked("AXP SoftAmp PSA", "OS_Gain", "OS_Enab", label: "GAIN");
            AddLinked("AXP SoftAmp PSA", "OS_Bias", "OS_Enab", label: "BIAS");
            AddLinked("AXP SoftAmp PSA", "OS_Level", "OS_Enab", label: "LEVEL");

            // Izotope

            AddParamSetting("Neutron 4 Transient Shaper", "TS B1 Attack", new PlugParamSettings { Label = "1: Attack", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B1 Sustain", new PlugParamSettings { Label = "1: Sustain", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B1 Bypass", new PlugParamSettings { Label = "Bypass", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B2 Attack", new PlugParamSettings { Label = "2: Attack", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B2 Sustain", new PlugParamSettings { Label = "2: Sustain", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B2 Bypass", new PlugParamSettings { Label = "Bypass", OnColor = new FinderColor(63, 191, 173), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B3 Attack", new PlugParamSettings { Label = "3: Attack", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B3 Sustain", new PlugParamSettings { Label = "3: Sustain", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            AddParamSetting("Neutron 4 Transient Shaper", "TS B3 Bypass", new PlugParamSettings { Label = "Bypass", OnColor = new FinderColor(196, 232, 107), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "Global Input Gain", new PlugParamSettings { Label = "In" });
            AddParamSetting("Neutron 4 Transient Shaper", "Global Output Gain", new PlugParamSettings { Label = "Out" });
            AddParamSetting("Neutron 4 Transient Shaper", "Sum to Mono", new PlugParamSettings { Label = "Mono", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "Swap Channels", new PlugParamSettings { Label = "Swap", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "Invert Phase", new PlugParamSettings { OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            AddParamSetting("Neutron 4 Transient Shaper", "TS Global Mix", new PlugParamSettings { Label = "Mix", OnColor = new FinderColor(255, 96, 28) });

            AddParamSetting("Trash", "B2 Trash Drive", new PlugParamSettings { Label = "Drive", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Tilt Gain", new PlugParamSettings { Label = "Tilt", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Tilt Frequency", new PlugParamSettings { Label = "Frequency", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Mix", new PlugParamSettings { Label = "Mix", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Blend X", new PlugParamSettings { Label = "X", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Blend Y", new PlugParamSettings { Label = "Y", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Top Left Style", new PlugParamSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Top Right Style", new PlugParamSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Bottom Left Style", new PlugParamSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "B2 Trash Bottom Right Style", new PlugParamSettings { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSettings.PotMode.Symmetric, PaintLabelBg = false });
            AddParamSetting("Trash", "Global Input Gain", new PlugParamSettings { Label = "IN" });
            AddParamSetting("Trash", "Global Output Gain", new PlugParamSettings { Label = "OUT" });
            AddParamSetting("Trash", "Auto Gain Enabled", new PlugParamSettings { Label = "Auto Gain" });
            AddParamSetting("Trash", "Limiter Enabled", new PlugParamSettings { Label = "Limiter" });


            // Tokio Dawn Labs
            AddParamSetting("TDR Kotelnikov", "", new PlugParamSettings { OnColor = new FinderColor(42, 75, 124) });
            AddParamSetting("TDR Kotelnikov", "SC Stereo Diff", new PlugParamSettings { Label = "Stereo Diff", OnColor = new FinderColor(42, 75, 124) });
        }
    }
}
