using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public class ActionContext
    {
        private readonly BackgroundAction _action;

        public object ContextData {get; set;}
        public BackgroundActionSequence ActiveSequence { get; set; }

        internal ActionContext(BackgroundAction action)
        {
            _action = action;
        }

        public BackgroundAction ActiveAction
        {
            get { return _action; }
        }
    }
}
