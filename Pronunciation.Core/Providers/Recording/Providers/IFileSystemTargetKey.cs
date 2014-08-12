using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording.Providers
{
    public interface IFileSystemTargetKey : IRecordingTargetKey
    {
        bool IsFolder { get; }
        string RelativeTargetPath { get; }
    }
}
