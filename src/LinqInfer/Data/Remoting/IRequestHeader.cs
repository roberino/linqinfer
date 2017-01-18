using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public interface IRequestHeader : IHttpHeader
    {
        Encoding ContentEncoding { get; }
        long ContentLength { get; }
        string ContentMimeType { get; }
        string HttpVerb { get; }
        string Path { get; }
        IDictionary<string, string[]> Query { get; }
        TransportProtocol TransportProtocol { get; }
        Verb Verb { get; }

        string PreferredMimeType(string[] supportedMimeTypes);
    }
}