using System;

namespace BlazingQuartz.Core.Events
{
    public class EventArgs<TArgs> : EventArgs
    {
        public TArgs Args { get; init; }
        public CancellationToken CancelToken { get; init; }

        public EventArgs(TArgs args, CancellationToken cancelToken = default(CancellationToken))
        {
            Args = args;
            CancelToken = cancelToken;
        }
    }
}
