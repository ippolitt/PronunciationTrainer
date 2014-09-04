using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Dictionary
{
    public interface IDictionaryProvider
    {
        bool IsWordsIndexCached { get; }
        List<IndexEntry> GetWordsIndex(bool lpdDataOnly);
        ArticlePage PrepareArticlePage(string articleKey);
        PageInfo PrepareGenericPage(Uri pageUrl);
        PlaybackData GetAudio(string soundKey);
        PlaybackData GetAudioFromScriptData(string scriptData);
    }
}
