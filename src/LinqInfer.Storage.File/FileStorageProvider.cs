using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace LinqInfer.Storage.File
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

        public IQueryable<Uri> ListSamples()
        {
            return ListSamples(_baseDir).AsQueryable();
        }

        public IEnumerable<Uri> ListSamples(DirectoryInfo dir)
        {
            foreach (var file in dir.GetFiles())
            {
                if(file.Extension == ".dat")
                {
                    yield return _uriProvider.Create("samples", file.Name.Substring(0, file.Name.Length - 4));
                }
            }

            foreach (var subDir in dir.GetDirectories())
            {
                foreach (var uri in ListSamples(subDir))
                {
                    yield return uri;
                }
            }
        }

        public Task<DataSample> DeleteSample(Uri sampleUri)
        {
            throw new NotImplementedException();
        }

        public Task<Uri> StoreSample(DataSample sample)
        {
            return Task<Uri>.Factory.StartNew(() => Write(sample, sample.Uri));
        }

        public Task<Uri> UpdateSample(Uri sampleId, IEnumerable<DataItem> items, Func<DataSample, SampleSummary> onUpdate)
        {
            throw new NotImplementedException();
        }

        public Task<DataSample> RetrieveSample(Uri sampleUri)
        {
            return Task<DataSample>.Factory.StartNew(() => Read<DataSample>(sampleUri));
        }

        public Task<DataItem> RetrieveItem(Uri itemUri)
        {
            throw new NotImplementedException();
        }

        private Uri Write<T>(T data, Uri uri)
        {
            var bs = new BinaryFormatter();
            var path = new FileInfo(Path.Combine(_baseDir.FullName, uri.PathAndQuery.Substring(1) + ".dat"));

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
            var bs = new BinaryFormatter();
            var path = new FileInfo(Path.Combine(_baseDir.FullName, uri.PathAndQuery.Substring(1) + ".dat"));

            lock (string.Intern(path.FullName))
            {
                if (!path.Directory.Exists) path.Directory.Create();

                using (var stream = path.OpenRead())
                {
                    return (T)bs.Deserialize(stream);
                }
            }
        }

        public void Dispose()
        {
        }
    }
}
