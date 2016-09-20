using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum Verb : byte
    {
        Default = 0,
        Create = 1,
        Get = 2,
        Delete = 4,
        All = Create | Get | Delete
    }
}