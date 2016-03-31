using System;

namespace LinqInfer.Sampling
{
    public class UriProvider : IUriProvider
    {
        public Uri Create(string type, string path)
        {
            return new Uri("storage://data/" + type + '/' + path);
        }
    }
}
