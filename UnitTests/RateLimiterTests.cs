using System;
using System.Threading;
using FunctionApp.CircuitBreaker;
using NUnit.Framework;

namespace UnitTests
{
    public class RateLimiterTests
    {
        [Test]
        public void First_request_should_increment_request_count_and_set_datetime()
        {
            var entity = new RateLimiter();

            entity.RecordRequest();

            Assert.AreEqual(1, entity.RequestCount);
            //todo: datetime
        }

        [Test]
        public void Second_request_should_increment_request_count()
        {
            var entity = new RateLimiter();

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
            var entity = new RateLimiter();
            entity.Window = TimeSpan.FromSeconds(10);
            entity.MaxRequests = 2;

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
            var entity = new RateLimiter();
            entity.Window = TimeSpan.FromSeconds(2);
            entity.MaxRequests = 2;

            Assert.IsTrue(entity.IsRequestPermitted());
            entity.RecordRequest();
            entity.RecordRequest();
            Assert.IsFalse(entity.IsRequestPermitted());
            
            // wait
            Thread.Sleep(2000);

            Assert.IsTrue(entity.IsRequestPermitted());
            Assert.AreEqual(0, entity.RequestCount);
        }
    }
}