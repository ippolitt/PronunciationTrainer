using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Providers
{
    public class ScriptArgs
    {
        public string ScriptName {get; private set;}
        public object[] Args { get; private set; }

        public ScriptArgs(string scriptName, object[] args)
        {
            ScriptName = scriptName;
            Args = args;
        }
    }
}
