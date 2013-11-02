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
    public class RecordAudioAction : BackgroundActionWithArgs<RecordingArgs, string>
    {
        public RecordAudioAction(Func<ActionContext, ActionArgs<RecordingArgs>> argsBuilder,
            Action<ActionContext, ActionResult<string>> resultProcessor)
            : base(argsBuilder, null, resultProcessor)
        {
            base.Worker = RecordAudio;
            IsAbortable = true;
        }

        private string RecordAudio(ActionContext context, RecordingArgs args)
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

            return args.FilePath;
        }
    }
}
