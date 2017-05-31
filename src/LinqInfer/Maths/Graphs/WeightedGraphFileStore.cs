﻿using LinqInfer.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqInfer.Maths.Graphs
{
    public class WeightedGraphFileStore : IWeightedGraphStore<string, double>
    {
        private const string JsonFileExt = ".jnod";
        private readonly ReaderWriterLockSlim _lock;
        
        private readonly DirectoryInfo _storageDir;
        private readonly FileInfo _indexFile;
        private readonly IDictionary<string, string> _index;
        private readonly IObjectSerialiser _serialiser;
        private readonly string _dataFileExt;

        public WeightedGraphFileStore(string storagePath, 
            IObjectSerialiser serialiser, 
            string dataFileExt = JsonFileExt)
        {
            if (serialiser == null) throw new ArgumentNullException();

            _serialiser = serialiser;
            _dataFileExt = dataFileExt ?? JsonFileExt;
            _storageDir = new DirectoryInfo(storagePath);

            _lock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
            _indexFile = new FileInfo(Path.Combine(_storageDir.FullName, "index.json"));

            if (!_dataFileExt.StartsWith(".")) _dataFileExt = "." + _dataFileExt;

            if (_indexFile.Exists)
            {
                _index = ReadJsonFile<Dictionary<string, string>>(_indexFile).Result;
            }
            else
            {
                _index = new Dictionary<string, string>();
            }
        }

        public Task<bool> DeleteAllData()
        {
            _lock.EnterWriteLock();

            try
            {
                foreach (var file in _storageDir.GetFiles("*" + _dataFileExt))
                {
                    file.Delete();
                }

                _indexFile.Delete();

                _index.Clear();

                return Task.FromResult(true);
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public async Task<bool> CreateOrUpdateVertex(string label, IDictionary<string, double> edges = null)
        {
            _lock.EnterWriteLock();

            try
            {
                var data = await GetNodeData(label, true);

                data.Edges = edges ?? new Dictionary<string, double>();
                data.Modified = DateTime.UtcNow;

                var fileInfo = GetNodeFile(data.Id);

                await WriteJsonFile(fileInfo, data);
                await WriteJsonFile(_indexFile, _index);

                return true;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public Task<IQueryable<string>> FindVertices(Expression<Func<string, bool>> predicate)
        {
            return Task<IQueryable<string>>.Factory.StartNew(() =>
            {
                var exp = predicate.Compile();

                _lock.TryEnterReadLock(500);

                try
                {
                    return _index.Keys.Where(exp).ToList().AsQueryable();
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            });
        }

        public Task<long> GetVerticeCount()
        {
            return Task<long>.Factory.StartNew(() => _storageDir.GetFiles("*" + _dataFileExt).LongCount());
        }

        public async Task<IDictionary<string, double>> ResolveVertexEdges(string label)
        {
            _lock.TryEnterReadLock(500);

            try
            {
                var data = await GetNodeData(label, false);

                return data.Edges;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public Task<bool> VertexExists(string label)
        {
            return Task<bool>.Factory.StartNew(() =>
            {
                _lock.TryEnterReadLock(500);

                try
                {
                    return _index.ContainsKey(label);
                }
                finally
                {
                    _lock.ExitReadLock();
                }
            });
        }

        private FileInfo GetNodeFile(string fileId)
        {
            return new FileInfo(Path.Combine(_storageDir.FullName, fileId + JsonFileExt));
        }

        private async Task<GraphNodeData> GetNodeData(string label, bool createIfMissing)
        {
            string fileId;

            if (!_index.TryGetValue(label, out fileId))
            {
                if (!createIfMissing) throw new FileNotFoundException();

                _index[label] = fileId = GenerateId(label);
            }

            var fileInfo = GetNodeFile(fileId);

            GraphNodeData data;

            if (fileInfo.Exists)
            {
                data = await ReadJsonFile<GraphNodeData>(fileInfo);
            }
            else
            {
                data = new GraphNodeData();
            }

            data.Id = fileId;
            data.Label = label;

            return data;
        }

        private string GenerateId(string seed)
        {
            int n;
            string key;

            do
            {
                n = Functions.Random(_index.Count * 100);
                key = Convert.ToBase64String(Encoding.UTF8.GetBytes(seed.Substring(0, Math.Min(seed.Length, 3)))).Replace("=", "").ToLower() + n;
            }
            while (_index.Values.Contains(key));

            return key;
        }

        protected virtual async Task<T> ReadJsonFile<T>(FileInfo file)
        {
            using (var fs = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return await _serialiser.Deserialise<T>(fs, Encoding.UTF8, _serialiser.SupportedMimeTypes.First());
            }
        }

        protected virtual async Task WriteJsonFile<T>(FileInfo file, T data)
        {
            if (!file.Directory.Exists)
            {
                file.Directory.Create();
            }

            using (var fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await _serialiser.Serialise(data, Encoding.UTF8, _serialiser.SupportedMimeTypes.First(), fs);
            }
        }

        private class GraphNodeData
        {
            public GraphNodeData()
            {
                Created = DateTime.UtcNow;
            }

            public string Id { get; set; }
            public DateTime Created { get; set; }
            public DateTime Modified { get; set; }
            public string Label { get; set; }

            public IDictionary<string, double> Edges { get; set; }
        }
    }
}