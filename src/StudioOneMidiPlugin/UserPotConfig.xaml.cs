namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for UserPotConfig.xaml
    /// </summary>
    /// 

    public class UserPotConfigData
    {
        public String PluginName { get; set; }
        public String PluginParameter { get; set; }
        public enum PotMode { Positive, Symmetric };
        public PotMode Mode { get; set; }
        public Byte R { get; set; }
        public Byte G { get; set; }
        public Byte B { get; set; }
        public String Label { get; set; }
    }

    public partial class UserPotConfig : Window
    {
        UserPotConfigData ConfigData;
        public UserPotConfig(String pluginName, String pluginParameter)
        {
            this.InitializeComponent();

            ConfigData = new UserPotConfigData();
            ConfigData.PluginName = pluginName;
            ConfigData.PluginParameter = pluginParameter;

            this.DataContext = ConfigData;
        }

        private void ColorChangedHandler(Object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Text.ParseInt32() > 255)
            {
                ((TextBox)sender).Text = "255";
            }

            if (this.ConfigData != null)
            {
                if (this.tbColorR != null)
                    this.ConfigData.R = (Byte)this.tbColorR.Text.ParseInt32();
                if (this.tbColorG != null)
                    this.ConfigData.G = (Byte)this.tbColorR.Text.ParseInt32();
                if (this.tbColorB != null)
                    this.ConfigData.B = (Byte)this.tbColorR.Text.ParseInt32();

                this.rColorPatch.Fill = new SolidColorBrush(Color.FromArgb(255, this.ConfigData.R,
                                                                                this.ConfigData.G,
                                                                                this.ConfigData.B));
            }
        }

       
        private static readonly Regex _regex = new Regex("[^0-9]"); //regex that matches numbers only
        private void CheckNumberInput(Object sender, TextCompositionEventArgs e)
        {
            e.Handled = (((TextBox)sender).Text.Length > 2) || _regex.IsMatch(e.Text) ;
        }
    }
}
