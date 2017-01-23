using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Xml.Linq;

namespace LinqInfer.Tests.Text.Analysis
{
    [TestFixture]
    public class CorpusAnalyserTests
    {
        [Test]
        public void DocumentTermCovarianceMatrix()
        {
            var docs = TestData.TestCorpus().Select(t => XDocument.Parse(t)).ToList().AsQueryable();

            var analyser = new CorpusAnalyser(docs.Select(x => x.Root.Value));

            analyser.DocumentTermCovarianceMatrix.WriteAsCsv(Console.Out);
        }

        [Test]
        public void DocumentTermCovarianceMatrix_SimpleExample_FindSynonyms()
        {
            var analyser = new CorpusAnalyser(new[] {
                "a b b b x",
                "a a b b b b y",
                "a a a b b b b b z"
                });

            foreach (var term in analyser.Terms)
            {
                Console.Write("{0} {1}\t", term.Index, term.Label);
            }

            //Console.WriteLine();

            //analyser.DocumentTermMatrix.WriteAsCsv(Console.Out, '\t', 2);

            //Console.WriteLine();

            analyser.DocumentTermCovarianceMatrix.WriteAsCsv(Console.Out, '\t', 2);

            foreach(var synGroup in analyser.FindCorrelations(0.1))
            {
                Console.Write(synGroup.Key.Label + ": ");

                foreach (var syn in synGroup.Value)
                {
                    Console.Write(syn.Key.Label + ", ");
                }

                Console.WriteLine();
            }
        }
    }
}