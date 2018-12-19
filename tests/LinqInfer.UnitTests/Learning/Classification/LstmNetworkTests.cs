using System;
using System.Collections.Generic;
using System.Text;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning;
using LinqInfer.Utility;
using NUnit.Framework;

namespace LinqInfer.UnitTests.Learning.Classification
{
    [TestFixture]
    public class LstmNetworkTests
    {
        [Test]
        public void T()
        {
            var seq = new[] {'a', 'b', 'c', 'a', 'b', 'c', 'd', 'e', 'a', 'b', 'c', 'd', 'b', 'c', 'd', 'e', 'b', 'c'};

            var asyncSeq = seq.AsAsyncEnumerator();

            var categoricalData = asyncSeq.CreateCategoricalPipelineAsync();

            
        }
    }
}
