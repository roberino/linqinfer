﻿using LinqInfer.Text;
using NUnit.Framework;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class HtmlParserTests
    {
        [TestCase("<p>Hello</p>", "p", "Hello", 1)]
        [TestCase("<p>Hello <sub>node</sub></p>", "p", "Hello node", 2)]
        [TestCase("<p>Hello<sub>node</sub></p>", "p", "Hellonode", 2)]
        [TestCase("<p>Hello<sub>node</sub> <x/>1</p>", "p", "Hellonode 1", 5)]
        public void Parse_SimpleHtml(string html, string expectedRoot, string expectedText, int expectedChildCount)
        {
            var parser = new HtmlParser();

            var elements = parser.Parse(html);

            var root = elements.Single();

            Assert.That(root is XElement);
            Assert.That(((XElement)root).Name == expectedRoot);
            Assert.That(((XElement)root).Value == expectedText);
            Assert.That(((XElement)root).Nodes().Count(), Is.EqualTo(expectedChildCount));
        }

        [TestCase("<p s='1'>Hello</p>", "<p s=\"1\">Hello</p>")]
        [TestCase("<p s=\"1\">Hello</p>", "<p s=\"1\">Hello</p>")]
        [TestCase("<p s='1p'>Hello</p>", "<p s=\"1p\">Hello</p>")]
        [TestCase("<p s='1p' new=\"abc\">Hello</p>", "<p s=\"1p\" new=\"abc\">Hello</p>")]
        [TestCase("<p x=15 y=20>Hello</p>", "<p x=\"15\" y=\"20\">Hello</p>")]
        [TestCase("<p x=15 y=20 >Hello</p>", "<p x=\"15\" y=\"20\">Hello</p>")]
        [TestCase("<p s='1'>Hello<a href='b'>...</a> </p>", "<p s=\"1\">Hello<a href=\"b\">...</a> </p>")]
        public void Parse_HtmlWithAttributes(string html, string expectedXml)
        {
            var parser = new HtmlParser();

            var elements = parser.Parse(html);

            var root = elements.Single();

            Assert.That(root is XElement);
            Assert.That(root.ToString(), Is.EqualTo(expectedXml));
        }

        [TestCase("<a><!- abc -->n</a>", "<a>n</a>")]
        public void Parse_HtmlWithComment(string html, string expectedXml)
        {
            var parser = new HtmlParser();

            var elements = parser.Parse(html);

            var root = elements.Single();

            Assert.That(root is XElement);
            Assert.That(root.ToString(), Is.EqualTo(expectedXml));
        }

        [TestCase("<a><span>&pi;</span></a>", "<a><span>π</span></a>")]
        public void Parse_HtmlWithEntity(string html, string expectedXml)
        {
            var parser = new HtmlParser();

            var elements = parser.Parse(html);

            var root = elements.Single();

            Assert.That(root is XElement);
            Assert.That(root.ToString(SaveOptions.DisableFormatting), Is.EqualTo(expectedXml));
        }
    }
}
