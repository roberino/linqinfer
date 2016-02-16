namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractor<TInput, TVector> where TVector : struct
    {
        int VectorSize { get; }
        TVector[] CreateNormalisingVector(TInput sample = default(TInput));
        TVector[] ExtractVector(TInput obj);
    }
}
