using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    /// <summary>
    /// Represents a classifying algorithm which can classify an object instance and return a set of potential matches.
    /// </summary>
    /// <typeparam name="TClass">The class type</typeparam>
    /// <typeparam name="TInput">The input type</typeparam>
    public interface IObjectClassifier<TClass, TInput>
    {
        /// <summary>
        /// Returns an enumeration of classify results for an input.
        /// </summary>
        IEnumerable<ClassifyResult<TClass>> Classify(TInput input);
    }
}
