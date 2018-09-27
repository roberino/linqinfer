using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;

namespace LinqInfer.Learning.Classification
{
    class GenericClassifier<TInput, TOutput> : IObjectClassifier<TOutput, TInput>
        where TOutput : IEquatable<TOutput>
    {
        readonly IFloatingPointFeatureExtractor<TInput> _featureExtractor;
        readonly ICategoricalOutputMapper<TOutput> _outputMapper;
        readonly Func<IVector, IVector> _classifyingFunc;

        public GenericClassifier(
            IFloatingPointFeatureExtractor<TInput> featureExtractor,
            ICategoricalOutputMapper<TOutput> outputMapper,
            Func<IVector, IVector> classifyingFunc
            )
        {
            _featureExtractor = featureExtractor;
            _outputMapper = outputMapper;
            _classifyingFunc = classifyingFunc;
        }

        public IEnumerable<ClassifyResult<TOutput>> Classify(TInput input)
        {
            var inputVector = _featureExtractor.ExtractIVector(input);
            var outputVector = _classifyingFunc(inputVector);

            return _outputMapper.Map(outputVector);
        }
    }
}