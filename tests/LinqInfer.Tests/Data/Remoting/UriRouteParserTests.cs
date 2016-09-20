﻿using LinqInfer.Data.Remoting;
using NUnit.Framework;
using System;

namespace LinqInfer.Tests.Data.Remoting
{
    [TestFixture]
    public class UriRouteParserTests
    {
        [TestCase("tcp://localhost:9432/test/123", "/test/{param1}", "param1", "123")]
        [TestCase("tcp://localhost/test/abc/hhh", "/test/{x}/hhh", "x", "abc")]
        public void Parse_Example1(string uriString, string template, string param1, string value1)
        {
            var uri = new Uri(uriString);
            var route = new UriRoute(new Uri(uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port > 0 ? (":" + uri.Port) : null)), template);
            var parser = new UriRoutingTemplate(route);

            Assert.That(parser.IsMatch(uri));

            var parameters = parser.Parse(uri);

            Assert.That(parameters[param1] == value1);
        }

        [TestCase("tcp://localhost:9432/test/123", "/test/123")]
        [TestCase("tcp://localhost/", "/")]
        public void Parse_NoParams(string uriString, string template)
        {
            var uri = new Uri(uriString);
            var route = new UriRoute(new Uri(uri.Scheme + Uri.SchemeDelimiter + uri.Host + (uri.Port > 0 ? (":" + uri.Port) : null)), template);
            var parser = new UriRoutingTemplate(route);

            Assert.That(parser.IsMatch(uri));

            var parameters = parser.Parse(uri);
        }
    }
}