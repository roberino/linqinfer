using LinqInfer.Data.Orm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class SQLiteDbController : IDisposable
    {
        private const string ConnectionStringTemplate = "Data Source={0};Version=3;";

        private readonly DirectoryInfo _dataDir;
        private readonly string _dbName;
        private readonly IList<DbConnection> _openConnections;
        protected readonly Lazy<DbConnection> _conn;

        private bool _disposed;

        public SQLiteDbController(string dbName, string dataDir = null, bool createIfMissing = true)
        {
            _dbName = dbName;
            _dataDir = new DirectoryInfo(dataDir);
            _openConnections = new List<DbConnection>();
            _conn = new Lazy<DbConnection>(() => CreateConnection());
        }

        public DbConnection CreateConnection(bool open = true)
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);

            var dbFile = GetDbFile();

            if (!_dataDir.Exists)
            {
                _dataDir.Create();

                SQLiteConnection.CreateFile(dbFile.FullName);
            }
            else
            {
                if (!dbFile.Exists)
                {
                    SQLiteConnection.CreateFile(dbFile.FullName);
                }
            }

            var conn = new SQLiteConnection(string.Format(ConnectionStringTemplate, dbFile.FullName));

            _openConnections.Add(conn);

            if (open)
            {
                conn.Open();
            }

            return conn;
        }

        public async Task CreateTableFor<T>(bool onlyCreateIfMissing = false) where T : class
        {
            var tbleSql = new TableDef<T>();

            if (Exists<T>())
            {
                if (onlyCreateIfMissing) return;

                Execute("DROP TABLE " + tbleSql.Name);
            }

            await ExecuteAsync(tbleSql.ToString());
        }

        public void Transact(Action action)
        {
            using(var tx = _conn.Value.BeginTransaction())
            {
                action.Invoke();
                tx.Commit();
            }
        }

        public async Task TransactAsync(Func<Task> action)
        {
            using (var tx = _conn.Value.BeginTransaction())
            {
                await action.Invoke();
                tx.Commit();
            }
        }

        public Task<int> DeleteAsync<T>(Expression<Func<T, bool>> where = null)
        {
            var tableName = TypeMappingCache.GetMapping<T>().TableName;
            string sql;
            string whereClause = null;

            if (where != null)
            {
                whereClause = "WHERE " + new ExpressionParser().ParseWhereClause(where);
            }

            sql = string.Format("DELETE FROM {0} {1};", tableName, whereClause);

            return ExecuteAsync(sql);
        }

        public long? GetLastIdOf<T>() where T : class
        {
            using (var reader = ExecuteReader(string.Format("SELECT Id FROM {0} ORDER BY Id DESC LIMIT 1", TypeMappingCache.GetMapping<T>().TableName)))
            {
                if (reader.Read())
                {
                    return reader.GetInt64(0);
                }
            }

            return null;
        }

        public long? Insert<T>(T data) where T : class
        {
            var mapper = new SQLiteObjectMapper<T>(GetSqlSchema<T>());
            var tblDef = new TableDef<T>();
            var cmdText = tblDef.GetInsertSql();

            int c = 0;
            long? id = null;

            using (var tx = _conn.Value.BeginTransaction())
            {
                using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
                {
                    tblDef.CreateColumnParameters(cmd, mapper.GetValues(data));

                    cmd.CommandText = cmdText;

                    c += cmd.ExecuteNonQuery();
                }

                tx.Commit();

                if (c > 0 && tblDef.Mapping.HasPrimaryKey)
                {
                    id = GetLastIdOf<T>();

                    if (id.HasValue) tblDef.Mapping.SetPrimaryKey(data, id.Value);
                }
            }

            return id;
        }

        public async Task<long?> InsertAsync<T>(T data) where T : class
        {
            var mapper = new SQLiteObjectMapper<T>(GetSqlSchema<T>());
            var tblDef = new TableDef<T>();
            var cmdText = tblDef.GetInsertSql();

            int c = 0;
            long? id = null;

            using (var tx = _conn.Value.BeginTransaction())
            {
                using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
                {
                    tblDef.CreateColumnParameters(cmd, mapper.GetValues(data));

                    cmd.CommandText = cmdText;

                    c += await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();

                if (c > 0 && tblDef.Mapping.HasPrimaryKey)
                {
                    id = GetLastIdOf<T>();

                    if (id.HasValue) tblDef.Mapping.SetPrimaryKey(data, id.Value);
                }
            }

            return id;
        }

        public async Task<int> InsertManyAsync<T>(IEnumerable<T> data) where T : class
        {
            var mapper = new SQLiteObjectMapper<T>(GetSqlSchema<T>());
            var tblDef = new TableDef<T>();
            var cmdText = tblDef.GetInsertSql();
            var hasPk = tblDef.Mapping.HasPrimaryKey;

            int c = 0;

            using (var tx = _conn.Value.BeginTransaction())
            {
                foreach (var row in data)
                {
                    using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
                    {
                        tblDef.CreateColumnParameters(cmd, mapper.GetValues(row));

                        cmd.CommandText = cmdText;

                        var n = await cmd.ExecuteNonQueryAsync();

                        c += n;

                        if (n > 0 && hasPk)
                        {
                            var id = await GetLastInsertedId();

                            tblDef.Mapping.SetPrimaryKey(row, id);
                        }
                    }
                }

                tx.Commit();
            }

            return c;
        }

        public int InsertMany<T>(IEnumerable<T> data) where T : class
        {
            var mapper = new SQLiteObjectMapper<T>(GetSqlSchema<T>());
            var tblDef = new TableDef<T>();
            var cmdText = tblDef.GetInsertSql();
            var hasPk = tblDef.Mapping.HasPrimaryKey;

            int c = 0;

            using (var tx = _conn.Value.BeginTransaction())
            {
                foreach (var row in data)
                {
                    using (var cmd = _conn.Value.CreateCommand())
                    {
                        tblDef.CreateColumnParameters(cmd, mapper.GetValues(row));

                        cmd.CommandText = cmdText;

                        var n = cmd.ExecuteNonQuery();

                        c += n;

                        if (n > 0 && hasPk)
                        {
                            var id = GetLastIdOf<T>();

                            if (id.HasValue) tblDef.Mapping.SetPrimaryKey(row, id.Value);
                        }
                    }
                }

                tx.Commit();
            }

            return c;
        }

        public Task<IEnumerable<T>> QueryAsync<T>(Expression<Func<T, bool>> where = null, int? maxResults = null) where T : class, new()
        {
            var tableName = TypeMappingCache.GetMapping<T>().TableName;

            string sql = GetQuerySql(where, maxResults);

            using (var mapper = new RelationalDataMapper(() => CreateConnection(false), new SQLiteObjectMapperFactory()))
            {
                return mapper.QueryAsync<T>(sql);
            }
        }

        public IQueryable<T> Query<T>(Expression<Func<T, bool>> where = null, int? maxResults = null) where T : class, new()
        {
            var tableName = TypeMappingCache.GetMapping<T>().TableName;

            string sql = GetQuerySql(where, maxResults);

            using (var mapper = new RelationalDataMapper(() => CreateConnection(false), new SQLiteObjectMapperFactory()))
            {
                return mapper.Query<T>(sql).AsQueryable();
            }
        }

        public bool Exists<T>()
        {
            if (!GetDbFile().Exists) return false;

            var tableName = TypeMappingCache.GetMapping<T>().TableName;
            try
            {
                using (ExecuteReader("SELECT 1 FROM " + tableName + " LIMIT 1")) return true;
            }
            catch (SQLiteException ex)
            {
                Trace.WriteLine(ex);
                return false;
            }
        }

        protected DataTable GetSqlSchema<T>()
        {
            var tableName = TypeMappingCache.GetMapping<T>().TableName;
            using (var reader = ExecuteReader("SELECT 1 FROM " + tableName + " LIMIT 1"))
            {
                return reader.GetSchemaTable();
            }
        }

        protected async Task<IDataReader> ExecuteReaderAsync(string sql)
        {
            using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                var reader = await cmd.ExecuteReaderAsync();

                return reader;
            }
        }

        protected IDataReader ExecuteReader(string sql)
        {
            using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;

                return cmd.ExecuteReader();
            }
        }

        protected int Execute(string sql)
        {
            using (var tx = _conn.Value.BeginTransaction())
            {
                int x;

                using (var cmd = _conn.Value.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;

                    x = cmd.ExecuteNonQuery();
                }

                tx.Commit();

                return x;
            }
        }

        protected async Task<int> ExecuteAsync(string sql)
        {
            using (var tx = _conn.Value.BeginTransaction())
            {
                int x;

                using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;

                    x = await cmd.ExecuteNonQueryAsync();
                }

                tx.Commit();

                return x;
            }
        }

        public void Destroy()
        {
            if (_disposed) throw new ObjectDisposedException(GetType().Name);
            
            CloseAll();

            var file = GetDbFile();

            if (file.Exists)
            {
                try
                {
                    file.Delete();
                }
                catch (IOException)
                {
                    AppDomain.CurrentDomain.DomainUnload += (s, e) =>
                    {
                        file.Delete();
                    };
                }
            }
        }

        public void Dispose()
        {
            CloseAll();
            _disposed = true;
        }

        private async Task<long> GetLastInsertedId()
        {
            using (var cmd = (SQLiteCommand)_conn.Value.CreateCommand())
            {
                cmd.CommandText = "SELECT last_insert_rowid()";
                cmd.CommandType = CommandType.Text;

                var res = await cmd.ExecuteScalarAsync();

                return (long)res;
            }
        }

        private void CloseAll()
        {
            if (_conn.IsValueCreated && IsNotClosed(_conn.Value))
            {
                _conn.Value.Close();
                _conn.Value.Dispose();
            }

            foreach (var conn in _openConnections.Where(IsNotClosed))
            {
                conn.Close();
                conn.Dispose();
            }

            _openConnections.Clear();
        }

        private FileInfo GetDbFile()
        {
            var dbFile = new FileInfo(Path.Combine(_dataDir.FullName, _dbName + ".sqlite"));
            return dbFile;
        }

        private bool IsNotClosed(IDbConnection conn)
        {
            try
            {
                return conn.State != ConnectionState.Closed;
            }
            catch
            {
                return false;
            }
        }

        private static string GetQuerySql<T>(Expression<Func<T, bool>> where = null, int? maxResults = null) where T : class, new()
        {
            var tableName = TypeMappingCache.GetMapping<T>().TableName;
            string sql;
            string whereClause = null;

            if (where != null)
            {
                whereClause = "WHERE " + new ExpressionParser().ParseWhereClause(where);
            }

            if (maxResults.HasValue)
            {
                sql = string.Format("SELECT * FROM {0} {1} LIMIT {2};", tableName, whereClause, maxResults);
            }
            else
            {
                sql = string.Format("SELECT * FROM {0} {1};", tableName, whereClause);
            }

            return sql;
        }
    }
}