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
using System.Windows.Shapes;
using Pronunciation.Core.Audio;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for WaveFormsComparison.xaml
    /// </summary>
    public partial class WaveFormsComparison : Window
    {
        public PlaybackResult ReferenceResult { get; set; }
        public PlaybackResult RecordedResult { get; set; }

        public WaveFormsComparison()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReferenceResult != null)
            {
                lblFooterReference.Text = BuildDurationText(ReferenceResult.SamplesDuration, ReferenceResult.TotalDuration);
                waveReference.AudioSamples = ReferenceResult.Samples;              
            }

            if (RecordedResult != null)
            {
                lblFooterRecorded.Text = BuildDurationText(RecordedResult.SamplesDuration, RecordedResult.TotalDuration);
                waveRecorded.AudioSamples = RecordedResult.Samples;  
            }

            if (RecordedResult != null && ReferenceResult != null)
            {
                if (ReferenceResult.SamplesDuration > RecordedResult.SamplesDuration)
                {
                    waveRecorded.WidthFactor = CalculateWidthFactor(ReferenceResult.SamplesDuration, RecordedResult.SamplesDuration);
                }
                else
                {
                    waveReference.WidthFactor = CalculateWidthFactor(RecordedResult.SamplesDuration, ReferenceResult.SamplesDuration);
                }
            }

            waveReference.DrawWaveForm();
            waveRecorded.DrawWaveForm();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private string BuildDurationText(TimeSpan samplesDuration, TimeSpan totalDuration)
        {
            string text = string.Format("Duration: {0}s", BuildDurationValue(samplesDuration));
            if (samplesDuration != totalDuration)
            {
                text += string.Format(" (total duration: {0}s)", BuildDurationValue(totalDuration));
            }

            return text;
        }

        private string BuildDurationValue(TimeSpan duration)
        {
            return string.Format("{0:0.#}", duration.TotalSeconds);
        }

        private static double CalculateWidthFactor(TimeSpan baseDuration, TimeSpan duration)
        {
            if (baseDuration.TotalMilliseconds == 0)
                return 1;

            return duration.TotalMilliseconds/baseDuration.TotalMilliseconds;
        }
    }
}
