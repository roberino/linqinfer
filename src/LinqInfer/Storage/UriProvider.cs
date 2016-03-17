using System;

namespace LinqInfer.Storage
{
    public class UriProvider : IUriProvider
    {
        public Uri Create(string type, string path)
        {
            return new Uri("storage://data/" + type + '/' + path);
        }
    }
}
