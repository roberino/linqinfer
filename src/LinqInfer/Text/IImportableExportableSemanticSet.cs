using LinqInfer.Data;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Text
{
    public interface IImportableExportableSemanticSet : ISemanticSet, IXmlExportable, IXmlImportable
    {
    }
}