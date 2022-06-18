using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;

namespace DurableRateLimiting
{
    public static class Extensions
    {
        public static async Task RateLimiterExecuteAsync(this IDurableOrchestrationContext context, Func<Task> action, Action<TimeSpan> ifDelayRequired = null)
        {
            //todo: pass this in
            var entityId = DurableRateLimitingEntity.GetEntityId("test-limiter");

            var rateLimiter = context.CreateEntityProxy<IDurableRateLimitingEntity>(entityId);

            using (await context.LockAsync(entityId))
            {
                var delayRequired = await rateLimiter.GetDelayBeforeNextRequest();

                if (!context.IsReplaying)
                {
                    if (delayRequired > TimeSpan.Zero)
                    {
                        Console.WriteLine($"DELAY REQUIRED - {delayRequired.TotalMilliseconds}");
                    }
                }

                if (delayRequired != TimeSpan.Zero)
                {
                    await context.CreateTimer(context.CurrentUtcDateTime.Add(delayRequired), CancellationToken.None);
                }

                await rateLimiter.RecordRequest();
                await action();
            }
        }
    }
}