using LinqInfer.Data.Sampling;
using LinqInfer.Data.Sampling.Parsers;
using LinqInfer.Learning;
using LinqInfer.Storage.SQLite.Providers;
using NUnit.Framework;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace LinqInfer.Storage.SQLite.Tests
{
    [TestFixture]
    public class DataUploadTests
    {
        private const string StorageFolder = @"..\..\..\..\src\LinqInfer.Api\App_Data\storage";
        private const string TestDataFolder = @"..\..\..\TestData";

        [TestCase("deaths-sample.csv")]
        public async Task LoadTestData(string fileName)
        {
            var storagePath = GetStorageDir();
            var dataFile = GetDataFilePath(fileName);

            using (var store = new SampleStore(storagePath.FullName))
            {
                await store.Setup();

                var parser = new CsvSampleParser();

                using (var file = dataFile.OpenRead())
                {
                    var sample = parser.ReadFromStream(file);

                    sample.SourceDataUrl = dataFile.FullName;

                    await store.StoreSample(sample);
                }
            }
        }

        [TestCase("deaths-sample.csv")]
        public async Task RetrieveTestData_ToNaiveBayesClassifier(string fileName)
        {
            var storagePath = GetStorageDir();

            using (var store = new SampleStore(storagePath))
            {
                var samples = store.ListSamples();

                var item = samples.Where(s => s.SourceDataUrl != null && s.SourceDataUrl.EndsWith(fileName)).FirstOrDefault();

                var sample = await store.RetrieveSample(item.Uri);

                var pipe = sample.CreatePipeline();

                var classifier = pipe.ToNaiveBayesClassifier(x => x.Label).Execute();

                var classOfFirst = classifier.Classify(sample.SampleData.First());

                Assert.That(classOfFirst.Any());
                Assert.That(classOfFirst.First().Score > 0);
            }
        }

        [TestCase("deaths-sample.csv")]
        public async Task RetrieveTestData_ToMultilayerNetworkClassifier(string fileName)
        {
            var storagePath = GetStorageDir();

            using (var blobs = new BlobStore(storagePath))
            using (var store = new SampleStore(storagePath))
            {
                await blobs.Setup(true);

                var samples = store.ListSamples();

                var item = samples.Where(s => s.SourceDataUrl != null && s.SourceDataUrl.EndsWith(fileName)).FirstOrDefault();

                var sample = await store.RetrieveSample(item.Uri);

                var pipe = sample.CreatePipeline().OutputResultsTo(blobs);
                
                var classifier = pipe.ToMultilayerNetworkClassifier(x => x.Label).Execute(fileName);

                var classOfFirst = classifier.Classify(sample.SampleData.First());

                var blob = blobs.OpenAsMultilayerNetworkClassifier<DataItem, string>(fileName, sample.CreateFeatureExtractor());

                Assert.That(classOfFirst.Any());
                Assert.That(classOfFirst.First().Score > 0);
            }
        }

        private static FileInfo GetDataFilePath(string fileName)
        {
            var asmPath = Assembly.GetExecutingAssembly().Location;
            var asmFile = new FileInfo(asmPath);
            return new FileInfo(Path.Combine(asmFile.DirectoryName, TestDataFolder, fileName));
        }

        private static DirectoryInfo GetStorageDir()
        {
            var asmPath = Assembly.GetExecutingAssembly().Location;
            var asmFile = new FileInfo(asmPath);
            var storagePath = Path.Combine(asmFile.DirectoryName, StorageFolder);

            return new DirectoryInfo(storagePath);
        }
    }
}
