using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace LinqInfer.Storage.File
{
    public class FileStorageProvider : ISampleStorageProvider
    {
        private readonly DirectoryInfo _baseDir;

        public FileStorageProvider(string path)
        {
            _baseDir = new DirectoryInfo(path);
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
