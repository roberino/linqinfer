using System;
using System.Net;

namespace LinqInfer.Data.Remoting
{
    public class HttpException : Exception
    {
        internal HttpException(int status, string message) : base(string.Format("{0} {1}", (HttpStatusCode)status, message))
        {
            Status = (HttpStatusCode)status;
        }

        public HttpStatusCode Status { get; private set; }
    }
}