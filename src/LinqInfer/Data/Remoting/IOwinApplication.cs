using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IOwinApplication
    {
        void Map(UriRoute route, Func<IOwinContext, Task> handler);
    }
}