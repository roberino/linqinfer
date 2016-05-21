using LinqInfer.Storage.SQLite.Models;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal static class TypeMetadataHelper
    {
        public static string TableName<T>()
        {
            return typeof(T).Name;
        }

        public static IEnumerable<string> GetSqlFieldNames<T>()
        {
            var t = new SqlTypeTranslator();

            return typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => p.Name);
        }

        public static IDictionary<string, ColumnDef> GetSqlFieldDef<T>()
        {
            var t = new SqlTypeTranslator();

            return typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Select(p => new ColumnDef()
                {
                    Name = p.Name,
                    DataType = t.TranslateToSqlTypeName(p.PropertyType),
                    PrimaryKey = p.PropertyType == typeof(long) && p.Name == "Id",
                    ClrType = p.PropertyType
                })
                .ToDictionary(x => x.Name);
        }
    }
}
