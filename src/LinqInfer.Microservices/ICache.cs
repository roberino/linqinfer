namespace LinqInfer.Microservices
{
    public interface ICache
    {
        T Get<T>(object key);
        void Set<T>(object key, T item);
    }
}
