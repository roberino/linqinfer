using LinqInfer.Data.Remoting;
using LinqInfer.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.AspNetCore
{
    public class HttpContextConverter
    {
        public IOwinContext Convert(HttpContext context, bool bufferResponse)
        {
            return new ContextWrapper(context, bufferResponse);
        }

        private class ContextWrapper : IOwinContext
        {
            private readonly HttpContext _owinContext;

            public ContextWrapper(HttpContext owinContext, bool cloneable)
            {
                _owinContext = owinContext;

                Request = new TcpRequest(new RequestHeaderWrapper(owinContext.Request), cloneable ? Clone(owinContext.Request.Body) : owinContext.Request.Body);
                Response = new TcpResponse(new ResponseHeaderWrapper(owinContext.Request.Protocol, owinContext.Response), owinContext.Response.Body, cloneable);
            }

            private Stream Clone(Stream other)
            {
                var ms = new MemoryStream();

                other.CopyTo(ms);

                return ms;
            }

            public object this[string key]
            {
                get
                {
                    return _owinContext.Items[key];
                }

                set
                {
                    _owinContext.Items[key] = value;
                }
            }

            public int Count
            {
                get
                {
                    return _owinContext.Items.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return _owinContext.Items.IsReadOnly;
                }
            }

            public ICollection<string> Keys
            {
                get
                {
                    return _owinContext.Items.Keys.Select(k => k.ToString()).ToArray();
                }
            }

            public Uri RequestUri
            {
                get
                {
                    var uriStr = _owinContext.Request.Scheme + "://" + _owinContext.Request.Host + _owinContext.Request.Path + (_owinContext.Request.QueryString.HasValue ? "?" + _owinContext.Request.QueryString.Value : "");

                    return new Uri(uriStr);
                }
            }

            public TcpRequest Request { get; private set; }

            public TcpResponse Response { get; private set; }

            public ClaimsPrincipal User
            {
                get
                {
                    return _owinContext.User;
                }
            }

            public ICollection<object> Values
            {
                get
                {
                    return _owinContext.Items.Values;
                }
            }

            public void Add(KeyValuePair<string, object> item)
            {
                _owinContext.Items.Add(new KeyValuePair<object, object>(item.Key, item.Value));
            }

            public void Add(string key, object value)
            {
                _owinContext.Items.Add(key, value);
            }

            public void Cancel()
            {

            }

            public void Clear()
            {
                _owinContext.Items.Clear();
            }

            public IOwinContext Clone(bool deep)
            {
                string host = _owinContext.Request.Host.Host;

                IPAddress ipHost;

                if (IPAddress.TryParse(host, out ipHost))
                {
                    if (ipHost.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                    {
                        host = (ipHost.IsIPv6SiteLocal || host == "::1") ? "localhost" : ipHost.MapToIPv4().ToString();
                    }
                }

                var clientUri = new Uri(_owinContext.Request.Scheme + "://" + host + ":" + _owinContext.Request.Host.Port);

                return new OwinContext(Request.Clone(deep), Response.Clone(deep), clientUri);
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                return _owinContext.Items.Contains(new KeyValuePair<object, object>(item.Key, item.Value));
            }

            public bool ContainsKey(string key)
            {
                return _owinContext.Items.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                _owinContext.Items.Select(x => new KeyValuePair<string, object>(x.Key.ToString(), x.Value)).ToArray().CopyTo(array, arrayIndex);
            }

            public void Dispose()
            {
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                return _owinContext.Items.Select(x => new KeyValuePair<string, object>(x.Key.ToString(), x.Value)).GetEnumerator();
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                return _owinContext.Items.Remove(item);
            }

            public bool Remove(string key)
            {
                return _owinContext.Items.Remove(key);
            }

            public bool TryGetValue(string key, out object value)
            {
                return _owinContext.Items.TryGetValue(key, out value);
            }

            public async Task WriteTo(Stream output)
            {
                await Request.WriteTo(output);
                await Response.WriteTo(output);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _owinContext.Items.GetEnumerator();
            }
        }

        private class ResponseHeaderWrapper : IResponseHeader
        {
            private ConstrainableDictionary<string, string[]> _headers;
            private readonly HttpResponse _response;
            private readonly string _httpProtocol;

            public ResponseHeaderWrapper(string httpProtocol, HttpResponse response)
            {
                _response = response;
                _httpProtocol = httpProtocol;

                Date = DateTime.UtcNow;
                TextEncoding = Encoding.UTF8;

                _headers = new ConstrainableDictionary<string, string[]>(response.Headers.ToDictionary(v => v.Key, v => v.Value.ToArray()));

                _headers.AddContraint((k, v) =>
                {
                    try
                    {
                        _response.Headers[k] = new StringValues(v);
                    }
                    catch
                    {
                        return false;
                    }
                    return true;
                });
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
                    return _headers;
                }
            }

            public string HttpProtocol
            {
                get
                {
                    return HttpHeaderFormatter.DefaultHttpProtocol;
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
                    return _response.StatusCode.ToString(); // TODO: Fix this
                }

                set
                {
                    throw new NotSupportedException();
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
                    _response.Headers[header.Key] = new StringValues(header.Value.ToArray());
                }
            }

            public byte[] GetBytes()
            {
                using (var writer = new StringWriter())
                {
                    var formatter = new HttpHeaderFormatter(writer, true);

                    formatter.WriteResponseProtocolAndStatus(_httpProtocol, _response.StatusCode);
                    formatter.WriteDate();
                    formatter.WriteHeaders(_response.Headers.ToDictionary(h => h.Key, v => v.Value.ToArray()));
                    formatter.WriteEnd();

                    return Encoding.ASCII.GetBytes(writer.ToString());
                }
            }
        }

        private class RequestHeaderWrapper : IRequestHeader
        {
            private readonly HttpRequest _request;

            public RequestHeaderWrapper(HttpRequest request)
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
                    if (_request.Body.CanSeek) return _request.Body.Length;

                    var header = _request.Headers[HttpHeaderFormatter.ContentLengthHeaderName];

                    int len = 0;

                    if (header.Count > 0)
                    {
                        int.TryParse(header, out len);
                    }

                    return len;
                }
            }

            public string ContentMimeType { get; private set; }

            public IDictionary<string, string[]> Headers
            {
                get
                {
                    return _request.Headers.ToDictionary(h => h.Key, h => h.Value.ToArray());
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
                    return _request.Query.ToDictionary(q => q.Key, q => q.Value.ToArray());
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
                var accept = _request.Headers["Accept"];

                if (accept.Count > 0)
                {
                    var best = supportedMimeTypes.FirstOrDefault(m => accept.ToString().ToLower().Contains(m.ToLower()));

                    if (best != null) return best;
                }

                return supportedMimeTypes.First();
            }
        }
    }
}