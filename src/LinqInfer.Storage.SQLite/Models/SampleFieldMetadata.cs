using LinqInfer.Data.Sampling;

namespace LinqInfer.Storage.SQLite.Models
{
    public class SampleFieldMetadata : FieldDescriptor
    {
        public long SampleId { get; set; }
    }
}
