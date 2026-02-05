using TriggerLAG;
using TriggerLAG.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Diagnostics;
using System.Windows.Media.Animation;
using System.Windows.Interop;


using Button = System.Windows.Controls.Button;
using MessageBox = System.Windows.MessageBox;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Control = System.Windows.Controls.Control;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Application = System.Windows.Application;
using CheckBox = System.Windows.Controls.CheckBox;
using RadioButton = System.Windows.Controls.RadioButton;
using TextBox = System.Windows.Controls.TextBox;
using ComboBox = System.Windows.Controls.ComboBox;
using Label = System.Windows.Controls.Label;

namespace TriggerLAG
{
    public partial class MainWindow : Window
    {
        
        private readonly NetworkEngine _networkEngine;
        private readonly MacroEngine _macroEngine;
        private readonly SystemOptimizer _systemOptimizer;
        private readonly SnowflakeManager _snowflakeManager;

        private System.Windows.Forms.NotifyIcon? _notifyIcon;
        private GlobalKeyboardHook? _keyboardHook;
        internal GlobalMouseHook? _mouseHook;
        
        
        private IntPtr _handleOwner = IntPtr.Zero;
        private IntPtr _handleSatellite = IntPtr.Zero;

        private enum BindingContext { None, MainHotkey, DoubleEditTrigger, ItemCollectTrigger }
        private volatile BindingContext _bindingContext = BindingContext.None;

        private int _boundKey = 0;
        private int _boundMouseBtn = 0;

        private int _doubleEditTriggerKey = 0;
        private int _doubleEditTriggerMouseBtn = 0;
        private bool _doubleEditEnabled = false;

        private int _itemCollectTriggerKey = 0;
        private int _itemCollectTriggerMouseBtn = 0;
        private bool _itemCollectEnabled = false;

        private NotificationWindow? _currentNotification;
        private ModeInfoWindow? _modeInfoWindow;
        private bool _modeInfoVisible = false;

        public MainWindow()
        {
            InitializeComponent();

            _networkEngine = new NetworkEngine();
            _networkEngine.StatusChanged += (isRunning) => Dispatcher.Invoke(() => UpdateUI(isRunning));
            _networkEngine.ErrorOccurred += (msg) => Dispatcher.Invoke(() => MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error));

            _macroEngine = new MacroEngine();
            _systemOptimizer = new SystemOptimizer();
            _snowflakeManager = new SnowflakeManager(SnowCanvas);

            
            string title = GenerateRandomTitle(8);
            this.Title = "TriggerLAG - " + title;

            InitializeHooks();

            this.Loaded += (s, e) => {
                if (_snowflakeManager != null) _snowflakeManager.Initialize();
                InitializeNotifyIcon();
                LoadConfig();

                
            };

            this.Deactivated += (s, e) => {
                try {
                    if (_modeInfoVisible && _modeInfoWindow != null && _modeInfoWindow.IsLoaded)
                    {
                        _modeInfoWindow.Hide();
                    }
                } catch (Exception ex) { 
                    ErrorLogger.Log("Error handling window deactivation", ex);
                }
            };

            this.Activated += (s, e) => {
                try {
                    if (_modeInfoVisible && _modeInfoWindow != null && _modeInfoWindow.IsLoaded)
                    {
                        _modeInfoWindow.Show();
                        _modeInfoWindow.AttachToWindow(this);
                    }
                } catch (Exception ex) {
                    ErrorLogger.Log("Error handling window activation", ex);
                }
            };

