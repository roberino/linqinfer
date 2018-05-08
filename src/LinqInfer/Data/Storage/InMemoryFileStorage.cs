using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LinqInfer.Data.Storage
{
    public sealed class InMemoryFileStorage : IVirtualFileStore
    {
        private readonly IDictionary<string, InMemoryFileStorage> _childContainers;
        private readonly IDictionary<string, VirtualFile> _files;
        private readonly InMemoryFileStorage _parent;
        private readonly string _name;

        public InMemoryFileStorage() : this(null, null)
        {
        }

        private InMemoryFileStorage(InMemoryFileStorage parent = null, string name = null)
        {
            _parent = parent;
            _name = name;
            _files = new Dictionary<string, VirtualFile>();
            _childContainers = new Dictionary<string, InMemoryFileStorage>();
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
            if (!_childContainers.TryGetValue(name, out InMemoryFileStorage childStore))
            {
                _childContainers[name] = childStore = new InMemoryFileStorage(this, name);
            }

            return childStore;
        }

        public Task<IVirtualFile> GetFile(string name)
        {
            if (!_files.TryGetValue(name, out VirtualFile file))
            {
                var data = new VirtualFileData();

                _files[name] = file = new VirtualFile(name,
                    _ => data.GetReadDataAsync(),
                    (_, d) => data.WriteDataAsync(d),
                    async _ =>
                    {
                        await data.DeleteAsync();

                        file.Exists = false;

                        if (_files.ContainsKey(name)) _files.Remove(name);
                    },
                    _ => data.GetWriteData())
                {
                };

                data.AfterWrite += (s, e) =>
                {
                    if (!file.Exists)
                    {
                        file.Created = DateTime.UtcNow;
                        file.Exists = true;
                    }

                    file.Modified = DateTime.UtcNow;
                };
            }

            return Task.FromResult((IVirtualFile)file);
        }

        public Task<List<IVirtualFile>> ListFiles()
        {
            return Task.FromResult(_files.Values.Cast<IVirtualFile>().ToList());
        }

        private class VirtualFileData
        {
            private readonly Lazy<Stream> _fileData;

            public VirtualFileData()
            {
                _fileData = new Lazy<Stream>(() => new MemoryStream());
            }

            public event EventHandler AfterWrite;

            public async Task<Stream> GetReadDataAsync()
            {
                var data = new MemoryStream();

                var pos = _fileData.Value.Position;

                _fileData.Value.Position = 0;

                await _fileData.Value.CopyToAsync(data);

                _fileData.Value.Position = pos;

                data.Position = 0;

                return data;
            }

            public Stream GetWriteData()
            {
                var writeStream = new DelegateWriteStream(_fileData.Value);

                writeStream.Disposed += (s, e) =>
                {
                    AfterWrite?.Invoke(this, e);
                };

                return writeStream;
            }

            public async Task WriteDataAsync(Stream data)
            {
                await data.CopyToAsync(_fileData.Value);

                AfterWrite?.Invoke(this, EventArgs.Empty);
            }

            public Task DeleteAsync()
            {
                if (_fileData.IsValueCreated) _fileData.Value.Dispose();

                return Task.FromResult(true);
            }
        }
    }
}