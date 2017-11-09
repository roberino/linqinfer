using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace LinqInfer.Learning
{
    public static class AsyncPipelineExtensions
    {
        /// <summary>
        /// Creates a training set from an async feature pipeline
        /// </summary>
        /// <typeparam name="TInput">The input type</typeparam>
        /// <typeparam name="TClass">The classification type</typeparam>
        /// <param name="pipeline">A feature pipeline</param>
        /// <param name="classf">A classifying expression</param>
        /// <param name="outputMapper">An output mapper</param>
        /// <returns>A training set</returns>
        public static IAsyncTrainingSet<TInput, TClass> AsTrainingSet<TInput, TClass>(
            this IAsyncFeatureProcessingPipeline<TInput> pipeline, 
            Expression<Func<TInput, TClass>> classf,
            ICategoricalOutputMapper<TClass> outputMapper)
            where TInput : class
            where TClass : IEquatable<TClass>
        {
            return new AsyncTrainingSet<TInput, TClass>(pipeline, classf, outputMapper);
        }
    }
}
