using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractor<TInput, TVector> where TVector : struct
    {
        int VectorSize { get; }
        TVector[] CreateNormalisingVector(TInput sample = default(TInput));
        TVector[] ExtractVector(TInput obj);
        IDictionary<string, int> Labels { get; }
    }
}
