using LinqInfer.Data.Remoting;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
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
                server.AddHandler(server.BaseEndpoint.CreateRoute("x"), (d, r) =>
                {
                    foreach (var vect in d.Vectors)
                    {
                        Console.WriteLine(vect);
                    }

                    if (!d.KeepAlive)
                    {
                        var response = Encoding.ASCII.GetBytes("hi");
                        r.Content.Write(response, 0, response.Length);
                    }

                    return true;
                });

                server.Start();

                using (var client = new VectorTransferClient())
                {
                    client.Timeout = 25000;

                    using (var handle = await client.BeginTransfer("x"))
                    {
                        var data = new[] { ColumnVector1D.Create(1, 2, 3), ColumnVector1D.Create(4, 5, 6) };

                        await handle.Send(data);
                        var res = await handle.End();

                        using (var ms = new MemoryStream())
                        {
                            await res.CopyToAsync(ms);

                            var text = Encoding.ASCII.GetString(ms.ToArray());

                            Assert.That(text, Is.EqualTo("hi"));
                        }
                    }
                }
            }
        }
    }
}
