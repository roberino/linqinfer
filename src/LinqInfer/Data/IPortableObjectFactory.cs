namespace LinqInfer.Data
{
    public interface IPortableObjectFactory
    {
        IExportableAsVectorDocument Create(string typeInfo);
    }
}