using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Contexts
{
    public class RecordingSettings
    {
        public bool IsTemporaryFile { get; private set; }
        public string OutputFilePath { get; private set; }

        public RecordingSettings(string outputFilePath)
            : this(outputFilePath, false)
        { 
        }

        public RecordingSettings(string outputFilePath, bool isTemporaryFile)
        {
            OutputFilePath = outputFilePath;
            IsTemporaryFile = isTemporaryFile;
        }
    }
}
