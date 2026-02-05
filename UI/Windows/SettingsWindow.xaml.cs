using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using TriggerLAG.Core;


using RadioButton = System.Windows.Controls.RadioButton;
using Application = System.Windows.Application;

namespace TriggerLAG
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            var currentLang = ConfigManager.Load().Language;
            if (string.IsNullOrEmpty(currentLang) || currentLang == "en")
            {
                RadioEnglish.IsChecked = true;
            }
            else
            {
                RadioRussian.IsChecked = true;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Language_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.IsChecked == true)
            {
                string? lang = rb.Tag?.ToString();
                if (lang != null)
                {
                    ((App)Application.Current).ChangeLanguage(lang);
                    
                    
                    var config = ConfigManager.Load();
                    config.Language = lang;
                    ConfigManager.Save(config);
                }
            }
        }
    }
}
