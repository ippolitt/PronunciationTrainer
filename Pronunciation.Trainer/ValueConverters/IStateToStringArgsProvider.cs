using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.ValueConverters
{
    public interface IStateToStringArgsProvider
    {
        object[] GetTrueStringFormatArgs();
        object[] GetFalseStringFormatArgs();
    }
}
