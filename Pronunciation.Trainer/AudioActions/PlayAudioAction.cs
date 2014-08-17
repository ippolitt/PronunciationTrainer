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
        private volatile Mp3Player _activePlayer;

        public PlayAudioAction(Func<PlaybackArgs> argsBuilder, Action<PlaybackArgs, ActionResult<PlaybackResult>> resultProcessor) 
            : base(argsBuilder, null, resultProcessor)
        {
            base.Worker = (context, args) => PlayAudio(args);
            IsAbortable = true;
            IsSuspendable = true;
        }

        public Mp3Player ActivePlayer
        {
            get { return _activePlayer; }
        }

        private PlaybackResult PlayAudio(PlaybackArgs args)
        {
            // We can only decrease the volume so we treat positive values as negative ones
            float volumeDb = args.PlaybackVolumeDb <= 0 ? args.PlaybackVolumeDb : -args.PlaybackVolumeDb;

            PlaybackResult result;
            using (var player = new Mp3Player())
            {
                player.PlayingStarted += player_PlayingStarted;
                player.PlayingCompleted += player_PlayingCompleted;

                if (args.IsFilePath)
                {
                    result = player.PlayFile(args.FilePath, volumeDb, args.SkipMs);
                }
                else
                {
                    result = player.PlayRawData(args.RawData, volumeDb, args.SkipMs);
                }
            }

            return result;
        }

        public override void RequestAbort(bool isSoftAbort)
        {
            base.RequestAbort(false);

            var player = _activePlayer;
            if (player != null)
            {
                player.Stop();
            }
        }

        public override void Suspend()
        {
            base.Suspend();

            var player = _activePlayer;
            if (player != null)
            {
                if (player.Pause())
                {
                    base.RegisterSuspended();
                }
            }
        }

        public override void Resume()
        {
            base.Resume();

            var player = _activePlayer;
            if (player != null)
            {
                if (player.Resume())
                {
                    base.RegisterResumed();
                }
            }
        }

        private void player_PlayingStarted(Mp3Player player)
        {
            _activePlayer = player;
        }

        private void player_PlayingCompleted(Mp3Player player)
        {
            _activePlayer = null;
        }

        // TrackBar trackBarPosition.Maximum = (int)fileWaveStream.TotalTime.TotalSeconds;
        // trackBarPosition.TickFrequency = trackBarPosition.Maximum / 30;
        //
        // TimeSpan currentTime = (waveOut.PlaybackState == PlaybackState.Stopped) ? TimeSpan.Zero : fileWaveStream.CurrentTime;
        // trackBarPosition.Value = (int)currentTime.TotalSeconds;
    }
}
