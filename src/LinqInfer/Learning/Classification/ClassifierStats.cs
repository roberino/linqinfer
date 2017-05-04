using System;
using LinqInfer.Data;
using System.Threading;

namespace LinqInfer.Learning.Classification
{
    public sealed class ClassifierStats : IExportableAsVectorDocument, IImportableAsVectorDocument, ICloneableObject<ClassifierStats>
    {
        private long _classificationCount;
        private long _trainingInterationCount;

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

        public void FromVectorDocument(BinaryVectorDocument doc)
        {
            _trainingInterationCount = doc.PropertyOrDefault(() => TrainingSampleCount, 0);
            _classificationCount = doc.PropertyOrDefault(() => ClassificationCount, 0);
        }

        public BinaryVectorDocument ToVectorDocument()
        {
            var doc = new BinaryVectorDocument();

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

                stats.FromVectorDocument(ToVectorDocument());

                return stats;
            }

            return this;
        }
    }
}