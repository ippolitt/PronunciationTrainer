using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public class ActionContext
    {
        private readonly BackgroundAction _action;
        private readonly object _contextData;

        public ActionContext(BackgroundAction action, object contextData)
        {
            _action = action;
            _contextData = contextData;
        }

        public BackgroundAction ActiveAction
        {
            get { return _action; }
        } 

        public object ContextData
        {
            get { return _contextData; }
        }
    }
}
