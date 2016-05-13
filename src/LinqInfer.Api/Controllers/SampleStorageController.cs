using LinqInfer.Api.Models;
using LinqInfer.Data.Sampling;
using LinqInfer.Data.Sampling.Parsers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Http;

namespace LinqInfer.Api.Controllers
{
    [RoutePrefix("api/data")]
    public class SampleStorageController : DataApiControllerBase
    {
        [Route("samples")]
        [HttpGet]
        public ResourceList<object> ListSamples()
        {
            return new ResourceList<object>(Storage
                .ListSamples()
                .AsEnumerable().Select(s =>
                    new
                    {
                        uri = ToConcreteUri(s.Uri),
                        header = s
                    }));
        }

        [Route("samples")]
        public async Task<object> PostSample([FromBody] DataSample data)
        {
            if (data == null) throw new ArgumentNullException();

            data.Recalculate();

            var uri = await Storage.StoreSample(data);

            return new { uri = ToConcreteUri(uri), created = data.Created, summary = data.Summary };
        }

        [Route("samples/{id}")]
        public async Task<Resource<DataSample>> GetSample(string id)
        {
            var sample = await GetSampleById(id);

            var sampleResource = new Resource<DataSample>(sample, ToConcreteUri(sample.Uri));

            sample.SampleData = null;
            sampleResource.Views["self organising feature map"] = ToConcreteUri(sample.Uri, "/sofm");

            return sampleResource;
        }

        [Route("samples/{id}/items/{itemId}")]
        public async Task<Resource<DataItem>> GetSample(string id, string itemId)
        {
            var sample = await GetSampleById(id);

            var sampleItem = new Resource<DataItem>(sample.SampleData.FirstOrDefault(d => d.Id == itemId), ToConcreteUri(sample.Uri));

            return sampleItem;
        }

        [Route("sample-file")]
        [HttpPost]
        public async Task<object> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            } 

            var path = HostingEnvironment.MapPath("~/App_Data/uploads");
            var streamProvider = new MultipartFormDataStreamProvider(path);
            await Request.Content.ReadAsMultipartAsync(streamProvider);

            var csvReader = new CsvSampleParser();
            var sampleUris = new List<Uri>();

            foreach (var file in streamProvider.FileData)
            {
                using (var fs = File.OpenRead(file.LocalFileName))
                {
                    var sample = csvReader.ReadFromStream(fs);

                    sample.Label = file.Headers.ContentDisposition.FileName;
                    sample.Description = streamProvider.FormData["description"];

                    var sampleResult = await Storage.StoreSample(sample);

                    sampleUris.Add(sampleResult);
                }
            }

            return new
            {
                //FileNames = streamProvider.FileData.Select(entry => Path.GetFileName(entry.LocalFileName)),
                Names = streamProvider.FileData.Select(entry => entry.Headers.ContentDisposition.FileName),
                ContentTypes = streamProvider.FileData.Select(entry => entry.Headers.ContentType.MediaType),
                Created = DateTime.UtcNow,
                Samples = sampleUris
            };
        }

        [Route("samples-csv")]
        public async Task<object> PostRawCsvSample([FromBody] string data)
        {
            if (data == null) throw new ArgumentNullException();

            var sample = new DataSample()
            {
                SampleData = new List<DataItem>()
            };

            foreach (var line in data.Split('\n', ']'))
            {
                var row = new DataItem();

                if (string.IsNullOrWhiteSpace(line)) continue;

                row.FeatureVector = line
                        .Split(',')
                        .Select(v => v.Trim())
                        .Where(v => v != "[" && !string.IsNullOrEmpty(v))
                        .Select(v => double.Parse(v))
                        .ToArray();

                sample.SampleData.Add(row);
            }

            return await PostSample(sample);
        }

        [Route("samples-naked")]
        public async Task<object> PostRawSample([FromBody] List<double[]> data)
        {
            if (data == null) throw new ArgumentNullException();

            var sample = new DataSample()
            {
                SampleData = new List<DataItem>()
            };

            foreach (var vect in data)
            {
                var row = new DataItem()
                {
                    FeatureVector = vect
                };

                sample.SampleData.Add(row);
            }

            return await PostSample(sample);
        }
    }
}
