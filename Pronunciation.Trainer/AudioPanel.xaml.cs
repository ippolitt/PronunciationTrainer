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
using Pronunciation.Trainer.Commands;
using System.IO;

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for AudioPanel.xaml
    /// </summary>
    public partial class AudioPanel : UserControlExt
    {
        public delegate void RecordingCompletedHandler(string recordedFilePath, bool isTemporaryFile);

        private class DelayedActionArgs
        {
            public BackgroundAction Target;
        }

        private readonly DispatcherTimer _delayedActionTimer = new DispatcherTimer();
        private readonly DispatcherTimer _moveSliderTimer = new DispatcherTimer();
        private DelayedActionArgs _delayedActionContext;
        private bool _ignoreSliderValueChange;
        private bool _rewindPlayer;

        private IAudioContext _audioContext;
        private BackgroundAction _activeAction;
        private ActionButton[] _actionButtons;

        private ExecuteActionCommand _playReferenceCommand;
        private ExecuteActionCommand _playRecordedCommand;
        private ExecuteActionCommand _startRecordingCommand;
        private ExecuteActionCommand _stopCommand;
        private ExecuteActionCommand _showWaveformCommand;
        private ExecuteActionCommand _showHistoryCommand;

        private const string RecordProgressTemplate = "Recording, {0} seconds left..";
        private const int DelayedPlayIntervalMs = 500;
        private const int MoveSliderIntervalMs = 200;

        public event RecordingCompletedHandler RecordingCompleted;

        public AudioPanel()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _delayedActionTimer.Tick += _delayedActionTimer_Tick;
            _delayedActionTimer.Interval = TimeSpan.FromMilliseconds(DelayedPlayIntervalMs);

            _moveSliderTimer.Tick += _moveSliderTimer_Tick;
            _moveSliderTimer.Interval = TimeSpan.FromMilliseconds(MoveSliderIntervalMs);

            _playReferenceCommand = new ExecuteActionCommand(PlayReferenceAudio, false);
            _playRecordedCommand = new ExecuteActionCommand(PlayRecordedAudio, false);
            _startRecordingCommand = new ExecuteActionCommand(StartRecording, false);
            _stopCommand = new ExecuteActionCommand(() => StopAction(true), false);
            _showWaveformCommand = new ExecuteActionCommand(ShowWaveformsDialog, false);
            _showHistoryCommand = new ExecuteActionCommand(ShowHistoryDialog, false);

            btnShowWaveforms.Command = _showWaveformCommand;
            btnShowHistory.Command = _showHistoryCommand;
            btnPlayReference.Target = new PlayAudioAction(PrepareReferencePlaybackArgs, (x, y) => ProcessPlaybackResult(x, y, true));
            btnPlayRecorded.Target = new PlayAudioAction(PrepareRecordedPlaybackArgs, (x, y) => ProcessPlaybackResult(x, y, false));
            btnRecord.Target = new RecordAudioAction(PrepareRecordingArgs, ProcessRecordingResult);

            _actionButtons = new[] { btnPlayReference, btnPlayRecorded, btnRecord };
            foreach (var actionButton in _actionButtons)
            {
                actionButton.Target.ActionStarted += ActionButton_ActionStarted;
                actionButton.Target.ActionCompleted += ActionButton_ActionCompleted;
            }

            sliderPlay.Visibility = Visibility.Hidden;
        }

        protected override void OnVisualTreeBuilt(bool isFirstBuild)
        {
            base.OnVisualTreeBuilt(isFirstBuild);

            if (isFirstBuild)
            {
                var container = ControlsHelper.GetContainer(this);
                container.InputBindings.Add(new KeyBinding(_playReferenceCommand, KeyGestures.PlayReference));
                container.InputBindings.Add(new KeyBinding(_playRecordedCommand, KeyGestures.PlayRecorded));
                container.InputBindings.Add(new KeyBinding(_startRecordingCommand, KeyGestures.StartRecording));
                container.InputBindings.Add(new KeyBinding(_stopCommand, KeyGestures.StopAudio));
                container.InputBindings.Add(new KeyBinding(_showWaveformCommand, KeyGestures.ShowWaveform));
                container.InputBindings.Add(new KeyBinding(_showHistoryCommand, KeyGestures.ShowHistory));

                container.PreviewKeyDown += container_PreviewKeyDown;

                btnPlayReference.DefaultTooltip = string.Format(btnPlayReference.DefaultTooltip, KeyGestures.PlayReference.DisplayString);
                btnPlayReference.RunningTooltip = string.Format(btnPlayReference.RunningTooltip, KeyGestures.StopAudio.DisplayString);
                btnPlayReference.PausedTooltip = string.Format(btnPlayReference.PausedTooltip, KeyGestures.StopAudio.DisplayString);
                btnPlayReference.RefreshDefaultTooltip();

                btnPlayRecorded.DefaultTooltip = string.Format(btnPlayRecorded.DefaultTooltip, KeyGestures.PlayRecorded.DisplayString);
                btnPlayRecorded.RunningTooltip = string.Format(btnPlayRecorded.RunningTooltip, KeyGestures.StopAudio.DisplayString);
                btnPlayRecorded.PausedTooltip = string.Format(btnPlayRecorded.PausedTooltip, KeyGestures.StopAudio.DisplayString);
                btnPlayRecorded.RefreshDefaultTooltip();

                btnRecord.DefaultTooltip = string.Format(btnRecord.DefaultTooltip, KeyGestures.StartRecording.DisplayString);
                btnRecord.RunningTooltip = string.Format(btnRecord.RunningTooltip, KeyGestures.StopAudio.DisplayString);
                btnRecord.RefreshDefaultTooltip();

                btnShowWaveforms.ToolTip = string.Format(btnShowWaveforms.ToolTip.ToString(), KeyGestures.ShowWaveform.DisplayString);
                btnShowHistory.ToolTip = string.Format(btnShowHistory.ToolTip.ToString(), KeyGestures.ShowHistory.DisplayString);
            }
        }

        private void container_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.None && e.Key == Key.Space)
            {
                bool isExecuted = false;
                if (_activeAction != null)
                {
                    if (_activeAction.ActionState == BackgroundActionState.Suspended)
                    {
                        _activeAction.Resume();
                        isExecuted = true;
                    }
                    else if (_activeAction.ActionState == BackgroundActionState.Running)
                    {
                        if (_activeAction.IsSuspendable)
                        {
                            _activeAction.Suspend();
                        }
                        else
                        {
                            _activeAction.RequestAbort(true);
                        }
                        isExecuted = true;
                    }
                }

                // Prevent other controls from handling a Space if playback/recording is in progress
                if (isExecuted)
                {
                    e.Handled = true;
                }
            }
        }

        public void AttachContext(IAudioContext audioContext)
        {
            _audioContext = audioContext;
            _audioContext.ContextChanged -= AudioContext_ContextChanged;
            _audioContext.ContextChanged += AudioContext_ContextChanged;

            RefreshControls();
        }

        public bool HasKeyboardFocus
        {
            get { return false; }
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

        private void AudioContext_ContextChanged(PlayAudioMode playMode)
        {
            _delayedActionContext = null;
            RefreshControls();

            BackgroundAction target = null;
            if (playMode == PlayAudioMode.PlayReference && _audioContext.IsReferenceAudioExists)
            {
                target = btnPlayReference.Target;
            }
            else if (playMode == PlayAudioMode.PlayRecorded && _audioContext.IsRecordedAudioExists)
            {
                target = btnPlayRecorded.Target;
            }
            StartAction(target, true);
        }

        private void ActionButton_ActionStarted(BackgroundAction action)
        {
            _activeAction = action;
            _delayedActionContext = null;

            _stopCommand.UpdateState(true);
            _showWaveformCommand.UpdateState(false);
            _showHistoryCommand.UpdateState(false);

            foreach (var actionButton in _actionButtons)
            {
                if (!ReferenceEquals(actionButton.Target, action))
                {
                    actionButton.IsEnabled = false;
                }
            }

            ResetSlider();
            if (sliderPlay.Visibility == Visibility.Visible)
            {
                InitSliderPolling();
            }
        }

        private void ActionButton_ActionCompleted(BackgroundAction action)
        {
            _activeAction = null;
            _stopCommand.UpdateState(false);

            StopSliderPolling();
            RefreshControls();
        }

        private void _delayedActionTimer_Tick(object sender, EventArgs e)
        {
            _delayedActionTimer.Stop();

            if (_delayedActionContext != null)
            {
                _delayedActionContext.Target.StartAction();
                _delayedActionContext = null;
            }
        }

        private void _moveSliderTimer_Tick(object sender, EventArgs e)
        {
            var playAction = _activeAction as PlayAudioAction;
            if (playAction == null)
                return;

            var player = playAction.ActivePlayer;
            if (player == null)
                return;

            sliderPlay.IsEnabled = true;
            double max = player.TotalLength.TotalMilliseconds;
            if (max != sliderPlay.Maximum)
            {
                sliderPlay.Minimum = 0;
                sliderPlay.Maximum = max;
            }

            if (_rewindPlayer)
            {
                _rewindPlayer = false;
                player.CurrentPosition = TimeSpan.FromMilliseconds(sliderPlay.Value);
            }
            else
            {
                _ignoreSliderValueChange = true;
                sliderPlay.Value = player.CurrentPosition.TotalMilliseconds;
                _ignoreSliderValueChange = false;
            }
        }

        private void sliderPlay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!_ignoreSliderValueChange)
            {
                _rewindPlayer = true;
            }
        }

        private void RefreshControls()
        {
            ResetSlider();

            btnPlayReference.IsEnabled = _audioContext.IsReferenceAudioExists;
            btnPlayRecorded.IsEnabled = _audioContext.IsRecordedAudioExists;
            btnRecord.IsEnabled = _audioContext.IsRecordingAllowed;

            _playReferenceCommand.UpdateState(btnPlayReference.IsEnabled);
            _playRecordedCommand.UpdateState(btnPlayRecorded.IsEnabled);
            _startRecordingCommand.UpdateState(btnRecord.IsEnabled);
            // At least one audio must exist (either reference or recording one)
            _showWaveformCommand.UpdateState(btnPlayReference.IsEnabled || btnPlayRecorded.IsEnabled);
            // Allow recordings history dialog if at least one recording exists
            _showHistoryCommand.UpdateState(_audioContext.CanShowRecordingsHistory && btnPlayRecorded.IsEnabled);
        }

        private void ResetSlider()
        {
            sliderPlay.Minimum = 0;
            sliderPlay.Maximum = 10;
            sliderPlay.Value = 0;
            sliderPlay.IsEnabled = false;
            _rewindPlayer = false;
        }

        private void InitSliderPolling()
        {
            if (_activeAction is PlayAudioAction)
            {
                if (_moveSliderTimer.IsEnabled)
                {
                    _moveSliderTimer.Stop();
                }
                _moveSliderTimer.Start();
            }
        }

        private void StopSliderPolling()
        {
            if (_moveSliderTimer.IsEnabled)
            {
                _moveSliderTimer.Stop();
            }
        }

        private PlaybackArgs PrepareReferencePlaybackArgs()
        {
            PlaybackData args = _audioContext.GetReferenceAudio();
            if (args == null)
                return null;

            return new PlaybackArgs
            {
                IsReferenceAudio = true,
                IsFilePath = args.IsFilePath,
                FilePath = args.FilePath,
                RawData = args.RawData,
                PlaybackVolumeDb = AppSettings.Instance.ReferenceDataVolume
            };
        }

        private PlaybackArgs PrepareRecordedPlaybackArgs()
        {
            PlaybackData args = _audioContext.GetRecordedAudio();
            if (args == null)
                return null;

            return new PlaybackArgs
            {
                IsReferenceAudio = false,
                IsFilePath = args.IsFilePath,
                FilePath = args.FilePath,
                RawData = args.RawData,
                SkipMs = AppSettings.Instance.SkipRecordedAudioMs
            };
        }

        private RecordingArgs PrepareRecordingArgs(ActionContext context)
        {
            RecordingSettings args = _audioContext.GetRecordingSettings();
            if (args == null)
                return null;

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
            context.ActiveSequence = new BackgroundActionSequence(postActions);

            return new RecordingArgs
            {
                FilePath = args.OutputFilePath,
                IsTemporaryFile = args.IsTemporaryFile
            };
        }

        private void ProcessPlaybackResult(PlaybackArgs args, ActionResult<PlaybackResult> result, bool isReferenceAudio)
        {
            if (result.Error != null)
                throw new Exception(string.Format("There was an error during audio playback: {0}", result.Error.Message));
        }

        private void ProcessRecordingResult(RecordingArgs args, ActionResult result)
        {
            if (result.Error != null)
            {
                throw new Exception(string.Format(
                    "There was an error during audio recording: {0} (file path is '{1}')",
                    result.Error.Message, args.FilePath));
            } 

            if (RecordingCompleted != null)
            {
                RecordingCompleted(args.FilePath, args.IsTemporaryFile);
            }

            // Delete temporary files only if everything went OK otherwise leave them on disk for troubleshooting
            if (args.IsTemporaryFile)
            {
                SafeDeleteFile(args.FilePath);
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

        private void ShowWaveformsDialog()
        {
            WaveFormsComparison window = new WaveFormsComparison();
            window.ReferenceAudio = _audioContext.GetReferenceAudio();
            window.RecordedAudio = _audioContext.GetRecordedAudio();
            window.Show();
        }

        private void ShowHistoryDialog()
        {
            RecordingHistory window = new RecordingHistory();
            window.Owner = ControlsHelper.GetWindow(this);
            window.InitContext(_audioContext.GetRecordingHistoryProvider(), _audioContext.GetReferenceAudio());
            window.ShowDialog();

            // Refresh controls as the current recording may have been deleted or a new recording may have been added
            RefreshControls();
        }

        private void btnShowSlider_Click(object sender, RoutedEventArgs e)
        {
            if (sliderPlay.Visibility == Visibility.Visible)
            {
                sliderPlay.Visibility = Visibility.Hidden;
                StopSliderPolling();
            }
            else
            {
                sliderPlay.Visibility = Visibility.Visible;
                InitSliderPolling();
            }
        }

        private void SafeDeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            try
            {
                File.Delete(filePath);
            }
            catch 
            { }
        }
    }
}
