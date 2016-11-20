namespace LinqInfer.Data.Remoting
{
    public enum ServerStatus
    {
        Unknown,
        Connecting,
        Waiting,
        Running,
        ShuttingDown,
        Stopped,
        Error,
        Broken,
        Disposed
    }
}