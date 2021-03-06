﻿using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using LinqInfer.Maths.Probability;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    class CorpusAnalyser
    {
        readonly int _vectorSize = 1024;

        readonly DocumentIndex _index;
        readonly DiscreteMarkovChain<string> _markovChain;
        readonly Matrix _documentTermMatrix;

        IFloatingPointFeatureExtractor<IEnumerable<IToken>> _featureExtractor;

        public CorpusAnalyser(IEnumerable<TokenisedTextDocument> samples)
        {
            _index = new DocumentIndex();
            _markovChain = new DiscreteMarkovChain<string>();

            _documentTermMatrix = Analyse(samples);
        }

        public CorpusAnalyser(IEnumerable<string> samples)
        {
            _index = new DocumentIndex();
            _markovChain = new DiscreteMarkovChain<string>();

            _documentTermMatrix = Analyse(samples.Select(s => new TokenisedTextDocument(Guid.NewGuid().ToString(), _index.Tokeniser.Tokenise(s))));
        }

        public Matrix DocumentTermMatrix
        {
            get { return _documentTermMatrix; }
        }

        public IEnumerable<IFeature> Terms
        {
            get
            {
                return _featureExtractor.FeatureMetadata;
            }
        }

        public IDictionary<IFeature, IDictionary<IFeature, double>> FindCorrelations(double minCovariance = 0)
        {
            var results = new Dictionary<IFeature, IDictionary<IFeature, double>>();

            int index = 0;

            foreach (var row in _documentTermMatrix.CovarianceMatrix.Rows)
            {
                var term = Terms.Single(t => t.Index == index);

                results[term] = row
                    .ToColumnVector()
                    .IndexedValues
                    .Where(v => v.Value >= minCovariance)
                    .ToDictionary(v => Terms.Single(t => t.Index == v.Key), v => v.Value);

                index++;
            }

            return results;
        }

        public Matrix DocumentTermCovarianceMatrix
        {
            get
            {
                return _documentTermMatrix.CovarianceMatrix;
            }
        }

        Matrix Analyse(IEnumerable<TokenisedTextDocument> samples)
        {
            foreach (var sample in samples) Append(sample);

            _featureExtractor = _index.CreateVectorExtractor(_vectorSize, false);

            return new Matrix(samples.Select(s => _featureExtractor.ExtractIVector(s.Tokens).ToColumnVector()));
        }

        void Append(TokenisedTextDocument document)
        {
            _index.IndexDocument(document);

            var corpus = new Corpus(document.Tokens);

            foreach (var block in corpus.Blocks)
            {
                _markovChain.AnalyseSequence(block.Select(b => b.Text));
            }
        }
    }
}