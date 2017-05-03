using LinqInfer.Data;
using System;
using System.Diagnostics.Contracts;
using System.Linq;

namespace LinqInfer.Maths
{
    public sealed class SerialisableVectorTransformation : IExportableAsVectorDocument, IImportableAsVectorDocument, IVectorTransformation
    {
        private readonly Matrix _multiplier;
        private Vector _transposer;

        public SerialisableVectorTransformation(Matrix multiplier, Vector transposer = null)
        {
            Contract.Ensures(multiplier != null);

            Type = TransformType.Multiply;

            _multiplier = multiplier;
            _transposer = transposer;
        }

        public Vector Apply(Vector vector)
        {
            if (vector == null) return null;

            if (_transposer != null)
            {
                if (vector is ColumnVector1D)
                {
                    return _multiplier * new ColumnVector1D((vector - _transposer));
                }
                else
                {
                    return _multiplier * (vector - _transposer);
                }
            }

            return _multiplier * vector;
        }

        public TransformType Type { get; set; }

        public int InputSize
        {
            get
            {
                return _multiplier.Width;
            }
        }

        public int OutputSize
        {
            get
            {
                return _multiplier.Height;
            }
        }

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            Type = (TransformType)Enum.Parse(typeof(TransformType), doc.Properties["Type"]);

            _multiplier.FromVectorDocument(doc.Children.First());

            if (doc.Children.Count > 1)
            {
                _transposer = doc.Children[1].Vectors.First();
            }
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

            doc.Properties["Type"] = Type.ToString();

            doc.Children.Add(_multiplier.ToVectorDocument());

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
            Multiply
        }
    }
}