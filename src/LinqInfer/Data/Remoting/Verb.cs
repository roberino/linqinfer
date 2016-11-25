using System;

namespace LinqInfer.Data.Remoting
{
    [Flags]
    public enum Verb : byte
    {
        Default = 0,
        Create = 1,
        Get = 2,
        Update = 4,
        Delete = 8,
        Options = 16,
        All = Create | Update | Get | Delete | Options
    }
}