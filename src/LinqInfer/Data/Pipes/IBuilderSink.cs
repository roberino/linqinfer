namespace LinqInfer.Data.Pipes
{
    public interface IBuilderSink<T, O> : IAsyncSink<T>
    {
        O Output { get; }
    }
}