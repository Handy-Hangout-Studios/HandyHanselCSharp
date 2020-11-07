using System;
using System.Collections.Generic;
using System.Text;

namespace HandyHansel.Utilities
{
    public class CustomResult<T>
    {
        public bool TimedOut { get; private set; }
        public bool Cancelled { get; private set; }
        public T Result { get; private set; }
        public CustomResult(T result = default, bool timedOut = false, bool cancelled = false)
        {
            Result = result;
            TimedOut = timedOut;
            Cancelled = cancelled;
        }
    }
}
