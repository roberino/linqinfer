﻿using LinqInfer.Learning.Features;
using System;
using System.Collections.Generic;
using System.Text;
using LinqInfer.Data.Pipes;
using System.Threading.Tasks;
using System.Threading;
using LinqInfer.Data;
using LinqInfer.Text.Indexing;

namespace LinqInfer.Text.VectorExtraction
{
    class TextFeatureExtractionStrategy<T> : FeatureExtractionStrategy<T>
        where T : class
    {
        readonly int _maxVectorSize;
        readonly ITokeniser _tokeniser;

        public TextFeatureExtractionStrategy(int maxVectorSize = 128, ITokeniser tokeniser = null)
        {
            _maxVectorSize = maxVectorSize;
            _tokeniser = tokeniser;
        }

        public override IAsyncBuilderSink<T, IVectorFeatureExtractor<T>> CreateBuilder()
        {
            return new Builder(Properties, _maxVectorSize, _tokeniser);
        }

        class Builder : IAsyncBuilderSink<T, IVectorFeatureExtractor<T>>
        {
            readonly int _maxVectorSize;
            readonly DocumentIndex _index;
            readonly IList<PropertyExtractor<T>> _properties;
            int _counter;

            public Builder(IList<PropertyExtractor<T>> properties, int maxVectorSize = 128, ITokeniser tokeniser = null)
            {
                _maxVectorSize = maxVectorSize;
                _properties = properties;
                _index = new DocumentIndex(tokeniser);
            }

            public bool CanReceive => true;

            public Task<IVectorFeatureExtractor<T>> BuildAsync()
            {
                var f = new Func<T, IEnumerable<IToken>>(x => _index.Tokeniser.Tokenise(GetText(x)));

                var fe = _index.CreateVectorExtractor(f, _maxVectorSize);

                return Task.FromResult(fe);
            }

            public Task ReceiveAsync(IBatch<T> dataBatch, CancellationToken cancellationToken)
            {
                _counter++;

                foreach (var item in dataBatch.Items)
                {
                    _index.IndexText(GetText(item), $"{dataBatch.BatchNumber}.{_counter}");
                }

                return Task.FromResult(true);
            }

            string GetText(T item)
            {
                var sb = new StringBuilder();

                foreach (var p in _properties)
                {
                    if (p.GetValue(item) is string text)
                    {
                        sb.AppendLine(text);
                    }
                }

                return sb.ToString();
            }
        }

        public override bool CanHandle(PropertyExtractor<T> propertyExtractor)
        {
            return propertyExtractor.Property.PropertyType == typeof(string) && propertyExtractor.FeatureMetadata.Model == FeatureVectorModel.Semantic;
        }
    }
}