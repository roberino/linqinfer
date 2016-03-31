namespace LinqInfer.Math
{
    public interface IHypothetical
    {
        string Name { get; }
        Fraction PriorProbability { get; }
        Fraction PosteriorProbability { get; }
    }
}
