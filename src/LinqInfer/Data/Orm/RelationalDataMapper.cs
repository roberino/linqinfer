using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;

namespace LinqInfer.Data.Orm
{
    internal class RelationalDataMapper : IDisposable
    {
        private readonly Func<IDbConnection> _connectionFactory;
        private Lazy<IDbConnection> _connection;

        public RelationalDataMapper(IDbConnection connection) : this(() => connection)
        {
        }

        public RelationalDataMapper(Func<IDbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory;
            _connection = new Lazy<IDbConnection>(_connectionFactory);
        }

        public IEnumerable<T> Query<T>(string cmdText = null, CommandType cmdType = CommandType.Text) where T : new()
        {
            Action<T, Type, string, object> mapper = null;

            //TODO: Clunky function call

            return Read(cmdText ?? string.Format("select * from {0}", typeof(T).Name), cmdType, s => mapper = new ObjectMapper<T>(s).Map, () => new T(), (c, t, r, v) => mapper(r, t, c, v));
        }

        public IEnumerable<dynamic> Query(string cmdText, CommandType cmdType = CommandType.Text)
        {
            IDictionary<string, string> names = null;

            return Read<dynamic>(cmdText, cmdType, s => { names = s.Rows.Cast<DataRow>().GroupBy(c => c.Field<string>(0)).ToDictionary(k => k.Key, v => GetPropName(v.First())); }, () => new ExpandoObject(), (c, t, r, v) => ((IDictionary<string, object>)r)[names[c]] = v);
        }

        protected IEnumerable<T> Read<T>(string cmdText, CommandType cmdType, Action<DataTable> schemaInit, Func<T> rowFact, Action<string, Type, T, object> mapper)
        {
            var cmd = Open().CreateCommand();

            cmd.CommandText = cmdText;
            cmd.CommandType = cmdType;

            using (var reader = cmd.ExecuteReader())
            {
                var schema = reader.GetSchemaTable();
                var rows = schema.Rows.Cast<DataRow>().ToList();

                schemaInit(schema);

                while (reader.Read())
                {
                    var row = rowFact();
                    int i = 0;

                    foreach (var fld in rows)
                    {
                        mapper(reader.GetName(i), reader.GetFieldType(i), row, reader.GetValue(i));
                        i++;
                    }

                    yield return row;
                }
            }
        }

        protected IDbConnection Open()
        {
            if(_connection.Value.State == ConnectionState.Closed)
            {
                _connection.Value.Open();
            }
            if(_connection.Value.State == ConnectionState.Broken)
            {
                try
                {
                    _connection.Value.Dispose();
                }
                catch
                {
                }
                _connection = new Lazy<IDbConnection>(_connectionFactory);
            }
            return _connection.Value;
        }

        public void Dispose()
        {
            if (_connection.IsValueCreated)
            {
                _connection.Value.Close();
                _connection.Value.Dispose();
            }
        }

        private static string GetPropName(DataRow col)
        {
            var name = col.Field<string>(0);
            if (!char.IsLetter(name[0]))
            {
                return "_" + new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
            }
            return new string(name.Where(c => char.IsLetterOrDigit(c)).ToArray());
        }
    }
}
