using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public class ActionArgs<TArgs>
    {
        public bool IsAllowed { get; private set; }
        public TArgs Args {get; private set;}

        public ActionArgs(bool isAllowed, TArgs args)
        {
            IsAllowed = isAllowed;
            Args = args;
        }

        public ActionArgs(TArgs args) : this(true, args)
        {
        }
    }
}
