using FunctionApp;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace UnitTests
{
    public class DurableRateLimitingEntityTests
    {
        private DurableRateLimitingEntity _DurableRateLimitingEntity;
        private Mock<ILogger> _mockLoger;

        [SetUp]
        public void SetUp()
        {
            _mockLoger = new Mock<ILogger>();
            _DurableRateLimitingEntity = new DurableRateLimitingEntity(_mockLoger.Object);
        }

        [Test]
        public async Task x()
        {
            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 0);

            _DurableRateLimitingEntity.MaxRequests = 1;
            _DurableRateLimitingEntity.Window = TimeSpan.FromSeconds(10);

            await _DurableRateLimitingEntity.RecordRequest();

            var delay = await _DurableRateLimitingEntity.GetDelayBeforeNextRequest();

            Assert.AreEqual(10, delay.TotalSeconds);
        }

        [Test]
        public async Task x1()
        {
            _DurableRateLimitingEntity.MaxRequests = 1;
            _DurableRateLimitingEntity.Window = TimeSpan.FromSeconds(10);

            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 0);

            await _DurableRateLimitingEntity.RecordRequest();

            // 5 seconds later
            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 5);

            var delay = await _DurableRateLimitingEntity.GetDelayBeforeNextRequest();

            Assert.AreEqual(5, delay.TotalSeconds);
        }

        [Test]
        public async Task x3()
        {
            _DurableRateLimitingEntity.MaxRequests = 1;
            _DurableRateLimitingEntity.Window = TimeSpan.FromSeconds(10);

            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 0);

            await _DurableRateLimitingEntity.RecordRequest();

            // 5 seconds later
            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 5);

            var delay = await _DurableRateLimitingEntity.GetDelayBeforeNextRequest();

            Assert.AreEqual(5, delay.TotalSeconds);
        }

        [Test]
        public async Task x4()
        {
            _DurableRateLimitingEntity.MaxRequests = 1;
            _DurableRateLimitingEntity.Window = TimeSpan.FromSeconds(10);

            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 0);

            await _DurableRateLimitingEntity.RecordRequest();

            // 5.5 seconds later
            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 5, 500);

            var delay = await _DurableRateLimitingEntity.GetDelayBeforeNextRequest();

            Assert.AreEqual(4.5, delay.TotalSeconds);
        }

        [Test]
        public async Task x5()
        {
            _DurableRateLimitingEntity.MaxRequests = 1;
            _DurableRateLimitingEntity.Window = TimeSpan.FromSeconds(10);

            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 0);

            _DurableRateLimitingEntity.LastDelay = TimeSpan.FromSeconds(5);

            await _DurableRateLimitingEntity.RecordRequest();

            // 5.5 seconds later
            _DurableRateLimitingEntity.CurrentDate = () => new DateTime(2021, 1, 1, 10, 0, 5, 500);

            var delay = await _DurableRateLimitingEntity.GetDelayBeforeNextRequest();

             Assert.AreEqual(4.5, delay.TotalSeconds);
        }
    }
}
