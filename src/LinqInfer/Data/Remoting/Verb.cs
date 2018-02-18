using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum Verb : byte
    {
        Default = 0,

        /// <summary>
        /// Creates a new resource
        /// </summary>
        Put = 1,

        /// <summary>
        /// Gets a resource
        /// </summary>
        Get = 2,

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

        /// <summary>
        /// Updates part of a resource
        /// </summary>
        Patch = 32,

        All = Put | Post | Get | Delete | Options | Patch
    }
}