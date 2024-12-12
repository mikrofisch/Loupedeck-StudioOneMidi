namespace Loupedeck.StudioOneMidiPlugin
{
    using System;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Input;

    /// <summary>
    /// Interaction logic for UserControlConfig.xaml
    /// </summary>
    /// 

    public class UserControlConfigData
    {
        public String Title { get; set; }
        public String PluginName { get; set; }
        public String PluginParameter { get; set; }
        public PlugSettingsFinder.PlugParamSettings.PotMode Mode { get; set; }
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
        private PlugSettingsFinder UserPlugSettingsFinder;

        private Plugin Plugin { get; set; }
        public UserControlConfig(WindowMode mode, Plugin plugin, PlugSettingsFinder cf, UserControlConfigData configData)
        {
            this.Plugin = plugin;
            this.UserPlugSettingsFinder = cf;

            configData.Title = mode == WindowMode.Dial ? "User Dial Configuration" : "User Button Configuration";
           
            this.InitializeComponent();

            this.ConfigData = configData;

            this.DataContext = this.ConfigData;

            if (mode == WindowMode.Dial)
            {
                this.spShowCircle.Visibility = Visibility.Collapsed;
                this.rbPositive.IsChecked = configData.Mode == PlugSettingsFinder.PlugParamSettings.PotMode.Positive;
                this.rbSymmetric.IsChecked = configData.Mode == PlugSettingsFinder.PlugParamSettings.PotMode.Symmetric;
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
            this.Plugin.SetPluginSetting(PlugSettingsFinder.SettingName(this.ConfigData.PluginName,
                                                                 this.ConfigData.PluginParameter,
                                                                 valueID), value, true);
        private void Reset(Object sender, RoutedEventArgs e)
        {
            var settingsList = this.Plugin.ListPluginSettings();

            foreach (var setting in settingsList)
            {
                if (setting.StartsWith(PlugSettingsFinder.SettingName(this.ConfigData.PluginName,
                                                                    this.ConfigData.PluginParameter, "")))
                {
                    this.Plugin.DeletePluginSetting(setting);
                }
            }
            PlugSettingsFinder.Init(this.Plugin, forceReload: true);

            var deviceEntry = this.UserPlugSettingsFinder.GetPlugParamDeviceEntry(this.ConfigData.PluginName);

            var onColor = this.UserPlugSettingsFinder.GetOnColor(deviceEntry, this.ConfigData.PluginParameter, 0);

            this.ConfigData.Mode = this.UserPlugSettingsFinder.GetMode(deviceEntry, this.ConfigData.PluginParameter, 0);
            this.rbPositive.IsChecked = this.ConfigData.Mode == PlugSettingsFinder.PlugParamSettings.PotMode.Positive;
            this.rbSymmetric.IsChecked = this.ConfigData.Mode == PlugSettingsFinder.PlugParamSettings.PotMode.Symmetric;
            this.tbColorR.Text = onColor.R.ToString();
            this.tbColorG.Text = onColor.G.ToString();
            this.tbColorB.Text = onColor.B.ToString();
            this.tbLabel.Text = this.UserPlugSettingsFinder.GetLabel(deviceEntry, this.ConfigData.PluginParameter, 0);
        }
        private void SaveAndClose(Object sender, RoutedEventArgs e)
        {
            if (this.gPotMode.IsVisible)
            {
                this.SetPluginSetting(PlugSettingsFinder.PlugParamSettings.strMode, $"{(this.rbPositive.IsChecked == true ? 0 : 1)}");
            }
            if (this.spShowCircle.IsVisible)
            {
                this.SetPluginSetting(PlugSettingsFinder.PlugParamSettings.strShowCircle, $"{(this.chShowCircle.IsChecked == true ? 1 : 0)}");
            }

            var onColorHex = ((Byte)this.tbColorR.Text.ParseInt32()).ToString("X2") +
                             ((Byte)this.tbColorG.Text.ParseInt32()).ToString("X2") +
                             ((Byte)this.tbColorB.Text.ParseInt32()).ToString("X2");
            this.SetPluginSetting(PlugSettingsFinder.PlugParamSettings.strOnColor, onColorHex);
            this.SetPluginSetting(PlugSettingsFinder.PlugParamSettings.strLabel, this.tbLabel.Text);
            if (this.tbLinkedParam.IsVisible)
            {
                this.SetPluginSetting(PlugSettingsFinder.PlugParamSettings.strLinkedParameter, this.tbLinkedParam.Text);
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
