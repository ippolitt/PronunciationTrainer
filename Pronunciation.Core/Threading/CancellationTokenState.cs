using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Threading
{
    internal class CancellationTokenState
    {
        public bool IsCancellationRequested { get; set; }
        public bool IsThrowCanceledException { get; set; }
    }
}
