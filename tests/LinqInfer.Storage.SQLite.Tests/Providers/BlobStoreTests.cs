using LinqInfer.Data;
using LinqInfer.Storage.SQLite.Providers;
using NUnit.Framework;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text;

namespace LinqInfer.Storage.SQLite.Tests.Providers
{
    [TestFixture]
    public class BlobStoreTests
    {
        [Test]
        public async Task Setup_And_Destory()
        {
            using (var store = new BlobStore())
            {
                await store.Setup();

                store.Destroy();
            }
        }

        [Test]
        public async Task StoreAsync()
        {
            using (var store = new BlobStore())
            {
                await store.Setup();

                var binobj = new BinObj() { Data = "X O 9" };

                await store.StoreAsync("K1", binobj);

                store.Destroy();
            }
        }

        [Test]
        public async Task RestoreAsyncAsync()
        {
            using (var store = new BlobStore())
            {
                await store.Setup();

                var binobj = new BinObj() { Data = "X O 9" };

                await store.StoreAsync("K2", binobj);

                var binobj2 = new BinObj() { Data = "0" };

                await store.RestoreAsync("K2", binobj2);

                Assert.That(binobj2.Data, Is.EqualTo("X 0 9"));

                store.Destroy();
            }
        }

        private class BinObj : IBinaryPersistable
        {
            public string Data { get; set; }

            public void Load(Stream input)
            {
                var data = ((MemoryStream)input).ToArray();

                Data = Encoding.UTF8.GetString(data);
            }

            public void Save(Stream output)
            {
                var bytes = Encoding.UTF8.GetBytes(Data);
                output.Write(bytes, 0, bytes.Length);
            }
        }
    }
}