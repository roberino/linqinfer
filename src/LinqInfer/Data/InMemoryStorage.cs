using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data
{
    class InMemoryStorage : IVirtualFileStore
    {
        private readonly IDictionary<string, InMemoryStorage> _childContainers;
        private readonly IDictionary<string, IVirtualFile> _files;
        private readonly InMemoryStorage _parent;
        private readonly string _name;

        public InMemoryStorage(InMemoryStorage parent = null, string name = null)
        {
            _parent = parent;
            _name = name;
            _files = new Dictionary<string, IVirtualFile>();
            _childContainers = new Dictionary<string, InMemoryStorage>();
        }

        public async Task<bool> Delete()
        {
            _parent?._childContainers.Remove(_name);

            foreach (var child in _childContainers)
            {
                await child.Value.Delete();
            }

            foreach (var file in _files)
            {
                await file.Value.Delete();
            }

            _childContainers.Clear();
            _files.Clear();

            return true;
        }

        public IVirtualFileStore GetContainer(string name)
        {
            if(!_childContainers.TryGetValue(name, out InMemoryStorage childStore))
            {
                _childContainers[name] = childStore = new InMemoryStorage(this, name);
            }

            return childStore;
        }

        public Task<IVirtualFile> GetFile(string name)
        {
            if (!_files.TryGetValue(name, out IVirtualFile file))
            {
                var data = new VirtualFileData();

                _files[name] = file = new VirtualFile(name, _ => data.GetData(), (_, d) => data.Write(d), _ => data.Delete(), _ => data.GetData().Result);
            }

            return Task.FromResult(file);
        }

        public Task<List<IVirtualFile>> ListFiles()
        {
            return Task.FromResult(_files.Values.ToList());
        }

        private class VirtualFileData
        {
            private readonly Lazy<Stream> _fileData;

            public VirtualFileData()
            {
                _fileData = new Lazy<Stream>(() => new MemoryStream());
            }

            public Task<Stream> GetData()
            {
                return Task.FromResult(_fileData.Value);
            }

            public async Task Write(Stream data)
            {
                var file = await GetData();

                await data.CopyToAsync(file);
            }

            public async Task Delete()
            {
                var file = await GetData();

                file.Dispose();
            }
        }
    }
}