using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pronunciation.Core.Actions
{
    public abstract class BackgroundAction
    {
        private class ExecutionInfo
        {
            public ActionContext Context;
            public object Args;
            public object Result;
            public Exception Error; 
        }

        private enum AbortRequest
        {
            None,
            SoftAbort,
            HardAbort
        }

        public delegate void ActionStartedDelegate(BackgroundAction action);
        public delegate void ActionCompletedDelegate(BackgroundAction action);

        public event ActionStartedDelegate ActionStarted;
        public event ActionCompletedDelegate ActionCompleted; 

        private readonly BackgroundWorker _worker;
        private Action<object> _progressReporter;
        private Action _aborter;
        private BackgroundActionSequence _actionSequence;
        private AbortRequest _abortRequest;
        private bool _isRunning;

        protected abstract ActionArgs<object> PrepareArgs(ActionContext context);
        protected abstract object DoWork(ActionContext context, object args);
        protected abstract void ProcessCompleted(ActionContext context, bool isSuccess, Exception error, object result);

        public BackgroundAction()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerSupportsCancellation = true;
            _worker.DoWork += DoWork;
            _worker.ProgressChanged += ProgressChanged;
            _worker.RunWorkerCompleted += RunWorkerCompleted;
        }

        public bool IsRunning
        {
            get { return _isRunning; }
        }

        public bool IsAbortRequested
        {
            get { return _abortRequest != AbortRequest.None; }
        }

        public bool StartAction()
        {
            return StartAction(null);
        }

        public bool StartAction(object contextData)
        {
            if (_isRunning)
                return false;

            _abortRequest = AbortRequest.None;
            ActionContext context = new ActionContext(this, contextData);
            ActionArgs<object> actionArgs = PrepareArgs(context);
            if (actionArgs == null || !actionArgs.IsAllowed)
                return false;

            _worker.RunWorkerAsync(new ExecutionInfo { Context = context, Args = actionArgs.Args });

            _isRunning = true;
            if (ActionStarted != null)
            {
                ActionStarted(this);
            }

            return true;
        }

        public virtual void RequestAbort(bool isSoftAbort)
        {
            _abortRequest = isSoftAbort ? AbortRequest.SoftAbort : AbortRequest.HardAbort;
            if (_aborter != null)
            {
                _aborter();
            }
        }

        public void ReportProgress(object progress)
        {
            if (_worker.WorkerReportsProgress)
            {
                _worker.ReportProgress(0, progress);
            }
        }

        public Action<object> ProgressReporter
        {
            get 
            { 
                return _progressReporter; 
            }
            set 
            {
                _progressReporter = value;
                _worker.WorkerReportsProgress = (value != null);
            }
        }

        public Action Aborter
        {
            get { return _aborter; }
            set { _aborter = value; }
        }

        public BackgroundActionSequence ActionSequence
        {
            get { return _actionSequence; }
            set { _actionSequence = value; }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            ExecutionInfo info = (ExecutionInfo)e.Argument;
            try
            {
                info.Result = DoWork(info.Context, info.Args);
            }
            catch (Exception ex)
            {
                info.Error = ex;
            }

            e.Result = info;
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _progressReporter(e.UserState);
        }

        private void RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ExecutionInfo info = (ExecutionInfo)e.Result;
            bool isSuccess = (info.Error == null && _abortRequest != AbortRequest.HardAbort);
            try
            {
                ProcessCompleted(info.Context, isSuccess, info.Error, info.Result);
            }
            finally
            {
                _isRunning = false;
                _abortRequest = AbortRequest.None;
                if (ActionCompleted != null)
                {
                    ActionCompleted(this);
                }
            }

            if (isSuccess && _actionSequence != null)
            {
                _actionSequence.StartNextAction();
            }
        }
    }

    public class BackgroundActionWithoutArgs : BackgroundAction
    {
        public Func<ActionContext, bool> Validator { get; protected set; }
        public Action<ActionContext> Worker { get; protected set; }
        public Action<ActionContext, ActionResult> ResultProcessor { get; protected set; }

        public BackgroundActionWithoutArgs(Func<ActionContext, bool> validator,
            Action<ActionContext> worker, Action<ActionContext, ActionResult> resultProcessor)
        {
            Validator = validator;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override ActionArgs<object> PrepareArgs(ActionContext context)
        {
            bool isAllowed = Validator == null ? false : Validator(context);
            return new ActionArgs<object>(isAllowed, null);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            Worker(context);
            return null;
        }

        protected override void ProcessCompleted(ActionContext context, bool isSuccess, Exception error, object result)
        {
            ResultProcessor(context, new ActionResult(isSuccess, error));
        }
    }

    public class BackgroundActionWithoutArgs<TResult> : BackgroundAction
    {
        public Func<ActionContext, bool> Validator { get; protected set; }
        public Func<ActionContext, TResult> Worker { get; protected set; }
        public Action<ActionContext, ActionResult<TResult>> ResultProcessor { get; protected set; }

        public BackgroundActionWithoutArgs(Func<ActionContext, bool> validator,
            Func<ActionContext, TResult> worker, Action<ActionContext, ActionResult<TResult>> resultProcessor)
        {
            Validator = validator;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override ActionArgs<object> PrepareArgs(ActionContext context)
        {
            bool isAllowed = Validator == null ? false : Validator(context);
            return new ActionArgs<object>(isAllowed, null);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            return Worker(context);
        }

        protected override void ProcessCompleted(ActionContext context, bool isSuccess, Exception error, object result)
        {
            ResultProcessor(context, new ActionResult<TResult>(isSuccess, error, (TResult)result));
        }
    }

    public class BackgroundActionWithArgs<TArgs> : BackgroundAction
    {
        public Func<ActionContext, ActionArgs<TArgs>> ArgsBuilder { get; protected set; }
        public Action<ActionContext, TArgs> Worker { get; protected set; }
        public Action<ActionContext, ActionResult> ResultProcessor { get; protected set; }

        public BackgroundActionWithArgs(Func<ActionContext, ActionArgs<TArgs>> argsBuilder,
            Action<ActionContext, TArgs> worker, Action<ActionContext, ActionResult> resultProcessor)
        {
            ArgsBuilder = argsBuilder;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override ActionArgs<object> PrepareArgs(ActionContext context)
        {
            ActionArgs<TArgs> actionArgs = ArgsBuilder(context);
            return (actionArgs == null) ? new ActionArgs<object>(false, null)
                : new ActionArgs<object>(actionArgs.IsAllowed, actionArgs.Args);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            Worker(context, (TArgs)args);
            return null;
        }

        protected override void ProcessCompleted(ActionContext context, bool isSuccess, Exception error, object result)
        {
            ResultProcessor(context, new ActionResult(isSuccess, error));
        }
    }

    public class BackgroundActionWithArgs<TArgs, TResult> : BackgroundAction
    {
        public Func<ActionContext, ActionArgs<TArgs>> ArgsBuilder { get; protected set; }
        public Func<ActionContext, TArgs, TResult> Worker { get; protected set; }
        public Action<ActionContext, ActionResult<TResult>> ResultProcessor { get; protected set; }

        public BackgroundActionWithArgs(Func<ActionContext, ActionArgs<TArgs>> argsBuilder,
            Func<ActionContext, TArgs, TResult> worker, Action<ActionContext, ActionResult<TResult>> resultProcessor)
        {
            ArgsBuilder = argsBuilder;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override ActionArgs<object> PrepareArgs(ActionContext context)
        {
            ActionArgs<TArgs> actionArgs = ArgsBuilder(context);
            return (actionArgs == null) ? new ActionArgs<object>(false, null) 
                : new ActionArgs<object>(actionArgs.IsAllowed, actionArgs.Args);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            return Worker(context, (TArgs)args);
        }

        protected override void ProcessCompleted(ActionContext context, bool isSuccess, Exception error, object result)
        {
            ResultProcessor(context, new ActionResult<TResult>(isSuccess, error, (TResult)result)); 
        }
    }
}
