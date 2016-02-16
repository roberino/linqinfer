namespace LinqInfer.Learning
{
    public interface INeuronGenerator<T>
    {
        INeuron<T> Create();
    }
}
