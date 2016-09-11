using LinqInfer.Data.Remoting;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class VectorTransferServerTests
    {
        [Test]
        public async Task Server_ReceiveData()
        {
            using (var server = new VectorTransferServer())
            {
                server.AddHandler("x", (d, r) =>
                {
                    foreach (var vect in d.Vectors)
                    {
                        Console.WriteLine(vect);
                    }
                    return true;
                });

                server.Start();

                var client = new VectorTransferClient();

                var handle = await client.BeginTransfer("x");

                var data = new[] { ColumnVector1D.Create(1, 2, 3), ColumnVector1D.Create(4, 5, 6) };

                await handle.Send(data);

                Thread.Sleep(500);
            }
        }
    }
}
