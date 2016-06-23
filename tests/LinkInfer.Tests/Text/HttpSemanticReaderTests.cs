using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class HttpSemanticReaderTests
    {
        [TestCase("http://localhost/test.html")]
        public async Task Read_TestUrl_ReturnsTokenStream(string url)
        {
            var reader = new HttpSemanticReader();

            await reader.Read(new Uri(url), x =>
            {
                foreach(var t in x.Item2)
                {
                    Console.WriteLine(t);
                }

                return true;
            });
        }
    }
}
