using LinqInfer.Microservices.Resources;
using System;

namespace LinqInfer.Microservices.Text
{
    public class RequestBase : ResourceHeader
    {
        public bool Confirmed  { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}