namespace LinqInfer.Maths
{
    public interface IVectorTransformation
    {
        int InputSize { get; }
        int OutputSize { get; }
        Vector Apply(Vector vector);
    }
}