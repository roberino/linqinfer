using System;

namespace LinqInfer.Storage.SQLite.Models
{
    internal class ColumnDef
    {
        public string Name { get; set; }
        public string DataType { get; set; }
        public string Mods { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public Type ClrType { get; set; }
    }
}
