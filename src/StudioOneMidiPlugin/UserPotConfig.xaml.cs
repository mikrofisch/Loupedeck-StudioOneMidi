namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
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
        public ColorFinder.ColorSettings.PotMode Mode { get; set; }
        public Byte R { get; set; }
        public Byte G { get; set; }
        public Byte B { get; set; }
        public String Label { get; set; }
        public UserPotConfigData() { }
        public UserPotConfigData(UserPotConfigData u)
        {
            this.PluginName = u.PluginName;
            this.PluginParameter = u.PluginParameter;
            this.Mode = u.Mode;
            this.R = u.R;
            this.G = u.G;
            this.B = u.B;
            this.Label = u.Label;
        }
    }
    public partial class UserPotConfig : Window
    {
        private UserPotConfigData ConfigData;


        private Plugin Plugin { get; set; }
        public UserPotConfig(Plugin plugin, UserPotConfigData configData)
        {
            this.Plugin = plugin;

            this.InitializeComponent();

//            this.ConfigData = new UserPotConfigData(configData);
            this.ConfigData = configData;

            this.DataContext = this.ConfigData;

            this.rbPositive.IsChecked  = configData.Mode == ColorFinder.ColorSettings.PotMode.Positive;
            this.rbSymmetric.IsChecked = configData.Mode == ColorFinder.ColorSettings.PotMode.Symmetric;
        }

        private void ColorChangedHandler(Object sender, TextChangedEventArgs e)
        {
            if (((TextBox)sender).Text.ParseInt32() > 255)
            {
                ((TextBox)sender).Text = "255";
            }

            if (this.tbColorR != null && this.tbColorG != null && this.tbColorB != null)
            {
                this.rColorPatch.Fill = new SolidColorBrush(Color.FromArgb(255, (Byte)this.tbColorR.Text.ParseInt32(),
                                                                                (Byte)this.tbColorG.Text.ParseInt32(),
                                                                                (Byte)this.tbColorB.Text.ParseInt32()));
            }
        }

       
        private static readonly Regex _regex = new Regex("[^0-9]"); //regex that matches non-numbers only
        private void CheckNumberInput(Object sender, TextCompositionEventArgs e)
        {
            e.Handled = (((TextBox)sender).Text.Length > 2) || _regex.IsMatch(e.Text) ;
        }

        private void Cancel(Object sender, RoutedEventArgs e)
        {

        }

        private void CloseNoSave(Object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetPluginSetting(String valueID, String value) => 
            this.Plugin.SetPluginSetting(ColorFinder.settingName(this.ConfigData.PluginName,
                                                                 this.ConfigData.PluginParameter,
                                                                 valueID), value);
        private void SaveAndClose(Object sender, RoutedEventArgs e)
        {
            var textOnColorHex = ((Byte)this.tbColorR.Text.ParseInt32()).ToString("X2") +
                                 ((Byte)this.tbColorG.Text.ParseInt32()).ToString("X2") +
                                 ((Byte)this.tbColorB.Text.ParseInt32()).ToString("X2");
            this.SetPluginSetting(ColorFinder.ColorSettings.strTextOnColor, textOnColorHex);
            this.SetPluginSetting(ColorFinder.ColorSettings.strLabel, this.tbLabel.Text);
            this.SetPluginSetting(ColorFinder.ColorSettings.strMode, $"{(this.rbPositive.IsChecked == true ? 0 : 1)}");
            this.Close();
        }
    }
}
