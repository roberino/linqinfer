using LinqInfer.Data;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Data
{
    [TestFixture]
    public class BinaryVectorDocumentTests
    {
        [Test]
        public void Checksum_ChangesWhenDataChanges()
        {
            var doc = new BinaryVectorDocument();

            doc.Version = 1;
            doc.Properties["title"] = "abc";

            var checksum1 = doc.Checksum;

            doc.Version = 2;

            var checksum2 = doc.Checksum;

            doc.Properties["title"] = "cba";

            var checksum3 = doc.Checksum;

            Assert.That(checksum1, Is.Not.EqualTo(checksum2));
            Assert.That(checksum2, Is.Not.EqualTo(checksum3));
        }

        [Test]
        public void SaveAndLoad_DocWithChildren()
        {
            var doc = new BinaryVectorDocument();

            doc.Version = 2;

            doc.Vectors.Add(ColumnVector1D.Create(1.2, 7.4));

            var doc1 = new BinaryVectorDocument();

            doc1.Vectors.Add(ColumnVector1D.Create(8.2432, 89.4));
            doc1.Vectors.Add(ColumnVector1D.Create(20, 0.14));

            doc.Children.Add(doc1);

            using (var store = new InMemoryBlobStore())
            {
                store.Store("a", doc);

                var doc2 = store.Restore("a", new BinaryVectorDocument());

                Assert.That(doc.Vectors[0][1], Is.EqualTo(7.4));
                Assert.That(doc.Children[0].Vectors[0][0], Is.EqualTo(8.2432));
                Assert.That(doc.Children[0].Vectors[1][1], Is.EqualTo(0.14));
            }
        }

        [Test]
        public void Export_DocWithChildren()
        {
            var doc = new BinaryVectorDocument();

            doc.Version = 2;

            doc.Vectors.Add(ColumnVector1D.Create(1.2, 7.4));

            var doc1 = new BinaryVectorDocument();

            doc1.Vectors.Add(ColumnVector1D.Create(8.2432, 89.4));
            doc1.Vectors.Add(ColumnVector1D.Create(20, 0.14));

            doc.Children.Add(doc1);

            using (var store = new InMemoryBlobStore())
            {
                store.Store("a", doc);

                var doc2 = store.Restore("a", new BinaryVectorDocument());

                Assert.That(doc.Vectors[0][1], Is.EqualTo(7.4));
                Assert.That(doc.Children[0].Vectors[0][0], Is.EqualTo(8.2432));
                Assert.That(doc.Children[0].Vectors[1][1], Is.EqualTo(0.14));
            }

            var xml = doc.ExportAsXml();

            Console.WriteLine(xml);
        }

        [Test]
        public void SetNullPropertyValue_ThrowsError()
        {
            var doc = new BinaryVectorDocument();

            Assert.Throws<ArgumentException>(() => doc.Properties["x"] = null);
        }

        [Test]
        public void SaveAndLoad_BasicDoc()
        {
            var doc = new BinaryVectorDocument();

            doc.Version = 1;
            doc.Properties["title"] = "abc";
            doc.Properties["value1"] = "12";
            //doc.Properties["null"] = null;

            doc.Vectors.Add(ColumnVector1D.Create(1.2, 7.4));
            doc.Vectors.Add(ColumnVector1D.Create(0.2, 3.9));
            doc.Vectors.Add(ColumnVector1D.Create(4.6, 5.1, 8.1));

            Console.WriteLine(doc.Checksum);

            using (var store = new InMemoryBlobStore())
            {
                store.Store("a", doc);

                var doc2 = store.Restore("a", new BinaryVectorDocument());

                Assert.That(doc2.Properties.Count, Is.EqualTo(2));
                Assert.That(doc2.Properties["title"], Is.EqualTo("abc"));
                Assert.That(doc2.Properties["value1"], Is.EqualTo("12"));
                //Assert.That(doc2.Properties["null"], Is.EqualTo(""));

                Assert.That(doc.Vectors[1][1], Is.EqualTo(3.9));
            }
        }
    }
}
