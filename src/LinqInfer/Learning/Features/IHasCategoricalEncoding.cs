namespace LinqInfer.Learning.Features
{
    public interface IHasCategoricalEncoding<TCategory>
    {
        IOneHotEncoding<TCategory> Encoder { get; }
    }
}