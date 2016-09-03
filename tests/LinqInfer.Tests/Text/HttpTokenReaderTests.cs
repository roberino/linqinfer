using LinqInfer.Text;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace LinqInfer.Tests.Text
{
    [TestFixture]
    public class HttpTokenReaderTests
    {
        [Ignore("Integration only")]
        [TestCase("http://localhost/test.html")]
        public async Task Read_TestUrl_ReturnsTokenStream(string url)
        {
            using (var reader = new HttpTokenReader())
            {
                await reader.Read(new Uri(url), x =>
                {
                    foreach (var t in x.Item2)
                    {
                        Console.WriteLine(t);
                    }

                    return false;
                });
            }
        }
    }
}
