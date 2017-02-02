using LinqInfer.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApiBuilder : IHttpApiBuilder
    {
        protected readonly IOwinAppBuilder _host;
        private readonly RoutingHandler _routes;
        private readonly FunctionBinder _binder;
        private readonly IObjectSerialiser _serialiser;
        private readonly Uri _baseUri;
        private readonly bool _bindToAnyHost;

        public HttpApiBuilder(IObjectSerialiser serialiser, IOwinAppBuilder host, Uri baseUri = null)
        {
            _serialiser = serialiser;
            _routes = new RoutingHandler();
            _binder = new FunctionBinder(serialiser);
            _host = host;
            _baseUri = baseUri ?? Util.Localhost;

            _bindToAnyHost = baseUri == null;

            _host.AddErrorHandler(StandardErrorHandler);
            _host.AddComponent(PostProcess, OwinPipelineStage.PostHandlerExecute);

            AddComponent(_routes.CreateApplicationDelegate());
        }

        public Task ProcessContext(IOwinContext context)
        {
            return _host.ProcessContext(context);
        }

        public Task<T> TestRoute<T>(Uri uri, T sampleResult, IDictionary<string, string[]> headers = null)
        {
            return TestRoute<T>(uri, headers);
        }

        public async Task<T> TestRoute<T>(Uri uri, 
            Verb httpVerb, 
            Stream requestBody = null,
            IDictionary<string, string[]> headers = null)
        {
            var baseUri = _host is IOwinApplication ? ((IOwinApplication)_host).BaseEndpoint : new Uri(uri, "/");

            TcpRequest request;

            using (var writer = new StringWriter())
            {
                using (var headerFormatter = new HttpHeaderFormatter(writer, true))
                {
                    headerFormatter.WriteRequestAndProtocol(HttpHeaderFormatter.TranslateVerb(httpVerb), uri.PathAndQuery);

                    if (headers == null) headers = new Dictionary<string, string[]>();

                    headers["Host"] = new[] { baseUri.Host + (baseUri.Port != 80 ? ":" + baseUri.Port : "") };

                    headerFormatter.WriteHeaders(headers);

                    headerFormatter.WriteEnd();
                }

                var headerBytes = Encoding.ASCII.GetBytes(writer.ToString());

                request = new TcpRequest(new TcpRequestHeader(headerBytes), requestBody ?? Stream.Null);
            }

            var localContext = new OwinContext(request, new TcpResponse(TransportProtocol.Http), baseUri);

            localContext["ext.ExpectedResponseType"] = typeof(T);

            await _host.ProcessContext(localContext);

            if (localContext.Response.Header.StatusCode.GetValueOrDefault(0) != 200)
            {
                throw new HttpException(localContext.Response.Header.StatusCode.GetValueOrDefault(0), localContext.Response.Header.StatusText ?? "HTTP Error");
            }

            var response = localContext.Response.GetSendStream();
            var mimeType = localContext.Request.Header.PreferredMimeType(_serialiser.SupportedMimeTypes);

            return await _serialiser.Deserialise<T>(response, localContext.Response.Header.TextEncoding, mimeType);
        }

        public Task<T> TestRoute<T>(Uri uri, IDictionary<string, string[]> headers = null)
        {
            return TestRoute<T>(uri, Verb.Get, null, headers);
        }

        public RouteBinder Bind(string routeTemplate, Verb verb = Verb.Get, Func<IOwinContext, bool> predicate = null)
        {
            return new RouteBinder(_baseUri.CreateRoute(routeTemplate, verb, predicate, _bindToAnyHost), _routes, _binder);
        }

        public IUriRoute ExportAsyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, Task<TResult>> func, string name = null)
        {
#if NET_STD
            var method = func.GetMethodInfo();
#else
            var method = func.Method;
#endif

            var parameters = GetParameters<TArg>(method.GetParameters().First());
            var route = _baseUri.CreateRoute("/" + GetPathForName(name ?? method.Name) + "/" + string.Join("/", parameters.ToArray()), Verb.Get);
            _routes.AddRoute(route, _binder.BindToAsyncMethod(func, defaultValue));
            return route;
        }

        public IUriRoute ExportSyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, TResult> func, string name = null)
        {
#if NET_STD
            var method = func.GetMethodInfo();
#else
            var method = func.Method;
#endif
            var parameters = GetParameters<TArg>(method.GetParameters().First());
            var route = _baseUri.CreateRoute("/" + GetPathForName(name ?? method.Name) + "/" + string.Join("/", parameters.Select(p => '{' + p + '}').ToArray()), Verb.Get);
            _routes.AddRoute(route, _binder.BindToSyncMethod(func, defaultValue));
            return route;
        }

        public void AddComponent(Func<IOwinContext, Task> handler, OwinPipelineStage stage = OwinPipelineStage.PreHandlerExecute)
        {
            _host.AddComponent(handler, stage);
        }

        public void AddErrorHandler(Func<IOwinContext, Exception, Task<bool>> errorHandler)
        {
            _host.AddErrorHandler(errorHandler);
        }

        private async Task<bool> PostProcess(IOwinContext context)
        {
            if (!context.Response.Header.StatusCode.HasValue && !context.Response.HasContent)
            {
                context.Response.CreateStatusResponse(404);
            }
            else
            {
                if (context.Response.Header.StatusCode == 405)
                {
                    var mappings = _routes.GetMappings(context.RequestUri).ToList();

                    if (mappings.Any())
                    {
                        await WriteOptions(context, mappings);
                    }
                }
            }

            return true;
        }

        private Task WriteOptions(IOwinContext context, IList<IUriRoute> mappings)
        {
            var supportedVerbs = mappings.SelectMany(m => m.Verbs.ToString().ToUpper().Split(',')).Distinct().ToArray();
                //.Aggregate(new StringBuilder(), (s,v) => (s.Length > 0 ? s.Append(',') : s).Append(v)).ToString();

            context.Response.Header.Headers["Accept"] = supportedVerbs;

            return _binder.SerialiseAndSetMimeAndStatus(context, new
            {
                routes = mappings.Select(m => new
                {
                    template = m.Template,
                    baseUri = m.BaseUri,
                    verbs = m.Verbs.ToString().ToUpper()
                })
            });
        }

        private Task<bool> StandardErrorHandler(IOwinContext context, Exception ex)
        {
            if (ex is ArgumentException && context.Response.Header.StatusCode.HasValue)
            {
                context.Response.CreateStatusResponse(context.Response.Header.StatusCode.Value);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        private string GetPathForName(string name)
        {
            return new string(name.Where(n => char.IsLetterOrDigit(n)).ToArray()).ToLower();
        }

        private IEnumerable<string> GetParameters<TArg>(ParameterInfo parameter)
        {
            var argType = typeof(TArg);
            var isObj = (Type.GetTypeCode(argType) == TypeCode.Object);

            if (!isObj)
            {
                yield return parameter.Name;
            }
            else
            {
                foreach (var prop in argType.GetTypeInf().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    yield return prop.Name;
                }
            }
        }
    }
}