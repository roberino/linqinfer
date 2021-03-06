﻿using System.Collections.Generic;
using LinqInfer.Maths;

namespace LinqInfer.Learning.Features
{
    public interface IFeatureDataSource
    {
        /// <summary>
        /// Returns the count of all the data
        /// </summary>
        int SampleCount { get; }

        /// <summary>
        /// Returns the vector size of the feature vector.
        /// </summary>
        int VectorSize { get; }

        /// <summary>
        /// Returns an enumeration of feature metadata.
        /// </summary>
        IEnumerable<IFeature> FeatureMetadata { get; }

        /// <summary>
        /// Returns an enumeration of extracted vector data.
        /// </summary>
        /// <returns></returns>
        IEnumerable<IVector> ExtractVectors();
    }
}