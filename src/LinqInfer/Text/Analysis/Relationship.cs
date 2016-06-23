namespace LinqInfer.Text.Analysis
{
    public struct Relationship
    {
        public int Target { get; set; }
        public RelationshipType Type { get; set; }
    }

    public enum RelationshipType
    {
        Semantic,
        Syntactic,
        Phonetic
    }
}
