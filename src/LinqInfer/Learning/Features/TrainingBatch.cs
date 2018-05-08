using LinqInfer.Data;
using LinqInfer.Maths;
using LinqInfer.Utility;
using System.Collections.Generic;
using System.Linq;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Features
{
    public class TrainingBatch : IBatch<TrainingPair<IVector, IVector>>, IExportableAsDataDocument, IImportableFromDataDocument
    {
        public TrainingBatch(IBatch<TrainingPair<IVector, IVector>> batch)
        {
            Items = batch.Items;
            BatchNumber = batch.BatchNumber;
            IsLast = batch.IsLast;
        }

        public IList<TrainingPair<IVector, IVector>> Items { get; }

        public int BatchNumber { get; private set; }

        public bool IsLast { get; private set; }

        public void ImportData(PortableDataDocument doc)
        {
            ArgAssert.Assert(() => doc.Children.Count > 0, nameof(doc.Children));

            var inputs = doc.Children[0];
            var outputs = doc.Children[1];

            BatchNumber = doc.PropertyOrDefault(() => BatchNumber, 0);
            IsLast = doc.PropertyOrDefault(() => IsLast, false);

            Items.Clear();

            foreach (var pair in inputs.Vectors.Zip(outputs.Vectors, (i, o) => new
            {
                input = i,
                output = o
            }))
            {
                Items.Add(new TrainingPair<IVector, IVector>(pair.input, pair.output));
            }
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();
            var inputs = new PortableDataDocument();
            var outputs = new PortableDataDocument();

            doc.Children.Add(inputs);
            doc.Children.Add(outputs);

            doc.SetPropertyFromExpression(() => BatchNumber);
            doc.SetPropertyFromExpression(() => IsLast);

            foreach (var pair in Items)
            {
                inputs.Vectors.Add(pair.Input);
                outputs.Vectors.Add(pair.TargetOutput);
            }

            return doc;
        }
    }
}