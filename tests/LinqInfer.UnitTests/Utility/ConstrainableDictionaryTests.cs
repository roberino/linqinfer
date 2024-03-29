﻿using System;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Utility
{
    [TestFixture]
    public class ConstrainableDictionaryTests
    {
        [Test]
        public void AddContraint_SimpleKeyRule_ThrowsArgExceptionWhenViolated()
        {
            var dict = new ConstrainableDictionary<string, object>();

            dict.AddContraint((k, v) => k.StartsWith("x"));

            dict["x-0123"] = "hi";

            Assert.Throws<ArgumentException>(() => dict["a"] = 1);
        }

        [Test]
        public void EnforceType_ThrowsArgExceptionWhenViolated()
        {
            var dict = new ConstrainableDictionary<string, object>();

            dict.EnforceType<int>("x");

            dict["x"] = 4323;

            Assert.Throws<ArgumentException>(() => dict["x"] = "hi");
        }
    }
}