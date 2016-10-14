using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;

namespace LinqInfer.Data.Remoting
{
    internal class OwinContext : ConstrainableDictionary<string, object>, IOwinContext
    {
        public OwinContext(TcpRequestHeader header, Stream requestBody, TcpResponse response, Uri clientBaseUri = null)
        {
            RequestHeader = header;
            RequestBody = requestBody;
            Response = response;
            User = new ClaimsPrincipal();

            this["owin.RequestHeaders"] = header.Headers;

            if (!header.Headers.ContainsKey("Host"))
            {
                if (clientBaseUri != null)
                {
                    SetRequestUri(clientBaseUri, header.Path);
                }
            }
            else
            {
                SetRequestUri(new Uri(header.TransportProtocol.ToString().ToLower() + Uri.SchemeDelimiter + header.Headers["Host"][0]), header.Path);
            }

            this["owin.RequestMethod"] = header.HttpVerb;
            this["owin.RequestBody"] = requestBody;
            this["owin.RequestProtocol"] = "HTTP/" + header.HttpProtocol;

            this["owin.ResponseBody"] = response.Content;
            this["owin.ResponseHeaders"] = response.Header.Headers;
            this["owin.ResponseStatusCode"] = 200;
            this["owin.ResponseReasonPhrase"] = "OK";

            this["owin.CallCancelled"] = false;
            this["owin.Version"] = "OWIN 1.0";

            LockKey("owin.Version");
            LockKey("owin.ResponseHeaders");
            LockKey("owin.RequestMethod");
            LockKey("owin.RequestProtocol");
            LockKey("owin.RequestScheme");

            EnforceType<string>("owin.ResponseReasonPhrase");
            EnforceType<int>("owin.ResponseStatusCode");
            EnforceType<bool>("owin.CallCancelled");
        }

        public ClaimsPrincipal User { get; set; }

        internal string Path
        {
            get
            {
                return this["owin.RequestPath"] as string;
            }
            set
            {
                Contract.Requires(value != null);

                var p = value.StartsWith("/") ? value : "/" + value;
                RequestHeader.Path = p;
                SetRequestUri(RequestUri, p, false);
            }
        }

        public void Cancel()
        {
            this["owin.CallCancelled"] = true;
        }

        public Uri RequestUri { get; private set; }
        public TcpRequestHeader RequestHeader { get; private set; }
        public Stream RequestBody { get; private set; }
        public TcpResponse Response { get; private set; }

        private Uri SetRequestUri(Uri baseUri, string pathAndQuery, bool setBaseUriParts = true)
        {
            if (setBaseUriParts)
            {
                var headers = this["owin.RequestHeaders"] as IDictionary<string, string[]>;

                this["owin.RequestScheme"] = baseUri.Scheme;
                this["owin.RequestPathBase"] = string.Empty;
                headers["Host"] = new[] { baseUri.Host };
            }

            RequestUri = new Uri(baseUri, pathAndQuery);
            
            this["owin.RequestPath"] = RequestUri.GetComponents(UriComponents.Path, UriFormat.Unescaped);

            this["owin.RequestQueryString"] = RequestUri.Query;

            return RequestUri;
        }

        internal void Syncronise(TcpResponse response)
        {
            response.Header.StatusCode = (int)this["owin.ResponseStatusCode"];
        }
    }
}