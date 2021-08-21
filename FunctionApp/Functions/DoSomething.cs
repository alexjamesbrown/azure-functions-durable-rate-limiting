using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace FunctionApp
{
    public class DoSomething
    {
        private readonly IRequestRepository _requestRepository;

        public DoSomething(IRequestRepository requestRepository)
        {
            _requestRepository = requestRepository;
        }

        [FunctionName(nameof(DoSomething))]
        public async Task Run([ActivityTrigger] int iterationNumber, ILogger log)
        {
            Console.WriteLine($"EXECUTED iterationNumber {iterationNumber}");
            await _requestRepository.RecordRequest(DateTime.UtcNow);
        }
    }
}