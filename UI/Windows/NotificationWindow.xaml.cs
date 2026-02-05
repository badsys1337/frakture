using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using TriggerLAG.Core;

using Color = System.Windows.Media.Color;

namespace TriggerLAG
{
    public partial class NotificationWindow : Window
    {
        private CancellationTokenSource? _closeCts;
        private int _styleIndex;

        public NotificationWindow(bool isOn, int styleIndex = 0)
        {
            InitializeComponent();
            _styleIndex = styleIndex;

            
            if (_styleIndex == 0)
            {
                StyleMinimal.Visibility = Visibility.Visible;
                StyleModern.Visibility = Visibility.Collapsed;
            }
            else
            {
                StyleMinimal.Visibility = Visibility.Collapsed;
                StyleModern.Visibility = Visibility.Visible;
            }
            
            
            this.Opacity = 0;
            UpdateState(isOn);
            
            
            this.Loaded += (s, e) =>
            {
                PositionWindow();
                AnimateIn();
            };
        }

        private TranslateTransform GetCurrentSlideTransform()
        {
            return _styleIndex == 0 ? SlideTransformMinimal : SlideTransformModern;
        }

        public void UpdateState(bool isOn)
        {
            string text = isOn ? "TRIGGERLAG ENABLED" : "TRIGGERLAG DISABLED";
            
            if (_styleIndex == 0)
            {
                MessageTextMinimal.Text = text;
                
                StatusIndicatorMinimal.Fill = new SolidColorBrush(Color.FromRgb(220, 220, 220));
            }
            else
            {
                MessageTextModern.Text = text;
            }
            
            
            ResetAutoCloseTimer();
        }

        private void PositionWindow()
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.ActualWidth - 20;
            this.Top = workArea.Bottom - this.ActualHeight - 20;
        }

        private void AnimateIn()
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var slideIn = new DoubleAnimation
            {
                From = 300, 
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseOut }
            };

            this.BeginAnimation(OpacityProperty, fadeIn);
            GetCurrentSlideTransform().BeginAnimation(TranslateTransform.XProperty, slideIn);
            
            ResetAutoCloseTimer();
        }

        private void ResetAutoCloseTimer()
        {
            _closeCts?.Cancel();
            _closeCts = new CancellationTokenSource();
            var token = _closeCts.Token;

            Task.Delay(2000, token).ContinueWith(t =>
            {
                if (t.IsCanceled) return;
                Dispatcher.Invoke(() =>
                {
                    if (!IsLoaded) return;
                    AnimateOut();
                });
            });
        }

        private void AnimateOut()
        {
            var fadeOut = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300)
            };

            var slideOut = new DoubleAnimation
            {
                To = 300,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuarticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOut.Completed += (s, e) => this.Close();

            this.BeginAnimation(OpacityProperty, fadeOut);
            GetCurrentSlideTransform().BeginAnimation(TranslateTransform.XProperty, slideOut);
        }
    }
}