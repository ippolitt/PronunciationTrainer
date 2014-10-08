using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Parser
{
    [Flags]
    public enum EnglishVariant
    {
        British = 1,
        American = 2,
        Australian = 4
    }
}
