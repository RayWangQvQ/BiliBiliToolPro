using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Ray.Serilog.Sinks.Batched
{
    internal class BoundedConcurrentQueue<T>
    {
        public const int Unbounded = -1;

        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();

        private readonly int _queueLimit;

        private int _counter;

        public int Count => _queue.Count;

        public BoundedConcurrentQueue(int? queueLimit = null)
        {
            if (queueLimit.HasValue && queueLimit <= 0)
            {
                throw new ArgumentOutOfRangeException("queueLimit", "Queue limit must be positive, or `null` to indicate unbounded.");
            }

            _queueLimit = (queueLimit ?? (-1));
        }

        public bool TryDequeue(out T item)
        {
            if (_queueLimit == -1)
            {
                return _queue.TryDequeue(out item);
            }

            bool result = false;
            try
            {
            }
            finally
            {
                if (_queue.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref _counter);
                    result = true;
                }
            }

            return result;
        }

        public bool TryEnqueue(T item)
        {
            if (_queueLimit == -1)
            {
                _queue.Enqueue(item);
                return true;
            }

            bool result = true;
            try
            {
            }
            finally
            {
                if (Interlocked.Increment(ref _counter) <= _queueLimit)
                {
                    _queue.Enqueue(item);
                }
                else
                {
                    Interlocked.Decrement(ref _counter);
                    result = false;
                }
            }

            return result;
        }
    }
}
