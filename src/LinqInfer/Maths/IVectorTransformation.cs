namespace LinqInfer.Maths
{
    public interface IVectorTransformation
    {
        int InputSize { get; }
        int OutputSize { get; }
        IVector Apply(IVector vector);
    }
}