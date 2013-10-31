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
        private readonly DispatcherTimer _paintWaveformTimer = new DispatcherTimer();
        private DelayedActionArgs _delayedActionContext;

        private IAudioContext _audioContext;
        private BackgroundAction _activeAction;
        private ActionButton[] _actionButtons;

        private ExecuteActionCommand _playReferenceCommand;
        private ExecuteActionCommand _playRecordedCommand;
        private ExecuteActionCommand _startRecordingCommand;
        private ExecuteActionCommand _pauseCommand;

        private const string _recordProgressTemplate = "Recording, {0} seconds left..";
        private const int DelayedPlayIntervalMs = 500;
        private const int PaintWaveformIntervalMs = 100;

        public event RecordingCompletedHandler RecordingCompleted;

        public AudioPanel()
        {
            InitializeComponent();
        }

        private void UserControl_Initialized(object sender, EventArgs e)
        {
            _delayedActionTimer.Tick += _delayedActionTimer_Tick;
            _delayedActionTimer.Interval = TimeSpan.FromMilliseconds(DelayedPlayIntervalMs);

            _paintWaveformTimer.Tick += _paintWaveformTimer_Tick;
            _paintWaveformTimer.Interval = TimeSpan.FromMilliseconds(PaintWaveformIntervalMs);

            _playReferenceCommand = new ExecuteActionCommand(PlayReferenceAudio, false);
            _playRecordedCommand = new ExecuteActionCommand(PlayRecordedAudio, false);
            _startRecordingCommand = new ExecuteActionCommand(StartRecording, false);
            _pauseCommand = new ExecuteActionCommand(() => PauseAudio(), false);

            btnPlayReference.Target = new PlayAudioAction(PrepareReferencePlaybackArgs, (x, y) => ProcessPlaybackResult(x, y, true));
            btnPlayRecorded.Target = new PlayAudioAction(PrepareRecordedPlaybackArgs, (x, y) => ProcessPlaybackResult(x, y, false));
            btnRecord.Target = new RecordAudioAction(PrepareRecordingArgs, ProcessRecordingResult);

            _actionButtons = new[] { btnPlayReference, btnPlayRecorded, btnRecord };
            foreach (var actionButton in _actionButtons)
            {
                actionButton.Target.ActionStarted += ActionButton_ActionStarted;
                actionButton.Target.ActionCompleted += ActionButton_ActionCompleted;
            }
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
                container.InputBindings.Add(new KeyBinding(_pauseCommand, KeyGestures.PauseAudio));

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
                // Prevent other controls from handling a Space if playback/recording is in progress
                if (PauseAudio())
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

            SetupControlsState();
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

        private bool PauseAudio()
        {
            if (_activeAction != null)
            {
                _activeAction.RequestAbort(true);
                return true;
            }

            return false;
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
            _playReferenceCommand.UpdateState(_audioContext.IsReferenceAudioExists);

            btnPlayRecorded.IsEnabled = _audioContext.IsRecordedAudioExists;
            _playRecordedCommand.UpdateState(_audioContext.IsRecordedAudioExists);

            btnRecord.IsEnabled = _audioContext.IsRecordingAllowed;
            _startRecordingCommand.UpdateState(_audioContext.IsRecordingAllowed);
        }

        private void AudioContext_ContextChanged(PlayAudioMode playMode)
        {
            _delayedActionContext = null;
            waveReference.Clear();
            waveRecorded.Clear();

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

            _pauseCommand.UpdateState(true);
            //_playReferenceCommand.UpdateState(false);
            //_playRecordedCommand.UpdateState(false);
            //_startRecordingCommand.UpdateState(false);

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
            _pauseCommand.UpdateState(false);

            if (_paintWaveformTimer.IsEnabled)
            {
                _paintWaveformTimer.Stop();
            }

            SetupControlsState();
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

        private void _paintWaveformTimer_Tick(object sender, EventArgs e)
        {
            var playAction = _activeAction as PlayAudioAction;
            if (playAction == null)
            {
                _paintWaveformTimer.Stop();
                return;
            }
        }

        private ActionArgs<PlaybackArgs> PrepareReferencePlaybackArgs(ActionContext context)
        {
            PlaybackSettings args = _audioContext.GetReferenceAudio();
            if (args == null)
                return null;

            waveReference.Clear();
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

            waveRecorded.Clear();
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

            //(isReferenceAudio ? waveReference : waveRecorded).DrawWaveForm(result.ReturnValue.Samples);
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
    }
}
