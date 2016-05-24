using LinqInfer.Data.Sampling.Parsers;
using LinqInfer.Storage.SQLite.Providers;
using NUnit.Framework;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Tests
{
    [TestFixture]
    public class DataUploadTests
    {
        [TestCase("deaths-sample.csv")]
        public async Task LoadTestData(string fileName)
        {
            var asmPath = Assembly.GetExecutingAssembly().Location;
            var asmFile = new FileInfo(asmPath);
            var storagePath = Path.Combine(asmFile.DirectoryName, @"..\..\..\..\src\LinqInfer.Api\App_Data\storage");
            var dataPath = Path.Combine(asmFile.DirectoryName, @"..\..\..\TestData", fileName);

            using (var store = new SampleStore(storagePath))
            {
                await store.Setup();

                var parser = new CsvSampleParser();

                using (var file = File.OpenRead(dataPath))
                {
                    var sample = parser.ReadFromStream(file);

                    await store.StoreSample(sample);
                }
            }
        }
    }
}
