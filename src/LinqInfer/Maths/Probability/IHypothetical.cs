namespace LinqInfer.Maths.Probability
{
    public interface IHypothetical
    {
        string Name { get; }
        Fraction PriorProbability { get; }
        Fraction PosteriorProbability { get; }
    }
}
