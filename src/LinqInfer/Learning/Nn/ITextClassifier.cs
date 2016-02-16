namespace LinqInfer.Learning
{
    public interface ITextClassifier<T>
    {
        ClassifyResult<T> Classify(string corpus);
    }
}