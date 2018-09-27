using System.Collections.Generic;
using System.Threading.Tasks;

namespace LinqInfer.Data.Storage
{
    /// <summary>
    /// Simple storage interface which can be implemented by both physical and cloud based storage systems
    /// </summary>
    public interface IVirtualFileStore
    {
        /// <summary>
        /// Deletes the storage container
        /// </summary>
        Task<bool> Delete();

        /// <summary>
        /// Returns a new virtual file store for the given name
        /// </summary>
        IVirtualFileStore GetContainer(string name);

        /// <summary>
        /// Lists files in the container / store
        /// </summary>
        Task<List<IVirtualFile>> ListFiles();

        /// <summary>
        /// Reads a file from storage
        /// </summary>
        Task<IVirtualFile> GetFile(string name);
    }
}