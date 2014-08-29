using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class SessionStatisticsCollector
    {
        private readonly HashSet<string> _pages;
        private readonly HashSet<string> _words;

        public delegate void SessionStatisticsChangedDelegate(int viewevPagesCount, int recordedWordsCount);
        public event SessionStatisticsChangedDelegate SessionStatisticsChanged;

        public SessionStatisticsCollector()
        {
            _pages = new HashSet<string>();
            _words = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public void RegisterViewedPage(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey))
                return;

            bool isAdded = _pages.Add(pageKey);
            if (SessionStatisticsChanged != null)
            {
                SessionStatisticsChanged(ViewevPagesCount, RecordedWordsCount);
            }
        }

        public void RegisterRecordedWord(string wordName)
        {
            if (string.IsNullOrEmpty(wordName))
                return;

            bool isAdded = _words.Add(wordName);
            if (SessionStatisticsChanged != null)
            {
                SessionStatisticsChanged(ViewevPagesCount, RecordedWordsCount);
            }
        }

        public int ViewevPagesCount
        {
            get { return _pages.Count; }
        }

        public int RecordedWordsCount
        {
            get { return _words.Count; }
        }
    }
}
