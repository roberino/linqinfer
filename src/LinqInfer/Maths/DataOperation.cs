using LinqInfer.Data.Serialisation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class DataOperation : IExportableAsDataDocument, IImportableFromDataDocument, IEquatable<DataOperation>
    {
        readonly IList<ColumnVector1D> _parameters;

        public DataOperation()
        {
            _parameters = new List<ColumnVector1D>();
        }

        public DataOperation(PortableDataDocument data) : this()
        {
            ImportData(data);
        }

        public DataOperation(VectorOperationType type, IEnumerable<Vector> parameters) : this()
        {
            foreach (var para in parameters) _parameters.Add(new ColumnVector1D(para));
        }

        public DataOperation(VectorOperationType type, Matrix transformation) : this(type, transformation.Rows.Select(r => r.ToColumnVector()))
        {
            Operation = type;
        }

        public DataOperation(VectorOperationType type, params Vector[] parameters) : this(type, (IEnumerable<Vector>)parameters)
        {
            Operation = type;
        }

        public int InputSize => _parameters.Any() ? _parameters.First().Size : 0;

        public int OutputSize => 
            Operation == VectorOperationType.EuclideanDistance ||
            Operation == VectorOperationType.MatrixMultiply
                ? _parameters.Count : (_parameters.Any() ? _parameters.First().Size : 0);

        public IVector Apply(IVector input)
        {
            switch (Operation)
            {
                case VectorOperationType.MatrixMultiply:
                    return AsMatrix() * input;
                case VectorOperationType.VectorMultiply:
                    return AsVector().MultiplyBy(input);
                case VectorOperationType.Divide:
                    return input.ToColumnVector() / AsVector();
                case VectorOperationType.SafeDivide:
                    return input.ToColumnVector().Divide(AsVector(), ZeroDivideBehaviour.ReturnZero);
                case VectorOperationType.Subtract:
                    return input.ToColumnVector() - AsVector();
                case VectorOperationType.EuclideanDistance:
                    var vect = input.ToColumnVector();

                    return new Vector(AsMatrix().Select(v => vect.Distance(new ColumnVector1D(v))).ToArray());
            }

            throw new NotSupportedException(Operation.ToString());
        }

        public VectorOperationType Operation { get; private set; }

        public void ImportData(PortableDataDocument doc)
        {
            _parameters.Clear();

            Operation = (VectorOperationType)Enum.Parse(typeof(VectorOperationType), doc.Name);

            foreach (var vect in doc.Vectors.Select(v => v.ToColumnVector())) _parameters.Add(vect);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetName(Operation.ToString());

            foreach (var vect in _parameters) doc.Vectors.Add(vect);

            return doc;
        }

        Vector AsVector() => _parameters[0];

        Matrix AsMatrix() => new Matrix(_parameters);

        public bool Equals(DataOperation other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            if (!(InputSize == other.InputSize && OutputSize == other.OutputSize && Operation == other.Operation)) return false;

            if (_parameters.Count != other._parameters.Count) return false;

            return _parameters.Zip(other._parameters, (x, y) => x.Equals(y)).All(x => x);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as DataOperation);
        }

        public override int GetHashCode()
        {
            return StructuralComparisons.StructuralEqualityComparer.GetHashCode(_parameters.Select(p => p.GetHashCode()).Concat(new[] { (int)Operation }).ToArray());
        }
    }
}
