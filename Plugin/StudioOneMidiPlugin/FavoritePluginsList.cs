using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;


namespace PluginSettings
{
    [XmlRoot("FavoritePlugins")]
    public class FavoritePluginsList : List<FavoritePluginsList.Plugin>
    {
        public const int MaxCount = 12;
        public const string ConfigFileName = "FavoritePlugins.xml";
        public class Plugin
        {
            [XmlAttribute]
            public string Name { get; set; } = "";
            public FinderColor? Color { get; set; }
            public List<string>? Variants { get; set; }
        }

        public void SaveToXmlFile()
        {
            var configFolderPath = PlugSettingsFinder.XmlConfig.ConfigFolderPath;

            if (!Directory.Exists(configFolderPath))
            {
                Directory.CreateDirectory(configFolderPath);
            }
            var configFilePath = System.IO.Path.Combine(configFolderPath, ConfigFileName);
            var writer = new StreamWriter(configFilePath);

            var serializer = new XmlSerializer(typeof(FavoritePluginsList));

            serializer.Serialize(writer, this);
            writer.Close();

        }
        public void ReadFromXmlFile()
        {
            var serializer = new XmlSerializer(typeof(FavoritePluginsList));

            var configFilePath = System.IO.Path.Combine(PlugSettingsFinder.XmlConfig.ConfigFolderPath, ConfigFileName);
            if (File.Exists(configFilePath))
            {
                using (Stream reader = new FileStream(configFilePath, FileMode.Open))
                {
                    // Call the Deserialize method to restore the object's state.
                    var p = (FavoritePluginsList?)serializer.Deserialize(reader);

                    if (p == null)
                    {
                        throw new System.Exception("Could not read favorite plugins from XML");
                    }

                    this.Clear();
                    foreach (var entry in p)
                    {
                        this.Add(entry);
                    }
                }
            }
        }
    }
}
