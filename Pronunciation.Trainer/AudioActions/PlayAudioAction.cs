using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Actions;
using Pronunciation.Core.Audio;

namespace Pronunciation.Trainer.AudioActions
{
    public class PlayAudioAction : BackgroundActionWithArgs<PlaybackArgs, PlaybackResult>
    {
        private readonly Mp3Player _player;

        public PlayAudioAction(Func<ActionContext, ActionArgs<PlaybackArgs>> argsBuilder,
            Action<ActionContext, ActionResult<PlaybackResult>> resultProcessor) 
            : base(argsBuilder, null, resultProcessor)
        {
            base.Worker = PlayAudio;
            _player = new Mp3Player(true);
        }

        public TimeSpan AudioPosition
        {
            get { return _player.CurrentPosition; }
            set { _player.CurrentPosition = value; }
        }

        public TimeSpan AudioLength
        {
            get { return _player.TotalLength; }
        }

        private PlaybackResult PlayAudio(ActionContext context, PlaybackArgs args)
        {
            // We can only decrease the volume so we treat positive values as negative ones
            float volumeDb = args.PlaybackVolumeDb <= 0 ? args.PlaybackVolumeDb : -args.PlaybackVolumeDb;

            PlaybackResult result;
            if (args.IsFilePath)
            {
                result = _player.PlayFile(args.PlaybackData, volumeDb, args.SkipMs);
            }
            else
            {
                result = _player.PlayRawData(args.PlaybackData, volumeDb, args.SkipMs);
            }

            return result;
        }

        public override void RequestAbort(bool isSoftAbort)
        {
            base.RequestAbort(false);
            _player.Stop();
        }

        // TrackBar trackBarPosition.Maximum = (int)fileWaveStream.TotalTime.TotalSeconds;
        // trackBarPosition.TickFrequency = trackBarPosition.Maximum / 30;
        //
        // TimeSpan currentTime = (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : fileWaveStream.CurrentTime;
        // trackBarPosition.Value = (int)currentTime.TotalSeconds;
    }
}
