namespace LinqInfer.Learning
{
    public interface IAssistedLearning<TClass, TVector>
    {
        double Train(TClass item, TVector[] sample);
    }
}
