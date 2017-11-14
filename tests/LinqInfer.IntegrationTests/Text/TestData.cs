using LinqInfer.Text;
using LinqInfer.Text.Analysis;
using System.Linq;
using System.Reflection;

namespace LinqInfer.XUnit.Tests.Text
{
    internal static class TestData
    {
        public static Corpus GetShakespeareCorpus()
        {
            var asm = typeof(TestData).GetTypeInfo().Assembly;
            var resourceName = asm.GetManifestResourceNames().Single(n => n.EndsWith("shakespeare.txt"));

            using (var resource = asm.GetManifestResourceStream(resourceName))
            {
                return new Corpus(resource.Tokenise());
            }
        }
    }
}