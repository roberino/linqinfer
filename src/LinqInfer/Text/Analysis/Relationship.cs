using System;

namespace LinqInfer.Text.Analysis
{
    public class Relationship : IEquatable<Relationship>
    {
        public int FormId { get; set;}
        public float Weight { get; set; }
        public float Score { get; set; }
        public Word Target { get; set; }
        public RelationshipType Type { get; set; }

        public bool Equals(Relationship other)
        {
            if (other.Target == null) return false;

            return (other.Target.Equals(Target) && other.Type == Type);
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            if (!(obj is Relationship)) return false;

            return Equals((Relationship)obj);
        }

        public override int GetHashCode()
        {
            if (Target == null) return -1;

            return new Tuple<int, RelationshipType>(Target.Id, Type).GetHashCode();
        }
    }

    public enum RelationshipType
    {
        Semantic,
        SyntacticSubstitute,
        SyntacticPreceedor,
        Phonetic
    }
}