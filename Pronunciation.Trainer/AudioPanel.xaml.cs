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

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;

using Pronunciation.Core.Actions;
using Pronunciation.Core.Audio;
using Pronunciation.Trainer.AudioActions;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for AudioPanel.xaml
    /// </summary>
    public partial class AudioPanel : UserControl
    {
        public delegate void RecordingCompletedHandler(string recordedFilePath);

        private class DelayedActionArgs
        {
            public BackgroundAction Target;
        }

        private DispatcherTimer _delayedActionTimer;
        private DelayedActionArgs _delayedActionContext;

        private IAudioContext _audioContext;
        private BackgroundAction _activeAction;
        private ActionButton[] _actionButtons; 

        private const string _recordProgressTemplate = "Recording, {0} seconds left..";
        private const int DelayedPlayIntervalMs = 500;

        public event RecordingCompletedHandler RecordingCompleted;

        public AudioPanel()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _delayedActionTimer = new DispatcherTimer();
            _delayedActionTimer.Tick += DelayedActionTimer_Tick;
            _delayedActionTimer.Interval = new TimeSpan(0, 0, 0, 0, DelayedPlayIntervalMs);

            lblSecondsLeft.Content = null;

            btnPlayReference.Target = new PlayAudioAction(PrepareReferencePlaybackArgs, ProcessPlaybackResult);
            btnPlayRecorded.Target = new PlayAudioAction(PrepareRecordedPlaybackArgs, ProcessPlaybackResult);
            btnRecord.Target = new RecordAudioAction(PrepareRecordingArgs, ProcessRecordingResult, ReportRecordingProgress);

            _actionButtons = new[] { btnPlayReference, btnPlayRecorded, btnRecord };
            foreach (var actionButton in _actionButtons)
            {
                actionButton.Target.ActionStarted += ActionButton_ActionStarted;
                actionButton.Target.ActionCompleted += ActionButton_ActionCompleted;
            }
        }

        public void AttachContext(IAudioContext audioContext)
        {
            _audioContext = audioContext;
            _audioContext.ContextChanged -= AudioContext_ContextChanged;
            _audioContext.ContextChanged += AudioContext_ContextChanged;

            if (!_audioContext.AutoStopRecording)
            {
                autoStopPanel.Visibility = System.Windows.Visibility.Collapsed;
            }

            SetupControlsState();
        }

        public bool HasKeyboardFocus
        {
            get { return txtAutoStop.IsKeyboardFocusWithin; }
        }

        public void PlayReferenceAudio()
        {
            if (_audioContext.IsReferenceAudioExists)
            {
                StartAction(btnPlayReference.Target, true);
            }
        }

        public void PlayRecordedAudio()
        {
            if (_audioContext.IsRecordedAudioExists)
            {
                StartAction(btnPlayRecorded.Target, true);
            }
        }

        public void StartRecording()
        {
            if (_audioContext.IsRecordingAllowed)
            {
                StartAction(btnRecord.Target, true);
            }
        }

        public void StopAction(bool isSoftAbort)
        {
            if (_activeAction != null)
            {
                _activeAction.RequestAbort(isSoftAbort);
            }
        }

        private void SetupControlsState()
        {
            btnPlayReference.IsEnabled = _audioContext.IsReferenceAudioExists;
            btnPlayRecorded.IsEnabled = _audioContext.IsRecordedAudioExists;
            btnRecord.IsEnabled = _audioContext.IsRecordingAllowed;
        }

        private void AudioContext_ContextChanged(PlayAudioMode playMode)
        {
            _delayedActionContext = null;

            BackgroundAction target = null;
            if (playMode == PlayAudioMode.PlayReference && _audioContext.IsReferenceAudioExists)
            {
                target = btnPlayReference.Target;
            }
            else if (playMode == PlayAudioMode.PlayRecorded && _audioContext.IsRecordedAudioExists)
            {
                target = btnPlayRecorded.Target;
            }

            bool isStarted = StartAction(target, true);
            if (!isStarted)
            {
                SetupControlsState();
            }
        }

        private void ActionButton_ActionStarted(BackgroundAction action)
        {
            _activeAction = action;
            _delayedActionContext = null;
            foreach (var actionButton in _actionButtons)
            {
                if (!ReferenceEquals(actionButton.Target, action))
                {
                    actionButton.IsEnabled = false;
                }
            }
        }

        private void ActionButton_ActionCompleted(BackgroundAction action)
        {
            _activeAction = null;
            SetupControlsState();
        }

        private void DelayedActionTimer_Tick(object sender, EventArgs e)
        {
            _delayedActionTimer.Stop();

            if (_delayedActionContext != null)
            {
                _delayedActionContext.Target.StartAction();
                _delayedActionContext = null;
            }
        }

        private ActionArgs<PlaybackArgs> PrepareReferencePlaybackArgs(ActionContext context)
        {
            PlaybackSettings args = _audioContext.GetReferenceAudio();
            if (args == null)
                return null;

            return new ActionArgs<PlaybackArgs>(new PlaybackArgs
            {
                IsReferenceAudio = true,
                IsFilePath = args.IsFilePath,
                PlaybackData = args.PlaybackData,
                PlaybackVolumeDb = AppSettings.Instance.ReferenceDataVolume
            });
        }

        private ActionArgs<PlaybackArgs> PrepareRecordedPlaybackArgs(ActionContext context)
        {
            PlaybackSettings args = _audioContext.GetRecordedAudio();
            if (args == null)
                return null;

            return new ActionArgs<PlaybackArgs>(new PlaybackArgs
            {
                IsReferenceAudio = false,
                IsFilePath = args.IsFilePath,
                PlaybackData = args.PlaybackData
            });
        }

        private ActionArgs<RecordingArgs> PrepareRecordingArgs(ActionContext context)
        {
            var args = _audioContext.GetRecordingSettings();
            if (args == null)
                return null;

            btnRecord.Style = (Style)(this.Resources["RecordingActive"]);
            float recordDuration = 0;
            if (autoStopPanel.IsVisible)
            {
                recordDuration = float.Parse(txtAutoStop.Text, System.Globalization.CultureInfo.InvariantCulture);
                lblSecondsLeft.Foreground = Brushes.Red;
                lblSecondsLeft.Content = string.Format(_recordProgressTemplate, txtAutoStop.Text);
            }

            BackgroundAction[] postActions;
            switch (AppSettings.Instance.RecordedMode)
            {
                case RecordedPlayMode.RecordedOnly:
                    postActions = new[] { btnPlayRecorded.Target };
                    break;
                case RecordedPlayMode.RecordedThenReference:
                    postActions = new[] { btnPlayRecorded.Target, btnPlayReference.Target };
                    break;
                case RecordedPlayMode.ReferenceThenRecorded:
                    postActions = new[] { btnPlayReference.Target, btnPlayRecorded.Target };
                    break;
                default:
                    postActions = null;
                    break;
            }
            btnRecord.Target.ActionSequence = new BackgroundActionSequence(postActions);

            return new ActionArgs<RecordingArgs>(new RecordingArgs
            {
                Duration = recordDuration,
                FilePath = args.OutputFilePath
            });
        }

        private void ReportRecordingProgress(object progress)
        {
            int secondsLeft = (int)Math.Round((float)progress / 1000);
            lblSecondsLeft.Content = string.Format(_recordProgressTemplate, secondsLeft);
        }

        private void ProcessPlaybackResult(ActionContext context, ActionResult result)
        {
            if (result.Error != null)
                throw new Exception(string.Format("There was an error during audio playback: {0}", result.Error.Message));
        }

        private void ProcessRecordingResult(ActionContext context, ActionResult<string> result)
        {
            btnRecord.Style = (Style)(this.Resources["RecordingStopped"]);
            lblSecondsLeft.Content = null;

            if (result.Error != null)
                throw new Exception(string.Format("There was an error during audio recording: {0}", result.Error.Message));

            if (RecordingCompleted != null)
            {
                RecordingCompleted(result.Result);
            }
        }

        private bool StartAction(BackgroundAction target, bool abortExecutingAction)
        {
            if (_activeAction == null)
                return target == null ? false : target.StartAction();

            if (!abortExecutingAction)
                return false;

            _activeAction.RequestAbort(false);

            // Just abort executing action
            if (target == null)
                return false;

            if (_delayedActionTimer.IsEnabled)
            {
                _delayedActionTimer.Stop();
            }
            _delayedActionContext = new DelayedActionArgs { Target = target };
            _delayedActionTimer.Start();

            return false;
        }
    }
}
