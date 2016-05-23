using LinqInfer.Data.Orm;
using NUnit.Framework;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Orm
{
    [TestFixture]
    public class DataMapperTests
    {
        Func<IDbConnection> _connFact;

        [OneTimeSetUp]
        public void Setup()
        {
            Console.WriteLine(Environment.CurrentDirectory);

            try
            {
                File.Delete("TestDb.sqlite");
            }
            catch { }

            _connFact = () => new SQLiteConnection("Data Source=TestDb.sqlite;Version=3;");

            using (var conn = _connFact())
            {
                var sqlCreate = "create table things (Name varchar(20), Frequency int)";
                var sqlInsert = "insert into things (name, frequency) values('{0}', {1})";

                conn.Open();

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sqlCreate;
                    cmd.CommandType = System.Data.CommandType.Text;
                    cmd.ExecuteNonQuery();
                }

                foreach (var r in Enumerable.Range(0, 100))
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = string.Format(sqlInsert, "Name " + r, r);
                        cmd.CommandType = System.Data.CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            AppDomain.CurrentDomain.DomainUnload += (s, e) =>
            {
                try
                {
                    File.Delete("TestDb.sqlite");
                }
                catch { }
            };
        }

        [Test]
        public void Query_Dynamic_TestData_MappsCorrectly()
        {
            using (var mapper = new RelationalDataMapper(_connFact))
            {
                var data = mapper.Query("select * from things").ToList();

                var name5 = (string)data[5].Name;
                var int6 = (int)data[6].Frequency;

                Assert.That(data.Count, Is.EqualTo(100));
                Assert.That(name5, Is.EqualTo("Name 5"));
                Assert.That(int6, Is.EqualTo(6));
            }
        }

        [Test]
        public void Query_Types_TestData_MappsCorrectly()
        {
            using (var mapper = new RelationalDataMapper(_connFact))
            {
                var data = mapper.Query<Things>().ToList();

                var name5 = (string)data[5].Name;
                var int6 = (int)data[6].Frequency;

                Assert.That(data.Count, Is.EqualTo(100));
                Assert.That(name5, Is.EqualTo("Name 5"));
                Assert.That(int6, Is.EqualTo(6));
            }
        }

        [Test]
        public async Task QueryAsync_Types_TestData_MappsCorrectly()
        {
            using (var mapper = new RelationalDataMapper(_connFact))
            {
                var data = (await mapper.QueryAsync<Things>()).ToList();

                var name5 = (string)data[5].Name;
                var int6 = (int)data[6].Frequency;

                Assert.That(data.Count, Is.EqualTo(100));
                Assert.That(name5, Is.EqualTo("Name 5"));
                Assert.That(int6, Is.EqualTo(6));
            }
        }

        public class Things
        {
            public string Name { get; set; }

            public int Frequency { get; set; }
        }
    }
}
