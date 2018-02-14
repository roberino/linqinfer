namespace LinqInfer.Utility
{
    public interface IFactory<T>
    {
        T Create(string parameters);
    }
}