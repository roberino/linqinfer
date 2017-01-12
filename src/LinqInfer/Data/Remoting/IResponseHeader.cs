using System;
using System.Collections.Generic;
using System.Text;

namespace LinqInfer.Data.Remoting
{
    public interface IResponseHeader : IHttpHeader
    {
        string ContentMimeType { get; set; }
        DateTime Date { get; set; }
        bool IsError { get; set; }
        int? StatusCode { get; set; }
        string StatusText { get; set; }
        Encoding TextEncoding { get; set; }
        TransportProtocol TransportProtocol { get; }

        byte[] GetBytes();
        void CopyFrom(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers);
    }
}