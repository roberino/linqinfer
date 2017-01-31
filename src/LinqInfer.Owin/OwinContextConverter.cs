using LinqInfer.Data.Remoting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using owin = Microsoft.Owin;
using System.Text;
using System.Linq;
using System.Net;

namespace LinqInfer.Owin
{
    public class OwinContextConverter
    {
        public IOwinContext Convert(owin.IOwinContext context, bool bufferResponse)
        {
            return new ContextWrapper(context, bufferResponse);
        }

        private class ContextWrapper : IOwinContext
        {
            private readonly owin.IOwinContext _owinContext;

            public ContextWrapper(owin.IOwinContext owinContext, bool bufferResponse)
            {
                _owinContext = owinContext;

                Request = new TcpRequest(new RequestHeaderWrapper(owinContext.Request), owinContext.Request.Body);
                Response = new TcpResponse(new ResponseHeaderWrapper(owinContext.Response), owinContext.Response.Body, bufferResponse);
            }

            public object this[string key]
            {
                get
                {
                    return _owinContext.Environment[key];
                }

                set
                {
                    _owinContext.Environment[key] = value;
                }
            }

            public int Count
            {
                get
                {
                    return _owinContext.Environment.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return _owinContext.Environment.IsReadOnly;
                }
            }

            public ICollection<string> Keys
            {
                get
                {
                    return _owinContext.Environment.Keys;
                }
            }

            public Uri RequestUri
            {
                get
                {
                    return _owinContext.Request.Uri;
                }
            }

            public TcpRequest Request { get; private set; }

            public TcpResponse Response { get; private set; }

            public ClaimsPrincipal User
            {
                get
                {
                    return _owinContext.Authentication.User;
                }
            }

            public ICollection<object> Values
            {
                get
                {
                    return _owinContext.Environment.Values;
                }
            }

            public void Add(KeyValuePair<string, object> item)
            {
                _owinContext.Environment.Add(item);
            }

            public void Add(string key, object value)
            {
                _owinContext.Environment.Add(key, value);
            }

            public void Cancel()
            {

            }

            public void Clear()
            {
                _owinContext.Environment.Clear();
            }

            public IOwinContext Clone(bool deep)
            {
                string host = _owinContext.Request.LocalIpAddress;

                IPAddress ipHost;

                if (IPAddress.TryParse(host, out ipHost))
                {
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        host = (ipHost.IsIPv6SiteLocal || host == "::1") ? "localhost" : ipHost.MapToIPv4().ToString();
                    }
                }

                var clientUri = new Uri(_owinContext.Request.Uri.Scheme + Uri.SchemeDelimiter + host + ":" + _owinContext.Request.LocalPort);

                return new OwinContext(Request.Clone(deep), Response.Clone(deep), clientUri);
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                return _owinContext.Environment.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return _owinContext.Environment.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                _owinContext.Environment.CopyTo(array, arrayIndex);
            }

            public void Dispose()
            {
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _owinContext.Environment.GetEnumerator();
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                return _owinContext.Environment.Remove(item);
            }

            public bool Remove(string key)
            {
                return _owinContext.Environment.Remove(key);
            }

            public bool TryGetValue(string key, out object value)
            {
                return _owinContext.Environment.TryGetValue(key, out value);
            }

            public Task WriteTo(Stream output)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _owinContext.Environment.GetEnumerator();
            }
        }

        private class ResponseHeaderWrapper : IResponseHeader
        {
            private readonly owin.IOwinResponse _response;

            public ResponseHeaderWrapper(owin.IOwinResponse response)
            {
                _response = response;

                Date = DateTime.UtcNow;
                TextEncoding = Encoding.UTF8;
            }

            public string ContentMimeType
            {
                get
                {
                    return string.IsNullOrEmpty(_response.ContentType) ? _response.ContentType : _response.ContentType.Split(';').First().Trim();
                }
                set
                {
                    _response.ContentType = value;
                }
            }

            public DateTime Date { get; set; }

            public IDictionary<string, string[]> Headers
            {
                get
                {
                    return _response.Headers;
                }
            }

            public string HttpProtocol
            {
                get
                {
                    return _response.Protocol;
                }
            }

            public bool IsError { get; set; }

            public int? StatusCode
            {
                get
                {
                    return _response.StatusCode;
                }

                set
                {
                    _response.StatusCode = value.GetValueOrDefault(200);
                }
            }

            public string StatusText
            {
                get
                {
                    return _response.ReasonPhrase;
                }

                set
                {
                    _response.ReasonPhrase = value;
                }
            }

            public Encoding TextEncoding { get; set; }

            public TransportProtocol TransportProtocol
            {
                get
                {
                    return TransportProtocol.Http;
                }
            }

            public void CopyFrom(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
            {
                foreach (var header in headers)
                {
                    _response.Headers.SetValues(header.Key, header.Value.ToArray());
                }
            }

            public byte[] GetBytes()
            {
                throw new NotSupportedException();
            }
        }

        private class RequestHeaderWrapper : IRequestHeader
        {
            private readonly owin.IOwinRequest _request;

            public RequestHeaderWrapper(owin.IOwinRequest request)
            {
                _request = request;

                ContentEncoding = Encoding.UTF8;

                string[] content;

                if (Headers.TryGetValue(HttpHeaderFormatter.ContentTypeHeaderName, out content) && content.Length > 0)
                {
                    var parts = content.First().Split(';');

                    if (parts.Length > 0)
                    {
                        ContentMimeType = parts[0];

                        if (parts.Length > 1)
                        {
                            try
                            {
                                ContentEncoding = Encoding.GetEncoding(parts[0]);
                            }
                            catch { }
                        }
                    }
                }
            }

            public Encoding ContentEncoding { get; private set; }

            public long ContentLength
            {
                get
                {
                    return _request.Body.Length;
                }
            }

            public string ContentMimeType { get; private set; }

            public IDictionary<string, string[]> Headers
            {
                get
                {
                    return _request.Headers;
                }
            }

            public string HttpProtocol
            {
                get
                {
                    return _request.Protocol;
                }
            }

            public string HttpVerb
            {
                get
                {
                    return _request.Method;
                }
            }

            public string Path
            {
                get
                {
                    return _request.Path.Value;
                }
            }

            public IDictionary<string, string[]> Query
            {
                get
                {
                    return _request.Query.ToDictionary(g => g.Key, q => q.Value);
                }
            }

            public TransportProtocol TransportProtocol
            {
                get
                {
                    return TransportProtocol.Http;
                }
            }

            public Verb Verb
            {
                get
                {
                    return HttpHeaderFormatter.ParseVerb(HttpVerb);
                }
            }

            public string PreferredMimeType(string[] supportedMimeTypes)
            {
                if (!string.IsNullOrEmpty(_request.Accept))
                {
                    var best = supportedMimeTypes.FirstOrDefault(m => _request.Accept.ToLower().Contains(m.ToLower()));

                    if (best != null) return best;
                }

                return supportedMimeTypes.First();
            }
        }
    }
}