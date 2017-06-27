using LinqInfer.Data;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class SerialisableVectorTransformation : IExportableAsVectorDocument, IImportableAsVectorDocument, IVectorTransformation
    {
        private readonly Matrix _transformer;
        private Vector _transposer;

        public SerialisableVectorTransformation(Matrix transformer, Vector transposer = null)
        {
            Contract.Ensures(transformer != null);

            Type = TransformType.Multiply;

            _transformer = transformer;
            _transposer = transposer;
        }

        public SerialisableVectorTransformation(Matrix transformer, TransformType type)
        {
            Contract.Ensures(transformer != null);

            Type = type;

            _transformer = transformer;
        }

        public Vector Apply(Vector vector)
        {
            if (vector == null) return null;

            if (Type == TransformType.Multiply)
            {
                if (_transposer != null)
                {
                    if (vector is ColumnVector1D)
                    {
                        return _transformer * new ColumnVector1D((vector - _transposer));
                    }
                    else
                    {
                        return _transformer * (vector - _transposer);
                    }
                }

                return _transformer * vector;
            }

            if (Type == TransformType.EuclideanDistance)
            {
                var vect = (_transposer != null) ? new ColumnVector1D(vector - _transposer) : new ColumnVector1D(vector);

                var diff = _transformer.Select(v => vect.Distance(new ColumnVector1D(v))).ToArray();

                return vector is ColumnVector1D ? new ColumnVector1D(vect) : new Vector(vect);
            }

            return vector;
        }

        public TransformType Type { get; set; }

        public int InputSize
        {
            get
            {
                return _transformer.Width;
            }
        }

        public int OutputSize
        {
            get
            {
                return _transformer.Height;
            }
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            Type = (TransformType)Enum.Parse(typeof(TransformType), doc.Properties["Type"]);

            _transformer.FromVectorDocument(doc.Children.First());

            if (doc.Children.Count > 1)
            {
                _transposer = doc.Children[1].Vectors.First();
            }
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["Type"] = Type.ToString();

            doc.Children.Add(_transformer.ToVectorDocument());

            if (_transposer != null)
            {
                var td = new BinaryVectorDocument();

                td.Vectors.Add(new ColumnVector1D(_transposer));

                doc.Children.Add(td);
            }

            return doc;
        }

        public enum TransformType
        {
            /// <summary>
            /// Multiplies the vector by the transforming matrix, returning a new vector
            /// </summary>
            Multiply,

            /// <summary>
            /// Calculates the Euclidean distance between each row of the transforming matrix, returning a new vector
            /// </summary>
            EuclideanDistance
        }
    }
}