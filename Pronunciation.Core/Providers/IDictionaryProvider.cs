using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers
{
    public interface IDictionaryProvider
    {
        List<IndexEntry> GetWords();
        List<KeyTextPair<string>> GetWordLists();

        PageInfo LoadArticlePage(string pageKey);
        PageInfo LoadListPage(string pageKey);
        PageInfo InitPageFromUrl(Uri pageUrl);

        PlaybackSettings GetReferenceAudio(string audioKey);
        PlaybackSettings GetAudioFromScriptData(string scriptData);
        PlaybackSettings GetRecordedAudio(string audioKey);
        RecordingSettings GetRecordingSettings(string audioKey);
        bool IsRecordedAudioExists(string audioKey); 
    }
}
