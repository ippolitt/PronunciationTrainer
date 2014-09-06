using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Trainer.Dictionary
{
    public class SessionStatisticsCollector
    {
        private readonly HashSet<string> _pages;
        private readonly HashSet<string> _audios;

        public delegate void SessionStatisticsChangedDelegate(int viewevPagesCount, int recordedAudiosCount);
        public event SessionStatisticsChangedDelegate SessionStatisticsChanged;

        public SessionStatisticsCollector()
        {
            _pages = new HashSet<string>();
            _audios = new HashSet<string>();
        }

        public void RegisterViewedPage(string pageKey)
        {
            if (string.IsNullOrEmpty(pageKey))
                return;

            bool isAdded = _pages.Add(pageKey);
            if (SessionStatisticsChanged != null)
            {
                SessionStatisticsChanged(ViewevPagesCount, RecordedAudiosCount);
            }
        }

        public void RegisterRecordedAudio(string audioKey)
        {
            if (string.IsNullOrEmpty(audioKey))
                return;

            bool isAdded = _audios.Add(audioKey);
            if (SessionStatisticsChanged != null)
            {
                SessionStatisticsChanged(ViewevPagesCount, RecordedAudiosCount);
            }
        }

        public int ViewevPagesCount
        {
            get { return _pages.Count; }
        }

        public int RecordedAudiosCount
        {
            get { return _audios.Count; }
        }
    }
}
