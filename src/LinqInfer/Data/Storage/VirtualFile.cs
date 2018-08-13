using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LinqInfer.Utility;

namespace LinqInfer.Data.Storage
{
    public sealed class VirtualFile : IVirtualFile
    {
        readonly bool _immediateWriteMode;
        Stream _dirtyData;
        Func<string, Task<Stream>> _getData;
        readonly Func<string, Stream, Task> _writeData;
        readonly Func<string, Stream> _getWriteStream;
        readonly Func<string, Task> _delete;

        /// <summary>
        /// Creates a new VirtualFile
        /// </summary>
        /// <param name="name">The file name</param>
        /// <param name="dataReader">A func which returns a readable stream</param>
        /// <param name="dataWriter">A func which writes a stream asyncronously to storage</param>
        /// <param name="deleteAction">A func which deletes asyncronously</param>
        /// <param name="writeStream">An optional func which returns a stream to write to</param>
        public VirtualFile(string name, Func<string, Task<Stream>> dataReader, Func<string, Stream, Task> dataWriter, Func<string, Task> deleteAction, Func<string, Stream> writeStream = null)
        {
            Contract.Assert(name != null);
            Contract.Assert(dataReader != null);
            Contract.Assert(dataWriter != null);
            Contract.Assert(deleteAction != null);

            _getData = dataReader;
            _writeData = dataWriter;
            _delete = deleteAction;
            _getWriteStream = writeStream;
            _immediateWriteMode = writeStream != null;

            Created = DateTime.UtcNow;
            Name = name;
            Attributes = new Dictionary<string, string>();
        }

        public static IVirtualFile FromFile(FileInfo file)
        {
            return new VirtualFile(file.Name, _ => Task.FromResult((Stream)file.OpenRead()), async (n, s) =>
            {
                if (!file.Directory.Exists) file.Directory.Create();

                using (var fs = file.OpenWrite())
                {
                    await s.CopyToAsync(fs);
                }
            }, _ =>
            {
                file.Delete();
                return Task.FromResult(true);
            }, _ =>
            {
                if (!file.Directory.Exists) file.Directory.Create();

                return file.OpenWrite();
            })
            {
                Exists = file.Exists,
                Created = !file.Exists ? DateTime.UtcNow : file.CreationTimeUtc,
                Modified = !file.Exists ? DateTime.UtcNow : file.LastWriteTimeUtc,
                Attributes = !file.Exists ? new Dictionary<string, string>() : file.Attributes.GetFlags().ToDictionary(f => f.ToString(), _ => "true")
            };
        }

        public string Name { get; private set; }
        public bool Exists { get; internal set; }
        public DateTime? Created { get; internal set; }
        public DateTime? Modified { get; internal set; }
        public IDictionary<string, string> Attributes { get; internal set; }

        public Task<Stream> ReadData()
        {
            return _getData(Name);
        }

        public Task WriteData(Stream input)
        {
            return _writeData(Name, input);
        }

        public Stream GetWriteStream()
        {
            if (_getWriteStream == null)
            {
                _dirtyData = new MemoryStream();
            }
            else
            {
                _dirtyData = _getWriteStream(Name);
            }

            return _dirtyData;
        }

        public async Task CommitWrites()
        {
            if (_dirtyData != null && !_immediateWriteMode)
            {
                _dirtyData.Position = 0;
                await WriteData(_dirtyData);
                _dirtyData.Dispose();
                _dirtyData = null;
            }
        }

        public void Load(Stream input)
        {
            _getData = _ => Task.FromResult(input);
        }

        public void Save(Stream output)
        {
            ReadData().Result.CopyTo(output);
        }

        public Task Delete()
        {
            return _delete(Name);
        }

        public void Dispose()
        {
            if (_dirtyData != null)
            {
                _dirtyData.Dispose();
                _dirtyData = null;
            }
        }
    }
}