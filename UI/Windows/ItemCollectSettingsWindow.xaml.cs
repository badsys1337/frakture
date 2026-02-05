using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using TextBox = System.Windows.Controls.TextBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using Control = System.Windows.Controls.Control;
using Brush = System.Windows.Media.Brush;
using TriggerLAG.Core;

namespace TriggerLAG
{
    public partial class ItemCollectSettingsWindow : Window
    {
        private int _collectKey;
        private int _collectKeyMouse;

        private GlobalMouseHook? _mouseHook;
        
        private enum BindingContext { None, Collect }
        private BindingContext _bindingContext = BindingContext.None;
        private TextBox? _currentBindingBox;

        public ItemCollectSettingsWindow(GlobalMouseHook? mouseHook = null)
        {
            InitializeComponent();
            _mouseHook = mouseHook;
            LoadSettings();

            if (_mouseHook != null)
            {
                _mouseHook.MouseEvent += OnMousePressed;
            }
            
            this.Closed += (s, e) => {
                if (_mouseHook != null)
                {
                    _mouseHook.MouseEvent -= OnMousePressed;
                }
            };
        }

        private void LoadSettings()
        {
            var config = ConfigManager.Load();
            _collectKey = config.ItemCollectBindKey;
            _collectKeyMouse = config.ItemCollectBindMouseBtn;
            
            UpdateKeyText(TxtCollectBind, _collectKey, _collectKeyMouse);

            if (config.ItemCollectMode == 0) RadioToggle.IsChecked = true;
            else RadioHold.IsChecked = true;
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
                string btnName = mouseBtn switch
                {
                    GlobalMouseHook.MOUSE_LEFT => "Left Click",
                    GlobalMouseHook.MOUSE_RIGHT => "Right Click",
                    GlobalMouseHook.MOUSE_MIDDLE => "Middle Click",
                    GlobalMouseHook.MOUSE_X1 => "Mouse 4",
                    GlobalMouseHook.MOUSE_X2 => "Mouse 5",
                    _ => $"Mouse {mouseBtn}"
                };
                box.Text = btnName;
            }
            else
            {
                box.Text = "None";
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

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigManager.Load();
            config.ItemCollectBindKey = _collectKey;
            config.ItemCollectBindMouseBtn = _collectKeyMouse;
            
            config.ItemCollectMode = RadioToggle.IsChecked == true ? 0 : 1;
            ConfigManager.Save(config);
            this.Close();
        }

        
        private void TxtCollectBind_GotFocus(object sender, RoutedEventArgs e)
        {
            StartBinding(TxtCollectBind, BindingContext.Collect);
        }

        private void StartBinding(TextBox box, BindingContext context)
        {
            _bindingContext = context;
            _currentBindingBox = box;
            box.Text = "Press key...";
            box.Background = (Brush)FindResource("BrushSurfaceHighlight");
            box.BorderBrush = (Brush)FindResource("BrushAccent");
        }

        private void TxtBind_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox box)
            {
                if (_bindingContext != BindingContext.None && _currentBindingBox == box)
                {
                    
                    _bindingContext = BindingContext.None;
                    _currentBindingBox = null;
                }
                
                box.ClearValue(Control.BackgroundProperty);
                box.ClearValue(Control.BorderBrushProperty);
                
                
                if (box == TxtCollectBind) UpdateKeyText(box, _collectKey, _collectKeyMouse);
            }
        }

        private void TxtCollectBind_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            HandleKeyDown(e, (key) => { _collectKey = key; _collectKeyMouse = 0; }, TxtCollectBind, BindingContext.Collect);
        }

        private void HandleKeyDown(KeyEventArgs e, Action<int> setKey, TextBox box, BindingContext context)
        {
            if (_bindingContext != context) return;
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

            int vk = KeyInterop.VirtualKeyFromKey(e.Key);
            setKey(vk);
            
            FinishBinding(box, vk, 0);
        }

        private void OnMousePressed(int btn, bool isDown)
        {
            if (_bindingContext == BindingContext.None) return;
            if (!isDown) return;

            if (btn == GlobalMouseHook.MOUSE_LEFT || btn == GlobalMouseHook.MOUSE_RIGHT) return;

            Dispatcher.Invoke(() =>
            {
                if (_bindingContext == BindingContext.Collect)
                {
                    _collectKey = 0;
                    _collectKeyMouse = btn;
                    FinishBinding(TxtCollectBind, 0, btn);
                }
            });
        }

        private void FinishBinding(TextBox box, int key, int mouseBtn)
        {
            _bindingContext = BindingContext.None;
            _currentBindingBox = null;
            UpdateKeyText(box, key, mouseBtn);
            Keyboard.ClearFocus();
        }
    }
}
