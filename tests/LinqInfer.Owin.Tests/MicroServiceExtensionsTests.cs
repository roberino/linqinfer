using LinqInfer.Data.Remoting;
using LinqInfer.Learning;
using LinqInfer.Learning.MicroServices;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace LinqInfer.Owin.Tests
{
    [TestFixture]
    public class MicroServiceExtensionsTests
    {
        [Test]
        public async Task CreateClassifierService()
        {
            var serverUri = new Uri("http://localhost:8023");

            var data = CreateData();

            var trainingData = data.AsQueryable().CreatePipeline().AsTrainingSet(x => x.z);
            var classifier = trainingData.ToNaiveBayesClassifier().Execute();

            using (var api = serverUri.CreateHttpApi())
            {
                api.CreateClassifierService(classifier, "/classifiers/c1");

                api.Start();

                using (var client = new HttpClient())
                {
                    var res = await client.PostAsync(new Uri(serverUri, "/classifiers/c1"), new StringContent("{'x':6,'y':2, 'z': 0 }"));

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That((int)res.StatusCode, Is.EqualTo(200));
                    Assert.That(text, Is.Not.Null);
                    Assert.That(text, Does.Match("classType"));
                }
            }
        }

        [Test]
        public async Task CreateContentStore()
        {
            var serverUri = new Uri("http://localhost:8023");
            var store = new Dictionary<string, string>();

            using (var api = serverUri.CreateHttpApi())
            {
                api.Bind("/content/{Key}", Verb.Post)
                    .To(new ContentDoc(),
                    c =>
                    {
                        store[c.Key] = c.Content;
                        return Task.FromResult(!string.IsNullOrEmpty(c.Content));
                    });

                api.Start();

                using (var client = new HttpClient())
                {
                    var res = await client.PostAsync(new Uri(serverUri, "/content/cls"), new StringContent("{'content':'hello there' }"));

                    var text = await res.Content.ReadAsStringAsync();

                    Assert.That((int)res.StatusCode, Is.EqualTo(200));
                    Assert.That(text, Is.EqualTo("true"));
                    Assert.That(store["cls"], Is.EqualTo("hello there"));
                }
            }
        }

        private D[] CreateData()
        {
            return new[]
            {
                new D
                {
                    x = 12,
                    y = 6,
                    z = 1
                },
                new D
                {
                    x = 1233,
                    y = 4444,
                    z = 2
                },
                new D
                {
                    x = 4,
                    y = 9,
                    z = 1
                },
                new D
                {
                    x = 9999,
                    y = 883,
                    z = 2
                }
            };
        }

        private class ContentDoc
        {
            public string Key { get; set; }
            public string Content { get; set; }
        }

        private class D
        {
            public int x { get; set; }
            public int y { get; set; }
            public int z { get; set; }
        }
    }
}