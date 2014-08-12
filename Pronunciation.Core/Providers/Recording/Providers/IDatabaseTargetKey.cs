using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers.Recording.Providers
{
    public interface IDatabaseTargetKey : IRecordingTargetKey
    {
        int TargetTypeId { get; }
        string TargetKey { get; }
    }
}
