using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum Verb : byte
    {
        Default = 0,

        /// <summary>
        /// Creates a new resource (equiv to PUT)
        /// </summary>
        Create = 1,

        /// <summary>
        /// Creates a new resource
        /// </summary>
        Put = 1,

        /// <summary>
        /// Gets a resource
        /// </summary>
        Get = 2,

        /// <summary>
        /// Updates a resource (equiv to POST)
        /// </summary>
        Update = 4,

        /// <summary>
        /// Posts data
        /// </summary>
        Post = 4,

        /// <summary>
        /// Deletes a resource
        /// </summary>
        Delete = 8,

        /// <summary>
        /// Requests options
        /// </summary>
        Options = 16,

        All = Create | Update | Get | Delete | Options
    }
}