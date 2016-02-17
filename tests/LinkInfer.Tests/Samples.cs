using LinqInfer.Learning;
using NUnit.Framework;
using System;
using System.Linq;

namespace LinqInfer.Tests
{
    [TestFixture]
    public class Samples
    {
        [Test]
        public void Run()
        {
            var data = new[] {
                new { x = 1, y = 1 },
                new { x = 2, y = 1 },
                new { x = 3, y = 1 },
                new { x = 2, y = 2 },
                new { x = 2, y = 2 },
                new { x = 2, y = 2 }
                };

            var map = data.AsQueryable().ToSofm(new { x = 3, y = 3 }, 2);

            int i = 0;

            foreach (var m in map)
            {
                Console.Write("Node " + ++i);

                foreach(var x in m.Members)
                {
                    Console.WriteLine(x.Key);
                }
            }
        }
    }
}
