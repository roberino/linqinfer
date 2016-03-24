using System;

namespace LinqInfer.Storage
{
    [Serializable]
    public class FieldDescriptor
    {
        public virtual string Name { get; set; }

        public virtual string Label { get; set; }

        public virtual FieldUsageType FieldUsage { get; set; }

        public virtual TypeCode DataType { get; set; }

        public virtual int? Index { get; set; }

        public override string ToString()
        {
            return (Label ?? Name) + " (" + FieldUsage + ")";
        }
    }
}