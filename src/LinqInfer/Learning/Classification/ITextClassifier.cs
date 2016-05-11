namespace LinqInfer.Learning.Classification
{
    public interface ITextClassifier<T>
    {
        ClassifyResult<T> Classify(string corpus);
    }
}