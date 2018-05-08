using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Storage
{
    public sealed class FileStorage : IVirtualFileStore
    {
        private readonly DirectoryInfo _storageDir;

        public FileStorage(DirectoryInfo storageDir)
        {
            _storageDir = storageDir;
        }

        public Task<bool> Delete()
        {
            if (_storageDir.Exists)
            {
                _storageDir.Delete(true);
            }

            return Task.FromResult(true);
        }

        public IVirtualFileStore GetContainer(string name)
        {
            return new FileStorage(new DirectoryInfo(Path.Combine(_storageDir.FullName, name)));
        }

        public Task<List<IVirtualFile>> ListFiles()
        {
            if (_storageDir.Exists)
            {
                return Task.FromResult(_storageDir.GetFiles().Select(VirtualFile.FromFile).ToList());
            }

            return Task.FromResult(new List<IVirtualFile>());
        }

        public Task<IVirtualFile> GetFile(string name)
        {
            var vf = VirtualFile.FromFile(new FileInfo(Path.Combine(_storageDir.FullName, name)));

            return Task.FromResult(vf);
        }
    }
}