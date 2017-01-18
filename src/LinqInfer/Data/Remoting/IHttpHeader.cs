using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public interface IHttpHeader
    {
        IDictionary<string, string[]> Headers { get; }
        string HttpProtocol { get; }
    }
}
