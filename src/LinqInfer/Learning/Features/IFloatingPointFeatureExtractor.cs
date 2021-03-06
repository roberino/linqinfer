﻿using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    /// <summary>
    /// Interface for extracting features from an object type as an array of 
    /// single precision floating point numbers.
    /// </summary>
    public interface IFloatingPointFeatureExtractor<T> : IVectorExtractor<T>, IFeatureExtractor<T, double>, IExportableAsDataDocument
    {
    }
}
