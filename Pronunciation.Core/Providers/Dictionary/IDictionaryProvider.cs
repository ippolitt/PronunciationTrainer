using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Contexts;

namespace Pronunciation.Core.Providers.Dictionary
{
    public interface IDictionaryProvider
    {
        List<IndexEntry> GetWordsIndex(int[] dictionaryIds);
        ArticlePage PrepareArticlePage(string articleKey);
        PageInfo PrepareGenericPage(Uri pageUrl);
        DictionarySoundInfo GetAudio(string soundKey);
        DictionarySoundInfo GetAudioFromScriptData(string soundKey, string scriptData);
        void WarmUp();
    }
}
