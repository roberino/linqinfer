namespace LinqInfer.Data
{
    public interface ICloneableObject<out T>
    {
        T Clone(bool deep);
    }
}