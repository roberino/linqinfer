using LinqInfer.Text;
using LinqInfer.Text.VectorExtraction;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Text.VectorExtraction
{
    [TestFixture]
    public class OneHotTextEncodingTests
    {
        [Test]
        public void Ctor_SomeSet_InitialisesVectorSizeAndFeatureMetadata()
        {
            var words = new HashSet<string>(new[] {"x", "y"});
            var semanticSet = new SemanticSet(words);
            var encoder = new OneHotTextEncoding<string>(semanticSet, x => new[] {x});

            Assert.That(encoder.VectorSize, Is.EqualTo(semanticSet.Count));
            Assert.That(encoder.FeatureMetadata.Count(), Is.EqualTo(semanticSet.Count));
            Assert.That(encoder.FeatureMetadata.ElementAt(0).Key, Is.EqualTo("x"));
            Assert.That(encoder.FeatureMetadata.ElementAt(1).Key, Is.EqualTo("y"));
        }

        [Test]
        public void ExtractIVector_KnownValue_ReturnsCorrectValueAtIndex()
        {
            var words = new HashSet<string>(new[] { "x", "y" });
            var semanticSet = new SemanticSet(words);
            var encoder = new OneHotTextEncoding<string>(semanticSet, x => new[] { x });

            var vector = encoder.ExtractIVector("x");

            Assert.That(vector[0], Is.EqualTo(1));
            Assert.That(vector[1], Is.EqualTo(0));
        }

        [Test]
        public void ExtractIVector_UnknownValue_ReturnsZeroVector()
        {
            var words = new HashSet<string>(new[] { "x", "y" });
            var semanticSet = new SemanticSet(words);
            var encoder = new OneHotTextEncoding<string>(semanticSet, x => new[] { x });

            var vector = encoder.ExtractIVector("z");

            Assert.That(vector.Sum, Is.EqualTo(0));
        }

        [Test]
        public void ExportData_ThenCreateNewEncoding_ReturnsEqualEncoder()
        {
            var words = new HashSet<string>(new[] { "x", "y" });
            var semanticSet = new SemanticSet(words);
            var encoder = new OneHotTextEncoding<string>(semanticSet, x => new[] { x });

            var data = encoder.ExportData();

            var newEncoder = OneHotTextEncoding<string>.Create(data);

            Assert.That(newEncoder.VectorSize, Is.EqualTo(2));
            Assert.That(newEncoder.Encoder.IndexTable.ElementAt(0).Key, Is.EqualTo("x"));
            Assert.That(newEncoder.Encoder.IndexTable.ElementAt(1).Key, Is.EqualTo("y"));
            Assert.That(newEncoder.ExtractIVector("x")[0], Is.EqualTo(1));
        }

        [Test]
        public void ExportData_ObjectEncoding_ReturnsDocument()
        {
            var words = new HashSet<string>(new[] { "x", "y" });
            var semanticSet = new SemanticSet(words);
            var encoder = new OneHotTextEncoding<MyObj>(semanticSet, x => new[] { x.X });

            var data = encoder.ExportData();

            var newEncoder = OneHotTextEncoding<MyObj>.Create(data);

            Assert.That(newEncoder, Is.Not.Null);
        }

        class MyObj
        {
            public string X { get; set; }
        }
    }
}