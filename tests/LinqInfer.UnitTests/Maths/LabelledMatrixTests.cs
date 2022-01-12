using LinqInfer.Learning.Features;
using LinqInfer.Maths;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LinqInfer.UnitTests.Maths
{
    [TestFixture]
    public class LabelledMatrixTests
    {
        [Test]
        public void WhenAccessedByKey_ThenCorrectRowReturned()
        {
            var matrix = new LabelledMatrix<string>(
                Matrix.RandomMatrix(5, 4, new LinqInfer.Maths.Range(1, -1)),
                Enumerable.Range(0, 4).ToDictionary(n => "x-" + n, n => n));

            Assert.That(matrix.Width, Is.EqualTo(5));
            Assert.That(matrix.Height, Is.EqualTo(4));
            Assert.That(matrix.LabelIndexes.Count, Is.EqualTo(4));

            Assert.That(matrix["x-3"].Size, Is.EqualTo(5));
            Assert.That(matrix["x-2"].Equals(matrix.Rows[2]));
        }

        [Test]
        public void WhenExportedToVectorDocument_ThenCanBeImported()
        {
            var matrix = new LabelledMatrix<string>(
                Matrix.RandomMatrix(5, 4, new LinqInfer.Maths.Range(1, -1)),
                Enumerable.Range(0, 4).ToDictionary(n => "x-" + n, n => n));

            var doc = matrix.ExportData();

            var matrix2 = new LabelledMatrix<string>(doc);

            matrix.Equals(matrix2);
        }

        [Test]
        public void WhenWrittenAsCsv_ThenFormattedAsExpected()
        {
            var matrix = new LabelledMatrix<string>(
                Matrix.RandomMatrix(5, 4, new LinqInfer.Maths.Range(1, -1)),
                Enumerable.Range(0, 4).ToDictionary(n => "x-" + n, n => n));

            using (var writer = new StringWriter())
            {
                matrix.WriteAsCsv(writer);

                var csv = writer.ToString();

                int r = 0;

                using (var reader = new StringReader(csv))
                {
                    while (true)
                    {
                        var line = reader.ReadLine();

                        if (line == null) break;

                        var parts = line.Split(',');

                        Assert.That(parts[0], Is.EqualTo("\"x-" + r + "\""));

                        var data = line.Substring(parts[0].Length + 1);

                        var vect = Vector.FromCsv(data);

                        Assert.That(vect.Size, Is.EqualTo(5));

                        r++;
                    }

                    Assert.That(r, Is.EqualTo(4));
                }
            }
        }
    }
}
