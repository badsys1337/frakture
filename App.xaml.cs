using System;
using System.Windows;
using System.Windows.Threading;

using System.Windows.Media;
using System.Windows.Interop;
using TriggerLAG.Core;

using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TriggerLAG
{
    
    
    
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
            
            
            var config = ConfigManager.Load();
            if (!string.IsNullOrEmpty(config.Language))
            {
                ChangeLanguage(config.Language);
            }

            base.OnStartup(e);
            
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }

        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled error occurred: {e.Exception.Message}\n\n{e.Exception.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"A fatal error occurred: {ex.Message}\n\n{ex.StackTrace}", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ChangeLanguage(string cultureCode)
        {
            try
            {
                ResourceDictionary dict = new ResourceDictionary();
                dict.Source = new Uri($"Resources/Strings.{cultureCode}.xaml", UriKind.Relative);

                
                if (Resources.MergedDictionaries.Count > 0)
                {
                    Resources.MergedDictionaries[0] = dict;
                }
                else
                {
                    Resources.MergedDictionaries.Add(dict);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to switch language: {ex.Message}");
            }
        }
    }
}
