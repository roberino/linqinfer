using LinqInfer.Storage.Parsers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Storage.Parsers
{
    [TestFixture]
    public class CsvParserTests : TestFixtureBase
    {
        [Test]
        public void ParseFromStream_ExampleFile_ParsesCorrectly()
        {
            using (var csvData = GetResource("example.csv"))
            {
                var parser = new CsvParser();

                var sample = parser.ReadFromStream(csvData);

                Assert.That(sample.Metadata.Fields.Count, Is.EqualTo(5));
                Assert.That(sample.Metadata.Fields.ElementAt(0).Name, Is.EqualTo("x"));
                Assert.That(sample.Metadata.Fields.ElementAt(1).Name, Is.EqualTo("y"));
                Assert.That(sample.Metadata.Fields.ElementAt(2).Name, Is.EqualTo("z"));
                Assert.That(sample.Metadata.Fields.ElementAt(3).Name, Is.EqualTo("feature_a"));
                Assert.That(sample.Metadata.Fields.ElementAt(4).Name, Is.EqualTo("feature_b"));
                Assert.That(sample.Summary.Size, Is.EqualTo(5));
                Assert.That(sample.Summary.Count, Is.EqualTo(500));
            }
        }
    }
}
