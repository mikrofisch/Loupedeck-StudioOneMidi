namespace PluginSettings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;


    // Nullable colour definition class with XML serialisation
    //
    [XmlSchemaProvider("GetSchemaMethod")]

    public class FinderColor : IXmlSerializable
    {
        public FinderColor() { this.A = A_default; }
        public FinderColor(Byte r, Byte g, Byte b)
        {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = A_default;
        }
        public FinderColor(Byte r, Byte g, Byte b, Byte a) : this(r, g, b)
        {
            this.A = a;
        }

        public FinderColor(String colorName, Byte r, Byte g, Byte b) : this(r, g, b)
        {
            this.Name = colorName;
            this.A = A_default;
        }
        public FinderColor(String colorName, Byte r, Byte g, Byte b, Byte a) : this(colorName, r, g, b)
        {
            this.A = a;
        }

        public FinderColor(FinderColor? c)
        {
            if (c == null) return;

            this.Name = c.Name;
            this.R = c.R;
            this.G = c.G;
            this.B = c.B;
            this.A = c.A;
            this.XmlRenderAsNameRef = c.XmlRenderAsNameRef;
        }
        public FinderColor(FinderColor? c, bool xmlRenderAsNameRef) : this(c)
        {
            this.XmlRenderAsNameRef = xmlRenderAsNameRef;
        }

        public FinderColor(FinderColor? c, byte a) : this(c)
        {
            this.A = a;
        }

        // public static FinderColor Transparent => new FinderColor(BitmapColor.Transparent);
        public static FinderColor White => new FinderColor(255, 255, 255);
        public static FinderColor Black => new FinderColor(0, 0, 0);

        public virtual byte A_default => 255;

        public byte R, G, B;
        public byte A; // Alpha channel

        public String Name { get; set; } = "";  // name of the referenced color in the device's color list

        public Boolean XmlRenderAsNameRef = false;

        #region IXmlSerializable members

        public System.Xml.Schema.XmlSchema? GetSchema() => null;

        public static XmlQualifiedName GetSchemaMethod(XmlSchemaSet xs)
        {
            // This provides a schema so that the serializer will render the element name
            // defined in the schema for arrays (which it doesn't do if [XmlRoot] is used
            // to set the name)
            var colorSchema = @"<xs:schema elementFormDefault=""qualified"" xmlns:xs=""http://www.w3.org/2001/XMLSchema""> " +
                              @"<xs:element name=""Color"" nillable=""true"" /> " +
                              @"<xs:complexType name=""Color""> " +
                              @"<xs:attribute name=""transparency"" type=""xs:integer"" /> " +
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

            A = A_default;

            if (reader.HasAttributes)
            {
                this.Name = reader.GetAttribute("name") ?? "";
                this.A = reader.GetAttribute("transparency") != null ? Convert.ToByte(reader.GetAttribute("transparency")) : A_default;
            }

            var content = reader.ReadElementContentAsString();
            if (content == "white")
            {
                R = G = B = 255;
            }
            else if (content == "black")
            {
                R = G = B = 0;
            }
            else if (content.StartsWith("rgb("))
            {
                var rgb = content.Substring(4, content.Length - 5).Split(',');
                R = Convert.ToByte(rgb[0]);
                G = Convert.ToByte(rgb[1]);
                B = Convert.ToByte(rgb[2]);
                A = rgb.Length > 3 ? Convert.ToByte(rgb[3]) : A;
            }
            else
            {
                this.Name = content;
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            if (!string.IsNullOrEmpty(Name))
            {
                if (this.XmlRenderAsNameRef)
                {
                    if (A != A_default)
                    {
                        writer.WriteStartAttribute("transparency");
                        writer.WriteValue(this.A.ToString());
                        writer.WriteEndAttribute();
                    }
                    writer.WriteString(this.Name);
                }
                else
                {
                    writer.WriteAttributeString("name", this.Name);
                }
            }
            if (!this.XmlRenderAsNameRef)
            {
                if (R == 255 && G == 255 && B == 255)
                {
                    writer.WriteString("white");
                }
                else if (R == 0 && G == 0 && B == 0)
                {
                    writer.WriteString("black");
                }
                else
                {
                    if (A != A_default)
                    {
                        writer.WriteAttributeString("transparency", this.A.ToString());
                    }
                    writer.WriteString($"rgb({this.R},{this.G},{this.B})");
                }
            }
        }

        #endregion
    }

    // OnColor is the normal background and uses a lower transparency by default to make it a little darker.
    // This makes it easier to use the same color for different purposes, and allows to use a
    // color sampled from a plugin directly. The transparency can be modified in the settings if needed.
    public class FinderColorOnColor : FinderColor
    {
        public FinderColorOnColor() : base() { }
        // public FinderColorOnColor(Byte r, Byte g, Byte b, Byte a = 80) : base(r, g, b, a) { }
        // public FinderColorOnColor(String colorName, Byte r, Byte g, Byte b, Byte a = 80) : base(colorName, r, g, b, a) { }

        public FinderColorOnColor(FinderColor? c) : base(c) { }
        public FinderColorOnColor(FinderColor? c, byte A) : base(c, A) { }
        public FinderColorOnColor(FinderColor? c, bool xmlRenderAsNameRef) : base(c, xmlRenderAsNameRef) { }

        public override byte A_default => 80;
    }

    public class PlugSettingsFinder
    {
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

            public FinderColorOnColor? OnColor { get; set; }
            public FinderColor? OffColor { get; set; }
            //            [XmlElement, DefaultValue(null)]
            public FinderColor? TextOnColor { get; set; }
            public FinderColor? TextOffColor { get; set; }
            public FinderColor? BarOnColor { get; set; }
            public String? IconName { get; set; }
            public String? IconNameOn { get; set; }
            public String? Label { get; set; }
            public String? LabelOn { get; set; }
            public String? LinkedParameter { get; set; }
            [DefaultValueAttribute(false)]
            public Boolean LinkReversed { get; set; } = false;
            public String? LinkedStates { get; set; }               // Comma separated list of indices for which the linked parameter is active
                                                                    // if the linked parameter has multipe states
            [DefaultValueAttribute(100)]
            public Int32 DialSteps { get; set; } = 100;             // Number of steps for a mode dial

            [DefaultValueAttribute(-1)]
            public Int32 MaxValuePrecision { get; set; } = -1;      // Maximum number of decimal places for the value display (-1 = no limit)

            [XmlArray("UserMenuItems")]
            [XmlArrayItem("Item")]
            public String[]? UserMenuItems;                         // Items for user button menu

            [XmlAttribute(AttributeName = "name")]
            public String Name = "";                                // For XML config file

            public const String strHideValueBar = "HideValueBar";

            public PlugParamSetting DeepClone()
            {
                var serializer = new DataContractSerializer(typeof(PlugParamSetting), [typeof(FinderColor)]);
                using var ms = new MemoryStream();
                serializer.WriteObject(ms, this);
                ms.Seek(0, SeekOrigin.Begin);
                return (PlugParamSetting)serializer.ReadObject(ms)!;
            }
        }

        public class PlugParamDeviceEntry
        {
            public string? PluginName
            {
                get
                {
                    // Find the key in PlugParamDict whose value is this instance
                    foreach (var kvp in PlugParamDict)
                    {
                        if (object.ReferenceEquals(kvp.Value, this))
                        {
                            return kvp.Key;
                        }
                    }
                    return null;
                }
                set { }
            }

            public String ManufacturerName = "";    // Used for categorizing the plugin in the Loupedeck plugin configuration app
            public String[]? UserPageNames;         // Names for user pages, if any
            public List<FinderColor> Colors = [];
            public ConcurrentDictionary<String, PlugParamSetting> ParamSettings = [];
        }
        private static readonly ConcurrentDictionary<String, PlugParamDeviceEntry> PlugParamDict = [];

        private String LastPluginName = "", LastPluginParameter = "";
        private PlugParamDeviceEntry? LastPlugParamDeviceEntry;
        private static PlugParamDeviceEntry? DefaultDeviceEntry;
        private PlugParamSetting LastParamSettings = new();

        public Int32 CurrentUserPage = 0;              // For tracking the current user page position

        public PlugParamSetting DefaultPlugParamSettings { get; private set; } = new PlugParamSetting
        {
            OnColor = new FinderColorOnColor(FinderColor.Black),
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

        public void ClearCache()
        {
            this.LastPluginName = "";
            this.LastPluginParameter = "";
            this.LastPlugParamDeviceEntry = null;
            this.LastParamSettings = new PlugParamSetting();
        }

        public class XmlConfig
        {
            public static readonly String ConfigFolderPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Loupedeck-AudioPluginConfig");
            public static readonly String ConfigFolderIconsPath = System.IO.Path.Combine(ConfigFolderPath, "Icons");
            public const String ConfigFileName = "AudioPluginConfig.xml";

            // Data structure for plugin parameter settings for use with XML serialisation
            // (using List instead of Dictionary because Dictionary is not serializable)
            public class PluginConfig
            {
                [XmlAttribute]
                public String PluginName = "";          // Used to identify the plugin configuration
                [XmlAttribute]
                public String ManufacturerName = "";    // Used for categorizing the plugin in the Loupedeck plugin configuration app

                [XmlArray("UserPageNames")]
                [XmlArrayItem("Page")]
                public String[]? UserPageNames;         // Names for user pages, if any

                public List<FinderColor> Colors = [];
                public List<PlugParamSetting> ParamSettings = [];
            }

            [XmlElement("PluginConfig")]
            public readonly List<PluginConfig> PluginConfigs = [];

            public static FinderColor? AddReferencedColorToList(List<FinderColor> colors, FinderColor? c)
            {
                if (c != null && !string.IsNullOrEmpty(c.Name))
                {
                    if (!colors.Any(cc => cc.Name == c.Name))
                    {
                        // Note we should never get here, it was needed when the color list was
                        // not stored explicitly in the XML file.
                        //
                        // The first time the color list is written those colors that are referenced
                        // by name in any parameter setting will be set to render as a name reference
                        // (see below). Therefore we need to make sure that the color is added to the
                        // color list with the XmlRenderAsNameRef flag set to false.
                        colors.Add(new FinderColor(c, xmlRenderAsNameRef: false));
                    }
                    c.XmlRenderAsNameRef = true;   // Write only name to XML in parameter settings entry
                }
                return c;
            }

            public static FinderColor? GetColorValuesFromList(List<FinderColor> colors, FinderColor? c)
            {
                // If the colour is referenced by name, we get the RGB values from
                // the colour list (in an automatically created config XML file there
                // won't be any RGB values in a colour that is referenced by name)
                //
                if (c != null && !string.IsNullOrEmpty(c.Name))
                {
                    // Find RGB values in colour list
                    foreach (var cc in colors)
                    {
                        if (cc.Name == c.Name)
                        {
                            // Make a copy of the referenced colour set to render to XML as a name reference
                            c = new FinderColor(cc, xmlRenderAsNameRef: true);
                            break;
                        }
                    }
                }
                return c;
            }

            public static void ProcessPluginColors(PlugParamSetting paramSettings, List<FinderColor> colors, Func<List<FinderColor>, FinderColor?, FinderColor?> processFunc)
            {
                // Need to explicitly assign the FinderColor values because they are not passed by reference
                paramSettings.OnColor = processFunc(colors, paramSettings.OnColor) is FinderColor onColor && paramSettings.OnColor != null ? new FinderColorOnColor(onColor, paramSettings.OnColor.A) : null;
                paramSettings.OffColor = processFunc(colors, paramSettings.OffColor);
                paramSettings.TextOnColor = processFunc(colors, paramSettings.TextOnColor);
                paramSettings.TextOffColor = processFunc(colors, paramSettings.TextOffColor);
                paramSettings.BarOnColor = processFunc(colors, paramSettings.BarOnColor);
            }

            public void ReadXmlStream(Stream reader, ConcurrentDictionary<String, PlugParamDeviceEntry> plugParamDict)
            {
                var serializer = new XmlSerializer(typeof(XmlConfig));

                // Call the Deserialize method to restore the object's state.
                var p = (XmlConfig?)serializer.Deserialize(reader);

                if (p == null)
                {
                    throw new Exception("Could not read XML plugin parameter configuration file");
                }

                // Create plugin device entries and dereference colour names in parameter settings
                //
                foreach (var cfgDeviceEntry in p.PluginConfigs)
                {
                    // PlugParamSetting? defaultSettings = null;

                    var deviceEntry = new PlugParamDeviceEntry { };
                    deviceEntry.ManufacturerName = cfgDeviceEntry.ManufacturerName;
                    deviceEntry.UserPageNames = cfgDeviceEntry.UserPageNames ?? [];
                    deviceEntry.Colors = cfgDeviceEntry.Colors;

                    foreach (var paramSettings in cfgDeviceEntry.ParamSettings)
                    {
                        // Get colour values for colours that are referenced by name.
                        ProcessPluginColors(paramSettings, cfgDeviceEntry.Colors, GetColorValuesFromList);

                        deviceEntry.ParamSettings.TryAdd(paramSettings.Name, paramSettings);
                    }

                    // Add or update the device entry in the dictionary.
                    plugParamDict[cfgDeviceEntry.PluginName] = deviceEntry;
                }
            }

            public void WriteXmlCfgFile(String configFileName, ConcurrentDictionary<String, PlugParamDeviceEntry> plugParamDict)
            {
                if (!Directory.Exists(ConfigFolderPath))
                {
                    Directory.CreateDirectory(ConfigFolderPath);
                }
                var configFilePath = System.IO.Path.Combine(ConfigFolderPath, configFileName);
                var writer = new StreamWriter(configFilePath);

                foreach (var deviceEntry in plugParamDict)
                {
                    var cfgDeviceEntry = new PluginConfig
                    {
                        PluginName = deviceEntry.Key,
                        ManufacturerName = deviceEntry.Value.ManufacturerName,
                        Colors = deviceEntry.Value.Colors,
                        UserPageNames = deviceEntry.Value.UserPageNames
                    };
                    foreach (var paramSettings in deviceEntry.Value.ParamSettings)
                    {
                        paramSettings.Value.Name = paramSettings.Key;

                        ProcessPluginColors(paramSettings.Value, cfgDeviceEntry.Colors, AddReferencedColorToList);

                        cfgDeviceEntry.ParamSettings.Add(paramSettings.Value);
                    }

                    foreach (var c in cfgDeviceEntry.Colors) c.XmlRenderAsNameRef = false;

                    this.PluginConfigs.Add(cfgDeviceEntry);
                }

                var serializer = new XmlSerializer(typeof(XmlConfig));
                serializer.Serialize(writer, this);
                writer.Close();

            }
        }

        public static void SaveDictToXmlConfig()
        {
            var xmlCfg = new XmlConfig();
            xmlCfg.WriteXmlCfgFile(XmlConfig.ConfigFileName, PlugParamDict);
        }

        public static void ReadDictFromXmlConfig()
        {
            var xmlCfg = new XmlConfig();
            PlugParamDict.Clear();
            

            var configFilePath = System.IO.Path.Combine(XmlConfig.ConfigFolderPath, XmlConfig.ConfigFileName);
            using (Stream reader = new FileStream(configFilePath, FileMode.Open))
            {
                xmlCfg.ReadXmlStream(reader, PlugParamDict);
            }
        }

        public static void AddPlugin(String manufacturerName, String pluginName)
        {
            PlugParamDict[pluginName] = new PlugParamDeviceEntry { ManufacturerName = manufacturerName };
        }

        public static void DuplicatePlugin(String pluginName, String newPluginName)
        {
            // Find the device entry for the plugin
            if (PlugParamDict.TryGetValue(pluginName, out var deviceEntry))
            {
                // Create a deep copy of the device entry 
                DataContractSerializer serializer = new(
                    type: deviceEntry.GetType(),
                    knownTypes:
                    [
                        typeof(PlugParamSetting),
                        typeof(FinderColor)
                    ]);
                Stream stream = new MemoryStream();
                using (stream)
                {
                    serializer.WriteObject(stream, deviceEntry);
                    stream.Seek(0, SeekOrigin.Begin);
                    PlugParamDict[newPluginName] = (PlugParamDeviceEntry?)serializer.ReadObject(stream) ?? new PlugParamDeviceEntry();
                }
            }
        }

        public static void RemovePlugin(string pluginName)
        {
            if (PlugParamDict.ContainsKey(pluginName))
            {
                PlugParamDict.TryRemove(pluginName, out _);
            }
        }

        public static void Init(Boolean forceReload = false)
        {
            if (forceReload)
            {
                PlugParamDict.Clear();
            }
            if (PlugParamDict.Count == 0)
            {
                var xmlCfg = new XmlConfig();

                // Read default settings from embedded resource.
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();

                try
                {
                    var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith(XmlConfig.ConfigFileName));

                    using (Stream? reader = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (reader == null)
                        {
                            throw new Exception("Could not load the default XML plugin parameter configuration data");
                        }
                        xmlCfg.ReadXmlStream(reader, PlugParamDict);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Resource not found in assembly, move on
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

                // Remove duplicate colors from the color lists in each device entry if they
                // somehow ended up there.
                //
                foreach (var deviceEntry in PlugParamDict.Values)
                {
                    if (deviceEntry.Colors == null || deviceEntry.Colors.Count <= 1)
                        continue;

                    var seenNames = new HashSet<string>();
                    // Use ToList() to avoid modifying the collection while iterating
                    foreach (var color in deviceEntry.Colors.ToList())
                    {
                        if (!string.IsNullOrEmpty(color.Name))
                        {
                            if (seenNames.Contains(color.Name))
                            {
                                Debug.WriteLine($"Warning: Removing duplicate color {color.Name} from plugin {deviceEntry.PluginName}");
                                deviceEntry.Colors.Remove(color);
                            }
                            else
                            {
                                seenNames.Add(color.Name);
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

        public Dictionary<String, List<String>> GetPluginsByManufacturer()
        {
            var pluginList = new Dictionary<String, List<String>>();
            foreach (var deviceEntry in PlugParamDict)
            {
                if (!pluginList.ContainsKey(deviceEntry.Value.ManufacturerName))
                {
                    var pluginNames = new List<String>();
                    foreach (var name in PlugParamDict)
                    {
                        if (name.Value.ManufacturerName == deviceEntry.Value.ManufacturerName)
                        {
                            pluginNames.Add(name.Key);
                        }
                    }
                    pluginNames.Sort();
                    pluginList.Add(deviceEntry.Value.ManufacturerName, pluginNames);
                }
            }
            return pluginList;
        }

        public PlugParamDeviceEntry? GetPlugParamDeviceEntry(String pluginName)
        {
            if (pluginName == null)
            {
                return new PlugParamDeviceEntry();
            }

            if (LastPlugParamDeviceEntry != null && pluginName == this.LastPluginName)
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

        public PlugParamSetting GetPlugParamSettings(PlugParamDeviceEntry? deviceEntry, String parameterName, Boolean isUser, Int32 buttonIdx = 0)
        {
            if (parameterName == null)
            {
                return this.DefaultPlugParamSettings;
            }

            if (this.LastParamSettings != null && deviceEntry == this.LastPlugParamDeviceEntry && parameterName == this.LastPluginParameter) return this.LastParamSettings;
            this.LastPluginParameter = parameterName;

            var userPagePos = $"{this.CurrentUserPage}:{buttonIdx}" + (isUser ? "U" : "");
            PlugParamSetting? paramSettings;

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

        public PlugParamSetting? GetDefaultPlugParamSettings(PlugParamDeviceEntry? deviceEntry)
        {
            if (deviceEntry == null) return null;
            if (deviceEntry.ParamSettings.TryGetValue("", out var paramSettings))
            {
                return paramSettings;
            }
            return null;
        }

        public PlugParamSetting.PotMode GetMode(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).Mode;
        public Boolean GetShowCircle(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).ShowUserButtonCircle;
        public Boolean GetPaintLabelBg(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).PaintLabelBg;

        public FinderColor? GetOnColor(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).OnColor ??
                                                                                                                                            this.GetDefaultPlugParamSettings(deviceEntry)?.OnColor ??
                                                                                                                                            this.DefaultPlugParamSettings.OnColor ?? FinderColor.Black;

        public FinderColor GetBarOnColor(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false)
        {
            var cs = this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx);
            return cs.BarOnColor ?? cs.OnColor ?? this.GetDefaultPlugParamSettings(deviceEntry)?.BarOnColor ?? this.GetDefaultPlugParamSettings(deviceEntry)?.OnColor ?? this.DefaultPlugParamSettings.OnColor ?? FinderColor.Black;
        }
        public FinderColor? GetOffColor(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).OffColor ??
                                                                                                                                             this.GetDefaultPlugParamSettings(deviceEntry)?.OffColor ??
                                                                                                                                             this.DefaultPlugParamSettings.OffColor ?? FinderColor.Black;
        public FinderColor GetTextOnColor(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).TextOnColor ??
                                                                                                                                               this.GetDefaultPlugParamSettings(deviceEntry)?.TextOnColor ??
                                                                                                                                               this.DefaultPlugParamSettings.TextOnColor ?? FinderColor.White;
        public FinderColor GetTextOffColor(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).TextOffColor ??
                                                                                                                                                this.GetDefaultPlugParamSettings(deviceEntry)?.TextOffColor ??
                                                                                                                                                this.DefaultPlugParamSettings.TextOffColor ?? FinderColor.White;
        public String GetLabel(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).Label ?? parameterName;
        public String GetLabelOn(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false)
        {
            var cs = this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx);
            return cs.LabelOn ?? cs.Label ?? parameterName;
        }
        public String GetLabelShort(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => stripLabel(this.GetLabel(deviceEntry, parameterName, buttonIdx, isUser));
        public String GetLabelOnShort(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => stripLabel(this.GetLabelOn(deviceEntry, parameterName, buttonIdx, isUser));
        public static String stripLabel(String label)
        {
            if (label.Length <= 12) return label;
            return Regex.Replace(label, "(?<!^)[aeiou](?!$)", "");
        }

        public String? GetLinkedParameter(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkedParameter;
        public Boolean GetLinkReversed(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkReversed;
        public String? GetLinkedStates(PlugParamDeviceEntry deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).LinkedStates;
        public Boolean HideValueBar(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).HideValueBar;
        public Boolean ShowUserButtonCircle(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).ShowUserButtonCircle;
        public Int32 GetDialSteps(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).DialSteps;
        public Int32 GetMaxValuePrecision(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).MaxValuePrecision;
        public String[]? GetUserMenuItems(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).UserMenuItems;
        public Boolean HasMenu(PlugParamDeviceEntry? deviceEntry, String parameterName, Int32 buttonIdx, Boolean isUser = false) => this.GetPlugParamSettings(deviceEntry, parameterName, isUser, buttonIdx).UserMenuItems != null;
    }
}
