namespace LinqInfer.Data.Remoting
{
    /// <summary>
    /// Represents a lightweight API
    /// for exposing functions over HTTP
    /// </summary>
    public interface IHttpApi : IHttpApiBuilder, IOwinApplication
    {
    }
}