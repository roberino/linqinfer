using System.IO;

namespace LinqInfer.Data.Serialisation
{
    /// <summary>
    /// Interface for objects which can export data in a JSON format
    /// </summary>
    public interface IJsonExportable
    {
        /// <summary>
        /// Writes the JSON data to a text writer
        /// </summary>
        /// <param name="output"></param>
        void WriteJson(TextWriter output);
    }
}
