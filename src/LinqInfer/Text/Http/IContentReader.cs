using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LinqInfer.Text.Http
{
    public interface IContentReader
    {
        Task<HttpDocument> ReadAsync(Uri uri, Stream content, IDictionary<string, string[]> headers, string mimeType, Encoding encoding, Func<XElement, XElement> targetElement);
    }
}