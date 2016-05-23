using System;

namespace LinqInfer.Data
{
    public interface IUriProvider
    {
        Uri Create(string type, string path);
    }
}
