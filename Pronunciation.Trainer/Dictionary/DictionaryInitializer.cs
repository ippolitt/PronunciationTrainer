﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pronunciation.Core.Providers.Dictionary;
using Pronunciation.Core.Actions;
using System.Threading.Tasks;

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

        public void InitializeAsync()
        {
            var action = new BackgroundActionWithoutArgs(BuildIndex, BuildIndexCompleted);
            action.StartAction();
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

        private void BuildIndex()
        {
            List<IndexEntry> data = _provider.GetWordsIndex(AppSettings.Instance.DisplayLPDDataOnly);
            _index.Build(data);
        }

        private void BuildIndexCompleted(ActionResult result)
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

            Task.Factory.StartNew(_provider.WarmUp);
        }
    }
}
