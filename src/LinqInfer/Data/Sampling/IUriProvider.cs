using System;

namespace LinqInfer.Data.Sampling
{
    public interface IUriProvider
    {
        Uri Create(string type, string path);
    }
}
