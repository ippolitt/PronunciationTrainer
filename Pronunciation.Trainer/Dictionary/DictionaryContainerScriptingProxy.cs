using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Pronunciation.Trainer.Dictionary
{
    [ComVisibleAttribute(true)]
    public class DictionaryContainerScriptingProxy
    {
        private readonly Action<string, string> _playAudioInvoker;
        private readonly Action<string> _loadPageInvoker;

        public DictionaryContainerScriptingProxy(Action<string, string> playAudioInvoker, Action<string> loadPageInvoker)
        {
            _playAudioInvoker = playAudioInvoker;
            _loadPageInvoker = loadPageInvoker;
        }

        public void PlayAudioExt(string soundKey, string audioData)
        {
            _playAudioInvoker(soundKey, audioData);
        }

        public void LoadPageExt(string wordName)
        {
            _loadPageInvoker(wordName);
        } 
    }
}
