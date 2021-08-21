using System;

namespace FunctionApp
{
    public class Limit
    {
        public Limit(int maxRequests, TimeSpan window)
        {
            MaxRequests = maxRequests;
            Window = window;
        }

        public TimeSpan Window { get; }
        public int MaxRequests { get; }
    }
}