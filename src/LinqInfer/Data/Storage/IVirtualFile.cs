using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Data.Storage
{
    public interface IVirtualFile : IBinaryPersistable, IDisposable
    {
        /// <summary>
        /// Gets the file name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Returns true if the file exists
        /// </summary>
        bool Exists { get; }

        /// <summary>
        /// Gets a set of attribute name values
        /// </summary>
        IDictionary<string, string> Attributes { get; }

        /// <summary>
        /// Gets the created time (if available)
        /// </summary>
        DateTime? Created { get; }

        /// <summary>
        /// Gets the last write / modified time (if available)
        /// </summary>
        DateTime? Modified { get; }

        /// <summary>
        /// Reads data asyncronously from storage
        /// </summary>
        Task<Stream> ReadData();

        /// <summary>
        /// Writes data asyncronously to storage
        /// </summary>
        Task WriteData(Stream input);

        /// <summary>
        /// Gets a stream for writing (alternative pattern to WriteData). Must be followed by commit to write to storage.
        /// </summary>
        Stream GetWriteStream();

        /// <summary>
        /// Commits dirty data to the storage and closes the current write stream
        /// </summary>
        Task CommitWrites();

        /// <summary>
        /// Deletes the file
        /// </summary>
        Task Delete();
    }
}