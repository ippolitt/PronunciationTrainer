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

namespace Pronunciation.Trainer
{
    /// <summary>
    /// Interaction logic for AudioPanel.xaml
    /// </summary>
    public partial class AudioPanel : UserControlExt
    {
        public delegate void RecordingCompletedHandler(string recordedFilePath);

        private class DelayedActionArgs
        {
            public BackgroundAction Target;
        }

        private readonly DispatcherTimer _delayedActionTimer = new DispatcherTimer();
        private readonly DispatcherTimer _moveSliderTimer = new DispatcherTimer();
        private DelayedActionArgs _delayedActionContext;
        private bool _ignoreSliderValueChange;
        private bool _rewindPlayer;

        private PlaybackResult _lastReferenceResult;
        private PlaybackResult _lastRecordedResult;

        private IAudioContext _audioContext;
        private BackgroundAction _activeAction;
        private ActionButton[] _actionButtons;

        private ExecuteActionCommand _playReferenceCommand;
        private ExecuteActionCommand _playRecordedCommand;
        private ExecuteActionCommand _startRecordingCommand;
        private ExecuteActionCommand _stopCommand;

        private const string _recordProgressTemplate = "Recording, {0} seconds left..";
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
                container.InputBindings.Add(new KeyBinding(_stopCommand, KeyGestures.PauseAudio));

                container.PreviewKeyDown += container_PreviewKeyDown;

                btnPlayReference.ToolTip += KeyGestures.PlayReference.GetTooltipString();
                btnPlayRecorded.ToolTip += KeyGestures.PlayRecorded.GetTooltipString();
                btnRecord.ToolTip += KeyGestures.StartRecording.GetTooltipString();
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

            ResetSamples();
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
            ResetSamples();
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

            foreach (var actionButton in _actionButtons)
            {
                if (!ReferenceEquals(actionButton.Target, action))
                {
                    actionButton.IsEnabled = false;
                }
            }

            btnShowWaveforms.IsEnabled = false;

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

            _playReferenceCommand.UpdateState(_audioContext.IsReferenceAudioExists);
            _playRecordedCommand.UpdateState(_audioContext.IsRecordedAudioExists);
            _startRecordingCommand.UpdateState(_audioContext.IsRecordingAllowed);

            btnPlayReference.IsEnabled = _audioContext.IsReferenceAudioExists;
            btnPlayRecorded.IsEnabled = _audioContext.IsRecordedAudioExists;
            btnRecord.IsEnabled = _audioContext.IsRecordingAllowed;
            btnShowWaveforms.IsEnabled = (_lastReferenceResult != null || _lastRecordedResult != null);
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

        private void ResetSamples()
        {
            _lastReferenceResult = null;
            _lastRecordedResult = null;
        }

        private ActionArgs<PlaybackArgs> PrepareReferencePlaybackArgs(ActionContext context)
        {
            PlaybackSettings args = _audioContext.GetReferenceAudio();
            if (args == null)
                return null;

            _lastReferenceResult = null;
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

            _lastRecordedResult = null;
            return new ActionArgs<PlaybackArgs>(new PlaybackArgs
            {
                IsReferenceAudio = false,
                IsFilePath = args.IsFilePath,
                PlaybackData = args.PlaybackData,
                SkipMs = AppSettings.Instance.SkipRecordedAudioMs
            });
        }

        private ActionArgs<RecordingArgs> PrepareRecordingArgs(ActionContext context)
        {
            var args = _audioContext.GetRecordingSettings();
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

            return new ActionArgs<RecordingArgs>(new RecordingArgs
            {
                FilePath = args.OutputFilePath
            });
        }

        private void ProcessPlaybackResult(ActionContext context, ActionResult<PlaybackResult> result, bool isReferenceAudio)
        {
            if (result.Error != null)
                throw new Exception(string.Format("There was an error during audio playback: {0}", result.Error.Message));

            if (isReferenceAudio)
            {
                _lastReferenceResult = result.ReturnValue;
            }
            else
            {
                _lastRecordedResult = result.ReturnValue;
            }
        }

        private void ProcessRecordingResult(ActionContext context, ActionResult<string> result)
        {
            if (result.Error != null)
                throw new Exception(string.Format("There was an error during audio recording: {0}", result.Error.Message));

            if (RecordingCompleted != null)
            {
                RecordingCompleted(result.ReturnValue);
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

        private void btnShowWaveforms_Click(object sender, RoutedEventArgs e)
        {
            WaveFormsComparison window = new WaveFormsComparison();
            window.ReferenceResult = _lastReferenceResult;
            window.RecordedResult = _lastRecordedResult;
            window.Show();
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
    }
}
