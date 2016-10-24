using LinqInfer.Data.Remoting;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Net;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class HttpApiTests
    {
        [Test]
        public async Task TestRoute_UndefinedRoute_Returns404()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                var url = new Uri(api.BaseEndpoint, "/nothing/10");

                try
                {
                    var result =
                        await api.TestRoute<int>(url);
                }
                catch (HttpException ex)
                {
                    Assert.That(ex.Status, Is.EqualTo(HttpStatusCode.NotFound));
                }
            }
        }

        [Test]
        public async Task ExportSyncMethod_CreatesRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.ExportSyncMethod(1, x => x * 12, "func1");

                var url = new Uri(api.BaseEndpoint, "/func1/10");

                var result =
                    await api.TestRoute<int>(url);

                Assert.That(result, Is.EqualTo(120));
            }
        }

        [Test]
        public async Task ExportDefinedSyncMethod_CreatesRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.ExportSyncMethod(50, Functions.Random);

                var url = new Uri(api.BaseEndpoint, "/random/10");

                var result =
                    await api.TestRoute<int>(url);

                Assert.That(result, Is.AtLeast(0));
                Assert.That(result, Is.AtMost(10));
            }
        }

        [Test]
        public async Task Bind_To_PrimativeArgFunc_TestRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.Bind("/funcy/{i}").ToSyncronousMethod<int, int>(i => i * 5);

                var url = new Uri(api.BaseEndpoint, "/funcy/10");
                
                var result =
                    await api.TestRoute<int>(url);

                Assert.That(result, Is.EqualTo(50));
            }
        }

        [Test]
        public async Task Bind_To_DefinedFunc_TestRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.Bind("/factorial/{x}").ToSyncronousMethod<int, long>(Functions.Factorial);

                var url = new Uri(api.BaseEndpoint, "/factorial/10");

                var expected = Functions.Factorial(10);

                Console.WriteLine("Expecting {0}", expected);

                var result =
                    await api.TestRoute<long>(url);

                Assert.That(result, Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task Bind_To_TestRouteWithInvalidReturnType()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.Bind("/factorial/{x}").To(new
                {
                    x = 1
                }, a => Task.FromResult(Functions.Factorial(a.x)));

                var url = new Uri(api.BaseEndpoint, "/factorial/10");

                try
                {
                    await api.TestRoute<double>(url);
                }
                catch (Exception ex)
                {
                    Assert.That(ex, Is.InstanceOf<HttpException>());
                }
            }
        }

        [Test]
        public async Task Bind_To_AnonymousMethodTwoParams_AndTestRoute()
        {
            var sz = new JsonSerialiser();

            using (var api = new HttpApi(sz, 9211))
            {
                api.Bind("/abc/{paramA}").To(new
                {
                    paramA = 123,
                    paramB = 16
                }, a =>
                Task.FromResult(new
                {
                    x = a.paramA * 5,
                    y = a.paramB * 4
                }));

                var url = new Uri(api.BaseEndpoint, "/abc/44");

                var result = await api.TestRoute(url, new
                {
                    x = 0,
                    y = 0
                });

                Assert.That(result.x, Is.EqualTo(220));
                Assert.That(result.y, Is.EqualTo(16 * 4));
            }
        }

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
