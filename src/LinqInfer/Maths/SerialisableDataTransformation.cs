using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    public sealed class SerialisableDataTransformation : ISerialisableDataTransformation, IEquatable<SerialisableDataTransformation>
    {
        private readonly IList<DataOperation> _operations;

        public SerialisableDataTransformation()
        {
            _operations = new List<DataOperation>();
        }

        public SerialisableDataTransformation(Matrix transformer, Vector transposer = null) : this()
        {
            Contract.Ensures(transformer != null);

            if (transposer != null)
            {
                _operations.Add(new DataOperation(VectorOperationType.Subtract, transposer));
            }

            _operations.Add(new DataOperation(VectorOperationType.MatrixMultiply, transformer));
        }

        public SerialisableDataTransformation(IEnumerable<DataOperation> operations)
        {
            _operations = operations.ToList();
        }

        public SerialisableDataTransformation(params DataOperation[] operations) : this()
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

        public static SerialisableDataTransformation LoadFromDocument(PortableDataDocument doc)
        {
            var transform = new SerialisableDataTransformation();

            transform.ImportData(doc);

            return transform;
        }

        public void ImportData(PortableDataDocument doc)
        {
            _operations.Clear();

            foreach(var child in doc.Children)
            {
                _operations.Add(new DataOperation(child));
            }
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            foreach (var op in _operations)
            {
                doc.Children.Add(op.ExportData());
            }

            return doc;
        }

        public bool Equals(SerialisableDataTransformation other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (!(InputSize == other.InputSize && OutputSize == other.OutputSize)) return false;

            if (_operations.Count != other._operations.Count) return false;

            return _operations.Zip(other._operations, (x, y) => x.Equals(y)).All(x => x);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SerialisableDataTransformation);
        }

        public override int GetHashCode()
        {
            return string.Join("/", _operations.Select(o => o.GetHashCode().ToString())).GetHashCode();
        }
    }
}