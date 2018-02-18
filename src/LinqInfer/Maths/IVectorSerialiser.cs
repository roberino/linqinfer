namespace LinqInfer.Maths
{
    public interface IVectorSerialiser
    {
        IVector Deserialize(string data, bool useBase64 = true);
        string Serialize(IVector vector, bool useBase64 = true);
    }
}