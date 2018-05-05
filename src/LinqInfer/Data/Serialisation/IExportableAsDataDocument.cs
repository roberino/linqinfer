namespace LinqInfer.Data.Serialisation
{
    public interface IExportableAsDataDocument
    {
        PortableDataDocument ToDataDocument();
    }
}