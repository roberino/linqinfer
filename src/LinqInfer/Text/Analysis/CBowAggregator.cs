﻿using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Classification.NeuralNetworks;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    sealed class CBowAggregator
    {
        readonly OneHotEncoding<string> _encoding;

        public CBowAggregator(
            ITransformingAsyncBatchSource<SyntacticContext> cbow,
            OneHotEncoding<string> encoding)
        {
            Cbow = cbow;
            _encoding = encoding;
        }

        public CBowAggregator(
            ITransformingAsyncBatchSource<SyntacticContext> cbow,
            ISemanticSet vocabulary)
        {
            Cbow = cbow;

            _encoding = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
        }

        public ITransformingAsyncBatchSource<SyntacticContext> Cbow { get; }

        public async Task<IDictionary<string, WordData>> AggregateVectorsAync(CancellationToken cancellationToken)
        {
            var aggregation = Cbow
                .CreatePipe()
                .AttachAggregator(c => new KeyValuePair<string, WordVectAggregation>(c.TargetWord.Text.ToLowerInvariant(), Extract(c)), Aggregate);

            await aggregation.Pipe.RunAsync(cancellationToken);

            var results = aggregation.Output.Select(kv => new WordData(kv.Key, kv.Value.Count, CreateVector(kv.Value))).ToDictionary(v => v.Word);

            return results;
        }

        public async Task<IAsyncTrainingSet<WordData, string>> GetTrainingSetAync(CancellationToken cancellationToken)
        {
            var aggregation = await AggregateVectorsAync(cancellationToken);

            var data = From.Enumerable(aggregation.Values);

            var pipeline = data.CreatePipeline(w =>
                  w.Vector.Size == 0 ? _encoding.Encode(w.Word) : w.Vector,
                _encoding.VectorSize);

            pipeline = await pipeline.CentreAndScaleAsync(Maths.Range.MinusOneToOne);

            return pipeline
                 .AsTrainingSet(w => w.Word, aggregation.Keys.ToArray());
        }

        public async Task<IDictionary<string, IVector>> ExtractVectorsAsync(CancellationToken cancellationToken, int vectorSize)
        {
            var trainingSet = await GetTrainingSetAync(cancellationToken);

            void NetworkBuilder(IConvolutionalNetworkBuilder b)
            {
                b
               .ConfigureLearningParameters(p =>
               {
                   p.LearningRate = 0.2;
                   p.Momentum = 0.1;
               })
               .AddHiddenLayer(vectorSize, Activators.None())
               .AddSoftmaxOutput();
            };

            var classifier = trainingSet.AttachMultilayerNetworkClassifier(NetworkBuilder);

            await trainingSet.RunAsync(cancellationToken);

            var doc = classifier.ExportData();

            var mln = doc.GetChildDoc<MultilayerNetwork>();

            return trainingSet
                  .OutputMapper
                  .FeatureMetadata
                  .Zip(mln.Children.Last().Vectors, (f, v) => new { f, v })
                  .ToDictionary(x => x.f.Label, v => v.v);
        }

        IVector CreateVector(WordVectAggregation aggregate)
        {
            var dvalues = new double[aggregate.Vector.Length];

            Array.Copy(aggregate.Vector, dvalues, aggregate.Vector.Length);

            var v = new Vector(dvalues);

            //v.Apply(x => x / aggregate.Count);

            return v;
        }

        WordVectAggregation Extract(SyntacticContext context)
        {
            var total = new uint[_encoding.VectorSize];

            var vector = _encoding.Encode(context.ContextualWords.Select(x => x.Text.ToLowerInvariant()));

            return new WordVectAggregation() { Vector = vector.ToUnsignedIntegerArray(), Count = 1 };
        }

        WordVectAggregation Aggregate(WordVectAggregation v1, WordVectAggregation v2)
        {
            for (var i = 0; i < v1.Vector.Length; i++)
            {
                v1.Vector[i] += v2.Vector[i];
            }

            v1.Count += v2.Count;

            return v1;
        }

        class WordVectAggregation
        {
            public uint[] Vector { get; set; }
            public long Count { get; set; }
        }
    }
}