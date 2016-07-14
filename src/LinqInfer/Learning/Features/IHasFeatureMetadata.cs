using System.Collections.Generic;

namespace LinqInfer.Learning.Features
{
    public interface IHasFeatureMetadata
    {
        /// <summary>
        /// Returns an enumeration of feature metadata.
        /// </summary>
        IEnumerable<IFeature> FeatureMetadata { get; }
    }
}
