using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public class RouteBinder
    {
        private readonly RoutingHandler _routes;
        private readonly FunctionBinder _binder;

        internal RouteBinder(IUriRoute route, RoutingHandler handler, FunctionBinder binder)
        {
            _routes = handler;
            _binder = binder;
            Route = route;
        }

        public IUriRoute Route { get; private set; }

        public void To<TArg, TResult>(Func<TArg, Task<TResult>> func)
        {
            _routes.AddRoute(Route, _binder.BindToAsyncMethod(func));
        }

        public void To<TArg, TResult>(TArg defaultValue, Func<TArg, Task<TResult>> func)
        {
            _routes.AddRoute(Route, _binder.BindToAsyncMethod(func, defaultValue));
        }
    }
}