            this.Closing += (s, e) => {
                _modeInfoVisible = false; 
                _networkEngine.Stop();
                _macroEngine.StopDoubleEditLoop();
                _macroEngine.StopItemCollectLoop();
                _systemOptimizer.StopFortniteOptimization();
                _snowflakeManager.Stop();
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.Dispose();
                }
                try { _modeInfoWindow?.Close(); } catch (Exception ex) { ErrorLogger.Log("Error closing ModeInfo window", ex); }
            };
        }

        public MainWindow(string key, ProfileInfo? profile = null) : this()
        {
        }

        private void StartLicenseCheck()
        {
             
        }

        private void HandleBan(string message)
        {
             string lang = ConfigManager.Load().Language;
             string msg;

             if (message == "license banned")
             {
                 msg = "your key are banned";
             }
             else
             {
                 msg = (lang == "ru") ? "сорян братанчик но тя забанили" : "sorry you are banned";
             }

             MessageBox.Show(msg, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
             Application.Current.Shutdown();
        }

        private void InitializeNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Visible = false,
                Text = "TriggerLAG"
            };

            try
            {
                var iconUri = new Uri("pack://application:,,,/TriggerLAG.ico");
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    _notifyIcon.Icon = new System.Drawing.Icon(streamInfo.Stream);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("Failed to load tray icon from resources", ex);
            }

            _notifyIcon.MouseDoubleClick += OnTrayMouseDoubleClick;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => ShowWindow());
            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                Application.Current.Shutdown();
            });
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void OnTrayMouseDoubleClick(object? sender, System.Windows.Forms.MouseEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            if (_notifyIcon != null) _notifyIcon.Visible = false;
        }

        private void LoadConfig()
        {
            var config = ConfigManager.Load();

            ModeCombo.SelectedIndex = config.ModeIndex;
            CheckInbound.IsChecked = config.Inbound;
            CheckOutbound.IsChecked = config.Outbound;
            ValueSlider.Value = config.Intensity;
            CheckMacroEnabled.IsChecked = config.MacroEnabled;

            _boundKey = config.BoundKey;
            _boundMouseBtn = config.BoundMouseBtn;
            BindKeyText.Text = config.BoundKeyName;
            
            
            if (ComboNotifyStyle != null) ComboNotifyStyle.SelectedIndex = config.NotificationStyle;

            
            _doubleEditTriggerKey = config.DoubleEditKey;
            _doubleEditTriggerMouseBtn = config.DoubleEditMouseBtn;
            _doubleEditEnabled = config.DoubleEditEnabled;
            if (CheckDoubleEdit != null)
            {
                CheckDoubleEdit.Checked -= CheckDoubleEdit_Checked;
                CheckDoubleEdit.IsChecked = config.DoubleEditEnabled;
                CheckDoubleEdit.Checked += CheckDoubleEdit_Checked;
            }
            UpdateKeyText(TxtDoubleEditTrigger, _doubleEditTriggerKey, _doubleEditTriggerMouseBtn);

            _macroEngine.MacroEditKey = config.DoubleEditKeyEdit;
            _macroEngine.MacroConfirmKey = config.DoubleEditKeyConfirm;
            _macroEngine.MacroEditKeyMouse = config.DoubleEditKeyEditMouse;
            _macroEngine.MacroConfirmKeyMouse = config.DoubleEditKeyConfirmMouse;
            _macroEngine.MacroDelay = config.DoubleEditDelay;
            _macroEngine.MacroDuration = config.DoubleEditDuration;
            _macroEngine.DoubleEditMode = config.DoubleEditMode;

            
            _itemCollectTriggerKey = config.ItemCollectTriggerKey;
            _itemCollectTriggerMouseBtn = config.ItemCollectTriggerMouseBtn;
            _itemCollectEnabled = config.ItemCollectEnabled;
            if (CheckItemCollect != null)
            {
                CheckItemCollect.Checked -= CheckItemCollect_Checked;
                CheckItemCollect.IsChecked = config.ItemCollectEnabled;
                CheckItemCollect.Checked += CheckItemCollect_Checked;
            }
            UpdateKeyText(TxtItemCollectTrigger, _itemCollectTriggerKey, _itemCollectTriggerMouseBtn);

            _macroEngine.ItemCollectBindKey = config.ItemCollectBindKey;
            _macroEngine.ItemCollectBindMouseBtn = config.ItemCollectBindMouseBtn;
            _macroEngine.ItemCollectMode = config.ItemCollectMode;

            if (config.HotkeyMode == 1) RadioHold.IsChecked = true;
            else RadioToggle.IsChecked = true;

            if (this.FindName("CheckFortniteOnly") is CheckBox checkFortnite)
            {
                checkFortnite.IsChecked = config.FortniteOnly;
                _networkEngine.FortniteOnly = config.FortniteOnly;
            }
            
            SyncNetworkEngineConfig();
        }

        private void SyncNetworkEngineConfig()
        {
            int val = (int)ValueSlider.Value;
            _networkEngine.Intensity = val * 5;
            
            _networkEngine.Inbound = CheckInbound.IsChecked == true;
            _networkEngine.Outbound = CheckOutbound.IsChecked == true;
            
            _networkEngine.IsInvisiblePeek = ModeCombo.SelectedIndex == 0;
            _networkEngine.IsDelayPeek = ModeCombo.SelectedIndex == 1;
            _networkEngine.IsStaticLag = ModeCombo.SelectedIndex == 2;
            _networkEngine.IsAdaptiveLag = ModeCombo.SelectedIndex == 3;
            
            _networkEngine.DelayPeekDuration = (int)DelayPeekSlider.Value;
            
            _networkEngine.IsDropMode = false;
            _networkEngine.IsBulletStacking = false;
            _networkEngine.IsManualMode = false;
            _networkEngine.IsSnapshotTrap = false;
            _networkEngine.IsVelocitySpoof = false;
            _networkEngine.IsPhasePeak = false;
            
            if (_networkEngine.IsStaticLag) _networkEngine.Intensity = val * 10;
            if (_networkEngine.IsAdaptiveLag) _networkEngine.Intensity = val * 10;
            
            
            if (this.FindName("CheckHighPriority") is CheckBox checkPriority)
            {
                _networkEngine.HighPriority = checkPriority.IsChecked == true;
            }
            
            if (this.FindName("CheckFortniteOnly") is CheckBox checkFortnite)
            {
                _networkEngine.FortniteOnly = checkFortnite.IsChecked == true;
            }
        }

        private void UpdateConfig(Action<AppConfig> updateAction)
        {
            var config = ConfigManager.Load();
            updateAction(config);
            ConfigManager.Save(config);
        }

        private void InitializeHooks()
        {
            try
            {
                _keyboardHook = new GlobalKeyboardHook();
                _keyboardHook.SetCallback(OnKeyPressed);

                _mouseHook = new GlobalMouseHook();
                _mouseHook.MouseEvent += OnMousePressed;
            }
            catch (Exception ex)
            {
                ErrorLogger.Log("Failed to initialize global hooks", ex);
            }
        }

        private void OnKeyPressed(int vkCode, bool isDown)
        {
            if (_bindingContext != BindingContext.None) return;

            
            if (vkCode == _boundKey && CheckMacroEnabled.IsChecked == true)
            {
                bool isHold = RadioHold.IsChecked == true;
                if (isHold)
                {
                    if (isDown) Start();
                    else Stop();
                }
                else if (isDown) 
                {
                    Toggle();
                }
            }

            
            if (_doubleEditEnabled && vkCode == _doubleEditTriggerKey)
            {
                if (_macroEngine.DoubleEditMode == 0) 
                {
                    if (isDown)
                    {
                        if (_macroEngine.IsDoubleEditRunning) _macroEngine.StopDoubleEditLoop();
                        else _macroEngine.StartDoubleEditLoop();
                    }
                }
                else 
                {
                    if (isDown) _macroEngine.StartDoubleEditLoop();
                    else _macroEngine.StopDoubleEditLoop();
                }
            }

            
            if (_itemCollectEnabled && vkCode == _itemCollectTriggerKey)
            {
                if (_macroEngine.ItemCollectMode == 0) 
                {
                    if (isDown)
                    {
                        if (_macroEngine.IsItemCollectRunning) _macroEngine.StopItemCollectLoop();
                        else _macroEngine.StartItemCollectLoop();
                    }
                }
                else 
                {
                    if (isDown) _macroEngine.StartItemCollectLoop();
                    else _macroEngine.StopItemCollectLoop();
                }
            }
        }

        private void OnMousePressed(int buttonCode, bool isDown)
        {
            if (_bindingContext != BindingContext.None)
            {
                
                if (isDown)
                {
                    if (_bindingContext == BindingContext.MainHotkey)
                    {
                        _boundMouseBtn = buttonCode;
                        _boundKey = 0;
                        BindKeyText.Text = GetMouseName(buttonCode);
                        
                        UpdateConfig(c => {
                            c.BoundMouseBtn = buttonCode;
                            c.BoundKey = 0;
                            c.BoundKeyName = BindKeyText.Text;
                        });

                        _bindingContext = BindingContext.None;
                        BindKeyText.ClearValue(Control.BackgroundProperty);
                        BindKeyText.ClearValue(Control.BorderBrushProperty);
                    }
                    else if (_bindingContext == BindingContext.DoubleEditTrigger)
                    {
                        _doubleEditTriggerMouseBtn = buttonCode;
                        _doubleEditTriggerKey = 0;
                        UpdateKeyText(TxtDoubleEditTrigger, 0, buttonCode);
                        _bindingContext = BindingContext.None;
                        TxtDoubleEditTrigger.ClearValue(Control.BackgroundProperty);
                        TxtDoubleEditTrigger.ClearValue(Control.BorderBrushProperty);
                    }
                    else if (_bindingContext == BindingContext.ItemCollectTrigger)
                    {
                        _itemCollectTriggerMouseBtn = buttonCode;
                        _itemCollectTriggerKey = 0;
                        UpdateKeyText(TxtItemCollectTrigger, 0, buttonCode);
                        _bindingContext = BindingContext.None;
                        TxtItemCollectTrigger.ClearValue(Control.BackgroundProperty);
                        TxtItemCollectTrigger.ClearValue(Control.BorderBrushProperty);
                    }
                }
                return;
            }

            
            if (buttonCode == _boundMouseBtn && CheckMacroEnabled.IsChecked == true)
            {
                bool isHold = RadioHold.IsChecked == true;
                if (isHold)
                {
                    if (isDown) Start();
                    else Stop();
                }
                else if (isDown)
                {
                    Toggle();
                }
            }

            if (_doubleEditEnabled && buttonCode == _doubleEditTriggerMouseBtn)
            {
                if (_macroEngine.DoubleEditMode == 0)
                {
                    if (isDown)
                    {
                        if (_macroEngine.IsDoubleEditRunning) _macroEngine.StopDoubleEditLoop();
                        else _macroEngine.StartDoubleEditLoop();
                    }
                }
                else
                {
                    if (isDown) _macroEngine.StartDoubleEditLoop();
                    else _macroEngine.StopDoubleEditLoop();
                }
            }

            if (_itemCollectEnabled && buttonCode == _itemCollectTriggerMouseBtn)
            {
                if (_macroEngine.ItemCollectMode == 0)
                {
                    if (isDown)
                    {
                        if (_macroEngine.IsItemCollectRunning) _macroEngine.StopItemCollectLoop();
                        else _macroEngine.StartItemCollectLoop();
                    }
                }
                else
                {
                    if (isDown) _macroEngine.StartItemCollectLoop();
                    else _macroEngine.StopItemCollectLoop();
                }
            }
        }

        private string GetMouseName(int code)
        {
            return code switch
            {
                GlobalMouseHook.MOUSE_LEFT => "Left Click",
                GlobalMouseHook.MOUSE_RIGHT => "Right Click",
                GlobalMouseHook.MOUSE_MIDDLE => "Middle Click",
                GlobalMouseHook.MOUSE_X1 => "Mouse 4",
                GlobalMouseHook.MOUSE_X2 => "Mouse 5",
                _ => $"Mouse {code}"
            };
        }

        private string GenerateRandomTitle(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new System.Text.StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                result.Append(chars[random.Next(chars.Length)]);
            }
            return result.ToString();
        }

        private void Toggle()
        {
            if (_networkEngine.IsRunning) Stop();
            else Start();
        }

        private void Start()
        {
            SyncNetworkEngineConfig();
            _networkEngine.Start();

            if (_networkEngine.IsDelayPeek)
            {
                int durationMs = _networkEngine.DelayPeekDuration;
                Task.Delay(durationMs).ContinueWith(t => {
                    Dispatcher.Invoke(() => {
                        if (_networkEngine.IsRunning) Stop();
                    });
                });
            }
        }

        private void Stop()
        {
            _networkEngine.Stop();
        }

        private void UpdateUI(bool running)
        {
            StatusText.Text = running ? "Running..." : "";
            StatusText.Foreground = running ? (Brush)FindResource("BrushAccent") : (Brush)FindResource("BrushTextSecondary");
            BtnStart.Content = running ? "STOP" : "START";

            if (running)
            {
                BtnStart.Background = (Brush)FindResource("BrushDanger");
                BtnStart.Foreground = Brushes.White;
            }
            else
            {
                BtnStart.ClearValue(Button.BackgroundProperty);
                BtnStart.ClearValue(Button.ForegroundProperty);
                BtnStart.Style = (Style)FindResource("StylePrimaryButton");
            }

            ModeCombo.IsEnabled = !running;
            CheckInbound.IsEnabled = !running;
            CheckOutbound.IsEnabled = !running;
            
            ShowNotification(running);
        }
        
        private void ShowNotification(bool isOn)
        {
            
            if (ConfigManager.Load().NotificationsEnabled)
            {
                if (_currentNotification != null && _currentNotification.IsLoaded)
                {
                    _currentNotification.UpdateState(isOn);
                }
                else
                {
                    int style = ComboNotifyStyle?.SelectedIndex ?? 0;
                    _currentNotification = new NotificationWindow(isOn, style);
                    _currentNotification.Closed += (s, e) => _currentNotification = null;
                    _currentNotification.Show();
                }
            }
        }

        private void UpdateKeyText(TextBox box, int key, int mouseBtn = 0)
        {
            if (key != 0)
            {
                try
                {
                    var k = KeyInterop.KeyFromVirtualKey(key);
                    box.Text = k.ToString();
                }
                catch
                {
                    box.Text = $"Key {key}";
                }
            }
            else if (mouseBtn != 0)
            {
                box.Text = GetMouseName(mouseBtn);
            }
            else
            {
                box.Text = "None";
            }
        }

        private void CheckDoubleEdit_Checked(object sender, RoutedEventArgs e)
        {
            _doubleEditEnabled = true;
            OpenMacroSettings();
        }

        private void CheckDoubleEdit_Unchecked(object sender, RoutedEventArgs e)
        {
            _doubleEditEnabled = false;
        }

        private void BtnDoubleEditSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenMacroSettings();
        }

        private void OpenMacroSettings()
        {
            var win = new MacroSettingsWindow(_mouseHook);
            win.Owner = this;
            win.ShowDialog();

            var config = ConfigManager.Load();
            _macroEngine.MacroEditKey = config.DoubleEditKeyEdit;
            _macroEngine.MacroConfirmKey = config.DoubleEditKeyConfirm;
            _macroEngine.MacroEditKeyMouse = config.DoubleEditKeyEditMouse;
            _macroEngine.MacroConfirmKeyMouse = config.DoubleEditKeyConfirmMouse;
            _macroEngine.MacroDelay = config.DoubleEditDelay;
            _macroEngine.MacroDuration = config.DoubleEditDuration;
            _macroEngine.DoubleEditMode = config.DoubleEditMode;
        }

        private void TxtDoubleEditTrigger_GotFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.DoubleEditTrigger;
            TxtDoubleEditTrigger.Text = "Press key...";
            TxtDoubleEditTrigger.Background = (Brush)FindResource("BrushSurfaceHighlight");
            TxtDoubleEditTrigger.BorderBrush = (Brush)FindResource("BrushAccent");
        }

        private void TxtDoubleEditTrigger_LostFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.None;
            TxtDoubleEditTrigger.ClearValue(Control.BackgroundProperty);
            TxtDoubleEditTrigger.ClearValue(Control.BorderBrushProperty);
            UpdateKeyText(TxtDoubleEditTrigger, _doubleEditTriggerKey, _doubleEditTriggerMouseBtn);
        }

        private void TxtDoubleEditTrigger_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_bindingContext != BindingContext.DoubleEditTrigger) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                return;
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.System)
                return;

            _doubleEditTriggerKey = KeyInterop.VirtualKeyFromKey(e.Key);
            _doubleEditTriggerMouseBtn = 0;
            _bindingContext = BindingContext.None;
            UpdateKeyText(TxtDoubleEditTrigger, _doubleEditTriggerKey, 0);
            Keyboard.ClearFocus();
        }

        private void BtnClearDoubleEditTrigger_Click(object sender, RoutedEventArgs e)
        {
            _doubleEditTriggerKey = 0;
            _doubleEditTriggerMouseBtn = 0;
            UpdateKeyText(TxtDoubleEditTrigger, 0, 0);
        }

        private void CheckItemCollect_Checked(object sender, RoutedEventArgs e)
        {
            _itemCollectEnabled = true;
            OpenItemCollectSettings();
        }

        private void CheckItemCollect_Unchecked(object sender, RoutedEventArgs e)
        {
            _itemCollectEnabled = false;
        }

        private void BtnItemCollectSettings_Click(object sender, RoutedEventArgs e)
        {
            OpenItemCollectSettings();
        }

        private void OpenItemCollectSettings()
        {
            var win = new ItemCollectSettingsWindow(_mouseHook);
            win.Owner = this;
            win.ShowDialog();

            var config = ConfigManager.Load();
            _macroEngine.ItemCollectBindKey = config.ItemCollectBindKey;
            _macroEngine.ItemCollectBindMouseBtn = config.ItemCollectBindMouseBtn;
            _macroEngine.ItemCollectMode = config.ItemCollectMode;
        }

        private void TxtItemCollectTrigger_GotFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.ItemCollectTrigger;
            TxtItemCollectTrigger.Text = "Press key...";
            TxtItemCollectTrigger.Background = (Brush)FindResource("BrushSurfaceHighlight");
            TxtItemCollectTrigger.BorderBrush = (Brush)FindResource("BrushAccent");
        }

        private void TxtItemCollectTrigger_LostFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.None;
            TxtItemCollectTrigger.ClearValue(Control.BackgroundProperty);
            TxtItemCollectTrigger.ClearValue(Control.BorderBrushProperty);
            UpdateKeyText(TxtItemCollectTrigger, _itemCollectTriggerKey, _itemCollectTriggerMouseBtn);
        }

        private void TxtItemCollectTrigger_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_bindingContext != BindingContext.ItemCollectTrigger) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                return;
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.System)
                return;

            _itemCollectTriggerKey = KeyInterop.VirtualKeyFromKey(e.Key);
            _itemCollectTriggerMouseBtn = 0;
            _bindingContext = BindingContext.None;
            UpdateKeyText(TxtItemCollectTrigger, _itemCollectTriggerKey, 0);
            Keyboard.ClearFocus();
        }

        private void BtnClearItemCollectTrigger_Click(object sender, RoutedEventArgs e)
        {
            _itemCollectTriggerKey = 0;
            _itemCollectTriggerMouseBtn = 0;
            UpdateKeyText(TxtItemCollectTrigger, 0, 0);
        }

        private void CheckTcpNoDelay_Click(object sender, RoutedEventArgs e)
        {
            _systemOptimizer.ApplyTcpRegistryTweak("TcpNoDelay", CheckTcpNoDelay.IsChecked == true ? 1 : 0);
        }

        private void CheckTcpAckFreq_Click(object sender, RoutedEventArgs e)
        {
            _systemOptimizer.ApplyTcpRegistryTweak("TcpAckFrequency", CheckTcpAckFreq.IsChecked == true ? 1 : 2);
        }

        private void BtnFlushDns_Click(object sender, RoutedEventArgs e)
        {
            _systemOptimizer.FlushDns();
        }

        private void BtnResetWinsock_Click(object sender, RoutedEventArgs e)
        {
            _systemOptimizer.ResetWinsock();
            MessageBox.Show("Winsock reset successfully.\nPlease restart your computer.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CheckFortniteQoS_Click(object sender, RoutedEventArgs e)
        {
            bool enabled = CheckFortniteQoS.IsChecked == true;
            _systemOptimizer.ApplyFortniteQosPolicy(enabled);

            if (enabled) _systemOptimizer.StartFortniteOptimization();
            else _systemOptimizer.StopFortniteOptimization();
        }

        
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
                UpdateSatellitePosition();
            }
        }

        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            UpdateSatellitePosition();
        }

        private void UpdateSatellitePosition()
        {
            if (_modeInfoWindow == null || !_modeInfoWindow.IsVisible) 
            {
                _handleSatellite = IntPtr.Zero;
                return;
            }

            if (_handleOwner == IntPtr.Zero) _handleOwner = new WindowInteropHelper(this).Handle;
            if (_handleSatellite == IntPtr.Zero) _handleSatellite = new WindowInteropHelper(_modeInfoWindow).Handle;
            
            if (_handleOwner == IntPtr.Zero || _handleSatellite == IntPtr.Zero) return;

            int targetX = (int)(this.Left + this.ActualWidth + 5);
            int targetY = (int)(this.Top + (this.ActualHeight - _modeInfoWindow.ActualHeight) / 2);

            NativeMethods.SetWindowPos(_handleSatellite, IntPtr.Zero, targetX, targetY, 0, 0, NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER | NativeMethods.SWP_NOACTIVATE);
        }

        
        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var win = new SettingsWindow();
            win.Owner = this;
            win.ShowDialog();
            LoadConfig();
            
            
            _modeInfoWindow?.UpdateMode(ModeCombo.SelectedIndex);
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            var win = new ExitConfirmationWindow();
            win.Owner = this;
            if (win.ShowDialog() == true)
            {
                if (win.Action == ExitAction.HideToTray)
                {
                    this.Hide();
                    if (_notifyIcon != null) _notifyIcon.Visible = true;
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
        }

        
        private void BindKeyText_GotFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.MainHotkey;
            BindKeyText.Text = (string)FindResource("StrPressKey");
            BindKeyText.Background = (Brush)FindResource("BrushSurfaceHighlight");
            BindKeyText.BorderBrush = (Brush)FindResource("BrushAccent");
        }

        private void BindKeyText_LostFocus(object sender, RoutedEventArgs e)
        {
            _bindingContext = BindingContext.None;
            BindKeyText.ClearValue(Control.BackgroundProperty);
            BindKeyText.ClearValue(Control.BorderBrushProperty);
            
            var config = ConfigManager.Load();
            BindKeyText.Text = config.BoundKeyName;
        }

        private void BindKeyText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_bindingContext != BindingContext.MainHotkey) return;
            e.Handled = true;

            if (e.Key == Key.Escape)
            {
                Keyboard.ClearFocus();
                return;
            }

            if (e.Key == Key.LeftShift || e.Key == Key.RightShift ||
                e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl ||
                e.Key == Key.LeftAlt || e.Key == Key.RightAlt ||
                e.Key == Key.System)
                return;

            _boundKey = KeyInterop.VirtualKeyFromKey(e.Key);
            _boundMouseBtn = 0;
            BindKeyText.Text = e.Key.ToString();
            
            UpdateConfig(c => {
                c.BoundKey = _boundKey;
                c.BoundMouseBtn = 0;
                c.BoundKeyName = e.Key.ToString();
            });

            _bindingContext = BindingContext.None;
            Keyboard.ClearFocus();
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            Toggle();
        }

        private void CheckInbound_Click(object sender, RoutedEventArgs e)
        {
            _networkEngine.Inbound = CheckInbound.IsChecked == true;
            UpdateConfig(c => c.Inbound = _networkEngine.Inbound);
        }

        private void CheckOutbound_Click(object sender, RoutedEventArgs e)
        {
            _networkEngine.Outbound = CheckOutbound.IsChecked == true;
            UpdateConfig(c => c.Outbound = _networkEngine.Outbound);
        }

        private void ModeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DelayPeekSettingsPanel != null)
                DelayPeekSettingsPanel.Visibility = ModeCombo.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
            
            if (ModeCombo.SelectedIndex == 3)
            {
                
                if (CheckInbound != null) CheckInbound.IsChecked = false;
                if (CheckOutbound != null && CheckOutbound.IsChecked != true) CheckOutbound.IsChecked = true;
            }
            
            SyncNetworkEngineConfig();
            UpdateConfig(c => c.ModeIndex = ModeCombo.SelectedIndex);
            _modeInfoWindow?.UpdateMode(ModeCombo.SelectedIndex);
        }

        private void BtnModeInfo_Click(object sender, RoutedEventArgs e)
        {
            if (_modeInfoVisible)
            {
                _modeInfoWindow?.FadeOut();
                _modeInfoVisible = false;
            }
            else
            {
                if (_modeInfoWindow == null || !_modeInfoWindow.IsLoaded)
                {
                    _modeInfoWindow = new ModeInfoWindow();
                    _modeInfoWindow.Owner = this;
                    _modeInfoWindow.Closed += (s, args) => { _modeInfoWindow = null; _modeInfoVisible = false; };
                    _modeInfoWindow.InboundClicked += HighlightInbound;
                }
                _modeInfoWindow.AttachToWindow(this);
                _modeInfoWindow.UpdateMode(ModeCombo.SelectedIndex);
                _modeInfoWindow.Show();
                _modeInfoWindow.FadeIn();
                _modeInfoVisible = true;
            }
        }

        private async void HighlightInbound()
        {
            if (SpotlightOverlay == null || HighlightRect == null || CheckInbound == null) return;

            
            System.Windows.Point relativePoint = CheckInbound.TransformToAncestor(MainBorder).Transform(new System.Windows.Point(0, 0));
            
            double padding = 8;
            Rect targetRect = new Rect(
                relativePoint.X - padding, 
                relativePoint.Y - padding, 
                CheckInbound.ActualWidth + padding * 2, 
                CheckInbound.ActualHeight + padding * 2
            );

            
            Rect startRect = new Rect(
                targetRect.X + targetRect.Width / 2, 
                targetRect.Y + targetRect.Height / 2, 
                0, 0
            );

            HighlightRect.Rect = startRect;

            
            var duration = TimeSpan.FromMilliseconds(300);
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };

            var fadeIn = new DoubleAnimation(0, 0.7, duration) { EasingFunction = ease };
            var expandRect = new RectAnimation(startRect, targetRect, duration) { EasingFunction = ease };

            
            SpotlightOverlay.BeginAnimation(OpacityProperty, fadeIn);
            HighlightRect.BeginAnimation(RectangleGeometry.RectProperty, expandRect);

            await Task.Delay(1200);

            
            
            if (SpotlightOverlay == null || HighlightRect == null || !this.IsLoaded) return;

            var fadeOut = new DoubleAnimation(0.7, 0, duration) { EasingFunction = ease };
            var shrinkRect = new RectAnimation(targetRect, startRect, duration) { EasingFunction = ease };

            SpotlightOverlay.BeginAnimation(OpacityProperty, fadeOut);
            HighlightRect.BeginAnimation(RectangleGeometry.RectProperty, shrinkRect);
        }

        private void DelayPeekSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DelayPeekDurationText != null)
            {
                int ms = (int)e.NewValue;
                if (ms < 1000)
                    DelayPeekDurationText.Text = $"{ms}ms";
                else
                    DelayPeekDurationText.Text = $"{(ms / 1000.0):F1}s";
                    
                if (_networkEngine != null) _networkEngine.DelayPeekDuration = ms;
            }
        }

        private void ValueSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_networkEngine != null) _networkEngine.Intensity = (int)e.NewValue;
            
        }

        private void OptimizerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
             
        }

        private void RadioHold_Checked(object sender, RoutedEventArgs e)
        {
            UpdateConfig(c => c.HotkeyMode = 1);
        }

        private void RadioToggle_Checked(object sender, RoutedEventArgs e)
        {
            UpdateConfig(c => c.HotkeyMode = 0);
        }

        

        private void Nav_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rb && rb.Tag is string tag)
            {
                UIElement? target = null;
                if (TabHome != null) TabHome.Visibility = tag == "Home" ? Visibility.Visible : Visibility.Hidden;
                if (TabLagTrigger != null) TabLagTrigger.Visibility = tag == "LagTrigger" ? Visibility.Visible : Visibility.Hidden;
                if (TabHotkeys != null) TabHotkeys.Visibility = tag == "Hotkeys" ? Visibility.Visible : Visibility.Hidden;
                if (TabMacros != null) TabMacros.Visibility = tag == "Macros" ? Visibility.Visible : Visibility.Hidden;
                if (TabOptimizer != null) TabOptimizer.Visibility = tag == "Optimizer" ? Visibility.Visible : Visibility.Hidden;
                if (TabAdvanced != null) TabAdvanced.Visibility = tag == "Advanced" ? Visibility.Visible : Visibility.Hidden;
                if (TabHelp != null) TabHelp.Visibility = tag == "Help" ? Visibility.Visible : Visibility.Hidden;

                switch (tag)
                {
                    case "Home": target = TabHome; break;
                    case "LagTrigger": target = TabLagTrigger; break;
                    case "Hotkeys": target = TabHotkeys; break;
                    case "Macros": target = TabMacros; break;
                    case "Optimizer": target = TabOptimizer; break;
                    case "Advanced": target = TabAdvanced; break;
                    case "Help": target = TabHelp; break;
                }

                if (target is FrameworkElement fe)
                {
                    var fadeIn = (System.Windows.Media.Animation.Storyboard)this.Resources["FadeIn"];
                    fadeIn?.Begin(fe);
                }
            }
        }

        private void BtnMinimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            
            BtnExit_Click(sender, e);
        }

        private void BtnDefaultSettings_Click(object sender, RoutedEventArgs e)
        {
             
             UpdateConfig(c => {
                 c.ModeIndex = 0;
                 c.Inbound = true;
                 c.Outbound = true;
                 c.Intensity = 50;
                 c.MacroEnabled = false;
             });
             LoadConfig(); 
             MessageBox.Show("Settings have been reset to defaults.", "TriggerLAG", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnTelegram_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://t.me/frakture") { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open Telegram: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClearBind_Click(object sender, RoutedEventArgs e)
        {
            _boundKey = 0;
            _boundMouseBtn = 0;
            BindKeyText.Text = "None";
            UpdateConfig(c => {
                c.BoundKey = 0;
                c.BoundMouseBtn = 0;
                c.BoundKeyName = "None";
            });
        }

        private void AdvancedScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            
        }

        private void ComboNotifyStyle_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboNotifyStyle.SelectedIndex >= 0)
            {
                UpdateConfig(c => c.NotificationStyle = ComboNotifyStyle.SelectedIndex);
            }
        }
    }
}