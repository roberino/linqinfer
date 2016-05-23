using LinqInfer.Storage.SQLite.Models;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class TableDef<T>
    {
        private const string Template = "CREATE TABLE {0} ({1})";
        private static ITypeMapping _mapping = TypeMappingCache.GetMapping<T>();

        public ITypeMapping Mapping { get; private set; } = _mapping;

        public string Name { get; private set; } = _mapping.TableName;

        public IDictionary<string, ColumnDef> Columns { get; private set; } = _mapping.GetSqlFieldDef();

        public IEnumerable<IDbDataParameter> CreateColumnParameters(IDbCommand cmd, object[] values)
        {
            var t = new SqlTypeTranslator();
            int i = 0;
            return Columns.Keys.Select(k =>
            {
                var p = cmd.CreateParameter();

                p.ParameterName = "@" + k;
                p.Value = t.ConvertToSqlValue(values[i++]);

                cmd.Parameters.Add(p);

                return p;
            })
            .ToList();
        }

        public string ColumnsString
        {
            get
            {
                return "(" + Columns.Where(c => !c.Value.PrimaryKey).Aggregate(new StringBuilder(), (s, c) => s.Append(c.Key).Append(",")).ToString().TrimEnd(',') + ")";
            }
        }

        public string ColumnsParametersString
        {
            get
            {
                return "(" + Columns.Where(c => !c.Value.PrimaryKey).Aggregate(new StringBuilder(), (s, c) => s.Append("@" + c.Key).Append(",")).ToString().TrimEnd(',') + ")";
            }
        }

        public string GetInsertSql()
        {
            var insert = "INSERT INTO " + Name;
            var cols = ColumnsString;
            var colParams = ColumnsParametersString;

            return insert + " " + cols + " VALUES " + colParams;
        }

        public string GetCreateSql()
        {
            var cols = Columns.Aggregate(new StringBuilder(), (s, c) => (s.Length > 0 ? s.Append(',') : s).AppendFormat("{0} {1} {2}", c.Key, c.Value.DataType, c.Value.PrimaryKey ? "PRIMARY KEY" : ""));

            return string.Format(Template, Name, cols);
        }

        public override string ToString()
        {
            return GetCreateSql();
        }
    }
}
