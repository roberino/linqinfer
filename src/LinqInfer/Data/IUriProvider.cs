using System;

namespace LinqInfer.Data
{
    /// <summary>
    /// Provides a resource URI for a type and path
    /// </summary>
    public interface IUriProvider
    {
        /// <summary>
        /// Creates a new URL for a type and path
        /// </summary>
        Uri Create(string type, string path);
    }
}
