using LinqInfer.Data;
using LinqInfer.Data.Pipes;
using LinqInfer.Utility;
using System.Threading;
using System.Threading.Tasks;
using LinqInfer.Text.Indexing;

namespace LinqInfer.Text.Http
{
    class IndexSink : IBuilderSink<HttpDocument, IDocumentIndex>
    {
        readonly DocumentIndex _index;

        public IndexSink(int maxCapacity = 1000)
        {
            ArgAssert.AssertGreaterThanZero(maxCapacity, nameof(maxCapacity));

            MaxCapacity = maxCapacity;

            _index = new DocumentIndex();
        }

        public int MaxCapacity { get; }

        public bool CanReceive => _index.DocumentCount < MaxCapacity;

        public IDocumentIndex Output => _index;

        public Task ReceiveAsync(IBatch<HttpDocument> dataBatch, CancellationToken cancellationToken)
        {
            _index.IndexDocuments(dataBatch.Items);

            return Task.FromResult(true);
        }
    }
}