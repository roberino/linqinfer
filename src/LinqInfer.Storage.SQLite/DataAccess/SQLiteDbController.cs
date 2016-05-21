using LinqInfer.Data.Orm;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class SQLiteDbController : IDisposable
    {
        private const string ConnectionStringTemplate = "Data Source={0}.sqlite;Version=3;";

        private readonly DirectoryInfo _dataDir;
        private readonly string _dbName;
        protected readonly Lazy<IDbConnection> _conn;

        private bool _disposed;

        public SQLiteDbController(string dbName, string dataDir = null, bool createIfMissing = true)
        {
            _dbName = dbName;
            _dataDir = new DirectoryInfo(dataDir);
            _conn = new Lazy<IDbConnection>(() => CreateConnection());
        }

        public IDbConnection CreateConnection(bool open = true)
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

            var conn = new SQLiteConnection(string.Format(ConnectionStringTemplate, _dbName));

            if (open)
            {
                conn.Open();
            }

            return conn;
        }

        public void CreateTableFor<T>(bool onlyCreateIfMissing = false) where T : class
        {
            if (onlyCreateIfMissing)
            {
                if (Exists<T>()) return;
            }

            var tbleSql = new TableDef<T>();

            Execute(tbleSql.ToString());
        }

        public int Insert<T>(IEnumerable<T> data) where T : class
        {
            var mapper = new SQLiteObjectMapper<T>(GetSqlSchema<T>());

            var tblDef = new TableDef<T>();
            var insert = "INSERT INTO " + tblDef.Name;
            var cols = tblDef.ColumnsString;
            var colParams = tblDef.ColumnsParametersString;

            var cmdText = insert + " " + cols + " VALUES " + colParams;

            int c = 0;

            using (var tx = _conn.Value.BeginTransaction())
            {
                foreach (var row in data)
                {
                    using (var cmd = _conn.Value.CreateCommand())
                    {
                        tblDef.CreateColumnParameters(cmd, mapper.GetValues(row));

                        cmd.CommandText = cmdText;

                        c += cmd.ExecuteNonQuery();
                    }
                }
            }

            return c;
        }

        public IQueryable<T> Query<T>(Expression<Func<T, bool>> where = null, int? maxResults = null) where T : class, new()
        {
            string sql;
            using (var mapper = new RelationalDataMapper(() => CreateConnection(false), new SQLiteObjectMapperFactory()))
            {
                string whereClause = null;

                if (where != null)
                {
                    whereClause = "WHERE " + new ExpressionParser().ParseWhereClause(where);
                }

                if (maxResults.HasValue)
                {
                    sql = string.Format("SELECT * FROM {0} {1};", TypeMetadataHelper.TableName<T>(), whereClause);
                    // sql = string.Format("SELECT TOP({0}) * FROM {1} {2};", maxResults, TypeMetadataHelper.TableName<T>(), whereClause);
                }
                else
                {
                    sql = string.Format("SELECT * FROM {0} {1};", TypeMetadataHelper.TableName<T>(), whereClause);
                }

                return mapper.Query<T>(sql).AsQueryable();
            }
        }

        public bool Exists<T>()
        {
            try
            {
                ExecuteReader("SELECT * FROM " + TypeMetadataHelper.TableName<T>());
                return true;
            }
            catch (SQLiteException ex)
            {
                Trace.WriteLine(ex);
                return false;
            }
        }

        protected DataTable GetSqlSchema<T>()
        {
            using (var reader = ExecuteReader("SELECT * FROM " + TypeMetadataHelper.TableName<T>()))
            {
                return reader.GetSchemaTable();
            }
        }

        protected IDataReader ExecuteReader(string sql)
        {
            using (var cmd = _conn.Value.CreateCommand())
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

        public void Destroy()
        {
            var file = GetDbFile();

            Dispose();

            file.Delete();
        }

        public void Dispose()
        {
            if (_conn.IsValueCreated)
            {
                _conn.Value.Dispose();
            }
            _disposed = true;
        }

        private FileInfo GetDbFile()
        {
            var dbFile = new FileInfo(Path.Combine(_dataDir.FullName, _dbName + ".sqlite"));
            return dbFile;
        }
    }
}
