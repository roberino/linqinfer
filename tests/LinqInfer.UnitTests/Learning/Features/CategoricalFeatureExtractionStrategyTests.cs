using System.Linq;
using LinqInfer.Data.Pipes;
using LinqInfer.Learning.Features;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.UnitTests.Learning.Features
{
    [TestFixture]
    public class CategoricalFeatureExtractionStrategyTests
    {
        [Test]
        public async Task BuildAsync_TwoCategorySample_ReturnsCorrectSizeVector()
        {
            var cfes = new CategoricalFeatureExtractionStrategy<CategoricalModel>();

            var builder = cfes.CreateBuilder();

            var items = CreateData();

            await RunPipelineAsync(items, builder);

            var extractor = await builder.BuildAsync();

            var vect = extractor.ExtractVector(new CategoricalModel()
            {
                Colour = "Red",
                TypeChar = "C",
                Weight = 4323
            });

            Assert.That(vect.Length, Is.EqualTo(6));
        }

        [Test]
        public async Task BuildAsync_UnknownCategory_ReturnsZero()
        {
            var cfes = new CategoricalFeatureExtractionStrategy<CategoricalModel>();

            var builder = cfes.CreateBuilder();

            var items = CreateData();

            await RunPipelineAsync(items, builder);

            var extractor = await builder.BuildAsync();

            var vect = extractor.ExtractIVector(new CategoricalModel()
            {
                Colour = "Purple",
                TypeChar = "C",
                Weight = 4323
            });

            Assert.That(vect.Sum, Is.EqualTo(1));
        }

        [Test]
        public async Task BuildAsync_NullValue_ReturnsZero()
        {
            var cfes = new CategoricalFeatureExtractionStrategy<CategoricalModel>();

            var builder = cfes.CreateBuilder();

            var items = CreateData();

            await RunPipelineAsync(items, builder);

            var extractor = await builder.BuildAsync();

            var vect = extractor.ExtractIVector(new CategoricalModel()
            {
                Colour = "Red",
                TypeChar = null,
                Weight = 4323
            });

            Assert.That(vect.Sum, Is.EqualTo(1));
        }

        [Test]
        public async Task BuildAsync_CovariantSample_ReturnsValues()
        {
            var cfes = new CategoricalFeatureExtractionStrategy<CategoricalModel>();

            var builder = cfes.CreateBuilder();

            var items = ExtendData(CreateData(),
                new CategoricalSubModel()
                {
                    Animal = "Dog",
                    Colour = "Brown",
                    TypeChar = "X",
                    Weight = 43
                }, 
                new CategoricalSubModel()
                {
                    Animal = "Cat",
                    Colour = "Black",
                    TypeChar = "Y",
                    Weight = 21
                });

            await RunPipelineAsync(items, builder);

            var extractor = await builder.BuildAsync();

            var vect = extractor.ExtractIVector(new CategoricalSubModel()
            {
                Animal = "Cat",
                Colour = "Red",
                TypeChar = "X",
                Weight = 3
            });

            Assert.That(vect.Size, Is.EqualTo(12));
            Assert.That(vect.Sum, Is.EqualTo(3));
        } 

        public static async Task RunPipelineAsync(CategoricalModel[] data, IAsyncSink<CategoricalModel> sink)
        {
            var dataset = From.Enumerable(data);

            var pipe = dataset.CreatePipe();

            pipe.RegisterSinks(sink);

            await pipe.RunAsync(CancellationToken.None);
        }

        public static CategoricalModel[] ExtendData(CategoricalModel[] items, params CategoricalModel[] newItems)
        {
            return items.Concat(newItems).ToArray();
        }

        public static CategoricalModel[] CreateData(){
            return new[]
            {
                new CategoricalModel()
                {
                    Colour = "Red",
                    TypeChar = "A",
                    Weight = 123
                },
                new CategoricalModel()
                {
                    Colour = "Green",
                    TypeChar = "B",
                    Weight = 443
                },
                new CategoricalModel()
                {
                    Colour = "Red",
                    TypeChar = "B",
                    Weight = 88
                },
                new CategoricalModel()
                {
                    Colour = "Blue",
                    TypeChar = "C",
                    Weight = 88
                },
                new CategoricalModel()
                {
                    Colour = "Green",
                    TypeChar = "C",
                    Weight = 88
                }
            };
        }
    }

    public class CategoricalModel
    {
        [Feature(Model = FeatureVectorModel.Categorical)]
        public string Colour { get; set; }

        [Feature(Model = FeatureVectorModel.Categorical)]
        public string TypeChar { get; set; }

        public int Weight { get; set; }
    }

    public class CategoricalSubModel : CategoricalModel
    {
        [Feature(Model = FeatureVectorModel.Categorical)]
        public string Animal { get; set; }
    }
}
