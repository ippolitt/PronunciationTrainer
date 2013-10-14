using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer
{
    public interface ISupportsKeyboardFocus
    {
        void CaptureKeyboardFocus();
        bool IsLoaded { get; }
    }
}
