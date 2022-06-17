using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace FunctionApp
{
    public interface IDurableRateLimitingEntity
    {
        Task<TimeSpan> GetDelayBeforeNextRequest();
        Task RecordRequest();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class DurableRateLimitingEntity : IDurableRateLimitingEntity
    {
        private readonly IDurableOrchestrationContext _context;
        private readonly ILogger _logger;
        private const string EntityName = nameof(DurableRateLimitingEntity);
        public static EntityId GetEntityId(string identifier) => new EntityId(EntityName, identifier);

        public DurableRateLimitingEntity(ILogger logger)
        {
            _logger = logger;
        }

        // Setup as a function to allow for unit testing
        public Func<DateTime> CurrentDate = () => DateTime.UtcNow;

        [JsonProperty]
        public TimeSpan Window { get; set; }

        [JsonProperty]
        public int MaxRequests { get; set; }

        [JsonProperty]
        public int RequestCount { get; set; }

        [JsonProperty]
        public DateTime? RequestDateTime { get; private set; }

        [JsonProperty]
        public TimeSpan? LastDelay { get; set; }

        public Task<TimeSpan> GetDelayBeforeNextRequest()
        {
            if (!RequestDateTime.HasValue)
                return Task.FromResult(TimeSpan.Zero);

            if (RequestCount < MaxRequests)
                return Task.FromResult(TimeSpan.Zero);

            var diff = CurrentDate().Subtract(RequestDateTime.Value);

            if (diff <= Window)
            {
                var delay = RequestDateTime
                    .Value.Add(Window)
                    .Subtract(CurrentDate());

                if (LastDelay.HasValue)
                {
                    delay = LastDelay.Value.Add(delay);
                }

                LastDelay = delay;

                return Task.FromResult(delay);
            }

            // window has expired, reset
            RequestDateTime = null;
            LastDelay = null;
            RequestCount = 0;

            return Task.FromResult(TimeSpan.Zero);
        }

        public Task RecordRequest()
        {
            RequestDateTime ??= CurrentDate();
            RequestCount++;

            return Task.CompletedTask;
        }

        [FunctionName(EntityName)]
        public static async Task Run(
            [EntityTrigger] IDurableEntityContext context,
            ILogger logger)
        {
            if (!context.HasState)
            {
                var e = new DurableRateLimitingEntity(logger)
                {
                    MaxRequests = 1,
                    Window = TimeSpan.FromSeconds(30),
                    RequestDateTime = null,
                    LastDelay = null
                };

                context.SetState(e);
            }

            await context.DispatchAsync<DurableRateLimitingEntity>(logger);
        }
    }
}