using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class UriRouteParserTests
    {
        [TestCase("tcp://localhost:9432/test/123", "/test/{param1}", "param1", "123")]
        [TestCase("tcp://localhost/test/abc/hhh", "/test/{x}/hhh", "x", "abc")]
        [TestCase("tcp://localhost/test/abc/hello?abc=1&def=2", "/test/abc/{x}", "x", "hello")]
        [TestCase("tcp://localhost/test/abc/hello?abc=1&def=2", "/test/abc/{x}", "query.def", "2")]
        [TestCase("tcp://localhost/test/abc/hello?abc=1&def=2&test=1", "/test/abc/{x}?test=1", "test", "1")]
        public void Parse_Examples(string uriString, string template, string param1, string value1)
        {
            var uri = new Uri(uriString);
            var route = new UriRoute(new Uri(uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port > 0 ? (":" + uri.Port) : null)), template);
            var parser = new UriRouteMapper(route);

            Assert.That(parser.CanMap(uri));

            var parameters = parser.Parse(uri);

            Assert.That(parameters[param1] == value1);
        }

        [Test]
        [Ignore("Not yet supported")]
        public void ParseWildCards()
        {
            var uri = new Uri("http://hosty/wild/man");
            var template = "/*/man";
            var route = new UriRoute(new Uri(uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port > 0 ? (":" + uri.Port) : null)), template);
            var parser = new UriRouteMapper(route);

            Assert.That(parser.CanMap(uri));

            var parameters = parser.Parse(uri);

            Assert.That(parameters["wild"] == "wild");
        }

        [TestCase("tcp://localhost:9432/test/123", "/test/123")]
        [TestCase("tcp://localhost/", "/")]
        public void Parse_NoParams(string uriString, string template)
        {
            var uri = new Uri(uriString);
            var route = new UriRoute(new Uri(uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port > 0 ? (":" + uri.Port) : null)), template);
            var parser = new UriRouteMapper(route);

            Assert.That(parser.CanMap(uri));

            var parameters = parser.Parse(uri);
        }
    }
}