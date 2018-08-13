using System;
using LinqInfer.Data;
using System.Threading;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Learning.Classification
{
    public sealed class ClassifierStats : IExportableAsDataDocument, IImportableFromDataDocument, ICloneableObject<ClassifierStats>
    {
        long _classificationCount;
        long _trainingInterationCount;

        internal ClassifierStats()
        {
        }

        public long TrainingSampleCount { get { return _trainingInterationCount; } }

        public long ClassificationCount { get { return _classificationCount; } }

        public long MisclassificationCount { get; internal set; }

        internal void IncrementClassificationCount()
        {
            Interlocked.Increment(ref _classificationCount);
        }

        internal void IncrementTrainingSampleCount()
        {
            Interlocked.Increment(ref _trainingInterationCount);
        }

        public void ImportData(PortableDataDocument doc)
        {
            _trainingInterationCount = doc.PropertyOrDefault(() => TrainingSampleCount, 0);
            _classificationCount = doc.PropertyOrDefault(() => ClassificationCount, 0);
        }

        public PortableDataDocument ExportData()
        {
            var doc = new PortableDataDocument();

            doc.SetPropertyFromExpression(() => TrainingSampleCount);
            doc.SetPropertyFromExpression(() => ClassificationCount);
            doc.SetPropertyFromExpression(() => MisclassificationCount);

            return doc;
        }

        public ClassifierStats Clone(bool deep)
        {
            if (deep)
            {
                var stats = new ClassifierStats();

                stats.ImportData(ExportData());

                return stats;
            }

            return this;
        }
    }
}