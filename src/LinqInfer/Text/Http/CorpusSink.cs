﻿using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Text.Analysis;
using LinqInfer.Utility;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Text.Http
{
    class CorpusSink : IBuilderSink<HttpDocument, ICorpus>
    {
        readonly Corpus _corpus;
        int _received;

        public CorpusSink(int maxCapacity = 1000)
        {
            ArgAssert.AssertGreaterThanZero(maxCapacity, nameof(maxCapacity));

            MaxCapacity = maxCapacity;

            _corpus = new Corpus();
        }

        public int MaxCapacity { get; }

        public bool CanReceive => _received < MaxCapacity;

        public ICorpus Output => _corpus;

        public Task ReceiveAsync(IBatch<HttpDocument> dataBatch, CancellationToken cancellationToken)
        {
            foreach (var doc in dataBatch.Items)
            {
                foreach (var token in doc.Tokens) _corpus.Append(token);
                Interlocked.Increment(ref _received);
            }

            return Task.FromResult(true);
        }
    }
}