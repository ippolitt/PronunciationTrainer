using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Actions;
using System.Threading;
using System.Diagnostics;
using Pronunciation.Core.Audio;

namespace Pronunciation.Trainer.AudioActions
{
    public class RecordAudioAction : BackgroundActionWithArgs<RecordingArgs>
    {
        public RecordAudioAction(Func<ActionContext, RecordingArgs> argsBuilder, Action<RecordingArgs, ActionResult> resultProcessor)
            : base(null, null, resultProcessor)
        {
            this.ArgsBuilder = (context) => argsBuilder(context);
            this.Worker = (context, args) => RecordAudio(args);
            IsAbortable = true;
        }

        private void RecordAudio(RecordingArgs args)
        {
            using (var recorder = new Mp3Recorder(AppSettings.Instance.SampleRate))
            {
                recorder.Start(args.FilePath);

                if (args.Duration > 0)
                {
                    float durationMs = args.Duration * 1000;
                    var watchTotal = new Stopwatch();
                    var watchRefresh = new Stopwatch();
                    watchTotal.Start();
                    watchRefresh.Start();
                    while (durationMs - watchTotal.ElapsedMilliseconds > 0)
                    {
                        if (this.IsAbortRequested)
                            break;

                        if (watchRefresh.ElapsedMilliseconds > 500)
                        {
                            this.ReportProgress(durationMs - watchTotal.ElapsedMilliseconds);
                            watchRefresh.Restart();
                        }

                        Thread.Sleep(100);
                    }
                }
                else
                {
                    while (!this.IsAbortRequested)
                    {
                        Thread.Sleep(100);
                    }
                }

                recorder.Stop();
            }
        }
    }
}
