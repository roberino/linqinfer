namespace LinqInfer.Learning
{
    public interface IAssistedLearning<TClass, TVector>
    {
        void Train(TClass item, TVector[] sample);
    }
}
