using LinqInfer.Data.Sampling;

namespace LinqInfer.Storage.SQLite.Models
{
    internal class SampleFieldMetadata : FieldDescriptor
    {
        public long Id { get; set; }
        public long SampleId { get; set; }
    }
}
