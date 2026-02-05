using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Input;
using TriggerLAG.Core;

namespace TriggerLAG
{
    public partial class ModeInfoWindow : Window
    {
        private SnowflakeManager? _snowflakeManager;
        public event Action? InboundClicked;

        public ModeInfoWindow()
        {
            InitializeComponent();
            _snowflakeManager = new SnowflakeManager(SnowCanvas);
            this.Opacity = 0;
            
            this.Loaded += (s, e) => {
                _snowflakeManager.Initialize(30);
            };
        }

        public void UpdateMode(int modeIndex)
        {
            string key = modeIndex switch
            {
                0 => "StrModeWorldFreezeNote",
                1 => "StrModeStaticLagNote",
                _ => ""
            };

            if (string.IsNullOrEmpty(key)) return;

            string fullText = FindResource(key)?.ToString() ?? "";
            TxtDescription.Inlines.Clear();

            
            int lastPos = 0;
            while (true)
            {
                int start = fullText.IndexOf('[', lastPos);
                if (start == -1) break;
                int end = fullText.IndexOf(']', start);
                if (end == -1) break;

                
                TxtDescription.Inlines.Add(new Run(fullText.Substring(lastPos, start - lastPos)));

                
                string word = fullText.Substring(start + 1, end - start - 1);
                var run = new Run(word) { Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(150, 150, 150)), Cursor = System.Windows.Input.Cursors.Hand };
                run.MouseDown += (s, e) => InboundClicked?.Invoke();
                TxtDescription.Inlines.Add(run);

                lastPos = end + 1;
            }
            
            if (lastPos < fullText.Length)
                TxtDescription.Inlines.Add(new Run(fullText.Substring(lastPos)));
        }

        public void AttachToWindow(Window owner)
        {
            this.Left = owner.Left + owner.Width + 5;
            this.Top = owner.Top + (owner.Height - this.Height) / 2;
        }

        public void FadeIn()
        {
            
            var anim = new DoubleAnimation
            {
                To = 1,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            this.BeginAnimation(OpacityProperty, anim);
        }

        public void FadeOut()
        {
            var anim = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };
            anim.Completed += (s, e) => this.Hide();
            this.BeginAnimation(OpacityProperty, anim);
        }
    }
}
