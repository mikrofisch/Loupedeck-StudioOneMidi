namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Schema;
    using System.Xml;
    using System.Xml.Serialization;
    using System.Reflection;



    // BitmapColor objects that have not been explicitly assigned to a
    // color are automatically replaced by the currently defined default color.
    // Since it is not possible to have a BitmapColor object that is not assigned
    // to a color (BitmapColor.NoColor evaluates to the same values as BitmapColor.White) and
    // it cannot be set to null, we define a new class that can be null.
    //
    [XmlSchemaProvider("GetSchemaMethod")]
    public class FinderColor : IXmlSerializable
    {
        public FinderColor() { }
        public FinderColor(BitmapColor b) => this.Color = b;
        public FinderColor(Byte r, Byte g, Byte b) => this.Color = new BitmapColor(r, g, b);

        public FinderColor(String colorName, Byte r, Byte g, Byte b)
        {
            this.Name = colorName;
            this.Color = new BitmapColor(r, g, b);
        }

        public FinderColor(String colorName, BitmapColor bc)
        {
            this.Name = colorName;
            this.Color = bc;
        }

        public FinderColor(FinderColor c)
        {
            this.Name = c.Name;
            this.Color = c.Color;
        }

        public static implicit operator BitmapColor(FinderColor f) => f != null ? f.Color : new BitmapColor();
        public static explicit operator FinderColor(BitmapColor b) => new FinderColor(b);

        // public static FinderColor Transparent => new FinderColor(BitmapColor.Transparent);
        public static FinderColor White => new FinderColor(BitmapColor.White);
        public static FinderColor Black => new FinderColor(BitmapColor.Black);

        public BitmapColor Color { get; set; }
        public String Name { get; private set; }   // name of the referenced color in the device's color list

        public Boolean XmlRenderAsNameRef = false;

        #region IXmlSerializable members

        public System.Xml.Schema.XmlSchema GetSchema() => null;

        public static XmlQualifiedName GetSchemaMethod(XmlSchemaSet xs)
        {
            // This provides a schema so that the serializer will render the element name
            // defined in the schema for arrays (which it doesn't do if [XmlRoot] is used
            // to set the name)
            var colorSchema = @"<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema""> " +
                              @"<xs:element name=""Color"" nillable=""true"" />" +
                              @"<xs:complexType name=""Color""> " +
                              @"<xs:attribute name=""name"" type=""xs:string"" /> " +
                              @"</xs:complexType> " +
                              @"</xs:schema>";
            using (var textReader = new StringReader(colorSchema))
            using (var schemaSetReader = System.Xml.XmlReader.Create(textReader))
            {
                xs.Add("", schemaSetReader);
            }
            // Return back the namespace and name to be used for this type.
            return new XmlQualifiedName("Color", "");
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            // var str = reader.ReadString();
            reader.MoveToContent();

            if (reader.HasAttributes)
            {
                this.Name = reader.GetAttribute("name");
            }

            var content = reader.ReadElementContentAsString();
            if (content == "white")
            {
                this.Color = BitmapColor.White;
            }
            else if (content == "black")
            {
                this.Color = BitmapColor.Black;
            }
            else if (content.StartsWith("rgb("))
            {
                var rgb = content.Substring(4, content.Length - 5).Split(',');
                this.Color = new BitmapColor(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            }
            else
            {
                this.Name = content;
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            if (!this.Name.IsNullOrEmpty())
            {
                if (this.XmlRenderAsNameRef)
                {
                    writer.WriteString(this.Name);
                }
                else
                {
                    writer.WriteAttributeString("name", this.Name);
                }
            }
            if (!this.XmlRenderAsNameRef)
            {
                if (this.Color == BitmapColor.White)
                {
                    writer.WriteString("white");
                }
                else if (this.Color == BitmapColor.Black)
                {
                    writer.WriteString("black");
                }
                else
                {
                    writer.WriteString($"rgb({this.Color.R},{this.Color.G},{this.Color.B})");
                }
            }
        }

        #endregion
    }

    public class PlugSettingsFinder
    {
        public const Int32 DefaultOnTransparency = 80;

        public class PlugParamSetting
        {
            [DefaultValueAttribute(Positive)]
            public enum PotMode { Positive, Symmetric };
            public PotMode Mode { get; set; } = PotMode.Positive;
            [DefaultValueAttribute(false)]
            public Boolean HideValueBar { get; set; } = false;
            [DefaultValueAttribute(false)]
            public Boolean ShowUserButtonCircle { get; set; } = false;
            [DefaultValueAttribute(true)]
            public Boolean PaintLabelBg { get; set; } = true;

            public FinderColor OnColor { get; set; }
            [DefaultValueAttribute(DefaultOnTransparency)]
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

            [DefaultValueAttribute(-1)]
            public Int32 MaxValuePrecision { get; set; } = -1;      // Maximum number of decimal places for the value display (-1 = no limit)

            public String[] UserMenuItems;                          // Items for user button menu

            [XmlAttribute (AttributeName = "name")]
            public String Name;                                     // For XML config file

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

        public class PlugParamDeviceEntry
        {
            public Dictionary<String, PlugParamSetting> ParamSettings = [];
        }

        private static readonly Dictionary<String, PlugParamDeviceEntry> PlugParamDict = [];
        private const String strPlugParamSettingsID = "[ps]";  // for plugin settings

        private String LastPluginName, LastPluginParameter;
        private PlugParamDeviceEntry LastPlugParamDeviceEntry;
        private static PlugParamDeviceEntry DefaultDeviceEntry;
        private PlugParamSetting LastParamSettings;

        public Int32 CurrentUserPage = 0;              // For tracking the current user page position

        public PlugParamSetting DefaultPlugParamSettings { get; private set; } = new PlugParamSetting
        {
            OnColor = FinderColor.Black,
            OffColor = FinderColor.Black,
            TextOnColor = FinderColor.White,
            TextOffColor = FinderColor.White,
            BarOnColor = FinderColor.White
        };

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public PlugSettingsFinder() { }

        // Need to call "Init()" to populate the ColorSettings dictionary!
        public PlugSettingsFinder(PlugParamSetting defaultPlugParamSettings)
        {
            this.DefaultPlugParamSettings = defaultPlugParamSettings;
        }


        public class XmlConfig
        {
            public static readonly String ConfigFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loupedeck-StudioOneMidiPlugin");
            public const String ConfigFileName = "AudioPluginConfig.xml";

            public class PluginConfig
            {
                [XmlAttribute]
                public String PluginName;

                public List<FinderColor> Colors = [];
                public List<PlugParamSetting> ParamSettings = [];
            }

            [XmlElement("PluginConfig")]
            public readonly List<PluginConfig> PluginConfigs = [];

            private void ProcessColorSettingForWrite(PluginConfig p, FinderColor c)
            {
                if (c!= null && !c.Name.IsNullOrEmpty())
                {
                    var colorExists = false;
                    foreach (var cc in p.Colors)
                    {
                        if (cc.Name == c.Name)
                        {
                            colorExists = true;
                            break;
                        }
                    }
                    if (!colorExists)
                    {
                        p.Colors.Add(new FinderColor(c));
                    }
                    c.XmlRenderAsNameRef = true;   // Write only name to XML in parameter settings entry
                }
            }

            private void ProcessColorSettingForRead(PluginConfig p, FinderColor c)
            {
                // If the colour is referenced by name, we get the RGB values from
                // the colour list (in an automatically created config XML file there
                // won't be any RGB values in a colour that is referenced by name)
                //
                if (c != null && !c.Name.IsNullOrEmpty())
                {
                    // Find RGB values in colour list
                    foreach (var cc in p.Colors)
                    {
                        if (cc.Name == c.Name)
                        {
                            c.Color = cc.Color;
                            break;
                        }
                    }
                }
            }



            public void ReadXmlStream(Stream reader, Dictionary<String, PlugParamDeviceEntry> plugParamDict)
            {
                var serializer = new XmlSerializer(typeof(XmlConfig));

                    // Call the Deserialize method to restore the object's state.
                    var p = (XmlConfig)serializer.Deserialize(reader);

                // Create plugin device entries and dereference colour names in parameter settings
                //
                foreach (var cfgDeviceEntry in p.PluginConfigs)
                {
                    PlugParamSetting defaultSettings = null;

                    var deviceEntry = new PlugParamDeviceEntry { };
                    foreach (var paramSettings in cfgDeviceEntry.ParamSettings)
                    {
                        // Get colour values for colours that are referenced by name.
                        this.ProcessColorSettingForRead(cfgDeviceEntry, paramSettings.OnColor);
                        this.ProcessColorSettingForRead(cfgDeviceEntry, paramSettings.OffColor);
                        this.ProcessColorSettingForRead(cfgDeviceEntry, paramSettings.TextOnColor);
                        this.ProcessColorSettingForRead(cfgDeviceEntry, paramSettings.TextOffColor);
                        this.ProcessColorSettingForRead(cfgDeviceEntry, paramSettings.BarOnColor);

                        if (paramSettings.Name == "")
                        {
                            defaultSettings = paramSettings;
                        }
                        else if (defaultSettings != null)
                        {
                            var globalDefaultSettings = new PlugParamSetting();

                            // If there is a parameter setting with an empty name, use it to set local default
                            // values for parameters that have the same value as the global default.
                            // This is using reflection to iterate over all properties of the PlugParamSetting class.
                            //
                            PropertyInfo[] properties = typeof(PlugParamSetting).GetProperties();
                            foreach (PropertyInfo property in properties)
                            {
                                var paramValue = property.GetValue(paramSettings);
                                var defaultValue = property.GetValue(defaultSettings);
                                var globalDefaultValue = property.GetValue(globalDefaultSettings);

                                if ((paramValue?.Equals(globalDefaultValue) == true) ||
                                    (paramValue == null && globalDefaultValue == null))
                                {
                                    property.SetValue(paramSettings, defaultValue);
                                }
                            }
                        }

                        deviceEntry.ParamSettings.Add(paramSettings.Name, paramSettings);
                    }

                    // Add or update the device entry in the dictionary.
                    plugParamDict[cfgDeviceEntry.PluginName] = deviceEntry;
                }
            }

            public void WriteXmlCfgFile(String configFileName, Dictionary<String, PlugParamDeviceEntry> plugParamDict)
            {
                if (!Directory.Exists(ConfigFolderPath))
                {
                    Directory.CreateDirectory(ConfigFolderPath);
                }
                var configFilePath = System.IO.Path.Combine(ConfigFolderPath, configFileName);
                var writer = new StreamWriter(configFilePath);

                foreach (var deviceEntry in plugParamDict)
                {
                    var cfgDeviceEntry = new PluginConfig { PluginName = deviceEntry.Key };
                    foreach (var paramSettings in deviceEntry.Value.ParamSettings)
                    {
                        paramSettings.Value.Name = paramSettings.Key;

                        this.ProcessColorSettingForWrite(cfgDeviceEntry, paramSettings.Value.OnColor);
                        this.ProcessColorSettingForWrite(cfgDeviceEntry, paramSettings.Value.OffColor);
                        this.ProcessColorSettingForWrite(cfgDeviceEntry, paramSettings.Value.TextOnColor);
                        this.ProcessColorSettingForWrite(cfgDeviceEntry, paramSettings.Value.TextOffColor);
                        this.ProcessColorSettingForWrite(cfgDeviceEntry, paramSettings.Value.BarOnColor);

                        cfgDeviceEntry.ParamSettings.Add(paramSettings.Value); 
                    }

                    this.PluginConfigs.Add(cfgDeviceEntry);
                }

                var serializer = new XmlSerializer(typeof(XmlConfig));

                serializer.Serialize(writer, this);
                writer.Close();

            }
        }

        public static void Init(Plugin plugin, Boolean forceReload = false)
        {
            if (forceReload)
            {
                PlugParamDict.Clear();
            }
            if (PlugParamDict.Count == 0)
            {
                var xmlCfg = new XmlConfig();

                // InitColorDict();

                // Read default settings from embedded resource.
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(XmlConfig.ConfigFileName));
                using (Stream reader = assembly.GetManifestResourceStream(resourceName))
                {
                    xmlCfg.ReadXmlStream(reader, PlugParamDict);
                }

                // Read user settings from file.
                var configFilePath = System.IO.Path.Combine(XmlConfig.ConfigFolderPath, XmlConfig.ConfigFileName);
                if (File.Exists(configFilePath))
                {
                    using (Stream reader = new FileStream(configFilePath, FileMode.Open))
                    {
                        xmlCfg.ReadXmlStream(reader, PlugParamDict);
                    }
                }
                else
                {
                    // Create user settings config file.
                    xmlCfg.WriteXmlCfgFile(XmlConfig.ConfigFileName, PlugParamDict);
                }

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
                            ps = new PlugParamSetting { };
                            pe.ParamSettings.Add(settingsParsed[0], ps);
                        }


                        if (plugin.TryGetPluginSetting(SettingName(settingsParsed[0], settingsParsed[1], settingsParsed[2]), out var val))
                        {
                            switch (settingsParsed[2])
                            {
                                case PlugParamSetting.strOnColor:
                                    ps.OnColor = new FinderColor(Convert.ToByte(val.Substring(0, 2), 16),
                                                                 Convert.ToByte(val.Substring(2, 2), 16),
                                                                 Convert.ToByte(val.Substring(4, 2), 16));
                                    break;
                                case PlugParamSetting.strLabel:
                                    ps.Label = val;
                                    break;
                                case PlugParamSetting.strLinkedParameter:
                                    ps.LinkedParameter = val;
                                    break;
                                case PlugParamSetting.strMode:
                                    ps.Mode = val.ParseInt32() == 0 ? PlugParamSetting.PotMode.Positive : PlugParamSetting.PotMode.Symmetric;
                                    break;
                                case PlugParamSetting.strShowCircle:
                                    ps.ShowUserButtonCircle = val.ParseInt32() == 1 ? true : false;
                                    break;
                            }
                        }
                    }
                }

                PlugParamDict.TryGetValue("", out DefaultDeviceEntry);
            }
        }

        private PlugParamSetting SaveLastSettings(PlugParamSetting paramSettings)
        {
            this.LastParamSettings = paramSettings;
            return paramSettings;
        }

        public PlugParamDeviceEntry GetPlugParamDeviceEntry(String pluginName)
        {
            if (pluginName == null)
            {
                return new PlugParamDeviceEntry();
            }

            if (pluginName == this.LastPluginName)
            {
                return this.LastPlugParamDeviceEntry;
            }

            this.LastPluginName = pluginName;

            if (!PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
            {
                // No full match, try partial match
                var partialMatchKeys = PlugParamDict.Keys.Where(key => key != "" && pluginName.StartsWith(key));
                if (partialMatchKeys.Any())
                {
                    if (!PlugParamDict.TryGetValue(partialMatchKeys.First(), out deviceEntry))
                    {
                        this.LastPlugParamDeviceEntry = new PlugParamDeviceEntry();
                        return this.LastPlugParamDeviceEntry;
                    }
                }
            }

            this.LastPlugParamDeviceEntry = deviceEntry;
            return deviceEntry;
        }

        public PlugParamSetting GetPlugParamSettings(PlugParamDeviceEntry deviceEntry, String parameterName, Boolean isUser, Int32 buttonIdx = 0)
        {
            if (parameterName == null)
            {
                return this.DefaultPlugParamSettings;
            }
                
//           if (this.LastParamSettings != null && deviceEntry == this.LastPlugParamDeviceEntry && parameterName == this.LastPluginParameter) return this.LastParamSettings;

            this.LastPluginParameter = parameterName;

            var userPagePos = $"{this.CurrentUserPage}:{buttonIdx}" + (isUser ? "U" : "");
            PlugParamSetting paramSettings;

            if (deviceEntry != null &&
                (deviceEntry.ParamSettings.TryGetValue(userPagePos, out paramSettings) ||
                deviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings) ||
                (DefaultDeviceEntry != null && DefaultDeviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings)) ||
                deviceEntry.ParamSettings.TryGetValue("", out paramSettings)))
            {
                return this.SaveLastSettings(paramSettings);
            }

            if (PlugParamDict.TryGetValue("", out deviceEntry) &&
                deviceEntry.ParamSettings.TryGetValue(parameterName, out paramSettings))
            {
                return this.SaveLastSettings(paramSettings);
            }

            return this.SaveLastSettings(this.DefaultPlugParamSettings);
        }

        private BitmapColor FindColor(FinderColor settingsColor, BitmapColor defaultColor) => settingsColor ?? defaultColor;

        public PlugParamSetting.PotMode GetMode(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).Mode;
        public Boolean GetShowCircle(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).ShowUserButtonCircle;
        public Boolean GetPaintLabelBg(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).PaintLabelBg;

        public BitmapColor GetOnColor(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false)
        {
            var cs = this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx);
            return cs != null ? new BitmapColor(cs.OnColor, isUser ? 255 : cs.OnTransparency)
                              : new BitmapColor(this.DefaultPlugParamSettings.OnColor, isUser ? 255 : this.DefaultPlugParamSettings.OnTransparency);
        }
        public BitmapColor GetBarOnColor(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false)
        {
            var cs = this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx);
            return cs.BarOnColor ?? this.FindColor(cs.OnColor, this.DefaultPlugParamSettings.OnColor);
        }
        public BitmapColor GetOffColor(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.FindColor(this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).OffColor,
                                                                                                                                                          this.DefaultPlugParamSettings.OffColor);
        public BitmapColor GetTextOnColor(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.FindColor(this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).TextOnColor,
                                                                                                                                                             this.DefaultPlugParamSettings.TextOnColor);
        public BitmapColor GetTextOffColor(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.FindColor(this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).TextOffColor,
                                                                                                                                                              this.DefaultPlugParamSettings.TextOffColor);
        public String GetLabel(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).Label ?? parameterName;
        public String GetLabelOn(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false)
        {
            var cs = this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx);
            return cs.LabelOn ?? cs.Label ?? parameterName;
        }
        public String GetLabelShort(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => stripLabel(this.GetLabel(deviceEntry, parameterName, buttonIdx, isUser));
        public String GetLabelOnShort(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => stripLabel(this.GetLabelOn(deviceEntry, parameterName, buttonIdx, isUser));
        public static String stripLabel(String label)
        {
            if (label.Length <= 12) return label;
            return Regex.Replace(label, "(?<!^)[aeiou](?!$)", "");
        }
        public BitmapImage GetIcon(PlugParamDeviceEntry deviceEntry, String parameterName)
        {
            var colorSettings = this.GetPlugParamSettings(deviceEntry, parameterName, false);
            if (colorSettings.IconName != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconName}_52px.png"));
            }
            return null;
        }

        public BitmapImage GetIconOn(PlugParamDeviceEntry deviceEntry, String parameterName)
        {
            var colorSettings = this.GetPlugParamSettings(deviceEntry, parameterName, false);
            if (colorSettings.IconNameOn != null)
            {
                return EmbeddedResources.ReadImage(EmbeddedResources.FindFile($"{colorSettings.IconNameOn}_52px.png"));
            }
            return null;
        }
        public String GetLinkedParameter(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkedParameter;
        public Boolean GetLinkReversed(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkReversed;
        public String GetLinkedStates(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkedStates;
        public Boolean HideValueBar(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).HideValueBar;
        public Boolean ShowUserButtonCircle(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).ShowUserButtonCircle;
        public Int32 GetDialSteps(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).DialSteps;
        public Int32 GetMaxValuePrecision(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).MaxValuePrecision;
        public String[] GetUserMenuItems(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).UserMenuItems;
        public Boolean HasMenu(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).UserMenuItems != null;
        public static String SettingName(String pluginName, String parameterName, String setting) => strPlugParamSettingsID + pluginName + "|" + parameterName + "|" + setting;

//        private static void AddParamSetting(String pluginName, String parameterName, PlugParamSettings setting)
//        {
//            deviceEntry.ParamSettings.Add(parameterName, setting);
//        }

        private static PlugParamDeviceEntry AddPlugParamDeviceEntry(String pluginName)
        {
            if (!PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
            {
                deviceEntry = new PlugParamDeviceEntry();
                PlugParamDict.Add(pluginName, deviceEntry);
            }
            return deviceEntry;
        }

        private static void AddLinked(PlugParamDeviceEntry deviceEntry, String parameterName, String linkedParameter,
                                      String label = null,
                                      PlugParamSetting.PotMode mode = PlugParamSetting.PotMode.Positive,
                                      Boolean linkReversed = false,
                                      String linkedStates = "",
                                      FinderColor onColor = null,
                                      Int32 onTransparency = DefaultOnTransparency,
                                      FinderColor textOnColor = null,
                                      FinderColor offColor = null,
                                      FinderColor textOffColor = null,
                                      String[] userMenuItems = null)
        {
            label ??= parameterName;
            var paramSettings = deviceEntry.ParamSettings[linkedParameter];
            deviceEntry.ParamSettings.Add(parameterName, new PlugParamSetting
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

        private static PlugParamSetting CreateS1TopControlSettings(String label)
        {
            return new PlugParamSetting
            {
                OnColor = new FinderColor(54, 84, 122),
                OffColor = new FinderColor(27, 34, 37),
                TextOffColor = new FinderColor(58, 117, 195),
                Label = label
            };
        }

        private static void InitColorDict()
        {
            // Default device.
            var deviceEntry = AddPlugParamDeviceEntry("");
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });
            deviceEntry.ParamSettings.Add("Global Bypass", new PlugParamSetting { OnColor = new FinderColor(204, 156, 107), IconName = "bypass" });

            deviceEntry = AddPlugParamDeviceEntry("Pro EQ");
            deviceEntry.ParamSettings.Add("Show Controls", CreateS1TopControlSettings("Band Controls"));
            deviceEntry.ParamSettings.Add("Show Dynamics", CreateS1TopControlSettings("Dynamics"));
            deviceEntry.ParamSettings.Add("High Quality", CreateS1TopControlSettings(""));
            deviceEntry.ParamSettings.Add("View Mode", CreateS1TopControlSettings("Curves"));
            deviceEntry.ParamSettings.Add("LF-Active", new PlugParamSetting { OnColor = new FinderColor(255, 120, 38), Label = "LF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("MF-Active", new PlugParamSetting { OnColor = new FinderColor(107, 224, 44), Label = "MF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("HF-Active", new PlugParamSetting { OnColor = new FinderColor(75, 212, 250), Label = "HF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("LMF-Active", new PlugParamSetting { OnColor = new FinderColor(245, 205, 58), Label = "LMF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("HMF-Active", new PlugParamSetting { OnColor = new FinderColor(70, 183, 130), Label = "HMF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("LC-Active", new PlugParamSetting { OnColor = new FinderColor(255, 74, 61), Label = "LC", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("HC-Active", new PlugParamSetting { OnColor = new FinderColor(158, 98, 255), Label = "HC", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("LLC-Active", new PlugParamSetting { OnColor = FinderColor.White, Label = "LLC", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Global Gain", new PlugParamSetting { OnColor = new FinderColor(200, 200, 200), Label = "Gain", Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Auto Gain", new PlugParamSetting { Label = "Auto" });
            AddLinked(deviceEntry, "LF-Gain", "LF-Active", label: "LF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "LF-Frequency", "LF-Active", label: "LF Freq");
            AddLinked(deviceEntry, "LF-Q", "LF-Active", label: "LF Q");
            AddLinked(deviceEntry, "MF-Gain", "MF-Active", label: "MF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "MF-Frequency", "MF-Active", label: "MF Freq");
            AddLinked(deviceEntry, "MF-Q", "MF-Active", label: "MF Q");
            AddLinked(deviceEntry, "HF-Gain", "HF-Active", label: "HF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "HF-Frequency", "HF-Active", label: "HF Freq");
            AddLinked(deviceEntry, "HF-Q", "HF-Active", label: "HF Q");
            AddLinked(deviceEntry, "LMF-Gain", "LMF-Active", label: "LMF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "LMF-Frequency", "LMF-Active", label: "LMF Freq");
            AddLinked(deviceEntry, "LMF-Q", "LMF-Active", label: "LMF Q");
            AddLinked(deviceEntry, "HMF-Gain", "HMF-Active", label: "HMF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "HMF-Frequency", "HMF-Active", label: "HMF Freq");
            AddLinked(deviceEntry, "HMF-Q", "HMF-Active", label: "HMF Q");
            AddLinked(deviceEntry, "LC-Frequency", "LC-Active", label: "LC Freq");
            AddLinked(deviceEntry, "HC-Frequency", "HC-Active", label: "HC Freq");
            deviceEntry.ParamSettings.Add("LF-Solo", new PlugParamSetting { OnColor = new FinderColor(224, 182, 69), Label = "LF Solo" });
            deviceEntry.ParamSettings.Add("MF-Solo", new PlugParamSetting { OnColor = new FinderColor(224, 182, 69), Label = "MF Solo" });
            deviceEntry.ParamSettings.Add("HF-Solo", new PlugParamSetting { OnColor = new FinderColor(224, 182, 69), Label = "HF Solo" });
            deviceEntry.ParamSettings.Add("LMF-Solo", new PlugParamSetting { OnColor = new FinderColor(224, 182, 69), Label = "LMF Solo" });
            deviceEntry.ParamSettings.Add("HMF-Solo", new PlugParamSetting { OnColor = new FinderColor(224, 182, 69), Label = "HMF Solo" });

            deviceEntry = AddPlugParamDeviceEntry("Fat Channel");
            deviceEntry.ParamSettings.Add("Loupedeck User Pages", new PlugParamSetting { UserMenuItems = ["Gate", "Comp", "EQ 1", "EQ 2", "Limiter"] });
            deviceEntry.ParamSettings.Add("Hi Pass Filter", new PlugParamSetting { Label = "Hi Pass" });
            deviceEntry.ParamSettings.Add("Gate On", new PlugParamSetting { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Gate ON" });
            deviceEntry.ParamSettings.Add("Range", new PlugParamSetting { OffColor = FinderColor.Black, LinkedParameter = "Expander", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Expander", new PlugParamSetting { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Key Listen", new PlugParamSetting { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Compressor On", new PlugParamSetting { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Cmpr ON" });
            deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { OffColor = FinderColor.Black, LinkedParameter = "Auto", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Release", new PlugParamSetting { OffColor = FinderColor.Black, LinkedParameter = "Auto", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Auto", new PlugParamSetting { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Soft Knee", new PlugParamSetting { OnColor = new FinderColor(193, 202, 214), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Peak Reduction", new PlugParamSetting { Label = "Pk Reductn" });
            deviceEntry.ParamSettings.Add("EQ On", new PlugParamSetting { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "EQ ON" });
            deviceEntry.ParamSettings.Add("Low On", new PlugParamSetting { OnColor = new FinderColor(241, 84, 220), Label = "LF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Low-Mid On", new PlugParamSetting { OnColor = new FinderColor(89, 236, 236), Label = "LMF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Hi-Mid On", new PlugParamSetting { OnColor = new FinderColor(241, 178, 84), Label = "HMF", ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("High On", new PlugParamSetting { OnColor = new FinderColor(122, 240, 79), Label = "HF", ShowUserButtonCircle = true });
            AddLinked(deviceEntry, "Low Gain", "Low On", label: "LF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Low Freq", "Low On", label: "LF Freq");
            AddLinked(deviceEntry, "Low Q", "Low On", label: "LMF Q");
            AddLinked(deviceEntry, "Low-Mid Gain", "Low-Mid On", label: "LMF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Low-Mid Freq", "Low-Mid On", label: "LMF Freq");
            AddLinked(deviceEntry, "Low-Mid Q", "Low-Mid On", label: "LMF Q");
            AddLinked(deviceEntry, "Hi-Mid Gain", "Hi-Mid On", label: "HMF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Hi-Mid Freq", "Hi-Mid On", label: "HMF Freq");
            AddLinked(deviceEntry, "Hi-Mid Q", "Hi-Mid On", label: "HMF Q");
            AddLinked(deviceEntry, "High Gain", "High On", label: "HF Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "High Freq", "High On", label: "HF Freq");
            AddLinked(deviceEntry, "High Q", "High On", label: "HF Q");
            deviceEntry.ParamSettings.Add("Low Boost", new PlugParamSetting { OnColor = new FinderColor(241, 84, 220) });
            deviceEntry.ParamSettings.Add("Low Atten", new PlugParamSetting { OnColor = new FinderColor(241, 84, 220) });
            deviceEntry.ParamSettings.Add("Low Frequency", new PlugParamSetting { Label = "LF Freq", OnColor = new FinderColor(241, 84, 220), DialSteps = 3 });
            deviceEntry.ParamSettings.Add("High Boost", new PlugParamSetting { OnColor = new FinderColor(122, 240, 79) });
            deviceEntry.ParamSettings.Add("High Atten", new PlugParamSetting { OnColor = new FinderColor(122, 240, 79) });
            deviceEntry.ParamSettings.Add("High Bandwidth", new PlugParamSetting { Label = "Bandwidth", OnColor = new FinderColor(122, 240, 79) });
            deviceEntry.ParamSettings.Add("Attenuation Select", new PlugParamSetting { Label = "Atten Sel", OnColor = new FinderColor(122, 240, 79), DialSteps = 2 });
            deviceEntry.ParamSettings.Add("Limiter On", new PlugParamSetting { OnColor = new FinderColor(250, 250, 193), TextOnColor = FinderColor.Black, Label = "Limiter ON" });

            deviceEntry = AddPlugParamDeviceEntry("Compressor");
            deviceEntry.ParamSettings.Add("LookAhead", CreateS1TopControlSettings(""));
            deviceEntry.ParamSettings.Add("Link Channels", CreateS1TopControlSettings("CH Link"));
            deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { OffColor = FinderColor.Black, LinkedParameter = "Auto Speed", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Release", new PlugParamSetting { OffColor = FinderColor.Black, LinkedParameter = "Auto Speed", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Auto Speed", new PlugParamSetting { Label = "Auto" });
            deviceEntry.ParamSettings.Add("Adaptive Speed", new PlugParamSetting { Label = "Adaptive" });
            deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { Label = "Makeup", OffColor = FinderColor.Black, LinkedParameter = "Auto Gain", LinkReversed = true });
            deviceEntry.ParamSettings.Add("Auto Gain", new PlugParamSetting { Label = "Auto" });
            deviceEntry.ParamSettings.Add("Sidechain LC-Freq", new PlugParamSetting { Label = "Side LC", OffColor = FinderColor.Black, LinkedParameter = "Sidechain Filter" });
            deviceEntry.ParamSettings.Add("Sidechain HC-Freq", new PlugParamSetting { Label = "Side HC", OffColor = FinderColor.Black, LinkedParameter = "Sidechain Filter" });
            deviceEntry.ParamSettings.Add("Sidechain Filter", new PlugParamSetting { Label = "Filter" });
            deviceEntry.ParamSettings.Add("Sidechain Listen", new PlugParamSetting { Label = "Listen" });
            deviceEntry.ParamSettings.Add("Swap Frequencies", new PlugParamSetting { Label = "Swap" });

            deviceEntry = AddPlugParamDeviceEntry("Limiter");
            deviceEntry.ParamSettings.Add("Mode ", new PlugParamSetting { Label = "A", LabelOn = "B", OnColor = new FinderColor(40, 40, 40), OffColor = new FinderColor(40, 40, 40),
                                                                             TextOnColor = new FinderColor(171, 197, 226), TextOffColor = new FinderColor(171, 197, 226) });
            deviceEntry.ParamSettings.Add("True Peak Limiting", CreateS1TopControlSettings("True Peak"));
            AddLinked(deviceEntry, "SoftClipper", "True Peak Limiting", label: " Soft Clip", linkReversed: true);
            deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { DialSteps = 2, HideValueBar = true } );

            deviceEntry = AddPlugParamDeviceEntry("Flanger");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103) });
            deviceEntry.ParamSettings.Add("Feedback", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("LFO Sync", new PlugParamSetting { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Depth", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });

            deviceEntry = AddPlugParamDeviceEntry("Phaser");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103) });
            deviceEntry.ParamSettings.Add("Center Frequency", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Label = "Center" });
            deviceEntry.ParamSettings.Add("Sweep Range", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Label = "Range" });
            deviceEntry.ParamSettings.Add("Stereo Spread", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Label = "Spread" });
            deviceEntry.ParamSettings.Add("Depth", new PlugParamSetting { OnColor = new FinderColor(238, 204, 103), Label = "Mix" });
            deviceEntry.ParamSettings.Add("LFO Sync", new PlugParamSetting { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Log. Sweep", new PlugParamSetting { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Soft", new PlugParamSetting { OnColor = new FinderColor(188, 198, 206), TextOnColor = FinderColor.Black });

            {
                deviceEntry = AddPlugParamDeviceEntry("Analog Delay");

                var ButtonOnColor = new FinderColor("ButtonOncolor", 255, 59, 58);
                var ButtonOffColor = new FinderColor("ButtonOffColor", 84, 18, 18);
                deviceEntry.ParamSettings.Add("Delay Beats", new PlugParamSetting { LinkedParameter = "Delay Sync", Label = "TIME", DialSteps = 40, OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(25, 28, 55), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Delay Time", new PlugParamSetting { LinkedParameter = "Delay Sync", LinkReversed = true, Label = "TIME", OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(25, 28, 55), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Delay Sync", new PlugParamSetting { Label = "SYNC", OnColor = ButtonOnColor, TextOnColor = FinderColor.White, OffColor = ButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Feedback Level", new PlugParamSetting { Label = "FEEDBACK", OnColor = new FinderColor(107, 113, 230), TextOnColor = FinderColor.White, OffColor = new FinderColor(46, 50, 84), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Feedback Boost", new PlugParamSetting { Label = "BOOST", OnColor = ButtonOnColor, TextOnColor = FinderColor.White, OffColor = ButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LFO Speed", new PlugParamSetting { LinkedParameter = "LFO Sync", LinkReversed = true, Label = "SPEED", OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White, OffColor = new FinderColor(26, 46, 29), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LFO Beats", new PlugParamSetting { LinkedParameter = "LFO Sync", Label = "SPEED", OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White, OffColor = new FinderColor(26, 46, 29), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LFO Width", new PlugParamSetting { Label = "AMOUNT", Mode = PlugParamSetting.PotMode.Symmetric, OnColor = new FinderColor(114, 202, 114), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("LFO Sync", new PlugParamSetting { Label = "SYNC", OnColor = ButtonOnColor, TextOnColor = FinderColor.White, OffColor = ButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LFO Waveform", new PlugParamSetting { Label = "", OnColor = new FinderColor(30, 51, 33), UserMenuItems = ["!ad_Triangle", "!ad_Sine", "!ad_Sawtooth", "!ad_Square"] });
                deviceEntry.ParamSettings.Add("Low Cut", new PlugParamSetting { Label = "LOW CUT", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("High Cut", new PlugParamSetting { Label = "HI CUT", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Saturation", new PlugParamSetting { Label = "DRIVE", OnColor = new FinderColor(145, 145, 23), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Speed", new PlugParamSetting { Label = "FACTOR", OnColor = new FinderColor(178, 103, 32), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Inertia", new PlugParamSetting { Label = "INERTIA", OnColor = new FinderColor(178, 103, 32), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Feedback Width", new PlugParamSetting { Label = "WIDTH", OnColor = new FinderColor(195, 81, 35), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Ping-Pong Swap", new PlugParamSetting { Label = "SWAP", OnColor = ButtonOnColor, TextOnColor = FinderColor.White, OffColor = ButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Ping-Pong Mode", new PlugParamSetting { Label = "PP", UserMenuItems = ["OFF", "SUM", "2-CH"], OnColor = new FinderColor(35, 23, 17), TextOnColor = FinderColor.White, OffColor = new FinderColor(35, 23, 17), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "DRY/WET", OnColor = new FinderColor(213, 68, 68), TextOnColor = FinderColor.White });
            }

            deviceEntry = AddPlugParamDeviceEntry("Alpine Desk");
            deviceEntry.ParamSettings.Add("Boost", new PlugParamSetting { DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Preamp On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            deviceEntry.ParamSettings.Add("Noise On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            deviceEntry.ParamSettings.Add("Noise Gate On", new PlugParamSetting { Label = "Noise Gate", OnColor = new FinderColor(0, 154, 144) });
            deviceEntry.ParamSettings.Add("Crosstalk", new PlugParamSetting { OnColor = new FinderColor(253, 202, 0) });
            deviceEntry.ParamSettings.Add("Crosstalk On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(0, 154, 144) });
            deviceEntry.ParamSettings.Add("Transformer", new PlugParamSetting { OnColor = new FinderColor(224, 22, 36), DialSteps = 1, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Master", new PlugParamSetting { Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Compensation", new PlugParamSetting { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(0, 154, 144), OffColor = new FinderColor(0, 154, 144), TextOffColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Character Enhancer", new PlugParamSetting { Label = "Character" });
            deviceEntry.ParamSettings.Add("Economy", new PlugParamSetting { Label = "Eco", OnColor = new FinderColor(0, 154, 144) });

            deviceEntry = AddPlugParamDeviceEntry("Brit Console");
            deviceEntry.ParamSettings.Add("Boost", new PlugParamSetting { DialSteps = 2, OnColor = new FinderColor(43, 128, 157), HideValueBar = true });
            deviceEntry.ParamSettings.Add("Drive", new PlugParamSetting { OnColor = new FinderColor(43, 128, 157) });
            deviceEntry.ParamSettings.Add("Preamp On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Noise", new PlugParamSetting { OnColor = new FinderColor(43, 128, 157) });
            deviceEntry.ParamSettings.Add("Noise On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Noise Gate On", new PlugParamSetting { Label = "Gate", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Crosstalk", new PlugParamSetting { OnColor = new FinderColor(43, 128, 157) });
            deviceEntry.ParamSettings.Add("Crosstalk On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Style", new PlugParamSetting { OnColor = new FinderColor(202, 74, 68), DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Harmonics", new PlugParamSetting { OnColor = new FinderColor(202, 74, 68) });
            deviceEntry.ParamSettings.Add("Compensation", new PlugParamSetting { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Character Enhancer", new PlugParamSetting { Label = "Character", OnColor = new FinderColor(43, 128, 157) });
            deviceEntry.ParamSettings.Add("Master", new PlugParamSetting { OnColor = new FinderColor(43, 128, 157), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Economy", new PlugParamSetting { Label = "Eco", OnColor = new FinderColor(202, 74, 68), ShowUserButtonCircle = true });

            deviceEntry = AddPlugParamDeviceEntry("CTC-1");
            deviceEntry.ParamSettings.Add("Boost", new PlugParamSetting { OnColor = new FinderColor(244, 104, 26) });
            deviceEntry.ParamSettings.Add("Preamp On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            deviceEntry.ParamSettings.Add("Noise", new PlugParamSetting { DialSteps = 4 });
            deviceEntry.ParamSettings.Add("Noise On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            deviceEntry.ParamSettings.Add("Noise Gate On", new PlugParamSetting { Label = "Gate", OnColor = new FinderColor(244, 104, 26) });
            deviceEntry.ParamSettings.Add("Preamp Type", new PlugParamSetting { Label = "Type", DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Crosstalk On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(244, 104, 26) });
            deviceEntry.ParamSettings.Add("Compensation", new PlugParamSetting { LabelOn = "Channel", Label = "Bus", OnColor = new FinderColor(69, 125, 159), OffColor = new FinderColor(69, 125, 159), TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Character Enhancer", new PlugParamSetting { Label = "Character" });
            deviceEntry.ParamSettings.Add("Master", new PlugParamSetting { Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Economy", new PlugParamSetting { Label = "Eco", OnColor = new FinderColor(69, 125, 159) });

            deviceEntry = AddPlugParamDeviceEntry("Porta Casstte");
            deviceEntry.ParamSettings.Add("Boost", new PlugParamSetting { OnColor = new FinderColor(251, 0, 3) });
            deviceEntry.ParamSettings.Add("Drive", new PlugParamSetting { OnColor = new FinderColor(226, 226, 226) });
            deviceEntry.ParamSettings.Add("Preamp On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Noise", new PlugParamSetting { OnColor = new FinderColor(226, 226, 226) });
            deviceEntry.ParamSettings.Add("Noise On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Noise Gate On", new PlugParamSetting { Label = "Gate", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Crosstalk", new PlugParamSetting { OnColor = new FinderColor(226, 226, 226) });
            deviceEntry.ParamSettings.Add("Crosstalk On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(251, 0, 3), ShowUserButtonCircle = true });
            deviceEntry.ParamSettings.Add("Pitch", new PlugParamSetting { OnColor = new FinderColor(144, 153, 153), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Compensation", new PlugParamSetting { LabelOn = "Channel", Label = "Bus", TextOffColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Character Enhancer", new PlugParamSetting { Label = "Character", OnColor = new FinderColor(226, 226, 226) });
            deviceEntry.ParamSettings.Add("Master", new PlugParamSetting { OnColor = new FinderColor(226, 226, 226), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Economy", new PlugParamSetting { Label = "Eco" });

            deviceEntry = AddPlugParamDeviceEntry("Console Shaper");
            deviceEntry.ParamSettings.Add("Preamp On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            deviceEntry.ParamSettings.Add("Noise", new PlugParamSetting { DialSteps = 4 });
            deviceEntry.ParamSettings.Add("Noise On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(114, 167, 204) });
            deviceEntry.ParamSettings.Add("Crosstalk On", new PlugParamSetting { Label = "ON", OnColor = new FinderColor(114, 167, 204) });

            // brainworx
            {
                var bxSslOnTransparency = 180;
                var bxSslLedRedColor = new FinderColor("BxSslLedRedColor", 255, 48, 24);
                var bxSslLedGreenColor = new FinderColor("BxSslLedGreenColor", 95, 255, 48);
                var bxSslLedOffColor = new FinderColor("BxSslLedOffColor", 50, 50, 50);
                var bxSslWhite = new FinderColor("BxSslWhite", 206, 206, 206);
                var bxSslRed = new FinderColor("BxSslRed", 184, 59, 55);
                var bxSslGreen = new FinderColor("BxSslGreen", 73, 109, 70);
                var bxSslBlue = new FinderColor("BxSslBlue", 70, 121, 162);
                var bxSslYellow = new FinderColor("BxSslYellow", 255, 240, 29);
                var bxSslBlack = new FinderColor("BxSslBlack", 53, 56, 56);
                var bxSslBrown = new FinderColor("BxSslBrown", 110, 81, 69);
                var bxSslButtonColor = new FinderColor("BxSslButtonOnColor", 198, 200, 195);
                var bxSslButtonOffColor = new FinderColor("BxSslButtonOffColor", 138, 140, 138);

                deviceEntry = AddPlugParamDeviceEntry("bx_console SSL 4000 E");
                deviceEntry.ParamSettings.Add("Loupedeck User Pages", new PlugParamSetting { UserMenuItems = ["EQ 1", "EQ 2", "DYN", "DN/MX"] });
                deviceEntry.ParamSettings.Add("EQ On/Off", new PlugParamSetting { Label = "EQ", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Dyn On/Off", new PlugParamSetting { Label = "DYN", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("HPF Frequency", new PlugParamSetting { Label = "HP Frq", LinkedParameter = "HPF On/Off", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("HPF On/Off", new PlugParamSetting { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LPF Frequency", new PlugParamSetting { Label = "LP Frq", LinkedParameter = "LPF On/Off", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LPF On/Off", new PlugParamSetting { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("FLT Position", new PlugParamSetting { Label = "DYN SC", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ High Gain", new PlugParamSetting { Label = "HF Gain", OnColor = bxSslRed, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Frequency", new PlugParamSetting { Label = "HF Freq", OnColor = bxSslRed, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Bell", new PlugParamSetting { Label = "SHELF", LabelOn = "BELL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Low Gain", new PlugParamSetting { Label = "LF Gain", LinkedParameter = "EQ Type", OnColor = bxSslBrown, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ Low Frequency", new PlugParamSetting { Label = "LF Freq", LinkedParameter = "EQ Type", OnColor = bxSslBrown, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ Low Bell", new PlugParamSetting { Label = "SHELF", LabelOn = "BELL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Type", new PlugParamSetting { Label = "EQ TYPE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ High Mid Gain", new PlugParamSetting { Label = "HMF Gain", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Mid Frequency", new PlugParamSetting { Label = "HMF Freq", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Mid Q", new PlugParamSetting { Label = "HMF Q", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ Low Mid Gain", new PlugParamSetting { Label = "LMF Gain", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("EQ Low Mid Frequency", new PlugParamSetting { Label = "LMF Freq", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("EQ Low Mid Q", new PlugParamSetting { Label = "LMF Q", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("LC Ratio", new PlugParamSetting { Label = "LC RATIO", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Threshold", new PlugParamSetting { Label = "LC THRES", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Release", new PlugParamSetting { Label = "LC REL", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Attack", new PlugParamSetting { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LC Link", new PlugParamSetting { Label = "LINK", OnColor = bxSslYellow, TextOnColor = FinderColor.Black, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Range", new PlugParamSetting { Label = "GE RANGE", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Threshold", new PlugParamSetting { Label = "GE THRES", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Release", new PlugParamSetting { Label = "GE REL", OnColor = bxSslGreen, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Attack", new PlugParamSetting { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Mode", new PlugParamSetting { Label = "EXP", LabelOn = "GATE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Invert", new PlugParamSetting { Label = "NORM", LabelOn = "INV", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Threshold Range", new PlugParamSetting { Label = "-30 dB", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LC Highpass", new PlugParamSetting { Label = "LC HPF", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC 2nd Thresh Level", new PlugParamSetting { Label = "LC REL2", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Mix", new PlugParamSetting { Label = "LC MIX", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("In Gain", new PlugParamSetting { Label = "IN GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Virtual Gain", new PlugParamSetting { Label = "V GAIN", OnColor = bxSslRed, OnTransparency = 180 });
                deviceEntry.ParamSettings.Add("Out Gain", new PlugParamSetting { Label = "OUT GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Phase", new PlugParamSetting { Label = "PHASE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = new FinderColor(100, 100, 100), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Stereo Mode", new PlugParamSetting { Label = "ANALOG", LabelOn = "DIGITAL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslYellow, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Position", new PlugParamSetting { Label = "", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, UserMenuItems = ["PRE DYN", "DYN SC", "POST DYN"] });
                deviceEntry.ParamSettings.Add("Dyn Key", new PlugParamSetting { Label = "D 2 KEY", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });

                deviceEntry = AddPlugParamDeviceEntry("bx_console SSL 4000 G");
                var bxSslCyan = new FinderColor("BxSslCyan", 54, 146, 124);
                var bxSslMagenta = new FinderColor("BxSslMagenta", 197, 80, 148);
                var bxSslOrange = new FinderColor("BxSslOrange", 218, 109, 44);
                deviceEntry.ParamSettings.Add("Loupedeck User Pages", new PlugParamSetting { UserMenuItems = ["EQ 1", "EQ 2", "DYN", "DN/MX"] });
                deviceEntry.ParamSettings.Add("EQ On/Off", new PlugParamSetting { Label = "EQ", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Dyn On/Off", new PlugParamSetting { Label = "DYN", OnColor = bxSslLedGreenColor, TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("HPF Frequency", new PlugParamSetting { Label = "HP Frq", LinkedParameter = "HPF On/Off", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("HPF On/Off", new PlugParamSetting { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LPF Frequency", new PlugParamSetting { Label = "LP Frq", LinkedParameter = "LPF On/Off", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LPF On/Off", new PlugParamSetting { Label = "Off", LabelOn = "On", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("FLT Position", new PlugParamSetting { Label = "DYN SC", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ High Gain", new PlugParamSetting { Label = "HF Gain", LinkedParameter = "EQ Type", OnColor = bxSslMagenta, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslRed, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ High Frequency", new PlugParamSetting { Label = "HF Freq", LinkedParameter = "EQ Type", OnColor = bxSslMagenta, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslRed, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ High Bell", new PlugParamSetting { Label = "SHELF", LabelOn = "BELL", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Low Gain", new PlugParamSetting { Label = "LF Gain", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslOrange, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ Low Frequency", new PlugParamSetting { Label = "LF Freq", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslOrange, OnTransparency = 255, TextOnColor = FinderColor.White, OffColor = bxSslBlack, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("EQ Low Bell", new PlugParamSetting { Label = "SHELF", LabelOn = "BELL", LinkedParameter = "EQ Type", LinkReversed = true, OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Type", new PlugParamSetting { Label = "EQ TYPE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ High Mid Gain", new PlugParamSetting { Label = "HMF Gain", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Mid Frequency", new PlugParamSetting { Label = "HMF Freq", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Mid Q", new PlugParamSetting { Label = "HMF Q", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("EQ High Mid x3", new PlugParamSetting { Label = "x3", LinkedParameter = "EQ Type", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Low Mid Gain", new PlugParamSetting { Label = "LMF Gain", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("EQ Low Mid Frequency", new PlugParamSetting { Label = "LMF Freq", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("EQ Low Mid Q", new PlugParamSetting { Label = "LMF Q", OnColor = bxSslBlue, OnTransparency = (Int32)(bxSslOnTransparency * 0.8) });
                deviceEntry.ParamSettings.Add("EQ Low Mid /3", new PlugParamSetting { Label = "/3", LinkedParameter = "EQ Type", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LC Ratio", new PlugParamSetting { Label = "LC RATIO", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Threshold", new PlugParamSetting { Label = "LC THRES", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Release", new PlugParamSetting { Label = "LC REL", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Attack", new PlugParamSetting { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LC Link", new PlugParamSetting { Label = "LINK", OnColor = bxSslYellow, TextOnColor = FinderColor.Black, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Range", new PlugParamSetting { Label = "GE RANGE", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Threshold", new PlugParamSetting { Label = "GE THRES", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Release", new PlugParamSetting { Label = "GE REL", OnColor = bxSslCyan, OnTransparency = bxSslOnTransparency });
                deviceEntry.ParamSettings.Add("GE Attack", new PlugParamSetting { Label = "FAST", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Mode", new PlugParamSetting { Label = "EXP", LabelOn = "GATE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Invert", new PlugParamSetting { Label = "NORM", LabelOn = "INV", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("GE Threshold Range", new PlugParamSetting { Label = "-30 dB", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslLedOffColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LC Highpass", new PlugParamSetting { Label = "LC HPF", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC 2nd Thresh Level", new PlugParamSetting { Label = "LC REL2", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("LC Mix", new PlugParamSetting { Label = "LC MIX", OnColor = bxSslWhite });
                deviceEntry.ParamSettings.Add("In Gain", new PlugParamSetting { Label = "IN GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Virtual Gain", new PlugParamSetting { Label = "V GAIN", OnColor = bxSslMagenta, OnTransparency = 180 });
                deviceEntry.ParamSettings.Add("Out Gain", new PlugParamSetting { Label = "OUT GAIN", OnColor = bxSslWhite, OnTransparency = 180, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Phase", new PlugParamSetting { Label = "PHASE", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = new FinderColor(100, 100, 100), TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Stereo Mode", new PlugParamSetting { Label = "ANALOG", LabelOn = "DIGITAL", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, OffColor = bxSslYellow, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("EQ Position", new PlugParamSetting { Label = "", OnColor = bxSslButtonColor, TextOnColor = FinderColor.Black, UserMenuItems = ["PRE DYN", "DYN SC", "POST DYN"] });
                deviceEntry.ParamSettings.Add("Dyn Key", new PlugParamSetting { Label = "D 2 KEY", OnColor = bxSslLedRedColor, TextOnColor = FinderColor.White, OffColor = bxSslButtonOffColor, TextOffColor = FinderColor.Black });
            }

            // Waves

            deviceEntry = AddPlugParamDeviceEntry("SSLGChannel");
            deviceEntry.ParamSettings.Add("HP Frq", new PlugParamSetting { OnColor = new FinderColor(220, 216, 207) });
            deviceEntry.ParamSettings.Add("LP Frq", new PlugParamSetting { OnColor = new FinderColor(220, 216, 207) });
            deviceEntry.ParamSettings.Add("FilterSplit", new PlugParamSetting { OnColor = new FinderColor(204, 191, 46), Label = "SPLIT" });
            deviceEntry.ParamSettings.Add("HF Gain", new PlugParamSetting { OnColor = new FinderColor(177, 53, 63), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("HF Frq", new PlugParamSetting { OnColor = new FinderColor(177, 53, 63) });
            deviceEntry.ParamSettings.Add("HMF X3", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Label = "HMFx3" });
            deviceEntry.ParamSettings.Add("LF Gain", new PlugParamSetting { OnColor = new FinderColor(180, 180, 180), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("LF Frq", new PlugParamSetting { OnColor = new FinderColor(180, 180, 180) });
            deviceEntry.ParamSettings.Add("LMF div3", new PlugParamSetting { OnColor = new FinderColor(22, 97, 120), Label = "LMF/3" });
            deviceEntry.ParamSettings.Add("HMF Gain", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("HMF Frq", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64) });
            deviceEntry.ParamSettings.Add("HMF Q", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("LMF Gain", new PlugParamSetting { OnColor = new FinderColor(22, 97, 120), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("LMF Frq", new PlugParamSetting { OnColor = new FinderColor(22, 97, 120) });
            deviceEntry.ParamSettings.Add("LMF Q", new PlugParamSetting { OnColor = new FinderColor(22, 97, 120), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("EQBypass", new PlugParamSetting { OnColor = new FinderColor(226, 61, 80), Label = "EQ BYP" });
            deviceEntry.ParamSettings.Add("EQDynamic", new PlugParamSetting { OnColor = new FinderColor(241, 171, 53), Label = "FLT DYN SC" });
            deviceEntry.ParamSettings.Add("CompRatio", new PlugParamSetting { OnColor = new FinderColor(220, 216, 207), Label = "C RATIO" });
            deviceEntry.ParamSettings.Add("CompThresh", new PlugParamSetting { OnColor = new FinderColor(220, 216, 207), Label = "C THRESH" });
            deviceEntry.ParamSettings.Add("CompRelease", new PlugParamSetting { OnColor = new FinderColor(220, 216, 207), Label = "C RELEASE" });
            deviceEntry.ParamSettings.Add("CompFast", new PlugParamSetting { Label = "F.ATK" });
            deviceEntry.ParamSettings.Add("ExpRange", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Label = "E RANGE" });
            deviceEntry.ParamSettings.Add("ExpThresh", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Label = "E THRESH" });
            deviceEntry.ParamSettings.Add("ExpRelease", new PlugParamSetting { OnColor = new FinderColor(27, 92, 64), Label = "E RELEASE" });
            deviceEntry.ParamSettings.Add("ExpAttack", new PlugParamSetting { Label = "F.ATK" });
            deviceEntry.ParamSettings.Add("ExpGate", new PlugParamSetting { Label = "GATE" });
            deviceEntry.ParamSettings.Add("DynamicBypass", new PlugParamSetting { OnColor = new FinderColor(226, 61, 80), Label = "DYN BYP" });
            deviceEntry.ParamSettings.Add("DynaminCHOut", new PlugParamSetting { OnColor = new FinderColor(241, 171, 53), Label = "DYN CH OUT" });
            deviceEntry.ParamSettings.Add("VUInOut", new PlugParamSetting { OnColor = new FinderColor(241, 171, 53), Label = "VU OUT" });

            deviceEntry = AddPlugParamDeviceEntry("RCompressor");
            deviceEntry.ParamSettings.Add("Threshold", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Ratio", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Release", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Trim", new PlugParamSetting { Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("ARC / Manual", new PlugParamSetting { Label = "ARC", LabelOn = "Manual", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            deviceEntry.ParamSettings.Add("Electro / Opto", new PlugParamSetting { Label = "Electro", LabelOn = "Opto", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });
            deviceEntry.ParamSettings.Add("Warm / Smooth", new PlugParamSetting { Label = "Warm", LabelOn = "Smooth", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0) });

            deviceEntry = AddPlugParamDeviceEntry("RBass");
            deviceEntry.ParamSettings.Add("Orig. In-Out", new PlugParamSetting { Label = "ORIG IN", OffColor = new FinderColor(230, 230, 230), TextOnColor = FinderColor.Black  });
            deviceEntry.ParamSettings.Add("Intensity", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Frequency", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Out Gain", new PlugParamSetting { Label = "Gain", OnColor = new FinderColor(243, 132, 1) });

            deviceEntry = AddPlugParamDeviceEntry("REQ");
            deviceEntry.ParamSettings.Add("Band1 On/Off", new PlugParamSetting { Label = "Band 1", OnColor = new FinderColor(196, 116, 100), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band1 Gain", "Band1 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band1 Frq", "Band1 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band1 Q", "Band1 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band1 Type", "Band1 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf", "!Hi-Pass", "!Low-RShelv"]);
            deviceEntry.ParamSettings.Add("Band2 On/Off", new PlugParamSetting { Label = "Band 2", OnColor = new FinderColor(175, 173, 29), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band2 Gain", "Band2 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band2 Frq", "Band2 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band2 Q", "Band2 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band2 Type", "Band2 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf"]);
            deviceEntry.ParamSettings.Add("Band3 On/Off", new PlugParamSetting { Label = "Band 3", OnColor = new FinderColor(57, 181, 74), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band3 Gain", "Band3 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band3 Frq", "Band3 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band3 Q", "Band3 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band3 Type", "Band3 On/Off", label: "", userMenuItems: ["!Bell", "!Low-Shelf"]);
            deviceEntry.ParamSettings.Add("Band4 On/Off", new PlugParamSetting { Label = "Band 4", OnColor = new FinderColor(56, 149, 203), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band4 Gain", "Band4 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band4 Frq", "Band4 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band4 Q", "Band4 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band4 Type", "Band4 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf"]);
            deviceEntry.ParamSettings.Add("Band5 On/Off", new PlugParamSetting { Label = "Band 5", OnColor = new FinderColor(130, 41, 141), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band5 Gain", "Band5 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band5 Frq", "Band5 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band5 Q", "Band5 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band5 Type", "Band5 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf"]);
            deviceEntry.ParamSettings.Add("Band6 On/Off", new PlugParamSetting { Label = "Band 6", OnColor = new FinderColor(199, 48, 105), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "Band6 Gain", "Band6 On/Off", label: "Gain", mode: PlugParamSetting.PotMode.Symmetric);
            AddLinked(deviceEntry, "Band6 Frq", "Band6 On/Off", label: "Freq");
            AddLinked(deviceEntry, "Band6 Q", "Band6 On/Off", label: "Q");
            AddLinked(deviceEntry, "Band6 Type", "Band6 On/Off", label: "", userMenuItems: ["!Bell", "!Hi-Shelf", "!Low-Pass", "!Hi-RShelv"]);
            deviceEntry.ParamSettings.Add("Fader left Out", new PlugParamSetting { Label = "Output", OnColor = new FinderColor(242, 101, 34) });
            deviceEntry.ParamSettings.Add("Gain-L (link)", new PlugParamSetting { Label = "Out L", OnColor = new FinderColor(242, 101, 34) });
            deviceEntry.ParamSettings.Add("Gain-R", new PlugParamSetting { Label = "Out R", OnColor = new FinderColor(242, 101, 34) });

            deviceEntry = AddPlugParamDeviceEntry("RVerb");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(244, 134, 2), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Dmp Low-F Ratio", new PlugParamSetting { Label = "Dmp Lo Rto", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("Dmp Low-F Freq", new PlugParamSetting { Label = "Dmp Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("Dmp Hi-F Ratio", new PlugParamSetting { Label = "Dmp Hi Rto", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("Dmp Hi-F Freq", new PlugParamSetting { Label = "Dmp Hi Frq", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("EQ Low-F Gain", new PlugParamSetting { Label = "EQ Lo Gn", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("EQ Low-F Freq", new PlugParamSetting { Label = "EQ Lo Frq", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("EQ Hi-F Gain", new PlugParamSetting { Label = "EQ Hi Gn", OnColor = new FinderColor(74, 149, 155) });
            deviceEntry.ParamSettings.Add("EQ Hi-F Freq", new PlugParamSetting { Label = "EQ Hi Frq", OnColor = new FinderColor(74, 149, 155) });

            deviceEntry = AddPlugParamDeviceEntry("L1 Limiter");
            deviceEntry.ParamSettings.Add("Threshold", new PlugParamSetting { OnColor = new FinderColor(243, 132, 1) });
            deviceEntry.ParamSettings.Add("Ceiling", new PlugParamSetting { OnColor = new FinderColor(255, 172, 66) });
            deviceEntry.ParamSettings.Add("Release", new PlugParamSetting { OnColor = new FinderColor(54, 206, 206) });
            deviceEntry.ParamSettings.Add("Auto Release", new PlugParamSetting { Label = "AUTO", OnColor = new FinderColor(54, 206, 206) });

            deviceEntry = AddPlugParamDeviceEntry("PuigTec EQP1A");
            deviceEntry.ParamSettings.Add("OnOff", new PlugParamSetting { Label = "IN", OnColor = new FinderColor(203, 53, 53) });
            deviceEntry.ParamSettings.Add("LowBoost", new PlugParamSetting { Label = "Low Boost", OnColor = new FinderColor(96, 116, 115) });
            deviceEntry.ParamSettings.Add("LowAtten", new PlugParamSetting { Label = "Low Atten", OnColor = new FinderColor(96, 116, 115) });
            deviceEntry.ParamSettings.Add("HiBoost", new PlugParamSetting { Label = "High Boost", OnColor = new FinderColor(96, 116, 115) });
            deviceEntry.ParamSettings.Add("HiAtten", new PlugParamSetting { Label = "High Atten", OnColor = new FinderColor(96, 116, 115) });
            deviceEntry.ParamSettings.Add("LowFrequency", new PlugParamSetting { Label = "Low Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 3 });
            deviceEntry.ParamSettings.Add("HiFrequency", new PlugParamSetting { Label = "High Freq", OnColor = new FinderColor(96, 116, 115), DialSteps = 6 });
            deviceEntry.ParamSettings.Add("Bandwidth", new PlugParamSetting { Label = "Bandwidth", OnColor = new FinderColor(96, 116, 115) });
            deviceEntry.ParamSettings.Add("AttenSelect", new PlugParamSetting { Label = "Atten Sel", OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            deviceEntry.ParamSettings.Add("Mains", new PlugParamSetting { OnColor = new FinderColor(96, 116, 115), DialSteps = 2 });
            deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { OnColor = new FinderColor(96, 116, 115), Mode = PlugParamSetting.PotMode.Symmetric });

            deviceEntry = AddPlugParamDeviceEntry("Smack Attack");
            deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { OnColor = new FinderColor(9, 217, 179), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("AttackSensitivity", new PlugParamSetting { Label = "Sensitivity", OnColor = new FinderColor(9, 217, 179) });
            deviceEntry.ParamSettings.Add("AttackDuration", new PlugParamSetting { Label = "Duration", OnColor = new FinderColor(9, 217, 179) });
            deviceEntry.ParamSettings.Add("AttackShape", new PlugParamSetting { Label = "", OnColor = new FinderColor(30, 30, 30), UserMenuItems = ["!sm_Needle", "!sm_Nail", "!sm_BluntA"], DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Sustain", new PlugParamSetting { OnColor = new FinderColor(230, 172, 5), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("SustainSensitivity", new PlugParamSetting { Label = "Sensitivity", OnColor = new FinderColor(230, 172, 5) });
            deviceEntry.ParamSettings.Add("SustainDuration", new PlugParamSetting { Label = "Duration", OnColor = new FinderColor(230, 172, 5) });
            deviceEntry.ParamSettings.Add("SustainShape", new PlugParamSetting { Label = "", OnColor = new FinderColor(30, 30, 30), UserMenuItems = ["!sm_Linear", "!sm_Nonlinear", "!sm_BluntS"], DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Guard", new PlugParamSetting { TextOnColor = new FinderColor(0, 198, 250), UserMenuItems = ["Off", "Clip", "Limit"], DialSteps = 2, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { OnColor = new FinderColor(0, 198, 250) });
            deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { OnColor = new FinderColor(0, 198, 250), Mode = PlugParamSetting.PotMode.Symmetric });

            {
                deviceEntry = AddPlugParamDeviceEntry("Brauer Motion");
                deviceEntry.ParamSettings.Add("Loupedeck User Pages", new PlugParamSetting { UserMenuItems = ["MAIN", "PNR 1", "PNR 2", "T/D 1", "T/D 2", "MIX"] });
                var path1Color = new FinderColor(139, 195, 74);
                var path2Color = new FinderColor(230, 74, 25);
                var bgColor = new FinderColor(12, 80, 124);
                var buttonBgColor = new FinderColor(3, 18, 31);
                var textColor = new FinderColor(105, 133, 157);
                var checkOnColor = new FinderColor(7, 152, 202);
                deviceEntry.ParamSettings.Add("Mute 1", new PlugParamSetting { Label = "MUTE 1", OnColor = path1Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path1Color });
                deviceEntry.ParamSettings.Add("Mute 2", new PlugParamSetting { Label = "MUTE 2", OnColor = path2Color, TextOnColor = buttonBgColor, OffColor = buttonBgColor, TextOffColor = path2Color });
                deviceEntry.ParamSettings.Add("Path 1 A Marker", new PlugParamSetting { Label = "A", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Path 1 B Marker", new PlugParamSetting { Label = "B", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Path 1 Start Marker", new PlugParamSetting { Label = "START", OnColor = bgColor, TextOnColor = path1Color, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Path 2 A Marker", new PlugParamSetting { Label = "A", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Path 2 B Marker", new PlugParamSetting { Label = "B", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Path 2 Start Marker", new PlugParamSetting { Label = "START", OnColor = bgColor, TextOnColor = path2Color, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Panner 1 Mode", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = path1Color, UserMenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
                deviceEntry.ParamSettings.Add("Panner 2 Mode", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = path2Color, UserMenuItems = ["SYNC", "FREE", "INPUT", "MANUAL"] });
                deviceEntry.ParamSettings.Add("Link", new PlugParamSetting { Label = "LINK", OnColor = buttonBgColor, TextOnColor = new FinderColor(0, 192, 255), OffColor = buttonBgColor, TextOffColor = new FinderColor(60, 60, 60) });
                deviceEntry.ParamSettings.Add("Path 1", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
                deviceEntry.ParamSettings.Add("Modulator 1", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
                deviceEntry.ParamSettings.Add("Reverse 1", new PlugParamSetting { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Mod Delay On/Off 1", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Speed 1", new PlugParamSetting { Label = "SPEED 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Offset 1", new PlugParamSetting { Label = "OFFSET 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Depth 1", new PlugParamSetting { Label = "DEPTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Width 1", new PlugParamSetting { Label = "WIDTH 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Pre Delay 1", new PlugParamSetting { Label = "PRE DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Mod Delay 1", new PlugParamSetting { Label = "MOD DLY 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Path 2", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["CLASSIC", "CIRCLE", "CIRC PHASE", "X LIGHTS"] });
                deviceEntry.ParamSettings.Add("Modulator 2", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["!bm_Sine", "!bm_Triangle", "!bm_Saw", "!bm_Square"] });
                deviceEntry.ParamSettings.Add("Reverse 2", new PlugParamSetting { Label = "REVERSE", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Mod Delay On/Off 2", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Speed 2", new PlugParamSetting { Label = "SPEED 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Offset 2", new PlugParamSetting { Label = "OFFSET 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Depth 2", new PlugParamSetting { Label = "DEPTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Width 2", new PlugParamSetting { Label = "WIDTH 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Pre Delay 2", new PlugParamSetting { Label = "PRE DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Mod Delay 2", new PlugParamSetting { Label = "MOD DLY 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Trigger Mode 1", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
                deviceEntry.ParamSettings.Add("Trigger A to B 1", new PlugParamSetting { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
                deviceEntry.ParamSettings.Add("Trigger Sensitivity 1", new PlugParamSetting { Label = "SENS 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Trigger HP 1", new PlugParamSetting { Label = "HOLD 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Dynamics 1", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
                deviceEntry.ParamSettings.Add("Drive 1", new PlugParamSetting { Label = "DRIVE 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Ratio 1", new PlugParamSetting { Label = "RATIO 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Dynamics HP 1", new PlugParamSetting { Label = "HP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Dynamics LP 1", new PlugParamSetting { Label = "LP 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Trigger Mode 2", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "SIMPLE", "ONE-SHOT", "RETRIGGER", "S-TRIG REV", "A TO B"] });
                deviceEntry.ParamSettings.Add("Trigger A to B 2", new PlugParamSetting { Label = "A TO B", LabelOn = "B TO A", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = buttonBgColor, TextOnColor = textColor });
                deviceEntry.ParamSettings.Add("Trigger Sensitivity 2", new PlugParamSetting { Label = "SENS 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Trigger HP 2", new PlugParamSetting { Label = "HOLD 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Dynamics 2", new PlugParamSetting { Label = "", OnColor = buttonBgColor, TextOnColor = new FinderColor(102, 157, 203), UserMenuItems = ["OFF", "PANNER 1", "DIRECT", "OUTPUT"] });
                deviceEntry.ParamSettings.Add("Drive 2", new PlugParamSetting { Label = "DRIVE 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Ratio 2", new PlugParamSetting { Label = "RATIO 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Dynamics HP 2", new PlugParamSetting { Label = "HP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Dynamics LP 2", new PlugParamSetting { Label = "LP 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Panner 1 Level", new PlugParamSetting { Label = "PANNER 1", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path1Color });
                deviceEntry.ParamSettings.Add("Panner 2 Level", new PlugParamSetting { Label = "PANNER 2", OnColor = bgColor, TextOnColor = textColor, BarOnColor = path2Color });
                deviceEntry.ParamSettings.Add("Input", new PlugParamSetting { Label = "INPUT", OnColor = bgColor, TextOnColor = textColor });
                deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", OnColor = bgColor, TextOnColor = textColor });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "MIX", OnColor = bgColor, TextOnColor = textColor });
                deviceEntry.ParamSettings.Add("Start/Stop 1", new PlugParamSetting { Label = "START 1", LabelOn = "STOP 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Start/Stop 2", new PlugParamSetting { Label = "START 2", LabelOn = "STOP 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Ex-SC 1", new PlugParamSetting { Label = "EXT SC 1", OffColor = buttonBgColor, TextOffColor = path1Color, OnColor = path1Color, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Ex-SC 2", new PlugParamSetting { Label = "EXT SC 2", OffColor = buttonBgColor, TextOffColor = path2Color, OnColor = path2Color, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Auto Reset", new PlugParamSetting { Label = "AUTO RESET", OffColor = buttonBgColor, TextOffColor = textColor, OnColor = checkOnColor, TextOnColor = FinderColor.Black });
            }
            {
                deviceEntry = AddPlugParamDeviceEntry("Abbey Road Chambers");
                var mixColor = new FinderColor("MixColor", 254, 251, 248);
                var mainCtrlColor = new FinderColor("MainCtrlColor", 52, 139, 125);
                var typeColor = new FinderColor("TypeColor", 90, 92, 88);
                var delayButtonColor = new FinderColor("DelayButtonColor", 38, 39, 37);
                var optionsOffBgColor = new FinderColor("OptionsOffBgColor", 100, 99, 95);
                deviceEntry.ParamSettings.Add("Input Level", new PlugParamSetting { Label = "INPUT", OnColor = mixColor });
                deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", OnColor = mixColor });
                deviceEntry.ParamSettings.Add("Reverb Mix", new PlugParamSetting { Label = "REVERB", OnColor = mixColor });
                deviceEntry.ParamSettings.Add("Dry/Wet", new PlugParamSetting { Label = "DRY/WET", OnColor = mixColor });
                deviceEntry.ParamSettings.Add("Reverb Time X", new PlugParamSetting { Label = "TIME X", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("RS106 Top Cut", new PlugParamSetting { Label = "TOP CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("RS106 Bass Cut", new PlugParamSetting { Label = "BASS CUT", OnColor = new FinderColor(222, 211, 202), TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("RS127 Gain", new PlugParamSetting { Label = "GAIN", Mode = PlugParamSetting.PotMode.Symmetric, OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("RS127 Freq", new PlugParamSetting { Label = "FREQ", OnColor = new FinderColor(123, 124, 119), TextOnColor = FinderColor.White, DialSteps = 2 });
                deviceEntry.ParamSettings.Add("Reverb Type", new PlugParamSetting { Label = "", OnColor = mixColor, TextOnColor = FinderColor.Black, UserMenuItems = ["CHMBR 2", "MIRROR", "STONE"] });
                AddLinked(deviceEntry, "Mic", "Reverb Type", label: "M", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["KM53", "MK2H"]);
                deviceEntry.ParamSettings.Add("Mic Position", new PlugParamSetting { Label = "P", OnColor = typeColor, TextOnColor = FinderColor.White, UserMenuItems = ["WALL", "CLASSIC", "ROOM"] });
                AddLinked(deviceEntry, "Speaker", "Reverb Type", label: "S", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["ALTEC", "B&W"]);
                AddLinked(deviceEntry, "Speaker Facing", "Reverb Type", label: "F", linkReversed: true, onColor: typeColor, textOnColor: FinderColor.White, userMenuItems: ["ROOM", "WALL"]);
                deviceEntry.ParamSettings.Add("Filters To Chamber On/Off", new PlugParamSetting { Label = "FILTERS", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("ARChambers On/Off", new PlugParamSetting { Label = "STEED", OnColor = optionsOffBgColor, TextOnColor = FinderColor.White, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Feedback", new PlugParamSetting { Label = "FEEDBACK", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Top Cut FB", new PlugParamSetting { Label = "TOP CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Mid FB", new PlugParamSetting { Label = "MID", Mode = PlugParamSetting.PotMode.Symmetric, OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Bass Cut FB", new PlugParamSetting { Label = "BASS CUT", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Drive On/Off", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = FinderColor.White, TextOnColor = FinderColor.Black, OffColor = optionsOffBgColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Drive", new PlugParamSetting { Label = "DRIVE", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Mod", new PlugParamSetting { Label = "MOD", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Time", new PlugParamSetting { Label = "DELAY L", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Time R", new PlugParamSetting { Label = "DELAY R", OnColor = mainCtrlColor, TextOnColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("Delay Link", new PlugParamSetting { Label = "LINK", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Delay Sync On/Off", new PlugParamSetting { Label = "SYNC", OnColor = delayButtonColor, TextOnColor = new FinderColor(255, 211, 10), OffColor = delayButtonColor, TextOffColor = FinderColor.Black });
            }
            {
                deviceEntry = AddPlugParamDeviceEntry("H-Delay");
                var hybridLineColor = new FinderColor("LineColor", 220, 148, 49);
                var hybridButtonOnColor = new FinderColor("ButtonOnColor", 142, 137, 116);
                var hybridButtonOffColor = new FinderColor("ButtonOffColor", 215, 209, 186);
                var hybridButtonTextOnColor = new FinderColor("TextOnColor", 247, 230, 25);
                var hybridButtonTextOffColor = new FinderColor("TextOffColor", BitmapColor.Black);
                deviceEntry.ParamSettings.Add("Sync", new PlugParamSetting { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["BPM", "HOST", "MS"] });
                deviceEntry.ParamSettings.Add("Delay BPM", new PlugParamSetting { Label = "DELAY", LinkedParameter = "Sync", LinkedStates = "0,1", DialSteps = 19, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Delay Sec", new PlugParamSetting { Label = "DELAY", LinkedParameter = "Sync", LinkedStates = "2", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Feedback", new PlugParamSetting { Label = "FEEDBACK", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "DRY/WET", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", Mode = PlugParamSetting.PotMode.Symmetric, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Analog", new PlugParamSetting { Label = "ANALOG", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black, DialSteps = 4 });
                deviceEntry.ParamSettings.Add("Phase L", new PlugParamSetting { Label = "PHASE L", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
                deviceEntry.ParamSettings.Add("Phase R", new PlugParamSetting { Label = "PHASE R", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
                deviceEntry.ParamSettings.Add("PingPong", new PlugParamSetting { Label = "PINGPONG", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
                deviceEntry.ParamSettings.Add("LoFi", new PlugParamSetting { Label = "LoFi", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
                deviceEntry.ParamSettings.Add("Depth", new PlugParamSetting { Label = "DEPTH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Rate", new PlugParamSetting { Label = "RATE", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("HiPass", new PlugParamSetting { Label = "HIPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("LoPass", new PlugParamSetting { Label = "LOPASS", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });

                deviceEntry = AddPlugParamDeviceEntry("H-Comp");
                deviceEntry.ParamSettings.Add("Threshold", new PlugParamSetting { Label = "THRESH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Meter Select", new PlugParamSetting { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["IN", "GR", "OUT"] });
                deviceEntry.ParamSettings.Add("Punch", new PlugParamSetting { Label = "PUNCH", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Ratio", new PlugParamSetting { Label = "RATIO", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Attack", new PlugParamSetting { Label = "ATTACK", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Limiter", new PlugParamSetting { Label = "LIMITER", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor });
                deviceEntry.ParamSettings.Add("Sync", new PlugParamSetting { Label = "", OnColor = hybridButtonOnColor, TextOnColor = hybridButtonTextOnColor, OffColor = hybridButtonOffColor, TextOffColor = hybridButtonTextOffColor, UserMenuItems = ["BPM", "HOST", "MS"] });
                deviceEntry.ParamSettings.Add("Release", new PlugParamSetting { LinkedParameter = "Sync", Label = "RELEASE", LinkedStates = "2", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("ReleaseBPM", new PlugParamSetting { LinkedParameter = "Sync", Label = "RELEASE", LinkedStates = "0,1", DialSteps = 19, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "DRY/WET", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", Mode = PlugParamSetting.PotMode.Symmetric, OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black });
                deviceEntry.ParamSettings.Add("Analog", new PlugParamSetting { Label = "ANALOG", OnColor = hybridLineColor, OnTransparency = 255, TextOnColor = FinderColor.Black, DialSteps = 4 });
            }

            deviceEntry = AddPlugParamDeviceEntry("Sibilance");
            deviceEntry.ParamSettings.Add("Monitor", new PlugParamSetting { OnColor = new FinderColor(0, 195, 230) });
            deviceEntry.ParamSettings.Add("Lookahead", new PlugParamSetting { OnColor = new FinderColor(0, 195, 230) });

            deviceEntry = AddPlugParamDeviceEntry("MondoMod");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(102, 255, 51) });
            deviceEntry.ParamSettings.Add("AM On/Off", new PlugParamSetting { Label = "AM", LabelOn = "AM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("FM On/Off", new PlugParamSetting { Label = "FM", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Pan On/Off", new PlugParamSetting { Label = "Pan", LabelOn = "FM ON", OnColor = new FinderColor(102, 255, 51), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Sync On/Off", new PlugParamSetting { Label = "Manual", LabelOn = "Auto", OnColor = new FinderColor(181, 214, 165), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Waveform", new PlugParamSetting { OnColor = new FinderColor(102, 255, 51), DialSteps = 4, HideValueBar = true });

            deviceEntry = AddPlugParamDeviceEntry("LoAir");
            deviceEntry.ParamSettings.Add("LoAir", new PlugParamSetting { Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Lo", new PlugParamSetting { Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Align", new PlugParamSetting { OnColor = new FinderColor(206, 175, 43), TextOnColor = FinderColor.Black });

            deviceEntry = AddPlugParamDeviceEntry("CLA Unplugged");
            deviceEntry.ParamSettings.Add("Bass Color", new PlugParamSetting { Label = "", UserMenuItems = [ "OFF", "SUB", "LOWER", "UPPER" ] });
            deviceEntry.ParamSettings.Add("Bass", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Treble Color", new PlugParamSetting { Label = "", UserMenuItems = ["OFF", "BITE", "TOP", "ROOF"] });
            deviceEntry.ParamSettings.Add("Treble", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Compress", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Compress Color", new PlugParamSetting { Label = "", UserMenuItems = ["OFF", "PUSH", "SPANK", "WALL"] });
            deviceEntry.ParamSettings.Add("Reverb 1", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Reverb 1 Color", new PlugParamSetting { Label = "", UserMenuItems = ["OFF", "ROOM", "HALL", "CHAMBER"] });
            deviceEntry.ParamSettings.Add("Reverb 2", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Reverb 2 Color", new PlugParamSetting { Label = "", UserMenuItems = ["OFF", "TIGHT", "LARGE", "CANYON"] });
            deviceEntry.ParamSettings.Add("Delay", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Delay Color", new PlugParamSetting { Label = "", UserMenuItems = ["OFF", "SLAP", "EIGHT", "QUARTER"] });
            deviceEntry.ParamSettings.Add("Sensitivity", new PlugParamSetting { Label = "Input Sens", OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { OnColor = new FinderColor(210, 209, 96), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("PreDelay 1", new PlugParamSetting { Label = "Pre Rvrb 1", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            deviceEntry.ParamSettings.Add("PreDelay 2", new PlugParamSetting { Label = "Pre Rvrb 2", OnColor = new FinderColor(210, 209, 96), DialSteps = 13 });
            deviceEntry.ParamSettings.Add("PreDelay 1 On", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("PreDelay 2 On", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(210, 209, 96), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Direct", new PlugParamSetting { OnColor = new FinderColor(80, 80, 80), OffColor = new FinderColor(240, 228, 87),
                                                                           TextOnColor = FinderColor.Black, TextOffColor = FinderColor.Black });

            deviceEntry = AddPlugParamDeviceEntry("CLA-76");
            deviceEntry.ParamSettings.Add("Revision", new PlugParamSetting { Label = "Bluey", LabelOn = "Blacky", OffColor = new FinderColor(62, 141, 180), TextOffColor = FinderColor.White, 
                                                                              OnColor = FinderColor.Black, TextOnColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Ratio", new PlugParamSetting { UserMenuItems = ["20", "12", "8", "4", "ALL"] });
            deviceEntry.ParamSettings.Add("Analog", new PlugParamSetting { Label = "A", UserMenuItems = ["50Hz", "60Hz", "Off"], TextOnColor = new FinderColor(254, 246, 212) });
            deviceEntry.ParamSettings.Add("Meter", new PlugParamSetting { UserMenuItems = ["GR", "IN", "OUT"] });
            deviceEntry.ParamSettings.Add("Comp Off", new PlugParamSetting { OnColor = new FinderColor(162, 38, 38), TextOnColor = FinderColor.White });

            // Black Rooster Audio

            {
                deviceEntry = AddPlugParamDeviceEntry("VLA-2A");
                var barOnColor = new FinderColor("BarOnColor", 242, 202, 75);
                var knobOnColor = new FinderColor("KnobOnColor", 210, 204, 182);
                deviceEntry.ParamSettings.Add("Power", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                deviceEntry.ParamSettings.Add("Mode", new PlugParamSetting { Label = "COMPRESS", LabelOn = "LIMIT", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("ExSidech", new PlugParamSetting { Label = "EXT SC OFF", LabelOn = "EXT SC ON", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("CellSel", new PlugParamSetting { Label = "CEL", UserMenuItems = ["A", "B", "C"] });
                deviceEntry.ParamSettings.Add("VULevel", new PlugParamSetting { Label = "VU", UserMenuItems = ["IN", "GR", "OUT"] });
                deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { Label = "GAIN", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("PeakRedc", new PlugParamSetting { Label = "PK REDCT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Emphasis", new PlugParamSetting { Label = "EMPHASIS", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Makeup", new PlugParamSetting { Label = "MAKEUP", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "MIX", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
            }
            {
                deviceEntry = AddPlugParamDeviceEntry("VLA-3A");
                var barOnColor = new FinderColor("BarOnColor", BitmapColor.White);
                deviceEntry.ParamSettings.Add("Power", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                deviceEntry.ParamSettings.Add("Mode", new PlugParamSetting { Label = "COMPRESS", LabelOn = "LIMIT", OffColor = FinderColor.Black, TextOffColor = FinderColor.White });
                deviceEntry.ParamSettings.Add("VULevel", new PlugParamSetting { Label = "VU", UserMenuItems = ["IN", "GR", "OUT"] });
                deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { Label = "GAIN", BarOnColor = barOnColor });
                deviceEntry.ParamSettings.Add("PeakRedc", new PlugParamSetting { Label = "PK REDCT", BarOnColor = barOnColor });
            }
            {
                deviceEntry = AddPlugParamDeviceEntry("RO-140");
                var barOnColor = new FinderColor("BarOnColor", 255, 161, 75);
                var knobOnColor = new FinderColor("KnobOnColor", 199, 183, 160);
                deviceEntry.ParamSettings.Add("Power", new PlugParamSetting { Label = "OFF", LabelOn = "ON", OnColor = new FinderColor(212, 86, 27) });
                deviceEntry.ParamSettings.Add("Material", new PlugParamSetting { Label = "", UserMenuItems = ["STEEL", "ALUMINUM", "BRONZE", "SILVER", "GOLD", "TITANIUM"] });
                deviceEntry.ParamSettings.Add("Mode", new PlugParamSetting { Label = "", UserMenuItems = ["MONO", "MONO>ST", "STEREO" ] });
                deviceEntry.ParamSettings.Add("Low", new PlugParamSetting { Label = "LOW", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Mid", new PlugParamSetting { Label = "MID", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("High", new PlugParamSetting { Label = "HIGH", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Damper", new PlugParamSetting { Label = "DAMPER", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = new FinderColor(200, 155, 127), OnTransparency = 255, DialSteps = 9 });
                deviceEntry.ParamSettings.Add("PreDelay", new PlugParamSetting { Label = "PRE/DELAY", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Size", new PlugParamSetting { Label = "SIZE", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("BassCut", new PlugParamSetting { Label = "BASS CUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Mix", new PlugParamSetting { Label = "MIX", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Input", new PlugParamSetting { Label = "INPUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
                deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", OnColor = knobOnColor, TextOnColor = FinderColor.Black, BarOnColor = barOnColor, OnTransparency = 255 });
            }

            // Analog Obsession
            deviceEntry = AddPlugParamDeviceEntry("Rare");
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { Label = "IN", OnColor = new FinderColor(191, 0, 22) });
            deviceEntry.ParamSettings.Add("Low Boost", new PlugParamSetting { Label = "Low Boost", OnColor = new FinderColor(93, 161, 183) });
            deviceEntry.ParamSettings.Add("Low Atten", new PlugParamSetting { Label = "Low Atten", OnColor = new FinderColor(93, 161, 183) });
            deviceEntry.ParamSettings.Add("High Boost", new PlugParamSetting { Label = "High Boost", OnColor = new FinderColor(93, 161, 183) });
            deviceEntry.ParamSettings.Add("High Atten", new PlugParamSetting { Label = "High Atten", OnColor = new FinderColor(93, 161, 183) });
            deviceEntry.ParamSettings.Add("Low Frequency", new PlugParamSetting { Label = "Low Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 3 });
            deviceEntry.ParamSettings.Add("High Freqency", new PlugParamSetting { Label = "High Freq", OnColor = new FinderColor(93, 161, 183), DialSteps = 6 });
            deviceEntry.ParamSettings.Add("High Bandwidth", new PlugParamSetting { Label = "Bandwidth", OnColor = new FinderColor(93, 161, 183) });
            deviceEntry.ParamSettings.Add("High Atten Freqency", new PlugParamSetting { Label = "Atten Sel", OnColor = new FinderColor(93, 161, 183), DialSteps = 2 });

            deviceEntry = AddPlugParamDeviceEntry("LALA");
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { Label = "OFF", LabelOn = "ON", TextOnColor = new FinderColor(0, 0, 0), TextOffColor = new FinderColor(0, 0, 0), OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("Gain", new PlugParamSetting { Label = "GAIN", OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("Peak Reduction", new PlugParamSetting { Label = "REDUCTION", OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("Mode", new PlugParamSetting { Label = "LIMIT", LabelOn = "COMP", TextOnColor = new FinderColor(0, 0, 0), 
                                                                                                   TextOffColor = new FinderColor(0, 0, 0),
                                                                                                   OnColor = new FinderColor(185, 182, 163),
                                                                                                   OffColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("1:3", new PlugParamSetting { Label = "MIX", OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("2:1", new PlugParamSetting { Label = "HPF", OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("MF", new PlugParamSetting { OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("MG", new PlugParamSetting { OnColor = new FinderColor(185, 182, 163), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("HF", new PlugParamSetting { OnColor = new FinderColor(185, 182, 163) });
            deviceEntry.ParamSettings.Add("External Sidechain", new PlugParamSetting { Label = "SIDECHAIN", OnColor = new FinderColor(185, 182, 163) });

            deviceEntry = AddPlugParamDeviceEntry("FETish");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(24, 86, 119) });
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { Label = "IN", OnColor = new FinderColor(24, 86, 119) });
            deviceEntry.ParamSettings.Add("Input", new PlugParamSetting { Label = "INPUT", OnColor = new FinderColor(186, 175, 176) });
            deviceEntry.ParamSettings.Add("Output", new PlugParamSetting { Label = "OUTPUT", OnColor = new FinderColor(186, 175, 176) });
            deviceEntry.ParamSettings.Add("Ratio", new PlugParamSetting { OnColor = new FinderColor(186, 175, 176), DialSteps = 16 });
            deviceEntry.ParamSettings.Add("Sidechain", new PlugParamSetting { Label = "EXT", OnColor = new FinderColor(24, 86, 119) });
            deviceEntry.ParamSettings.Add("Mid Frequency", new PlugParamSetting { Label = "MF", OnColor = new FinderColor(24, 86, 119) });
            deviceEntry.ParamSettings.Add("Mid Gain", new PlugParamSetting { Label = "MG", OnColor = new FinderColor(24, 86, 119), Mode = PlugParamSetting.PotMode.Symmetric });

            deviceEntry = AddPlugParamDeviceEntry("dBComp");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(105, 99, 94) });
            deviceEntry.ParamSettings.Add("Output Gain", new PlugParamSetting { Label = "Output", OnColor = new FinderColor(105, 99, 94) });
            deviceEntry.ParamSettings.Add("1:4U", new PlugParamSetting { Label = "EXT SC", OnColor = new FinderColor(208, 207, 203), TextOnColor = FinderColor.Black });

            deviceEntry = AddPlugParamDeviceEntry("BUSTERse");
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { Label = "MAIN", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Turbo", new PlugParamSetting { Label = "TURBO", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("XFormer", new PlugParamSetting { Label = "XFORMER", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Threshold", new PlugParamSetting { Label = "THRESH", OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("Attack Time", new PlugParamSetting { Label = "ATTACK", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            deviceEntry.ParamSettings.Add("Ratio", new PlugParamSetting { Label = "RATIO", OnColor = new FinderColor(174, 164, 167), DialSteps = 5 });
            deviceEntry.ParamSettings.Add("Make-Up Gain", new PlugParamSetting { Label = "MAKE-UP", OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("Release Time", new PlugParamSetting { Label = "RELEASE", OnColor = new FinderColor(174, 164, 167), DialSteps = 4 });
            deviceEntry.ParamSettings.Add("Compressor Mix", new PlugParamSetting { Label = "MIX", OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("External Sidechain", new PlugParamSetting { Label = "EXT", OnColor = new FinderColor(255, 254, 228), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("HF", new PlugParamSetting { OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("Mid Gain", new PlugParamSetting { Label = "MID", OnColor = new FinderColor(174, 164, 167), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("HPF", new PlugParamSetting { OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("Boost", new PlugParamSetting { Label = "TR BOOST", OnColor = new FinderColor(174, 164, 167) });
            deviceEntry.ParamSettings.Add("Transient Tilt", new PlugParamSetting { Label = "TR TILT", OnColor = new FinderColor(174, 164, 167), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Transient Mix", new PlugParamSetting { Label = "TR MIX", OnColor = new FinderColor(174, 164, 167) });

            deviceEntry = AddPlugParamDeviceEntry("BritChannel");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(141, 134, 137), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Bypass", new PlugParamSetting { Label = "IN", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Mic Pre", new PlugParamSetting { Label = "MIC", OnColor = new FinderColor(241, 223, 219), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Mid Freq", new PlugParamSetting { OnColor = new FinderColor(141, 134, 137), DialSteps = 6 });
            deviceEntry.ParamSettings.Add("Low Freq", new PlugParamSetting { OnColor = new FinderColor(141, 134, 137), DialSteps = 4 });
            deviceEntry.ParamSettings.Add("HighPass", new PlugParamSetting { Label = "High Pass", OnColor = new FinderColor(49, 81, 119), DialSteps = 4 });
            deviceEntry.ParamSettings.Add("Preamp Gain", new PlugParamSetting { Label = "PRE GAIN", OnColor = new FinderColor(160, 53, 50), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Output Trim", new PlugParamSetting { Label = "OUT TRIM", OnColor = new FinderColor(124, 117, 115), Mode = PlugParamSetting.PotMode.Symmetric });

            // Acon Digital

            deviceEntry = AddPlugParamDeviceEntry("Acon Digital Equalize 2");
            deviceEntry.ParamSettings.Add("Gain-bandwidth link", new PlugParamSetting { Label = "Link", OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Solo 1", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 1", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 1", new PlugParamSetting { OnColor = new FinderColor(221, 125, 125) });
            deviceEntry.ParamSettings.Add("Gain 1", new PlugParamSetting { OnColor = new FinderColor(221, 125, 125) });
            deviceEntry.ParamSettings.Add("Filter type 1", new PlugParamSetting { Label = "Filter 1", OnColor = new FinderColor(221, 125, 125), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 1", new PlugParamSetting { Label = "Bandwidth 1", OnColor = new FinderColor(221, 125, 125) });
            deviceEntry.ParamSettings.Add("Slope 1", new PlugParamSetting { OnColor = new FinderColor(221, 125, 125) });
            deviceEntry.ParamSettings.Add("Resonance 1", new PlugParamSetting { OnColor = new FinderColor(221, 125, 125) });
            deviceEntry.ParamSettings.Add("Solo 2", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 2", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 2", new PlugParamSetting { OnColor = new FinderColor(204, 133, 61) });
            deviceEntry.ParamSettings.Add("Gain 2", new PlugParamSetting { OnColor = new FinderColor(204, 133, 61) });
            deviceEntry.ParamSettings.Add("Filter type 2", new PlugParamSetting { Label = "Filter 2", OnColor = new FinderColor(204, 133, 61), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 2", new PlugParamSetting { Label = "Bandwidth 2", OnColor = new FinderColor(204, 133, 61) });
            deviceEntry.ParamSettings.Add("Slope 2", new PlugParamSetting { OnColor = new FinderColor(204, 133, 61) });
            deviceEntry.ParamSettings.Add("Resonance 2", new PlugParamSetting { OnColor = new FinderColor(204, 133, 61) });
            deviceEntry.ParamSettings.Add("Solo 3", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 3", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 3", new PlugParamSetting { OnColor = new FinderColor(204, 204, 61) });
            deviceEntry.ParamSettings.Add("Gain 3", new PlugParamSetting { OnColor = new FinderColor(204, 204, 61) });
            deviceEntry.ParamSettings.Add("Filter type 3", new PlugParamSetting { Label = "Filter 3", OnColor = new FinderColor(204, 204, 61), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 3", new PlugParamSetting { Label = "Bandwidth 3", OnColor = new FinderColor(204, 204, 61) });
            deviceEntry.ParamSettings.Add("Slope 3", new PlugParamSetting { OnColor = new FinderColor(204, 204, 61) });
            deviceEntry.ParamSettings.Add("Resonance 3", new PlugParamSetting { OnColor = new FinderColor(204, 204, 61) });
            deviceEntry.ParamSettings.Add("Solo 4", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 4", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 4", new PlugParamSetting { OnColor = new FinderColor(61, 204, 61) });
            deviceEntry.ParamSettings.Add("Gain 4", new PlugParamSetting { OnColor = new FinderColor(61, 204, 61) });
            deviceEntry.ParamSettings.Add("Filter type 4", new PlugParamSetting { Label = "Filter 4", OnColor = new FinderColor(61, 204, 61), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 4", new PlugParamSetting { Label = "Bandwidth 4", OnColor = new FinderColor(61, 204, 61) });
            deviceEntry.ParamSettings.Add("Slope 4", new PlugParamSetting { OnColor = new FinderColor(61, 204, 61) });
            deviceEntry.ParamSettings.Add("Resonance 4", new PlugParamSetting { OnColor = new FinderColor(61, 204, 61) });
            deviceEntry.ParamSettings.Add("Solo 5", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 5", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 5", new PlugParamSetting { OnColor = new FinderColor(61, 204, 133) });
            deviceEntry.ParamSettings.Add("Gain 5", new PlugParamSetting { OnColor = new FinderColor(61, 204, 133) });
            deviceEntry.ParamSettings.Add("Filter type 5", new PlugParamSetting { Label = "Filter 5", OnColor = new FinderColor(61, 204, 133), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 5", new PlugParamSetting { Label = "Bandwidth 5", OnColor = new FinderColor(61, 204, 133) });
            deviceEntry.ParamSettings.Add("Slope 5", new PlugParamSetting { OnColor = new FinderColor(61, 204, 133) });
            deviceEntry.ParamSettings.Add("Resonance 5", new PlugParamSetting { OnColor = new FinderColor(61, 204, 133) });
            deviceEntry.ParamSettings.Add("Solo 6", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Bypass 6", new PlugParamSetting { OnColor = new FinderColor(230, 159, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Frequency 6", new PlugParamSetting { OnColor = new FinderColor(173, 221, 125) });
            deviceEntry.ParamSettings.Add("Gain 6", new PlugParamSetting { OnColor = new FinderColor(173, 221, 125) });
            deviceEntry.ParamSettings.Add("Filter type 6", new PlugParamSetting { Label = "Filter 6", OnColor = new FinderColor(173, 221, 125), DialSteps = 8, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Band width 6", new PlugParamSetting { Label = "Bandwidth 6 ", OnColor = new FinderColor(173, 221, 125) });
            deviceEntry.ParamSettings.Add("Slope 6", new PlugParamSetting { OnColor = new FinderColor(173, 221, 125) });
            deviceEntry.ParamSettings.Add("Resonance 6", new PlugParamSetting { OnColor = new FinderColor(173, 221, 125) });

            deviceEntry = AddPlugParamDeviceEntry("Acon Digital Verberate 2");
            deviceEntry.ParamSettings.Add("Dry Mute", new PlugParamSetting { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Reverb Mute", new PlugParamSetting { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("ER Mute", new PlugParamSetting { Label = "Mute", OnColor = new FinderColor(212, 160, 40), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Freeze", new PlugParamSetting { OnColor = new FinderColor(230, 173, 43), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Stereo Spread", new PlugParamSetting { Label = "Spread" });
            deviceEntry.ParamSettings.Add("EarlyReflectionsType", new PlugParamSetting { Label = "ER Type", DialSteps = 14, HideValueBar = true });
            deviceEntry.ParamSettings.Add("Algorithm", new PlugParamSetting { Label = "Vivid", LabelOn = "Legacy", TextOnColor = FinderColor.White, TextOffColor = FinderColor.White });
            deviceEntry.ParamSettings.Add("Decay High Cut Enable", new PlugParamSetting { Label = "Decay HC", OnColor = new FinderColor(221, 85, 255) });
            AddLinked(deviceEntry, "Decay High Cut Frequency", "Decay High Cut Enable", label: "Freq");
            AddLinked(deviceEntry, "Decay High Cut Slope", "Decay High Cut Enable", label: "Slope");
            deviceEntry.ParamSettings.Add("EQ High Cut Enable", new PlugParamSetting { Label = "EQ HC", OnColor = new FinderColor(221, 85, 255) });
            AddLinked(deviceEntry, "EQ High Cut Frequency", "EQ High Cut Enable", label: "Freq");
            AddLinked(deviceEntry, "EQ High Cut Slope", "EQ High Cut Enable", label: "Slope");


            // AXP

            deviceEntry = AddPlugParamDeviceEntry("AXP SoftAmp PSA");
            deviceEntry.ParamSettings.Add("Enable", new PlugParamSetting { Label = "ENABLE" });
            deviceEntry.ParamSettings.Add("Preamp", new PlugParamSetting { Label = "PRE-AMP", OnColor = new FinderColor(200, 200, 200) });
            deviceEntry.ParamSettings.Add("Asymm", new PlugParamSetting { Label = "ASYMM", OnColor = new FinderColor(237, 244, 1), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Buzz", new PlugParamSetting { Label = "BUZZ", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Punch", new PlugParamSetting { Label = "PUNCH", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("Crunch", new PlugParamSetting { Label = "CRUNCH", OnColor = new FinderColor(200, 200, 200) });
            deviceEntry.ParamSettings.Add("SoftClip", new PlugParamSetting { Label = "SOFT CLIP", OnColor = new FinderColor(234, 105, 30), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Drive", new PlugParamSetting { Label = "DRIVE", OnColor = new FinderColor(200, 200, 200) });
            deviceEntry.ParamSettings.Add("Level", new PlugParamSetting { Label = "LEVEL", OnColor = new FinderColor(200, 200, 200) });
            deviceEntry.ParamSettings.Add("Limiter", new PlugParamSetting { Label = "LIMITER", OnColor = new FinderColor(237, 0, 0), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Low", new PlugParamSetting { Label = "LOW", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("High", new PlugParamSetting { Label = "HIGH", OnColor = new FinderColor(200, 200, 200), Mode = PlugParamSetting.PotMode.Symmetric });
            deviceEntry.ParamSettings.Add("SpkReso", new PlugParamSetting { Label = "SHAPE", OnColor = new FinderColor(120, 120, 120) });
            deviceEntry.ParamSettings.Add("SpkRoll", new PlugParamSetting { Label = "ROLL-OFF", OnColor = new FinderColor(120, 120, 120) });
            deviceEntry.ParamSettings.Add("PSI_En", new PlugParamSetting { Label = "PSI DNS", OnColor = new FinderColor(10, 178, 255), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "PSI_Thr", "PSI_En", label: "THRESHOLD");
            deviceEntry.ParamSettings.Add("OS_Enab", new PlugParamSetting { Label = "SQUEEZO", OnColor = new FinderColor(209, 155, 104), TextOnColor = FinderColor.Black });
            AddLinked(deviceEntry, "OS_Gain", "OS_Enab", label: "GAIN");
            AddLinked(deviceEntry, "OS_Bias", "OS_Enab", label: "BIAS");
            AddLinked(deviceEntry, "OS_Level", "OS_Enab", label: "LEVEL");

            // Izotope

            deviceEntry = AddPlugParamDeviceEntry("Neutron 4 Transient Shaper");
            deviceEntry.ParamSettings.Add("TS B1 Attack", new PlugParamSetting { Label = "1: Attack", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B1 Sustain", new PlugParamSetting { Label = "1: Sustain", OnColor = new FinderColor(255, 96, 28), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B1 Bypass", new PlugParamSetting { Label = "Bypass", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("TS B2 Attack", new PlugParamSetting { Label = "2: Attack", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B2 Sustain", new PlugParamSetting { Label = "2: Sustain", OnColor = new FinderColor(63, 191, 173), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B2 Bypass", new PlugParamSetting { Label = "Bypass", OnColor = new FinderColor(63, 191, 173), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("TS B3 Attack", new PlugParamSetting { Label = "3: Attack", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B3 Sustain", new PlugParamSetting { Label = "3: Sustain", OnColor = new FinderColor(196, 232, 107), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("TS B3 Bypass", new PlugParamSetting { Label = "Bypass", OnColor = new FinderColor(196, 232, 107), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Global Input Gain", new PlugParamSetting { Label = "In" });
            deviceEntry.ParamSettings.Add("Global Output Gain", new PlugParamSetting { Label = "Out" });
            deviceEntry.ParamSettings.Add("Sum to Mono", new PlugParamSetting { Label = "Mono", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Swap Channels", new PlugParamSetting { Label = "Swap", OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("Invert Phase", new PlugParamSetting { OnColor = new FinderColor(255, 96, 28), TextOnColor = FinderColor.Black });
            deviceEntry.ParamSettings.Add("TS Global Mix", new PlugParamSetting { Label = "Mix", OnColor = new FinderColor(255, 96, 28) });


            deviceEntry = AddPlugParamDeviceEntry("Trash");
            deviceEntry.ParamSettings.Add("B2 Trash Drive", new PlugParamSetting { Label = "Drive", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Tilt Gain", new PlugParamSetting { Label = "Tilt", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Tilt Frequency", new PlugParamSetting { Label = "Frequency", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Mix", new PlugParamSetting { Label = "Mix", OnColor = new FinderColor(240, 0, 133), PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Blend X", new PlugParamSetting { Label = "X", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Blend Y", new PlugParamSetting { Label = "Y", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Top Left Style", new PlugParamSetting { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Top Right Style", new PlugParamSetting { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Bottom Left Style", new PlugParamSetting { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("B2 Trash Bottom Right Style", new PlugParamSetting { Label = "Style", OnColor = new FinderColor(240, 0, 133), Mode = PlugParamSetting.PotMode.Symmetric, PaintLabelBg = false });
            deviceEntry.ParamSettings.Add("Global Input Gain", new PlugParamSetting { Label = "IN" });
            deviceEntry.ParamSettings.Add("Global Output Gain", new PlugParamSetting { Label = "OUT" });
            deviceEntry.ParamSettings.Add("Auto Gain Enabled", new PlugParamSetting { Label = "Auto Gain" });
            deviceEntry.ParamSettings.Add("Limiter Enabled", new PlugParamSetting { Label = "Limiter" });

            // Tokio Dawn Labs
            deviceEntry = AddPlugParamDeviceEntry("TDR Kotelnikov");
            deviceEntry.ParamSettings.Add("", new PlugParamSetting { OnColor = new FinderColor(42, 75, 124) });
            deviceEntry.ParamSettings.Add("SC Stereo Diff", new PlugParamSetting { Label = "Stereo Diff", OnColor = new FinderColor(42, 75, 124) });
        }
    }
}
