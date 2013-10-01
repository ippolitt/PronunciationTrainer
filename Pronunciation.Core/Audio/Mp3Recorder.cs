using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Pronunciation.Core.Audio
{
    public class Mp3Recorder : IDisposable
    {
        private class RecordingInfo
        {
            public InMemoryRecorder Recorder;
            public string OutputFilePath;
            public Mp3Recorder Owner;
        }

        private readonly int _sampleRate;

        private static RecordingInfo _activeRecording;
        private readonly static object _recordingLock = new object();

        public Mp3Recorder(int sampleRate)
        {
            _sampleRate = sampleRate;
        }

        public void Start(string outputFilePath)
        {
            lock (_recordingLock)
            {
                if (_activeRecording != null)
                    throw new InvalidOperationException("The recording is already in progress!");

                _activeRecording = new RecordingInfo
                {
                    Recorder = new InMemoryRecorder(_sampleRate),
                    OutputFilePath = outputFilePath,
                    Owner = this
                };

                _activeRecording.Recorder.Start();
            }   
        }

        public void Stop()
        {
            byte[] recordedData;
            string outputFilePath;
            lock (_recordingLock)
            {
                if (_activeRecording == null)
                    throw new InvalidOperationException("The recording is not in progress!");

                if (!ReferenceEquals(_activeRecording.Owner, this))
                    throw new InvalidOperationException("Another recording is currently in progress!");

                recordedData = _activeRecording.Recorder.Stop();
                outputFilePath = _activeRecording.OutputFilePath;
                _activeRecording = null;
            }

            var folder = Path.GetDirectoryName(outputFilePath);
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            EncodeStream(recordedData, outputFilePath);
        }

        private void EncodeStream(byte[] recordedData, string outputFilePath)
        {
            int lameRate = _sampleRate / 1000;

            Process p = new Process();
            p.StartInfo.FileName = @"lame.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.Arguments = string.Format("-r -s {0} -m m - \"{1}\"", lameRate, outputFilePath);
            p.StartInfo.CreateNoWindow = true;
            p.Start();

            try
            {
                p.StandardInput.BaseStream.Write(recordedData, 0, recordedData.Length);
            }
            finally
            {
                p.StandardInput.BaseStream.Close();
                p.WaitForExit();
            }
        }

        public void Dispose()
        {
            if (_activeRecording != null)
            {
                lock (_recordingLock)
                {
                    if (_activeRecording != null && ReferenceEquals(_activeRecording.Owner, this))
                    {
                        _activeRecording.Recorder.Stop();
                        _activeRecording = null;
                    }
                }
            }
        }
    }
}
