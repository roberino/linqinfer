using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace LinqInfer.Data.Sampling.File
{
    public class FileStorageProvider : ISampleStorageProvider
    {
        private readonly DirectoryInfo _baseDir;
        private readonly IUriProvider _uriProvider;

        public FileStorageProvider(string path)
        {
            _baseDir = new DirectoryInfo(path);
            _uriProvider = new UriProvider();
        }

        public IQueryable<DataSampleHeader> ListSamples()
        {
            return ListSamples(_baseDir).AsQueryable();
        }

        public async Task<DataSample> DeleteSample(Uri sampleUri)
        {
            var sample = await RetrieveSample(sampleUri);
            var file = GetFile(sampleUri);
            file.Delete();
            return sample;
        }

        public Task<Uri> StoreSample(DataSample sample)
        {
            return Task<Uri>.Factory.StartNew(() => Write(sample, sample.Uri));
        }

        public async Task<Uri> UpdateSample(Uri sampleId, IEnumerable<DataItem> items, Func<DataSample, SampleSummary> onUpdate)
        {
            var sample = await RetrieveSample(sampleId);

            foreach(var item in items)
            {
                var existing = sample.SampleData.FirstOrDefault(s => s.Uri == item.Uri);

                if (existing != null)
                {

                }
                else
                {
                    sample.SampleData.Add(item);
                }
            }

            return sampleId;
        }

        public Task<DataSample> RetrieveSample(Uri sampleUri)
        {
            return Task<DataSample>.Factory.StartNew(() => Read<DataSample>(sampleUri));
        }

        public Task<DataItem> RetrieveItem(Uri itemUri)
        {
            throw new NotImplementedException();
        }

        private FileInfo GetFile(Uri uri)
        {
            var path = new FileInfo(Path.Combine(_baseDir.FullName, uri.PathAndQuery.Substring(1) + ".dat"));

            return path;
        }

        private Uri Write<T>(T data, Uri uri)
        {
            var bs = new BinaryFormatter();
            var path = GetFile(uri);

            lock (string.Intern(path.FullName))
            {
                if (!path.Directory.Exists) path.Directory.Create();

                using (var stream = path.OpenWrite())
                {
                    bs.Serialize(stream, data);
                }
            }

            return uri;
        }

        private T Read<T>(Uri uri)
        {
            var path = new FileInfo(Path.Combine(_baseDir.FullName, uri.PathAndQuery.Substring(1) + ".dat"));

            return Read<T>(path);
        }

        private T Read<T>(FileInfo path)
        {
            var bs = new BinaryFormatter();

            lock (string.Intern(path.FullName))
            {
                if (!path.Directory.Exists) path.Directory.Create();

                using (var stream = path.OpenRead())
                {
                    return (T)bs.Deserialize(stream);
                }
            }
        }

        private IEnumerable<DataSampleHeader> ListSamples(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                if (file.Extension == ".dat")
                {
                    DataSample data = null;

                    try
                    {
                        data = Read<DataSample>(file);
                        data.SampleData.Clear();
                    }
                    catch (System.Runtime.Serialization.SerializationException ex)
                    {
                        Trace.WriteLine(ex);
                    }
                    if (data != null)
                    {
                        yield return data; // _uriProvider.Create("samples", file.Name.Substring(0, file.Name.Length - 4));
                    }
                }
            }

            foreach (var subDir in dir.GetDirectories())
            {
                foreach (var sample in ListSamples(subDir))
                {
                    yield return sample;
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
