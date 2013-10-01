using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Contexts
{
    public class RecordingSettings
    {
        public string OutputFilePath { get; private set; }

        public RecordingSettings(string outputFilePath)
        {
            OutputFilePath = outputFilePath;
        }
    }
}
