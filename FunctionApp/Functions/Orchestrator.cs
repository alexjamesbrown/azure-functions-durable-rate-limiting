using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FunctionApp
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

                    //ifDelayRequired?.Invoke(delayRequired);
                }

                await rateLimiter.RecordRequest();

                await action();
            }
        }
    }

    public class DurableRateLimitedExecutor
    {
        public async Task ExecuteAsync(Func<Task> action, IDurableOrchestrationContext context)
        {
            //todo: pass this in
            var entityId = DurableRateLimitingEntity.GetEntityId("test-limiter");

            var rateLimiter = context.CreateEntityProxy<IDurableRateLimitingEntity>(entityId);

            using (await context.LockAsync(entityId))
            {
                var delayRequired = await rateLimiter.GetDelayBeforeNextRequest();

                if (!context.IsReplaying)
                {
                    Console.WriteLine($"DELAY REQUIRED - {delayRequired.TotalMilliseconds}");
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

    public class Orchestrator
    {
        private readonly IRequestRepository _requestRepository;
        private readonly DurableRateLimitedExecutor _ex;

        public Orchestrator(IRequestRepository requestRepository)
        {
            _requestRepository = requestRepository;
            _ex = new DurableRateLimitedExecutor();
        }

        [FunctionName(nameof(Orchestrator))]
        public async Task RunOrchestrator([OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            if (!context.IsReplaying)
            {
                await _requestRepository.Reset();
            }

            var startTime = context.CurrentUtcDateTime;

            try
            {
                var tasks = new List<Task>();

                for (var i = 1; i <= 5; i++)
                {
                    var iterationNumber = i;

                    var task = context.RateLimiterExecuteAsync(async () =>
                    {
                        await context.CallActivityAsync(nameof(DoSomething), iterationNumber);
                    });

                    tasks.Add(task);
                }

                await Task.WhenAll(tasks);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            var timeTaken = context
                .CurrentUtcDateTime
                .Subtract(startTime);

            Console.WriteLine($"Took {timeTaken.TotalSeconds}s in total");

            var requests = await _requestRepository.GetRequests();

            Console.WriteLine($"Total requests: {requests.Count}");
            await _requestRepository.Reset();
        }

        //private async Task ExecuteAsync(IDurableOrchestrationContext context, int iterationNumber)
        //{
        //    var entityId = DurableRateLimitingEntity.GetEntityId("test-limiter");

        //    var rateLimiter = context.CreateEntityProxy<IDurableRateLimitingEntity>(entityId);

        //    using (await context.LockAsync(entityId))
        //    {
        //        var delayRequired = await rateLimiter.GetDelayBeforeNextRequest();
        //        Console.WriteLine($"DELAY REQUIRED - {delayRequired.TotalMilliseconds}ms for iteration {iterationNumber}");

        //        if (delayRequired != TimeSpan.Zero)
        //        {
        //            await context.CreateTimer(context.CurrentUtcDateTime.Add(delayRequired), CancellationToken.None);
        //        }

        //        await rateLimiter.RecordRequest();
        //        await context.CallActivityAsync(nameof(DoSomething), iterationNumber);
        //    }
        //}
    }
}