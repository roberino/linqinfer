using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Remoting
{
    /// <summary>
    /// Represents a lightweight API
    /// for exposing functions over HTTP
    /// </summary>
    public interface IHttpApi
    {
        /// <summary>
        /// Returns a binder for binding a function to the route template
        /// </summary>
        RouteBinder Bind(string routeTemplate, Verb verb = Verb.Get);

        /// <summary>
        /// Exports an async method
        /// </summary>
        IUriRoute ExportAsyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, Task<TResult>> func, string name = null);

        /// <summary>
        /// Exports an syncronous method
        /// </summary>
        IUriRoute ExportSyncMethod<TArg, TResult>(TArg defaultValue, Func<TArg, TResult> func, string name = null);

        /// <summary>
        /// Tests a route
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="uri">The uri to test</param>
        /// <param name="headers">Optional additional headers</param>
        /// <returns>The method result</returns>
        Task<T> TestRoute<T>(Uri uri, IDictionary<string, string[]> headers = null);

        /// <summary>
        /// Tests a route
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="uri">The uri to test</param>
        /// <param name="sampleResult">A sample result (useful when using anonymous types)</param>
        /// <param name="headers">Optional additional headers</param>
        /// <returns>The method result</returns>
        Task<T> TestRoute<T>(Uri uri, T sampleResult, IDictionary<string, string[]> headers = null);
    }
}