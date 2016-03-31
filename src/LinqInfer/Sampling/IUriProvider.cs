using System;

namespace LinqInfer.Sampling
{
    public interface IUriProvider
    {
        Uri Create(string type, string path);
    }
}
