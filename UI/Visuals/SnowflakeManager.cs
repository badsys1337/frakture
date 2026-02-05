using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Brushes = System.Windows.Media.Brushes;

namespace TriggerLAG
{
    public class SnowflakeManager
    {
        private class Snowflake
        {
            public double X;
            public double Y;
            public double Speed;
            public double Drift;
            public double DriftOffset;
            public Ellipse? Element;
        }

        private readonly Canvas _canvas;
        private readonly List<Snowflake> _snowflakes = new List<Snowflake>();
        private readonly Random _random = new Random();

        public SnowflakeManager(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void Initialize(int count = 60)
        {
            _snowflakes.Clear();
            _canvas.Children.Clear();

            for (int i = 0; i < count; i++)
            {
                var size = _random.NextDouble() * 4 + 2; 
                var opacity = _random.NextDouble() * 0.4 + 0.1; 

                var ellipse = new Ellipse
                {
                    Width = size,
                    Height = size,
                    Fill = Brushes.White,
                    Opacity = opacity,
                    IsHitTestVisible = false
                };

                var snowflake = new Snowflake
                {
                    X = _random.NextDouble() * _canvas.ActualWidth,
                    Y = _random.NextDouble() * _canvas.ActualHeight,
                    Speed = _random.NextDouble() * 1.0 + 0.2, 
                    Drift = _random.NextDouble() * 0.5,
                    DriftOffset = _random.NextDouble() * Math.PI * 2,
                    Element = ellipse
                };

                Canvas.SetLeft(ellipse, snowflake.X);
                Canvas.SetTop(ellipse, snowflake.Y);
                _canvas.Children.Add(ellipse);
                _snowflakes.Add(snowflake);
            }

            CompositionTarget.Rendering -= OnCompositionTargetRendering;
            CompositionTarget.Rendering += OnCompositionTargetRendering;
        }

        private void OnCompositionTargetRendering(object? sender, EventArgs e)
        {
            if (_canvas.ActualHeight == 0 || _canvas.ActualWidth == 0 || !_canvas.IsVisible) return;

            foreach (var flake in _snowflakes)
            {
                if (flake.Element == null) continue;

                flake.Y += flake.Speed;
                flake.X += Math.Sin(flake.Y * 0.02 + flake.DriftOffset) * 0.5;

                
                if (flake.Y > _canvas.ActualHeight)
                {
                    flake.Y = -10;
                    flake.X = _random.NextDouble() * _canvas.ActualWidth;
                }

                
                if (flake.X > _canvas.ActualWidth) flake.X = 0;
                else if (flake.X < 0) flake.X = _canvas.ActualWidth;

                Canvas.SetTop(flake.Element, flake.Y);
                Canvas.SetLeft(flake.Element, flake.X);
            }
        }
        
        public void Stop()
        {
             CompositionTarget.Rendering -= OnCompositionTargetRendering;
        }
    }
}
