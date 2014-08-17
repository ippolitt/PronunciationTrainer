using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Threading
{
    public class CancellationTokenSourceExt
    {
        public CancellationTokenExt Token { get; private set; }
        private readonly  CancellationTokenState _state;

        public CancellationTokenSourceExt()
        {
            _state = new CancellationTokenState();
            Token = new CancellationTokenExt(_state);
        }

        public void Cancel(bool isThrowCancelledException)
        {
            _state.IsCancellationRequested = true;
            _state.IsThrowCanceledException = isThrowCancelledException;
        }
    }
}
