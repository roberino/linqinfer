using LinqInfer.Data;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class SerialisableVectorTransformation : IExportableAsVectorDocument, IImportableAsVectorDocument, IVectorTransformation
    {
        private readonly IList<VectorOperation> _operations;

        public SerialisableVectorTransformation()
        {
            _operations = new List<VectorOperation>();
        }

        public SerialisableVectorTransformation(Matrix transformer, Vector transposer = null) : this()
        {
            Contract.Ensures(transformer != null);

            if (transposer != null)
            {
                _operations.Add(new VectorOperation(VectorOperationType.Subtract, transposer));
            }

            _operations.Add(new VectorOperation(VectorOperationType.MatrixMultiply, transformer));
        }

        public SerialisableVectorTransformation(IEnumerable<VectorOperation> operations)
        {
            _operations = operations.ToList();
        }

        public SerialisableVectorTransformation(params VectorOperation[] operations) : this()
        {
            foreach (var operation in operations) _operations.Add(operation);
        }

        public IVector Apply(IVector vector)
        {
            if (vector == null) return null;

            IVector result = vector;

            foreach(var op in _operations)
            {
                result = op.Apply(result);
            }

            return result;
        }

        public int InputSize => _operations.First().InputSize;

        public int OutputSize => _operations.Last().OutputSize;

        public static SerialisableVectorTransformation LoadFromDocument(BinaryVectorDocument doc)
        {
            var transform = new SerialisableVectorTransformation();

            transform.FromVectorDocument(doc);

            return transform;
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            _operations.Clear();

            foreach(var child in doc.Children)
            {
                _operations.Add(new VectorOperation(child));
            }
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            foreach (var op in _operations)
            {
                doc.Children.Add(op.ToVectorDocument());
            }

            return doc;
        }
    }
}