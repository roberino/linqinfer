﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Utility
{
    [TestFixture]
    public class DictionarySerialiserTests
    {
        [Test]
        public void Write_And_Read_StringIntDict()
        {
            var s = new DictionarySerialiser<string, int>();
            var data = new Dictionary<string, int>();

            data["a"] = 1;
            data["b"] = 2;
            data["c"] = 3;
            data["d"] = 5;
            data["e"] = 7;

            byte[] output;

            using (var ms = new MemoryStream())
            {
                s.Write(data, ms);

                output = ms.ToArray();
            }

            using (var ms = new MemoryStream(output))
            {
                var data2 = s.Read(ms);

                Assert.That(data2, Is.EquivalentTo(data));
            }
        }

        [Test]
        public void Write_And_Read_PirateIntDict()
        {
            var s = new DictionarySerialiser<TestData.Pirate, int>();
            var data = new Dictionary<TestData.Pirate, int>();

            data[new TestData.Pirate()] = 1;
            data[new TestData.Pirate() { Age = 16 }] = 2;
            data[new TestData.Pirate()] = 3;

            byte[] output;

            using (var ms = new MemoryStream())
            {
                s.Write(data, ms);

                output = ms.ToArray();
            }

            using (var ms = new MemoryStream(output))
            {
                var data2 = s.Read(ms);

                Assert.That(data2.Count, Is.EqualTo(data.Count));
                Assert.That(data2.Skip(1).First().Key.Age, Is.EqualTo(16));
                Assert.That(data2.Skip(2).First().Value, Is.EqualTo(3));
            }
        }

        [Test]
        public void Write_And_Read_StringIntConcurrentDict()
        {
            var s = new DictionarySerialiser<string, int>();
            var data = new ConcurrentDictionary<string, int>();

            data["a"] = 1;
            data["b"] = 2;
            data["c"] = 3;
            data["d"] = 5;
            data["e"] = 7;

            byte[] output;

            using (var ms = new MemoryStream())
            {
                s.Write(data, ms);

                output = ms.ToArray();
            }

            using (var ms = new MemoryStream(output))
            {
                var data2 = s.Read(ms);
                var type = data2.GetType();
                var type2 = typeof(ConcurrentDictionary<string, int>);

                Assert.That(data2, Is.EquivalentTo(data));
                Assert.That(type.Namespace + type.Name, Is.EqualTo(type2.Namespace + type2.Name));

                //Assert.That(data2, Is.InstanceOf<ConcurrentDictionary<string, int>>());
            }
        }
    }
}