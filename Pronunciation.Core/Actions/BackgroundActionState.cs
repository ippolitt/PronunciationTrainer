using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public enum BackgroundActionState
    {
        NotStarted,
        Running,
        Suspended,
        Aborted,
        Completed
    }
}
