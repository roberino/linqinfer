using System;

namespace LinqInfer.Data.Remoting
{
    public interface IUriRoute
    {
        Uri BaseUri { get; }
        IUriRouteMapper Mapper { get; }
        string Template { get; }
        Verb Verbs { get; }
    }
}