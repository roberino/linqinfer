namespace LinqInfer.Learning
{
    public interface IClassifier<TClass, TVector>
    {
        ClassifyResult<TClass> Classify(TVector[] vector);
    }
}
