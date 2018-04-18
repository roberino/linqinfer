namespace LinqInfer.Maths
{
    public interface IHasSerialisableTransformation
    {
        ISerialisableVectorTransformation VectorTransformation { get; }
    }
}