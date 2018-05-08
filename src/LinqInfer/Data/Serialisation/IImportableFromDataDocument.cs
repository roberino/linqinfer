namespace LinqInfer.Data.Serialisation
{
    public interface IImportableFromDataDocument
    {
        void ImportData(PortableDataDocument doc);
    }
}