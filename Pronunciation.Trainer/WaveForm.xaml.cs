using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for WaveForm.xaml
    /// </summary>
    public partial class WaveForm : UserControl
    {
        private const int PaddingHeight = 5; // for some reason Polyline draws lines over the borders so we have to add a padding
        private const int PaddingWidth = 0;

        private readonly DispatcherTimer _delayedResizeTimer = new DispatcherTimer();
        private const int DelayedResizeIntervalMs = 500;

        public float[] AudioSamples { get; set; }
        public double WidthFactor { get; set; }

        public WaveForm()
        {
            InitializeComponent();

            WidthFactor = 1;
            _delayedResizeTimer.Tick += new EventHandler(_delayedResizeTimer_Tick);
            _delayedResizeTimer.Interval = TimeSpan.FromMilliseconds(DelayedResizeIntervalMs);
        }

        public void Clear()
        {
            waveCanvas.Children.Clear();
        }

        public void DrawWaveForm()
        {
            Clear();
            if (AudioSamples == null)
                return;

            double maxWidth = WidthFactor * (waveCanvas.ActualWidth - 2 * PaddingWidth);
            double maxHeight = waveCanvas.ActualHeight - 2 * PaddingHeight;

            Polyline pl = new Polyline();
            pl.Stroke = Brushes.Blue;
            pl.Name = "waveform";
            pl.StrokeThickness = 1;
            pl.MaxWidth = waveCanvas.ActualWidth;
            pl.MaxHeight = waveCanvas.ActualHeight;

            WaveFormBuilder builder = new WaveFormBuilder();
            var points = builder.DrawNormalizedAudio(AudioSamples, maxWidth, maxHeight);
            foreach (var point in points)
            {
                pl.Points.Add(new Point(PaddingWidth + point.X, PaddingHeight + point.YMin));
                pl.Points.Add(new Point(PaddingWidth + point.X, PaddingHeight + point.YMax));
            }

            waveCanvas.Children.Add(pl);
        }

        private void waveCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (AudioSamples != null)
            {
                if (_delayedResizeTimer.IsEnabled)
                {
                    _delayedResizeTimer.Stop();
                }
                _delayedResizeTimer.Start();
            }
        }

        private void _delayedResizeTimer_Tick(object sender, EventArgs e)
        {
            _delayedResizeTimer.Stop();
            DrawWaveForm();
        }
    }
}
