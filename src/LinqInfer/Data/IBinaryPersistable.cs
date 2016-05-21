using System.IO;

namespace LinqInfer.Data
{
    public interface IBinaryPersistable
    {
        /// <summary>
        /// Saves the state.
        /// </summary>
        void Save(Stream output);

        /// <summary>
        /// Restores the state.
        /// </summary>
        void Load(Stream input);
    }
}