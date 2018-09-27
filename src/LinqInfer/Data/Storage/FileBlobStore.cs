using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Utility;

namespace LinqInfer.Data.Storage
{
    public class FileBlobStore : BlobStoreBase
    {
        protected readonly DirectoryInfo _baseDir;

        public FileBlobStore(string baseDirectory = null)
        {
            _baseDir = new DirectoryInfo(baseDirectory ?? "blobs");
        }

        public override Task<IEnumerable<string>> ListKeys<T>()
        {
            var baseKey = GetKey<T>("x");
            var basePath = GetFilePath(baseKey);
            var baseTypePartLen = (GetTypeKeyPart<T>() + KeyDelimitter).Length;

            var paths = basePath.Directory.GetFiles();

            return Task.FromResult(paths.Select(p => p.Name.Substring(baseTypePartLen)).ToList().AsEnumerable());
        }

        protected override Stream GetReadStream(string key)
        {
            if (!_baseDir.Exists)
            {
                _baseDir.Create();
            }

            lock (key)
            {
                var file = GetFilePath(key);

                if (!file.Exists)
                {
                    throw new KeyNotFoundException(key);
                }

                return new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                return new FileStream(GetFilePath(key).FullName, FileMode.Create, FileAccess.Write, FileShare.None);
            }
        }

        protected override bool RemoveBlob(string key)
        {
            var file = GetFilePath(key);

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

        protected virtual FileInfo GetFilePath(string key)
        {
            return new FileInfo(Path.Combine(_baseDir.FullName, key));
        }

        protected override string GetKey<T>(string key, T obj = default(T))
        {
            var keyr = base.GetKey<T>(key, obj);

            keyr = new string(keyr.Replace('$', Path.DirectorySeparatorChar).Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

            return keyr;
        }
    }
}