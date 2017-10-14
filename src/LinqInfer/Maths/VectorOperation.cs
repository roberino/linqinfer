using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class VectorOperation : IExportableAsVectorDocument, IImportableAsVectorDocument
    {
        private readonly IList<ColumnVector1D> _parameters;

        public VectorOperation()
        {
            _parameters = new List<ColumnVector1D>();
        }

        public VectorOperation(BinaryVectorDocument data) : this()
        {
            FromVectorDocument(data);
        }

        public VectorOperation(VectorOperationType type, IEnumerable<Vector> parameters) : this()
        {
            foreach (var para in parameters) _parameters.Add(new ColumnVector1D(para));
        }

        public VectorOperation(VectorOperationType type, Matrix transformation) : this(type, transformation.Rows)
        {
            Operation = type;
        }

        public VectorOperation(VectorOperationType type, params Vector[] parameters) : this(type, (IEnumerable<Vector>)parameters)
        {
            Operation = type;
        }

        public int InputSize => _parameters.Any() ? _parameters.First().Size : 0;

        public int OutputSize => _parameters.Any() ? _parameters.Count : 0;

        public Vector Apply(Vector input)
        {
            var result = ApplyInternal(input);

            if (input is ColumnVector1D) return new ColumnVector1D(result);

            return result;
        }

        private Vector ApplyInternal(Vector input)
        {
            switch (Operation)
            {
                case VectorOperationType.Multiply:
                    if (IsMultiRow)
                    {
                        return AsMatrix() * input;
                    }
                    else
                    {
                        return AsVector() * input;
                    }
                case VectorOperationType.Divide:
                    return input / AsVector();
                case VectorOperationType.Subtract:
                    return input - AsVector();
                case VectorOperationType.EuclideanDistance:
                    var vect = new ColumnVector1D(input);

                    return new Vector(AsMatrix().Select(v => vect.Distance(new ColumnVector1D(v))).ToArray());
            }

            throw new NotSupportedException(Operation.ToString());
        }

        public VectorOperationType Operation { get; private set; }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            _parameters.Clear();

            Operation = (VectorOperationType)Enum.Parse(typeof(VectorOperationType), doc.Properties["Type"]);

            foreach (var vect in doc.Vectors) _parameters.Add(vect);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["Operation"] = Operation.ToString();
            doc.Properties["Version"] = "1";

            foreach (var vect in _parameters) doc.Vectors.Add(vect);

            return doc;
        }

        private bool IsMultiRow => _parameters.Count == 1;

        private Vector AsVector() => _parameters[0];

        private Matrix AsMatrix() => new Matrix(_parameters);
    }

    public enum VectorOperationType
    {
        EuclideanDistance,
        Subtract,
        Multiply,
        Divide
    }
}
