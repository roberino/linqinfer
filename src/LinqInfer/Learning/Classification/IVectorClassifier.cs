using LinqInfer.Maths;

namespace LinqInfer.Learning.Classification
{
    public interface IVectorClassifier
    {
        IVector Evaluate(IVector inputVector);
    }
}
