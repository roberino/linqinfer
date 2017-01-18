using System.IO;

namespace LinqInfer.Data
{
    /// <summary>
    /// Represents an object whose state can be stored and restored to and from a binary stream
    /// </summary>
    public interface IBinaryPersistable
    {
        /// <summary>
        /// Saves the state
        /// </summary>
        void Save(Stream output);

        /// <summary>
        /// Restores the state
        /// </summary>
        void Load(Stream input);
    }
}