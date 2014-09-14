using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Pronunciation.Core.Utility
{
    public class Logger
    {
        private static string _logFile;
        private const string ErrorHeader = "ERROR";
        private readonly static object _syncLock = new object();

        public static void Initialize(string logFile)
        {
            _logFile = logFile;
            if (!Directory.Exists(Path.GetDirectoryName(logFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(logFile));
            }
        }

        public static void Info(string message)
        {
            WriteLine(message);
        }

        public static void Error(string message)
        {
            Error(message, null);
        }

        public static void Error(Exception ex)
        {
            Error(null, ex);
        }

        public static void Error(string message, Exception ex)
        {
            WriteLine(string.Format("{0}: {1}{2}{3}", ErrorHeader, message, 
                (!string.IsNullOrEmpty(message) && ex != null) ? Environment.NewLine : null,
                ex));
        }

        private static void WriteLine(string text)
        {
            if (string.IsNullOrEmpty(_logFile))
                return;

            DateTime eventDate = DateTime.Now;
            lock (_syncLock)
            {
                File.AppendAllText(_logFile, string.Format("{0:yyyy-MM-dd HH-mm-ss.fff} {1}{2}",
                    eventDate, text, Environment.NewLine));
            }
        }
    }
}
