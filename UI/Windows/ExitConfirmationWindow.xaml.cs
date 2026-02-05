using System;
using System.Windows;
using System.Windows.Input;
using TriggerLAG.Core;

namespace TriggerLAG
{
    public enum ExitAction
    {
        Cancel,
        Exit,
        HideToTray
    }

    public partial class ExitConfirmationWindow : Window
    {
        public ExitAction Action { get; private set; } = ExitAction.Cancel;

        public ExitConfirmationWindow()
        {
            InitializeComponent();
            this.MouseDown += Window_MouseDown;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void BtnHideToTray_Click(object sender, RoutedEventArgs e)
        {
            Action = ExitAction.HideToTray;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            Action = ExitAction.Exit;
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Action = ExitAction.Cancel;
            this.DialogResult = false;
            this.Close();
        }
    }
}