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
        DictionaryWordInfo GetWordInfo(int wordId);
        List<int> GetWordsWithNotes();
        void UpdateFavoriteSound(int wordId, string favoriteSoundKey);
        ArticlePage PrepareArticlePage(IndexEntry index);
        PageInfo PrepareGenericPage(Uri pageUrl);
        DictionarySoundInfo GetAudio(string soundKey);
        DictionarySoundInfo GetAudioFromScriptData(string soundKey, string scriptData);
        void WarmUpSoundsStore();
    }
}
