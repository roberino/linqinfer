using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApi : HttpApplicationHost, IHttpApi
    {
        private readonly RoutingHandler _routes;
        private readonly FunctionBinder _binder;
        private readonly IObjectSerialiser _serialiser;

        public HttpApi(IObjectSerialiser serialiser, int port, string host = "localhost") : base(null, port, host)
        {
            _serialiser = serialiser;
            _routes = new RoutingHandler();
            _binder = new FunctionBinder(serialiser);

            AddComponent(_routes.CreateApplicationDelegate());
            AddErrorHandler(StandardErrorHandler);
        }

        public Task<T> TestRoute<T>(Uri uri, T sampleResult, IDictionary<string, string[]> headers = null)
        {
            return TestRoute<T>(uri, headers);
        }

        public async Task<T> TestRoute<T>(Uri uri, IDictionary<string, string[]> headers = null)
        {
            TcpRequest request;

            using (var writer = new StringWriter())
            {
                using (var headerFormatter = new HttpHeaderFormatter(writer, true))
                {
                    headerFormatter.WriteRequestAndProtocol("GET", uri.PathAndQuery);

                    if (headers == null) headers = new Dictionary<string, string[]>();

                    headers["Host"] = new[] { BaseEndpoint.Host + (BaseEndpoint.Port != 80 ? ":" + BaseEndpoint.Port : "") };

                    headerFormatter.WriteHeaders(headers);

                    headerFormatter.WriteEnd();
                }

                var headerBytes = Encoding.ASCII.GetBytes(writer.ToString());

                request = new TcpRequest(new TcpRequestHeader(headerBytes), Stream.Null);
            }

            var localContext = new OwinContext(request, new TcpResponse(TransportProtocol.Http), BaseEndpoint);

            localContext["ext.ExpectedResponseType"] = typeof(T);
            
            await Process(localContext);

            if (localContext.Response.Header.StatusCode.GetValueOrDefault(0) != 200)
            {
                throw new HttpException(localContext.Response.Header.StatusCode.GetValueOrDefault(0), localContext.Response.Header.StatusText ?? "HTTP Error");
            }

            var response = localContext.Response.GetSendStream();

            return await _serialiser.Deserialise<T>(response, localContext.Response.Header.TextEncoding);
        }

        public RouteBinder Bind(string routeTemplate, Verb verb = Verb.Get)
        {
            return new RouteBinder(_baseEndpoint.CreateRoute(routeTemplate, verb), _routes, _binder);
        }

        public IUriRoute ExportAsyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, Task<TResult>> func, string name = null)
        {
            var parameters = GetParameters<TArg>(func.Method.GetParameters().First());
            var route = BaseEndpoint.CreateRoute("/" + GetPathForName(name ?? func.Method.Name) + "/" + string.Join("/", parameters.ToArray()), Verb.Get);
            _routes.AddRoute(route, _binder.BindToAsyncMethod(func, defaultValue));
            return route;
        }

        public IUriRoute ExportSyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, TResult> func, string name = null)
        {
            var parameters = GetParameters<TArg>(func.Method.GetParameters().First());
            var route = BaseEndpoint.CreateRoute("/" + GetPathForName(name ?? func.Method.Name) + "/" + string.Join("/", parameters.Select(p => '{' + p + '}').ToArray()), Verb.Get);
            _routes.AddRoute(route, _binder.BindToSyncMethod(func, defaultValue));
            return route;
        }

        protected async override Task<bool> Process(IOwinContext context)
        {
            var res = await base.Process(context);

            if (!context.Response.Header.StatusCode.HasValue && !context.Response.HasContent)
            {
                context.Response.CreateStatusResponse(404);
            }

            return res;
        }

        private Task<bool> StandardErrorHandler(IOwinContext context, Exception ex)
        {
            if (ex is ArgumentException && context.Response.Header.StatusCode.HasValue)
            {
                context.Response.CreateStatusResponse(400);
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
                foreach (var prop in argType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    yield return prop.Name;
                }
            }
        }
    }
}