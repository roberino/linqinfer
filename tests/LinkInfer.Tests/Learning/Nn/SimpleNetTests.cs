using LinqInfer.Learning;
using LinqInfer.Learning.Features;
using LinqInfer.Learning.Classification;
using NUnit.Framework;

namespace LinqInfer.Tests.Learning.Nn
{
    [TestFixture]
    public class SimpleNetTests
    {
        [Test]
        public void TrainAndClassify_SimpleSample_ReturnsCorrectClass()
        {
            var net = new NaiveBayesNormalClassifier<string>(4);

            var a1 = new byte[] { 10, 10, 100, 10 };
            var a2 = new byte[] { 8, 9, 101, 7 };
            var a3 = new byte[] { 8, 12, 99, 11 };
            var a4 = new byte[] { 8, 2, 109, 3 };

            var b1 = new byte[] { 100, 10, 10, 10 };
            var b2 = new byte[] { 95, 8, 12, 11 };
            var b3 = new byte[] { 102, 6, 9, 15 };
            var b4 = new byte[] { 88, 6, 64, 15 };

            net.Train("a", a1);
            net.Train("a", a2);
            net.Train("a", a3);
            net.Train("a", a4);

            net.Train("b", b1);
            net.Train("b", b2);
            net.Train("b", b3);
            net.Train("b", b4);

            var cls1 = net.Classify(new byte[] { 8, 9, 99, 7 });
            var cls2 = net.Classify(new byte[] { 99, 9, 10, 15 });
            var cls3 = net.Classify(new byte[] { 50, 9, 60, 15 });

            Assert.That(cls1.ClassType, Is.EqualTo("a"));
            Assert.That(cls2.ClassType, Is.EqualTo("b"));
            Assert.That(cls3.ClassType, Is.EqualTo("b"));
        }

        [Test]
        public void TrainAndClassify_HighVariance_ReturnsCorrectClass()
        {
            var net = new NaiveBayesNormalClassifier<string>(4);

            var a1 = new byte[] { 5, 5, 99, 9 };
            var a2 = new byte[] { 4, 4, 100, 90 };

            var b1 = new byte[] { 5, 5, 99, 50 };
            var b2 = new byte[] { 4, 4, 100, 49 };

            net.Train("a", a1);
            net.Train("a", a2);

            net.Train("b", b1);
            net.Train("b", b2);

            var cls1 = net.Classify(new byte[] { 5, 4, 99, 49 });

            Assert.That(cls1.ClassType, Is.EqualTo("b"));
            Assert.That(cls1.Score, Is.GreaterThan(0));
        }

        [Test]
        public void TrainAndClassify_NoVariance_ReturnsCorrectClass()
        {
            var net = new NaiveBayesNormalClassifier<string>(4);

            var a1 = new byte[] { 10, 1, 99, 0 };
            var a2 = new byte[] { 10, 1, 100, 0 };

            var b1 = new byte[] { 10, 1, 0, 0 };
            var b2 = new byte[] { 10, 2, 100, 0 };

            net.Train("a", a1);
            net.Train("a", a2);

            net.Train("b", b1);
            net.Train("b", b2);

            var cls1 = net.Classify(new byte[] { 9, 3, 99, 0 });
            var cls2 = net.Classify(new byte[] { 9, 3, 40, 0 });

            Assert.That(cls1.ClassType, Is.EqualTo("a"));
            Assert.That(cls2.ClassType, Is.EqualTo("b"));
        }

        [Test]
        public void TrainAndClassify_TextExample()
        {
            var word1a = "john";
            var word1b = "johns";
            var word2a = "james";
            var word2b = "jamie";

            var net = new NaiveBayesNormalClassifier<byte>(32);

            net.Train(1, StringToBytes(word1a));
            net.Train(1, StringToBytes(word1b));
            net.Train(2, StringToBytes(word2a));
            net.Train(2, StringToBytes(word2b));

            var cls1 = net.Classify(StringToBytes("juhn"));
            var cls2 = net.Classify(StringToBytes("jame"));

            Assert.That(cls1.ClassType, Is.EqualTo(1));
            Assert.That(cls2.ClassType, Is.EqualTo(2));
        }

        private byte[] StringToBytes(string data, int size = 5)
        {
            return new SingleWordVectoriser2().ExtractVector(data);
            //return new WordVectoriser(size).ExtractVector(data);
        }
    }
}
