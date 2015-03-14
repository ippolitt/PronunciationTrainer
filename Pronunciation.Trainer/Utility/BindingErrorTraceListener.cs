using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Pronunciation.Core.Utility;

namespace Pronunciation.Trainer.Utility
{
    public class BindingErrorTraceListener : DefaultTraceListener
    {
        private StringBuilder _builder = new StringBuilder();

        public static void Configure()
        {
            // As a side effect of this method WPF will enable tracing (by default, it's enabled only if attached 
            // to the debugger or ManagedTracing[REG_DWORD] = 1 in the registry key HKCU\software\microsoft\tracing\wpf)
            PresentationTraceSources.Refresh();
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, 
            string format, params object[] args)
        {
            TraceEvent(eventCache, source, eventType, id, string.Format(format, args));
        }

        public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, 
            string message)
        {
            if (eventType == TraceEventType.Error || eventType == TraceEventType.Critical)
            {             
                ProcessError(message);
            }
        }

        private void ProcessError(string errorMessage)
        {
            // Note, we can't show message box here - the dispatcher could be suspended because of this
            Logger.Error("WPF binding error:\r\n" + errorMessage);
        }
    }
}
