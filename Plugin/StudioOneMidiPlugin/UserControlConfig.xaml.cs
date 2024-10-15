namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Security.RightsManagement;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    // using System.Drawing;
    using System.Windows.Documents;
    //using System.Windows.Forms;
    using System.Windows.Forms.VisualStyles;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;

    /// <summary>
    /// Interaction logic for UserControlConfig.xaml
    /// </summary>
    /// 

    public class UserControlConfigData
    {
        public String Title { get; set; }
        public String PluginName { get; set; }
        public String PluginParameter { get; set; }
        public ColorFinder.ColorSettings.PotMode Mode { get; set; }
        public Boolean ShowCircle { get; set; }
        public Byte R { get; set; }
        public Byte G { get; set; }
        public Byte B { get; set; }
        public String LinkedParameter { get; set; }
        public String Label { get; set; }
        public UserControlConfigData() { }
        public UserControlConfigData(UserControlConfigData u)
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
    public partial class UserControlConfig : Window
    {
        public enum WindowMode
        {
            Dial, Button
        }
        private UserControlConfigData ConfigData;
        private ColorFinder UserColorFinder;

        private Plugin Plugin { get; set; }
        public UserControlConfig(WindowMode mode, Plugin plugin, ColorFinder cf, UserControlConfigData configData)
        {
            this.Plugin = plugin;
            this.UserColorFinder = cf;

            configData.Title = mode == WindowMode.Dial ? "User Dial Configuration" : "User Button Configuration";
           
            this.InitializeComponent();

            this.ConfigData = configData;

            this.DataContext = this.ConfigData;

            if (mode == WindowMode.Dial)
            {
                this.spShowCircle.Visibility = Visibility.Collapsed;
                this.rbPositive.IsChecked = configData.Mode == ColorFinder.ColorSettings.PotMode.Positive;
                this.rbSymmetric.IsChecked = configData.Mode == ColorFinder.ColorSettings.PotMode.Symmetric;
            }
            else if (mode == WindowMode.Button)
            {
                this.gPotMode.Visibility = Visibility.Collapsed;
                this.spLinkedParam.Visibility = Visibility.Collapsed;
                this.chShowCircle.IsChecked = configData.ShowCircle;
            }
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

        private void CloseNoSave(Object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SetPluginSetting(String valueID, String value) => 
            this.Plugin.SetPluginSetting(ColorFinder.settingName(this.ConfigData.PluginName,
                                                                 this.ConfigData.PluginParameter,
                                                                 valueID), value, true);
        private void Reset(Object sender, RoutedEventArgs e)
        {
            var settingsList = this.Plugin.ListPluginSettings();

            foreach (var setting in settingsList)
            {
                if (setting.StartsWith(ColorFinder.settingName(this.ConfigData.PluginName,
                                                                    this.ConfigData.PluginParameter, "")))
                {
                    this.Plugin.DeletePluginSetting(setting);
                }
            }
            this.UserColorFinder.Init(this.Plugin, forceReload: true);

            var onColor = this.UserColorFinder.getOnColor(this.ConfigData.PluginName, this.ConfigData.PluginParameter);

            this.ConfigData.Mode = this.UserColorFinder.getMode(this.ConfigData.PluginName, this.ConfigData.PluginParameter);
            this.rbPositive.IsChecked = this.ConfigData.Mode == ColorFinder.ColorSettings.PotMode.Positive;
            this.rbSymmetric.IsChecked = this.ConfigData.Mode == ColorFinder.ColorSettings.PotMode.Symmetric;
            this.tbColorR.Text = onColor.R.ToString();
            this.tbColorG.Text = onColor.G.ToString();
            this.tbColorB.Text = onColor.B.ToString();
            this.tbLabel.Text = this.UserColorFinder.getLabel(this.ConfigData.PluginName, this.ConfigData.PluginParameter);
        }
        private void SaveAndClose(Object sender, RoutedEventArgs e)
        {
            if (this.gPotMode.IsVisible)
            {
                this.SetPluginSetting(ColorFinder.ColorSettings.strMode, $"{(this.rbPositive.IsChecked == true ? 0 : 1)}");
            }
            if (this.spShowCircle.IsVisible)
            {
                this.SetPluginSetting(ColorFinder.ColorSettings.strShowCircle, $"{(this.chShowCircle.IsChecked == true ? 1 : 0)}");
            }

            var onColorHex = ((Byte)this.tbColorR.Text.ParseInt32()).ToString("X2") +
                             ((Byte)this.tbColorG.Text.ParseInt32()).ToString("X2") +
                             ((Byte)this.tbColorB.Text.ParseInt32()).ToString("X2");
            this.SetPluginSetting(ColorFinder.ColorSettings.strOnColor, onColorHex);
            this.SetPluginSetting(ColorFinder.ColorSettings.strLabel, this.tbLabel.Text);
            if (this.tbLinkedParam.IsVisible)
            {
                this.SetPluginSetting(ColorFinder.ColorSettings.strLinkedParameter, this.tbLinkedParam.Text);
            }
            this.Close();
        }

        private void ShowCircleClickHandler(Object sender, RoutedEventArgs e)
        {
            if (this.chShowCircle.IsChecked == true)
            {
                this.chShowCircle.IsChecked = false;
            }
            else
            {
                this.chShowCircle.IsChecked = true;
            }
        }
    }
}
