﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pronunciation.Core.Actions
{
    public class ActionResult
    {
        public bool IsSuccess { get; private set; }
        public Exception Error { get; private set; }

        public ActionResult(bool isSuccess, Exception error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }
    }

    public class ActionResult<TResult> : ActionResult
    {
        public TResult Result { get; private set; }

        public ActionResult(bool isSuccess, Exception error, TResult result) : base(isSuccess, error)
        {
            Result = result;
        }
    }
}