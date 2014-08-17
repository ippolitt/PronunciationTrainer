using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Threading;

namespace Pronunciation.Core.Audio
{
    public class AudioSamplesProcessingArgs
    {
        public int CollectionStep { get; private set; }
        public bool CollectOneChannelOnly { get; private set; }
        public CancellationTokenExt AbortToken { get; private set; }
        public TimeSpan StartFrom { get; private set; }

        public AudioSamplesProcessingArgs(bool collectOneChannelOnly)
        {
            CollectOneChannelOnly = collectOneChannelOnly;
            StartFrom = TimeSpan.Zero;
        }

        public AudioSamplesProcessingArgs(bool collectOneChannelOnly, int collectionStep, TimeSpan startFrom, CancellationTokenExt abortToken)
            : this (collectOneChannelOnly)
        {
            CollectionStep = collectionStep;
            StartFrom = startFrom;
            AbortToken = abortToken;
        }
    }
}
