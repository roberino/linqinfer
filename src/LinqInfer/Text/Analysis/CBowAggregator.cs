using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Analysis
{
    internal sealed class CBowAggregator
    {
        private readonly OneHotEncoding<string> _encoding;

        public CBowAggregator(
            IAsyncEnumerator<SyntacticContext> cbow,
            OneHotEncoding<string> encoding)
        {
            Cbow = cbow;
            _encoding = encoding;
        }

        public CBowAggregator(
            IAsyncEnumerator<SyntacticContext> cbow,
            ISemanticSet vocabulary)
        {
            Cbow = cbow;

            _encoding = new OneHotEncoding<string>(new HashSet<string>(vocabulary.Words));
        }

        public IAsyncEnumerator<SyntacticContext> Cbow { get; }

        public async Task<IAsyncTrainingSet<WordVector, string>> GetTrainingSetAync(CancellationToken cancellationToken)
        {
            var aggregation = Cbow
                .CreatePipe()
                .AttachAggregator(c => new KeyValuePair<string, uint[]>(c.TargetWord.Text.ToLowerInvariant(), Extract(c)), Aggregate);

            await aggregation.Pipe.RunAsync(cancellationToken);

            var data = From.Enumerable(aggregation.Output).TransformEachItem(kv => new WordVector(kv.Key, CreateVector(kv.Value)));

            return data.CreatePipeline(w => w.Vector, _encoding.VectorSize)
                 .AsTrainingSet(w => w.Word, aggregation.Output.Keys.ToArray());
        }

        private IVector CreateVector(uint[] values)
        {
            var dvalues = new double[values.Length];

            Array.Copy(values, dvalues, values.Length);

            return new Vector(dvalues);
        }

        private uint[] Extract(SyntacticContext context)
        {
            var total = new uint[_encoding.VectorSize];

            var vector = _encoding.Encode(context.ContextualWords.Select(x => x.Text.ToLowerInvariant()));

            return vector.ToUnsignedIntegerArray();
        }

        private uint[] Aggregate(uint[] v1, uint[] v2)
        {
            for (var i = 0; i < v1.Length; i++)
            {
                v1[i] += v2[i];
            }

            return v1;
        }
    }
}