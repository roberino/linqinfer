using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    public sealed class LabelledMatrix<T> : Matrix, IEquatable<LabelledMatrix<T>>
        where T : IEquatable<T>
    {
        const string indexLabel = "index_";
        bool _labelsAreColsAndRows;

        internal LabelledMatrix(
            Matrix baseMatrix,
            IDictionary<T, int> labelIndexes,
            bool labelsAreColsAndRows = false) : base(baseMatrix.Rows.Select(r => r.ToColumnVector()))
        {
            LabelIndexes = labelIndexes;
            _labelsAreColsAndRows = labelsAreColsAndRows;
        }

        internal LabelledMatrix(
            PortableDataDocument data) : base(Create(new Vector(0)))
        {
            LabelIndexes = new Dictionary<T, int>();

            ImportData(data);
        }

        /// <summary>
        /// Returns a row by label
        /// </summary>
        public IVector this[T label] => Rows[LabelIndexes[label]];

        /// <summary>
        /// Returns labels and respective row indexes
        /// </summary>
        public IDictionary<T, int> LabelIndexes { get; }

        /// <summary>
        /// Returns the covariance matrix with labels
        /// </summary>
        public LabelledMatrix<T> LabelledCovarianceMatrix => new LabelledMatrix<T>(Rotate().CovarianceMatrix, LabelIndexes, true);

        /// <summary>
        /// Returns the cosine simularity matrix with labels
        /// </summary>
        public LabelledMatrix<T> LabelledCosineSimularityMatrix => new LabelledMatrix<T>(Rotate().CosineSimularityMatrix, LabelIndexes, true);

        public override PortableDataDocument ExportData()
        {
            var doc = base.ExportData();

            doc.SetPropertyFromExpression(() => _labelsAreColsAndRows, _labelsAreColsAndRows);

            foreach (var item in LabelIndexes)
            {
                doc.Properties[indexLabel + item.Value] = item.Key.ToString();
            }

            return doc;
        }

        public override void ImportData(PortableDataDocument doc)
        {
            base.ImportData(doc);

            _labelsAreColsAndRows = doc.PropertyOrDefault(nameof(_labelsAreColsAndRows), false);

            int i = 0;

            LabelIndexes.Clear();

            foreach (var item in doc.Properties)
            {
                if (item.Key.StartsWith(indexLabel))
                {
                    i = int.Parse(item.Key.Substring(indexLabel.Length));

                    LabelIndexes[(T)Convert.ChangeType(item.Value, typeof(T))] = i;
                }
            }
        }

        public override void WriteAsCsv(TextWriter output, char delimitter = ',', int precision = 8)
        {
            WriteAsCsvAsync(output, delimitter, precision).Wait();
        }

        public async Task WriteAsCsvAsync(TextWriter output, char delimitter = ',', int precision = 8)
        {
            if (_labelsAreColsAndRows)
            {
                foreach (var rowLabel in LabelIndexes)
                {
                    await output.WriteAsync($",\"{rowLabel.Key}\"");
                }

                await output.WriteLineAsync();
            }

            foreach (var rowLabel in LabelIndexes)
            {
                var row = Rows[rowLabel.Value];

                await output.WriteAsync($"\"{rowLabel.Key.ToString()}\"{delimitter}");
                await output.WriteAsync(row.ToColumnVector().ToCsv(delimitter, precision));
                await output.WriteLineAsync();
            }
        }

        public bool Equals(LabelledMatrix<T> other)
        {
            if (!base.Equals(other)) return false;

            if (other.LabelIndexes.Count != LabelIndexes.Count) return false;

            return other.LabelIndexes.Zip(LabelIndexes, (x, y) => x.Key.Equals(y.Key) && x.Value.Equals(y.Value)).All(v => v);
        }
    }
}