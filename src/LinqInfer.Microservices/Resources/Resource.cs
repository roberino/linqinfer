using LinqInfer.Text.Http;
using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Microservices.Resources
{
    public class Resource<T> : ResourceHeader
    {
        public T Data { get; set; }
    }
}