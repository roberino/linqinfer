using System;
using System.Reflection;

namespace LinqInfer.Data.Orm
{
    public class ColumnMapping
    {
        public int? Index { get; set; }
        public string Name { get; set; }
        public string QualifiedColumnName { get; set; }
        public string DataType { get; set; }
        public string Mods { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public Type ClrType { get; set; }
        public PropertyInfo MappedProperty { get; set; }
    }
}
