using System;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    public sealed class RouteBinder
    {
        private readonly RoutingHandler _routes;
        private readonly FunctionBinder _binder;

        internal RouteBinder(IUriRoute route, RoutingHandler handler, FunctionBinder binder)
        {
            _routes = handler;
            _binder = binder;
            Route = route;
        }

        /// <summary>
        /// Returns the route
        /// </summary>
        public IUriRoute Route { get; private set; }

        /// <summary>
        /// Creates a new binder for a specific method
        /// </summary>
        public RouteBinder UsingMethod(Verb method)
        {
            return new RouteBinder(new UriRoute(Route.BaseUri, Route.Template, method), _routes, _binder);
        }

        public void ToMany(Action<RouteBinder> bindAction)
        {
            bindAction(this);
        }

        /// <summary>
        /// Binds the route to an async function
        /// </summary>
        /// <typeparam name="TArg">The argument type</typeparam>
        /// <typeparam name="TResult">The return type</typeparam>
        /// <param name="func">A function to call</param>
        public void To<TArg, TResult>(Func<TArg, Task<TResult>> func)
        {
            _routes.AddRoute(Route, _binder.BindToAsyncMethod(func));
        }

        /// <summary>
        /// Binds the route to a syncronous function
        /// </summary>
        /// <typeparam name="TArg">The argument type</typeparam>
        /// <typeparam name="TResult">The return type</typeparam>
        /// <param name="func">A function to call</param>
        public void ToSyncronousMethod<TArg, TResult>(Func<TArg, TResult> func)
        {
            _routes.AddRoute(Route, _binder.BindToSyncMethod(func));
        }

        /// <summary>
        /// Binds the route to an async function with default values
        /// </summary>
        /// <typeparam name="TArg">The argument type</typeparam>
        /// <typeparam name="TResult">The return type</typeparam>
        /// <param name="defaultValue">The default value for when values are not supplied within the route</param>
        /// <param name="func">A function to call</param>
        public void To<TArg, TResult>(TArg defaultValue, Func<TArg, Task<TResult>> func)
        {
            _routes.AddRoute(Route, _binder.BindToAsyncMethod(func, defaultValue));
        }
    }
}