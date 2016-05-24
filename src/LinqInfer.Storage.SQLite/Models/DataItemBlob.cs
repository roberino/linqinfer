using LinqInfer.Data.Sampling;
using LinqInfer.Maths;

namespace LinqInfer.Storage.SQLite.Models
{
    internal class DataItemBlob : DataItem
    {
        public static DataItemBlob Create(DataItem item, long sampleId)
        {
            return new DataItemBlob()
            {
                Key = item.Key,
                Label = item.Label,
                SampleId = sampleId,
                FeatureData = new ColumnVector1D(item.FeatureVector).ToByteArray()
            };
        }

        public DataItem Extract()
        {
            if (FeatureData != null) FeatureVector = ColumnVector1D.FromByteArray(FeatureData).ToDoubleArray();

            return this;
        }

        public byte[] FeatureData { get; set; }

        public long SampleId { get; set; }
    }
}