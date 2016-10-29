﻿using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class OwinContext : ConstrainableDictionary<string, object>, IOwinContext
    {
        private readonly bool _ready;
        private readonly Uri _clientBaseUri;

        public OwinContext(TcpRequest request, TcpResponse response, Uri clientBaseUri = null)
        {
            var header = request.Header;

            _clientBaseUri = clientBaseUri;

            Request = request;
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
            this["owin.RequestBody"] = request.Content;
            this["owin.RequestProtocol"] = "HTTP/" + header.HttpProtocol;

            this["owin.ResponseBody"] = response.Content;
            this["owin.ResponseHeaders"] = response.Header.Headers;
            this["owin.ResponseStatusCode"] = response.Header.StatusCode.GetValueOrDefault(200);
            this["owin.ResponseReasonPhrase"] = response.Header.StatusText ?? "OK";

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

            _ready = true;
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
                Request.Header.Path = p;
                SetRequestUri(RequestUri, p, false);
            }
        }

        public void Cancel()
        {
            this["owin.CallCancelled"] = true;
        }

        public Uri RequestUri { get; private set; }
        public TcpRequest Request { get; private set; }
        public TcpResponse Response { get; private set; }

        public async Task WriteTo(Stream output)
        {
            Request.Save(output);
            await Response.WriteTo(output);
        }

        public IOwinContext Clone(bool deep)
        {
            return new OwinContext(Request.Clone(deep), Response.Clone(deep), _clientBaseUri)
            {
                User = User
            };
        }

        public void Dispose()
        {
            Request.Content.Dispose();
            Response.Dispose();
        }

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

        protected override void OnKeyUpdated(string key)
        {
            if (!_ready) return;

            base.OnKeyUpdated(key);

            if (key.StartsWith("owin.Response"))
            {
                Syncronise();
            }
        }

        internal void Syncronise()
        {
            Response.Header.StatusCode = (int)this["owin.ResponseStatusCode"];
            Response.Header.StatusText = this["owin.ResponseReasonPhrase"] as string;
        }
    }
}