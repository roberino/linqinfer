using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents a system that can classify an vector, returning a class when given a sample vector
    /// </summary>
    /// <typeparam name="TClass">The type of class</typeparam>
    /// <typeparam name="TVector">The vector representation of the sample</typeparam>
    public interface IClassifier<TClass, TVector> : IObjectClassifier<TClass, TVector[]>
    {
        /// <summary>
        /// Returns a best match classification guess, given a vector
        /// </summary>
        ClassifyResult<TClass> ClassifyAsBestMatch(TVector[] vector);
    }
}
