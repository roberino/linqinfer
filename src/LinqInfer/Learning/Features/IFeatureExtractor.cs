using LinqInfer.Data;
using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractor<TInput, TVector> : IHasFeatureMetadata, IBinaryPersistable where TVector : struct
    {
        /// <summary>
        /// The size of the feature vector.
        /// </summary>
        int VectorSize { get; }
        
        /// <summary>
        /// Returns true if a feature extractor normalises the data post extraction.
        /// </summary>
        bool IsNormalising { get; }
        
        /// <summary>
        /// Normalises an extracted vector using an enumeration of samples.
        /// </summary>
        TVector[] NormaliseUsing(IEnumerable<TInput> samples);

        /// <summary>
        /// Extracts an array of primitive values which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        TVector[] ExtractVector(TInput obj);
    }
}