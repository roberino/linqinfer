using LinqInfer.Data;
using System.Collections.Generic;
using System.IO;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureExtractor<TInput, TVector> : IBinaryPersistable where TVector : struct
    {
        /// <summary>
        /// The size of the feature vector.
        /// </summary>
        int VectorSize { get; }
        
        /// <summary>
        /// Creates a vector from a sample which will be used to normalise data.
        /// </summary>
        TVector[] CreateNormalisingVector(TInput sample = default(TInput));
        
        /// <summary>
        /// Normalises an extracted vector using an enumeration of samples.
        /// </summary>
        TVector[] NormaliseUsing(IEnumerable<TInput> samples);

        /// <summary>
        /// Extracts an array of primitive values which can be used
        /// as a quantitative representation of an object.
        /// </summary>
        TVector[] ExtractVector(TInput obj);

        /// <summary>
        /// Returns a key to index dictionary for remapping labels back to raw extracted data.
        /// </summary>
        IDictionary<string, int> IndexLookup { get; }

        /// <summary>
        /// Returns an enumeration of feature metadata.
        /// </summary>
        IEnumerable<IFeature> FeatureMetadata { get; }
    }
}