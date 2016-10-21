using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    internal class HttpApi : HttpApplicationHost
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
            
            await Process(localContext);

            var response = localContext.Response.GetSendStream();

            return await _serialiser.Deserialise<T>(response, localContext.Response.Header.TextEncoding);
        }

        public RouteBinder Bind(string routeTemplate, Verb verb = Verb.Get)
        {
            return new RouteBinder(_baseEndpoint.CreateRoute(routeTemplate, verb), _routes, _binder);
        }

        public void ExportMethod<TArg, TResult>(Func<TArg, Task<TResult>> func, TArg defaultValue)
        {
            var parameters = GetParameters<TArg>(func.Method.GetParameters().First());
            var route = BaseEndpoint.CreateRoute("/" + func.Method.Name + "/" + string.Join("/", parameters.ToArray()), Verb.Get);
            _routes.AddRoute(route, _binder.BindToAsyncMethod(func, defaultValue));
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