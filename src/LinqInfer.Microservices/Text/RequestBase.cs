using System;

namespace LinqInfer.Microservices.Text
{
    public class RequestBase
    {
        public bool Confirmed { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}