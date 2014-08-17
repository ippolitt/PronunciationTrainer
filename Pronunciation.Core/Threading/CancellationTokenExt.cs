using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Threading
{
    public class CancellationTokenExt
    {
        private readonly CancellationTokenState _state;

        internal CancellationTokenExt(CancellationTokenState state)
        {
            _state = state;
        }

        public bool IsCancellationRequested 
        { 
            get {return _state.IsCancellationRequested; }
        }

        public bool IsThrowCanceledException
        {
            get { return _state.IsThrowCanceledException; }
        }
    }
}
