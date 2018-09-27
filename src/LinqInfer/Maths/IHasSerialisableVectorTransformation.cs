namespace LinqInfer.Maths
{
    public interface IHasSerialisableTransformation
    {
        ISerialisableDataTransformation DataTransformation { get; }
    }
}