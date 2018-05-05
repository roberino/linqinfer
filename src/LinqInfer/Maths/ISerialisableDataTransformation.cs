using LinqInfer.Data;
using LinqInfer.Data.Serialisation;

namespace LinqInfer.Maths
{
    public interface ISerialisableDataTransformation : IVectorTransformation, IExportableAsDataDocument, IImportableFromDataDocument
    {
    }
}