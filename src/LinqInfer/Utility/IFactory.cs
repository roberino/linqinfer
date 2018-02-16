namespace LinqInfer.Utility
{
    public interface IFactory<TResult, TArgs>
    {
        TResult Create(TArgs parameters);
    }
}