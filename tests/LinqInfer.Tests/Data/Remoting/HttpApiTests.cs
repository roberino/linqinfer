using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class HttpApiTests
    {
        [Test]
        public async Task Bind_To_AnonymousMethod_AndTestRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.Bind("/abc/{param}").To(new
                {
                    param = 123
                }, a =>
                Task.FromResult(new
                {
                    x = a.param * 5
                }));

                var url = new Uri(api.BaseEndpoint, "/abc/44");

                var result = await api.TestRoute(url, new
                {
                    x = 123
                });

                Assert.That(result.x, Is.EqualTo(220)); 
            }
        }
    }
}
