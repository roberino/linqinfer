using LinqInfer.Utility;
using System;
using System.IO;
using System.Linq;

namespace LinqInfer.Data
{
    public class FileBlobStore : BlobStoreBase
    {
        protected readonly DirectoryInfo _baseDir;

        public FileBlobStore(string baseDirectory = null)
        {
            _baseDir = new DirectoryInfo(baseDirectory ?? "blobs");
        }

        protected override Stream GetReadStream(string key)
        {
            if (!_baseDir.Exists)
            {
                _baseDir.Create();
            }

            lock (key)
            {
                return new FileStream(Path.Combine(_baseDir.FullName, key), FileMode.Open, FileAccess.Read, FileShare.Read);
            }
        }

        protected override Stream GetWriteStream(string key)
        {
            if (!_baseDir.Exists)
            {
                _baseDir.Create();
            }

            lock (key)
            {
                return new FileStream(Path.Combine(_baseDir.FullName, key), FileMode.Create, FileAccess.Write, FileShare.None);
            }
        }

        protected override bool RemoveBlob(string key)
        {
            var fullPath = Path.Combine(_baseDir.FullName, key);
            var file = new FileInfo(fullPath);

            if (file.Exists)
            {
                lock (key)
                {
                    try
                    {
                        file.Delete();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        DebugOutput.Log(ex);
                    }
                }
            }

            return false;
        }

        protected override string GetKey<T>(string key, T obj = default(T))
        {
            var keyr = base.GetKey<T>(key, obj);

            keyr = new string(keyr.Replace('$', Path.DirectorySeparatorChar).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

            return keyr;
        }
    }
}