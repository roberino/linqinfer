using LinqInfer.Maths;
using NUnit.Framework;
using System.Linq;
using System.Text;

namespace LinqInfer.IntegrationTests.Text
{
    [TestFixture]
    public class ShakespeareVsMonkey : TestFixtureBase
    {
        private string GenerateMonkeyText(int numOfLines)
        {
            var text = new StringBuilder();

            foreach (var x in Enumerable.Range(0, numOfLines))
            {
                text.Append(GenerateMonkeyParagraph());
                text.AppendLine();
                text.AppendLine();
            }

            return text.ToString();
        }

        private string GenerateMonkeyParagraph()
        {
            var text = new StringBuilder();
            var rnd = Functions.RandomPicker("monkey", "jungle", "banana", "vine", "tarzan");

            foreach (var x in Enumerable.Range(0, Functions.Random(8) + 1))
            {
                foreach (var y in Enumerable.Range(0, Functions.Random(8) + 3))
                {
                    text.Append(" " + rnd());
                }

                text.Append('.');
            }

            return text.ToString();
        }
    }
}