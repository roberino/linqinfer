using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Data.Remoting
{
    internal class OwinContext : ConstrainableDictionary<string, object>, IOwinContext
    {
        public OwinContext(TcpRequestHeader header, Stream requestBody, TcpResponse response, Uri clientBaseUri = null)
        {
            RequestHeader = header;
            RequestBody = requestBody;
            Response = response;

            var pathBits = header.Path.Split('?');

            this["owin.RequestHeaders"] = header.Headers;
            this["owin.RequestMethod"] = header.HttpVerb;
            this["owin.RequestBody"] = requestBody;
            this["owin.RequestPath"] = pathBits.Length > 0 ? pathBits[0] : "/";
            this["owin.RequestQueryString"] = pathBits.Length > 1 ? pathBits[1] : string.Empty;
            this["owin.RequestPathBase"] = string.Empty;
            this["owin.RequestProtocol"] = "HTTP/" + header.HttpProtocol;
            this["owin.RequestScheme"] = header.TransportProtocol == TransportProtocol.Http ? Uri.UriSchemeHttp : "tcp";

            this["owin.ResponseBody"] = response.Content;
            this["owin.ResponseHeaders"] = response.Header.Headers;
            this["owin.ResponseStatusCode"] = 200;
            this["owin.ResponseReasonPhrase"] = "OK";

            this["owin.CallCancelled"] = false;
            this["owin.Version"] = "OWIN 1.0";

            if (!header.Headers.ContainsKey("Host"))
            {
                if (clientBaseUri != null)
                {
                    header.Headers["Host"] = new[] { clientBaseUri.Host };
                    RequestUri = GetRequestUri();
                }
            }
            else
            {
                RequestUri = GetRequestUri();
            }

            LockKey("owin.Version");
            LockKey("owin.ResponseHeaders");
            LockKey("owin.ResponseBody");

            EnforceType<string>("owin.ResponseReasonPhrase");
            EnforceType<int>("owin.ResponseStatusCode");

            AddContraint((k, v) => !k.StartsWith("owin.Request"));
        }

        public Uri RequestUri { get; private set; }
        public TcpRequestHeader RequestHeader { get; private set; }
        public Stream RequestBody { get; private set; }
        public TcpResponse Response { get; private set; }

        private Uri GetRequestUri(string host = null)
        {
            var headers = this["owin.RequestHeaders"] as IDictionary<string, string[]>;

            var uri =
               (string)this["owin.RequestScheme"] +
               Uri.SchemeDelimiter +
               (host ?? headers["Host"][0]) +
               (string)this["owin.RequestPathBase"] +
               (string)this["owin.RequestPath"];

            if (!string.IsNullOrEmpty(this["owin.RequestQueryString"] as string))
            {
                uri += "?" + (string)this["owin.RequestQueryString"];
            }

            return new Uri(uri);
        }

        internal void Syncronise(TcpResponse response)
        {
            response.Header.StatusCode = (int)this["owin.ResponseStatusCode"];
        }
    }
}