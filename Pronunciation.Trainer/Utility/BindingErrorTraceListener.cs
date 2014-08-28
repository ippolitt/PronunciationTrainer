using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Pronunciation.Trainer.Utility
{
    public class BindingErrorTraceListener : DefaultTraceListener
    {
        private static BindingErrorTraceListener _listener;
        private StringBuilder _builder = new StringBuilder();
        
        private BindingErrorTraceListener()
        { 
        }

        public static void SetTrace()
        { 
            SetTrace(SourceLevels.Error, TraceOptions.None); 
        }

        public static void SetTrace(SourceLevels level, TraceOptions options)
        {
            if (_listener == null)
            {
                _listener = new BindingErrorTraceListener();
                PresentationTraceSources.DataBindingSource.Listeners.Add(_listener);
            }

            _listener.TraceOutputOptions = options;
            PresentationTraceSources.DataBindingSource.Switch.Level = level;
        }

        public static void CloseTrace()
        {
            if (_listener == null)
                return;

            _listener.Flush();
            _listener.Close();
            PresentationTraceSources.DataBindingSource.Listeners.Remove(_listener);
            _listener = null;
        }

        public override void Write(string message)
        { 
            _builder.Append(message); 
        }

        public override void WriteLine(string message)
        {
            _builder.Append(message);

            var final = _builder.ToString();
            _builder.Length = 0;

            throw new InvalidOperationException(final);
        }
  }
}
