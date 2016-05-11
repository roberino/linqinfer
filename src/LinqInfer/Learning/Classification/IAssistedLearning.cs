namespace LinqInfer.Learning.Classification
{
    public interface IAssistedLearning<TClass, TVector>
    {
        double Train(TClass item, TVector[] sample);
    }
}
