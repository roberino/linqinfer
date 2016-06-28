using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class WordGraphTests
    {
        [TestCase("https://en.wikipedia.org/wiki/Main_Page")]
        public async Task Analise_AndFindRelationships(string url)
        {
            int i = 0;
            var mapper = new WordGraph();

            using (var reader = new HttpSemanticReader(a => Regex.IsMatch(a.PathAndQuery, @"\/wiki\/")))
            {
                foreach (var u in url.Split(';'))
                {
                    await reader.Read(new Uri(u.Trim()), x =>
                    {
                        mapper.Analise(x.Item2);

                        return true;  // (i++ < 10);
                    }, x => x.Descendants().FirstOrDefault(d => d.Attribute("id")?.Value == "bodyContent"));
                }
            }

            foreach(var w in mapper.FindRelationships().Take(100))
            {
                Console.WriteLine("{0}: {1}", w.Id, w.Text);

                foreach(var r in w.Relationships.OrderByDescending(r => r.Score).Take(5))
                {
                    Console.WriteLine("\t{0}", r.Target);
                }
            }
        }

        [Test]
        public void Tuple_Compare()
        {
            var t1 = new Tuple<int, int>(11, 27);
            var t2 = new Tuple<int, int>(1, 2);
            var t3 = new Tuple<int, int>(11, 27);

            Assert.That(t1, Is.EqualTo(t3));
        }
    }
}
