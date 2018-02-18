using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using static LinqInfer.Tests.TestData;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class FunctionBinderTests
    {
        [Test]
        public async Task BindToAsyncMethod_AnonymousFuncObjectArgWithDefault_ThenExecute()
        {
            var sz = new MockSerialiser();
            var binder = new FunctionBinder(sz);

            var exec = binder.BindToAsyncMethod(a => Task.FromResult(new { x = a.id }), new { id = 123 });

            var context = new MockContext();

            await exec(context);

            var resultObj = sz.LastSerialisedObject;

            Assert.That(resultObj, Is.Not.Null);

            var xprop = resultObj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).FirstOrDefault();

            var val = xprop.GetValue(resultObj);

            Assert.That(val, Is.EqualTo(123));
        }

        [Test]
        public async Task BindToAsyncMethod_AnonymousFuncObjectArgWithParam_ThenExecute()
        {
            var sz = new MockSerialiser();
            var binder = new FunctionBinder(sz);

            var exec = binder.BindToAsyncMethod(a => Task.FromResult(new { x = a.id }), new { id = 123 });

            var context = new MockContext();

            context["route.id"] = 43;

            await exec(context);

            var resultObj = sz.LastSerialisedObject;

            Assert.That(resultObj, Is.Not.Null);

            var xprop = resultObj.GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance).FirstOrDefault();

            var val = xprop.GetValue(resultObj);

            Assert.That(val, Is.EqualTo(43));
        }

        [Test]
        public async Task BindToAsyncMethod_PrimativeArgWithDefault_ThenExecute()
        {
            var sz = new MockSerialiser();
            var binder = new FunctionBinder(sz);

            var exec = binder.BindToAsyncMethod(MyFunc, 55);

            var context = new MockContext();

            await exec(context);

            var resultObj = sz.LastSerialisedObject as Pirate;

            Assert.That(resultObj, Is.Not.Null);
            Assert.That(resultObj.Age, Is.EqualTo(55));
        }

        [Test]
        public async Task BindToAsyncMethod_PrimativeArgWithoutDefault_ThenExecute()
        {
            var sz = new MockSerialiser();
            var binder = new FunctionBinder(sz);

            var exec = binder.BindToAsyncMethod<int, Pirate>(MyFunc);

            var context = new MockContext();

            context["route.arg1"] = 43;

            await exec(context);

            var resultObj = sz.LastSerialisedObject as Pirate;

            Assert.That(resultObj, Is.Not.Null);
            Assert.That(resultObj.Age, Is.EqualTo(43));
        }

        [Test]
        public async Task BindToAsyncMethod_PrimativeArgNoDefault_ExecuteMissingParameter()
        {
            var sz = new MockSerialiser();
            var binder = new FunctionBinder(sz);

            var exec = binder.BindToAsyncMethod<int, Pirate>(MyFunc);

            var context = new MockContext();

            try
            {
                await exec(context);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.That(ex, Is.InstanceOf<ArgumentException>());
            }
        }

        private Task<Pirate> MyFunc(int arg1)
        {
            return Task.FromResult(new Pirate()
            {
                Age = arg1
            });
        }

        private class MockContext : Dictionary<string, object>, IOwinContext
        {
            public MockContext()
            {
                Response = new TcpResponse(TransportProtocol.Http);
                Request = new TcpRequest(new TcpRequestHeader(BitConverter.GetBytes((long)0)), Stream.Null);
            }

            public TcpRequest Request { get; private set; }

            public Uri RequestUri
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public TcpResponse Response { get; private set; }

            public ClaimsPrincipal User
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Cancel()
            {
                throw new NotImplementedException();
            }

            public IOwinContext Clone(bool deep)
            {
                throw new NotImplementedException();
            }

            public void Dispose()
            {
            }

            public Task WriteTo(Stream output)
            {
                throw new NotImplementedException();
            }
        }
    }
}
