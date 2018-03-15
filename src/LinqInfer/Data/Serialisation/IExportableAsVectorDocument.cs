namespace LinqInfer.Data
{
    public interface IExportableAsVectorDocument
    {
        BinaryVectorDocument ToVectorDocument();
    }
}