using LinqInfer.Data.Remoting;
using Microsoft.AspNetCore.Builder;

namespace LinqInfer.Microservices
{
    public interface IOwinApiBuilder : IHttpApiBuilder
    {
        IApplicationBuilder RegisterMiddleware(IApplicationBuilder appBuilder);
    }
}