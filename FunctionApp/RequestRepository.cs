using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FunctionApp
{
    public interface IRequestRepository
    {
        Task RecordRequest(DateTime dateTimeOfRequest);
        Task<List<DateTime>> GetRequests();
        Task Reset();
    }

    public class RequestRepository : IRequestRepository
    {
        private readonly List<DateTime> _record;

        public RequestRepository()
        {
            _record = new List<DateTime>();
        }


        public Task RecordRequest(DateTime dateTimeOfRequest)
        {
            _record.Add(dateTimeOfRequest);
            return Task.CompletedTask;
        }

        public Task<List<DateTime>> GetRequests()
            => Task.FromResult(_record.ToList());

        public Task Reset()
        {
            Console.WriteLine("Resetting request database");

            _record.Clear();
            return Task.CompletedTask;
        }
    }
}
