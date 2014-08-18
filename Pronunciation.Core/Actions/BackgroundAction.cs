using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Pronunciation.Core.Actions
{
    public abstract class BackgroundAction
    {
        public class ExecutionInfo
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

        public delegate void ActionStateChangedDelegate(BackgroundAction action);

        public event ActionStateChangedDelegate ActionStarted;
        public event ActionStateChangedDelegate ActionCompleted;
        public event ActionStateChangedDelegate ActionStateChanged;

        private readonly BackgroundWorker _worker;
        private Action<object> _progressReporter;
        private AbortRequest _abortRequest;
        private BackgroundActionState _actionState;
        private bool _isAbortable;
        private bool _isSuspendable;

        protected abstract bool PrepareArgs(ActionContext context, out object args);
        protected abstract object DoWork(ActionContext context, object args);
        protected abstract void ProcessCompleted(bool isSuccess, ExecutionInfo info);

        public BackgroundAction()
        {
            _worker = new BackgroundWorker();
            _worker.DoWork += DoWork;
            _worker.ProgressChanged += ProgressChanged;
            _worker.RunWorkerCompleted += RunWorkerCompleted;
        }

        public bool IsAbortable
        {
            get
            {
                return _isAbortable;
            }
            set
            {
                _isAbortable = value;
                _worker.WorkerSupportsCancellation = value;
            }
        }

        public bool IsSuspendable
        {
            get { return _isSuspendable; }
            set { _isSuspendable = value; }
        }

        public BackgroundActionState ActionState
        {
            get { return _actionState; }
            set 
            { 
                _actionState = value;
                if (ActionStateChanged != null)
                {
                    ActionStateChanged(this);
                }
            }
        }

        public bool IsAbortRequested
        {
            get { return _abortRequest != AbortRequest.None; }
        }

        public bool StartAction()
        {
            return StartAction(null, null);
        }

        public bool StartAction(object contextData, BackgroundActionSequence actionSequence)
        {
            if (ActionState == BackgroundActionState.Running || ActionState == BackgroundActionState.Suspended)
                return false;

            _abortRequest = AbortRequest.None;

            ActionContext context = new ActionContext(this)
            {
                ContextData = contextData,
                ActiveSequence = actionSequence
            };

            object args;
            if (!PrepareArgs(context, out args))
                return false;

            _worker.RunWorkerAsync(new ExecutionInfo { Context = context, Args = args });

            ActionState = BackgroundActionState.Running;
            if (ActionStarted != null)
            {
                ActionStarted(this);
            }
            return true;
        }

        public virtual void RequestAbort(bool isSoftAbort)
        {
            if (!_isAbortable)
                throw new InvalidOperationException("The operation is not abortable!");

            _abortRequest = isSoftAbort ? AbortRequest.SoftAbort : AbortRequest.HardAbort;
        }

        public virtual void Suspend()
        {
            if (!_isSuspendable)
                throw new InvalidOperationException("The operation is not suspendable!"); 
        }

        public virtual void Resume()
        {
            if (!_isSuspendable)
                throw new InvalidOperationException("The operation is not suspendable!");
        }

        protected void RegisterSuspended()
        {
            ActionState = BackgroundActionState.Suspended;
        }

        protected void RegisterResumed()
        {
            ActionState = BackgroundActionState.Running;
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
                ProcessCompleted(isSuccess, info);
            }
            finally
            {
                _abortRequest = AbortRequest.None;
                ActionState = isSuccess ? BackgroundActionState.Completed : BackgroundActionState.Aborted;
                if (ActionCompleted != null)
                {
                    ActionCompleted(this);
                }
            }

            if (isSuccess && info.Context != null && info.Context.ActiveSequence != null)
            {
                info.Context.ActiveSequence.StartNextAction(info.Context.ContextData);
            }
        }
    }

    public class BackgroundActionWithoutArgs : BackgroundAction
    {
        public Func<ActionContext, bool> Validator { get; protected set; }
        public Action<ActionContext> Worker { get; protected set; }
        public Action<ActionContext, ActionResult> ResultProcessor { get; protected set; }

        public BackgroundActionWithoutArgs(Action<ActionContext> worker, 
            Action<ActionContext, ActionResult> resultProcessor)
        {
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        public BackgroundActionWithoutArgs(Action worker, Action<ActionResult> resultProcessor)
        {
            Worker = (context) => worker();
            ResultProcessor = (context, result) => resultProcessor(result);
        }

        public BackgroundActionWithoutArgs(Func<ActionContext, bool> validator,
            Action<ActionContext> worker, Action<ActionContext, ActionResult> resultProcessor)
        {
            Validator = validator;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override bool PrepareArgs(ActionContext context, out object args)
        {
            args = null;
            return Validator == null ? true : Validator(context);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            Worker(context);
            return null;
        }

        protected override void ProcessCompleted(bool isSuccess, ExecutionInfo info)
        {
            ResultProcessor(info.Context, new ActionResult(isSuccess, info.Error));
        }
    }

    public class BackgroundActionWithoutArgs<TResult> : BackgroundAction
    {
        public Func<ActionContext, bool> Validator { get; protected set; }
        public Func<ActionContext, TResult> Worker { get; protected set; }
        public Action<ActionContext, ActionResult<TResult>> ResultProcessor { get; protected set; }

        public BackgroundActionWithoutArgs(Func<ActionContext, TResult> worker, 
            Action<ActionContext, ActionResult<TResult>> resultProcessor)
        {
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        public BackgroundActionWithoutArgs(Func<TResult> worker, Action<ActionResult<TResult>> resultProcessor)
        {
            Worker = (context) => worker();
            ResultProcessor = (context, result) => resultProcessor(result);
        }

        public BackgroundActionWithoutArgs(Func<ActionContext, bool> validator,
            Func<ActionContext, TResult> worker, Action<ActionContext, ActionResult<TResult>> resultProcessor)
        {
            Validator = validator;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        protected override bool PrepareArgs(ActionContext context, out object args)
        {
            args = null;
            return Validator == null ? true : Validator(context);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            return Worker(context);
        }

        protected override void ProcessCompleted(bool isSuccess, ExecutionInfo info)
        {
            ResultProcessor(info.Context, new ActionResult<TResult>(isSuccess, info.Error, (TResult)info.Result));
        }
    }

    public class BackgroundActionWithArgs<TArgs> : BackgroundAction
    {
        public Func<ActionContext, TArgs> ArgsBuilder { get; protected set; }
        public Action<ActionContext, TArgs> Worker { get; protected set; }
        public Action<ActionContext, TArgs, ActionResult> ResultProcessor { get; protected set; }
        public bool ContinueExecutionWithNullArgs { get; set; }

        public BackgroundActionWithArgs(Func<ActionContext, TArgs> argsBuilder,
            Action<ActionContext, TArgs> worker,
            Action<ActionContext, TArgs, ActionResult> resultProcessor)
        {
            ArgsBuilder = argsBuilder;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        public BackgroundActionWithArgs(Func<TArgs> argsBuilder,
            Action<TArgs> worker,
            Action<TArgs, ActionResult> resultProcessor)
        {
            ArgsBuilder = (context) => argsBuilder();
            Worker = (context, args) => worker(args);
            ResultProcessor = (context, args, result) => resultProcessor(args, result);
        }

        protected override bool PrepareArgs(ActionContext context, out object args)
        {
            args = ArgsBuilder(context);
            return (args == null ? ContinueExecutionWithNullArgs : true);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            Worker(context, (TArgs)args);
            return null;
        }

        protected override void ProcessCompleted(bool isSuccess, ExecutionInfo info)
        {
            ResultProcessor(info.Context, (TArgs)info.Args, new ActionResult(isSuccess, info.Error));
        }
    }

    public class BackgroundActionWithArgs<TArgs, TResult> : BackgroundAction
    {
        public Func<ActionContext, TArgs> ArgsBuilder { get; protected set; }
        public Func<ActionContext, TArgs, TResult> Worker { get; protected set; }
        public Action<ActionContext, TArgs, ActionResult<TResult>> ResultProcessor { get; protected set; }
        public bool ContinueExecutionWithNullArgs { get; set; }

        public BackgroundActionWithArgs(Func<ActionContext, TArgs> argsBuilder,
            Func<ActionContext, TArgs, TResult> worker,
            Action<ActionContext, TArgs, ActionResult<TResult>> resultProcessor)
        {
            ArgsBuilder = argsBuilder;
            Worker = worker;
            ResultProcessor = resultProcessor;
        }

        public BackgroundActionWithArgs(Func<TArgs> argsBuilder, 
            Func<TArgs, TResult> worker,
            Action<TArgs, ActionResult<TResult>> resultProcessor)
        {
            ArgsBuilder = (context) => argsBuilder();
            Worker = (context, args) => worker(args);
            ResultProcessor = (context, args, result) => resultProcessor(args, result);
        }

        protected override bool PrepareArgs(ActionContext context, out object args)
        {
            args = ArgsBuilder(context);
            return (args == null ? ContinueExecutionWithNullArgs : true);
        }

        protected override object DoWork(ActionContext context, object args)
        {
            return Worker(context, (TArgs)args);
        }

        protected override void ProcessCompleted(bool isSuccess, ExecutionInfo info)
        {
            ResultProcessor(info.Context, (TArgs)info.Args, new ActionResult<TResult>(isSuccess, info.Error, (TResult)info.Result)); 
        }
    }
}
