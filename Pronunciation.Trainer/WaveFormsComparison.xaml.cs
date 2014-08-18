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
using Pronunciation.Core.Contexts;
using Pronunciation.Core.Actions;
using System.Threading;
using Pronunciation.Core.Threading;
using Pronunciation.Trainer.Utility;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for WaveFormsComparison.xaml
    /// </summary>
    public partial class WaveFormsComparison : Window
    {
        public PlaybackData ReferenceAudio { get; set; }
        public PlaybackData RecordedAudio { get; set; }

        private AudioInfo _referenceInfo;
        private AudioInfo _recordedInfo;
        private CancellationTokenSourceExt _referenceAbort;
        private CancellationTokenSourceExt _recordedAbort;

        private const string StatusLoading = "loading...";

        public WaveFormsComparison()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _referenceInfo = GetAudioInfo(ReferenceAudio);
            _recordedInfo = GetAudioInfo(RecordedAudio);
            if (_referenceInfo != null && _recordedInfo != null)
            {
                if (_referenceInfo.Duration > _recordedInfo.Duration)
                {
                    waveRecorded.WidthFactor = CalculateWidthFactor(_referenceInfo.Duration, _recordedInfo.Duration);
                }
                else
                {
                    waveReference.WidthFactor = CalculateWidthFactor(_recordedInfo.Duration, _referenceInfo.Duration);
                }
            }

            int maxSamplesCount = AppSettings.Instance.MaxSamplesInWaveform;
            if (_referenceInfo != null)
            {
                lblStatusReference.Text = StatusLoading;
                lblFooterReference.Text = BuildDurationText(_referenceInfo.Duration);

                _referenceAbort = new CancellationTokenSourceExt();
                var loadArgs = new LoadSamplesArgs(ReferenceAudio, new AudioSamplesProcessingArgs(
                    true, 
                    CalculateSamplesStep(_referenceInfo.SamplesCount, maxSamplesCount),
                    TimeSpan.Zero,
                    _referenceAbort.Token));
                var samplesLoader = new LoadSamplesAction(() => loadArgs, (x, y) => ProcessSamplesResult(x, y, true));
                samplesLoader.StartAction();

                ReferenceAudio = null;
            }

            if (_recordedInfo != null)
            {
                lblStatusRecorded.Text = StatusLoading;
                lblFooterRecorded.Text = BuildDurationText(_recordedInfo.Duration);

                int skipMs =  AppSettings.Instance.SkipRecordedAudioMs;
                _recordedAbort = new CancellationTokenSourceExt();
                var loadArgs = new LoadSamplesArgs(RecordedAudio, new AudioSamplesProcessingArgs(
                    true, 
                    CalculateSamplesStep(_recordedInfo.SamplesCount, maxSamplesCount), 
                    skipMs <= 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(skipMs), 
                    _recordedAbort.Token));
                var samplesLoader = new LoadSamplesAction(() => loadArgs, (x, y) => ProcessSamplesResult(x, y, false));
                samplesLoader.StartAction();

                RecordedAudio = null;
            }
        }

        private void ProcessSamplesResult(LoadSamplesArgs args, ActionResult<AudioSamples> result, bool isReference)
        {
            if (isReference)
            {
                _referenceAbort = null;
            }
            else
            {
                _recordedAbort = null;
            }

            if (result.Error != null)
            {
                if (result.Error is OperationCanceledException)
                    return;

                throw new Exception(string.Format("There was an error during samples loading for the {0} audio: {1}",
                    isReference ? "reference" : "recorded",
                    result.Error.Message));
            }

            WaveForm wave = isReference ? waveReference : waveRecorded;
            wave.AudioSamples = result.ReturnValue == null ? null : result.ReturnValue.LeftChannel;
            wave.DrawWaveForm();

            TextBlock lblStatus = isReference ? lblStatusReference : lblStatusRecorded;
            lblStatus.Text = string.Empty;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_referenceAbort != null)
            {
               _referenceAbort.Cancel(true);
            }
            if (_recordedAbort != null)
            {
                _recordedAbort.Cancel(true);
            }
        }

        private string BuildDurationText(TimeSpan duration)
        {
            return string.Format("Duration: {0}", FormatHelper.ToTimeString(duration, true));
        }

        private static double CalculateWidthFactor(TimeSpan baseDuration, TimeSpan duration)
        {
            if (baseDuration.TotalMilliseconds == 0)
                return 1;

            return duration.TotalMilliseconds/baseDuration.TotalMilliseconds;
        }

        private static int CalculateSamplesStep(int totalSamplesCount, int maxSamplesCount)
        {
            if (maxSamplesCount == 0 || totalSamplesCount <= maxSamplesCount)
                return 1;

            // So if a result is "1.1" we return "2" which means "collect every second sample"
            return (int)Math.Ceiling((double)totalSamplesCount / (double)maxSamplesCount);
        }

        private static AudioInfo GetAudioInfo(PlaybackData audioData)
        {
            if (audioData == null)
                return null;

            return audioData.IsFilePath
                ? AudioHelper.GetAudioInfo(audioData.FilePath) : AudioHelper.GetAudioInfo(audioData.RawData);
        }

        private class LoadSamplesArgs
        {
            public PlaybackData AudioData;
            public AudioSamplesProcessingArgs ProcessingArgs;

            public LoadSamplesArgs(PlaybackData audioData, AudioSamplesProcessingArgs processingArgs)
            {
                AudioData = audioData;
                ProcessingArgs = processingArgs;
            }
        }

        private class LoadSamplesAction : BackgroundActionWithArgs<LoadSamplesArgs, AudioSamples>
        {
            public LoadSamplesAction(Func<LoadSamplesArgs> argsBuilder,
                Action<LoadSamplesArgs, ActionResult<AudioSamples>> resultProcessor)
                : base(argsBuilder, null, resultProcessor)
            {
                this.Worker = (context, args) => LoadSamples(args);
            }

            private AudioSamples LoadSamples(LoadSamplesArgs args)
            {
                AudioSamples samples;
                if (args.AudioData.IsFilePath)
                {
                    samples = AudioHelper.CollectSamples(args.AudioData.FilePath, args.ProcessingArgs);
                }
                else
                {
                    samples = AudioHelper.CollectSamples(args.AudioData.RawData, args.ProcessingArgs);
                }

                return samples;
            }
        }
    }
}
