using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FunctionApp;
using NUnit.Framework;
using RateLimiting;

namespace UnitTests
{
    public class RateLimiterTests
    {
        [Test]
        public void First_request_should_increment_request_count_and_set_datetime()
        {
            var entity = new RateLimiter(10, TimeSpan.FromSeconds(10));

            entity.RecordRequest();

            Assert.AreEqual(1, entity.RequestCount);
            //todo: datetime
        }

        [Test]
        public void Second_request_should_increment_request_count()
        {
            var entity = new RateLimiter(10, TimeSpan.FromSeconds(10));

            entity.RecordRequest();

            Assert.AreEqual(1, entity.RequestCount);
            //todo: datetime

            entity.RecordRequest();

            Assert.AreEqual(2, entity.RequestCount);
            //todo: datetime
        }

        [Test]
        public void Returns_false_when_Request_Count_exceeds_MaxRequests_within_timeframe()
        {
            var entity = new RateLimiter(2, TimeSpan.FromSeconds(10));

            //request 1
            Assert.IsTrue(entity.IsRequestPermitted());
            entity.RecordRequest();
            Thread.Sleep(500);

            //request 2
            Assert.IsTrue(entity.IsRequestPermitted());
            entity.RecordRequest();
            Thread.Sleep(500);

            //request 3
            Assert.IsFalse(entity.IsRequestPermitted());
        }

        [Test]
        public void IsRequestPermitted_resets_after_window()
        {
            var entity = new RateLimiter(2, TimeSpan.FromSeconds(2));

            Assert.IsTrue(entity.IsRequestPermitted());
            entity.RecordRequest();
            entity.RecordRequest();
            Assert.IsFalse(entity.IsRequestPermitted());

            // wait
            Thread.Sleep(2000);

            Assert.IsTrue(entity.IsRequestPermitted());
            Assert.AreEqual(0, entity.RequestCount);
        }

        [Test]
        public void TryPerform_performs_action_if_permitted()
        {
            var entity = new RateLimiter(10, TimeSpan.FromSeconds(10));

            int result = 0;

            entity.Do(
                () => { result = 1; },
                (ts) =>
                {
                    //do nothing on fail
                }
            );

            Assert.AreEqual(1, result);
        }

        [Test]
        public async Task TryPerformAsync_performs_async_action_if_permitted()
        {
            var entity = new RateLimiter(10, TimeSpan.FromSeconds(10));

            int result = 0;

            await entity
                .Do(async () =>
                    {
                        await Task.Delay(100);
                        await Task.Delay(100);
                        await Task.Delay(100);
                        result = 1;
                    },
                    (ts) =>
                    {
                        //do nothing on fail
                    }
                );

            Assert.AreEqual(1, result);
        }

        [Test]
        public void TryPerform_calls_failure_callback_with_timespan_if_not_permitted()
        {
            var entity = new RateLimiter(1, TimeSpan.FromSeconds(10));
            entity.RecordRequest();

            var result = 0;
            var retryIn = TimeSpan.Zero;

            entity.Do(
                () => { result = 1; },
                (ts) =>
                {
                    result = -1;
                    retryIn = ts;
                }
            );

            Assert.AreEqual(-1, result);
            Assert.AreEqual(9, retryIn.Seconds);
        }

        [Test]
        public void TryPerform_calls_failure_callback_with_timespan_if_not_permitted_multi_threaded()
        {
            //1 request every 5 seconds
            var entity = new RateLimiter(1, TimeSpan.FromSeconds(5));
            var numberOfRequests = 100;

            var failures = new ConcurrentBag<TimeSpan>();

            Parallel.For(0, numberOfRequests, c =>
            {
                entity.Do(
                    () => { Thread.Sleep(1000); },
                    (ts) => { failures.Add(ts); }
                );
            });

            //only one request should succeeed
            var expectedFailures = numberOfRequests - 1;

            Assert.AreEqual(expectedFailures, failures.Count);

            //time left should be less than 1 second
            Assert.Less(failures.First().Milliseconds, 1000);
        }

        [Test]
        public void TryPerform_calls_failure_callback_with_timespan_if_not_permitted_multi_threaded_async()
        {
            //1 request every 5 seconds
            var entity = new RateLimiter(1, TimeSpan.FromSeconds(5));
            var numberOfRequests = 100;

            var failures = new ConcurrentBag<TimeSpan>();

            Parallel.For(0, numberOfRequests, c =>
            {
                entity.Do((Action) (async () => { await Task.Delay(1000); }),
                    (ts) => { failures.Add(ts); }
                );
            });

            //only one request should succeed
            var expectedFailures = numberOfRequests - 1;

            Assert.AreEqual(expectedFailures, failures.Count);

            //time left should be less than 1 second
            Assert.Less(failures.First().Milliseconds, 1000);
        }

        [Test]
        public async Task
            TryPerform_calls_failure_callback_with_timespan_if_not_permitted_multi_threaded_async_with_result()
        {
            //1 request every 5 seconds
            var entity = new RateLimiter(1, TimeSpan.FromSeconds(5));
            var numberOfRequests = 1000;

            var failures = new List<TimeSpan>();

            var tasks = new List<Task>();

            for (var i = 0; i < numberOfRequests; i++)
            {
                var task = entity.Do(getResult,
                    (ts) => { failures.Add(ts); }
                );
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);

            //only one request should succeed
            var expectedFailures = numberOfRequests - 1;

            Assert.AreEqual(expectedFailures, failures.Count);

            //time left should be less than 1 second
            Assert.Less(failures.First().Milliseconds, 1000);
        }

        private Task<int> getResult() => Task.FromResult<int>(1234);
    }
}