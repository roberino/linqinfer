using System;

namespace LinqInfer.Storage
{
    public interface IUriProvider
    {
        Uri Create(string type, string path);
    }
}
