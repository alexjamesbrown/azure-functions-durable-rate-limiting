using System;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class RateLimiter
    {
        private static readonly object LockObj = new object();
        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        public RateLimiter(int maxRequests, TimeSpan window)
        {
            Limit = new Limit(maxRequests, window);
        }

        public Limit Limit { get; }

        private int _requestCount;
        public int RequestCount
            => _requestCount;

        public DateTime? RequestDateTime { get; private set; }

        public bool IsRequestPermitted()
        {
            if (!RequestDateTime.HasValue)
                return true;

            if (_requestCount < Limit.MaxRequests)
                return true;

            var diff = DateTime.UtcNow.Subtract(RequestDateTime.Value);

            if (diff <= Limit.Window)
                return false;

            // window has expired, reset
            RequestDateTime = null;
            _requestCount = 0;

            return true;
        }

        public void RecordRequest()
        {
            RequestDateTime ??= DateTime.UtcNow;
            Interlocked.Increment(ref _requestCount);
        }

        public void Do(Action actionToPerform, Action<TimeSpan> retryInCallback)
        {
            lock (LockObj)
            {
                if (IsRequestPermitted())
                {
                    RecordRequest();
                    actionToPerform();
                }

                else
                {
                    InitiateRetryInCallback(retryInCallback);
                }
            }
        }

        public async Task Do(Func<Task> actionToPerform, Action<TimeSpan> retryInCallback)
        {
            await SemaphoreSlim.WaitAsync();

            try
            {
                if (IsRequestPermitted())
                {
                    RecordRequest();
                    await actionToPerform();
                }

                else
                {
                    InitiateRetryInCallback(retryInCallback);
                }
            }
            finally
            {
                SemaphoreSlim.Release();
            }
        }

        private void InitiateRetryInCallback(Action<TimeSpan> failureCallback)
        {
            var timeLeft = RequestDateTime!
                .Value.Add(Limit.Window)
                .Subtract(DateTime.UtcNow);

            failureCallback(timeLeft);
        }
    }
}