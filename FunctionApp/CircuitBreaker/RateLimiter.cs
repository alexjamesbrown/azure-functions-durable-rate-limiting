using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace FunctionApp.CircuitBreaker
{
    [JsonObject(MemberSerialization.OptIn)]
    public class RateLimiter
    {
        private const string EntityName = nameof(RateLimiter);

        [JsonProperty]
        public TimeSpan Window { get; set; }

        [JsonProperty]
        public int MaxRequests { get; set; }

        [JsonProperty]
        public DateTime? InitialRequest { get; set; }

        [JsonProperty]
        public int RequestCount { get; set; }

        public bool IsRequestPermitted()
        {
            if (!InitialRequest.HasValue)
            {
                return true;
            }

            if (RequestCount < MaxRequests)
            {
                return true;
            }

            var diff = DateTime.Now.Subtract(InitialRequest.Value);

            if (diff <= Window)
                return false;

            // window has expired, reset
            InitialRequest = null;
            RequestCount = 0;

            return true;
        }

        public Task RecordRequest()
        {
            //var circuitBreakerId = Entity.Current.EntityKey;
            InitialRequest = DateTime.Now;
            RequestCount++;

            return Task.CompletedTask;
        }

        public static EntityId GetEntityId(string circuitBreakerId)
            => new EntityId(EntityName, circuitBreakerId);

        /// <summary>
        /// Function entry point; d
        /// </summary>
        /// <param name="context">An <see cref="IDurableEntityContext"/>, provided by dependency-injection.</param>
        /// <param name="logger">An <see cref="ILogger"/>, provided by dependency-injection.</param>
        [FunctionName(EntityName)]
        public static async Task Run(
            [EntityTrigger] IDurableEntityContext ctx,
            ILogger logger)
        {
            // The first time the circuit-breaker is accessed, it will self-configure.
            // if (!context.HasState)
            // {
            //     context.SetState(ConfigurationHelper.ConfigureCircuitBreaker(Entity.Current, logger));
            // }

            await ctx.DispatchAsync<RateLimiter>(logger);
        }
    }
}