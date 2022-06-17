using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class Orchestrator
    {
        private readonly IRequestRepository _requestRepository;

        public Orchestrator(IRequestRepository requestRepository)
        {
            _requestRepository = requestRepository;
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

                for (var i = 1; i <= 10; i++)
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

            Console.WriteLine("");
            Console.WriteLine($"Took {timeTaken.TotalSeconds}s in total");

            var requests = await _requestRepository.GetRequests();
            requests.Sort();

            Console.WriteLine("");
            Console.WriteLine($"Total requests: {requests.Count}");

            for (int i = 0; i < requests.Count; i++)
            {
                var d1 = requests.ElementAt(i);
                var d0 = requests.ElementAtOrDefault(i - 1);

                var diff = (d0 == default)
                    ? TimeSpan.FromSeconds(0)
                    : d1.Subtract(d0);

                Console.WriteLine($"Request {i} - {d1.ToString()} - happened {diff.TotalSeconds} after previous request");
            }

            await _requestRepository.Reset();
        }
    }
}