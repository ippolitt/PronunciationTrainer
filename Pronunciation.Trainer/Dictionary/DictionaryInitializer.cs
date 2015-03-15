using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Core.Actions;
using System.Threading.Tasks;
using Pronunciation.Core.Utility;

namespace Pronunciation.Trainer.Dictionary
{
    public class DictionaryInitializer
    {
        private readonly DictionaryIndex _index;
        private readonly IDictionaryProvider _provider;
        private readonly object _syncLock = new object();
        private bool _isInitialized;
        private Action _scheduledAction;

        public DictionaryInitializer(DictionaryIndex index, IDictionaryProvider provider)
        {
            _index = index;
            _provider = provider;
        }

        public void InitializeAsync(Task continuation)
        {
            var action = new BackgroundActionWithoutArgs(BuildIndex, BuildIndexCompleted);
            action.StartAction(continuation, null);
        }

        public bool IsInitialized
        {
            get { return _isInitialized; }
        }

        public void ExecuteOnInitialized(Action action)
        {
            bool executeImmediately;
            lock (_syncLock)
            {
                if (_isInitialized)
                {
                    executeImmediately = true;
                    _scheduledAction = null;
                }
                else
                {
                    executeImmediately = false;
                    _scheduledAction = action; 
                }
            }

            // Execute action out of lock block
            if (executeImmediately)
            {
                action();
            }
        }

        private void BuildIndex(ActionContext context)
        {
            Logger.Info("Build index started...");

            List<IndexEntry> entries = _provider.GetWordsIndex(AppSettings.Instance.ActiveDictionaryIds);
            _index.Build(entries, true);

            Logger.Info("Build index completed.");
        }

        private void BuildIndexCompleted(ActionContext context, ActionResult result)
        {
            if (result.Error != null)
                throw result.Error;

            Action action = null;
            lock (_syncLock)
            {
                _isInitialized = true;
                action = _scheduledAction;
                _scheduledAction = null;
            }

            if (action != null)
            {
                action();
            }

            if (context.ContextData != null)
            {
                ((Task)context.ContextData).Start();
            }
        }
    }
}
