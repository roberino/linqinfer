using System;

namespace LinqInfer.Text.Http
{
    public sealed class RelativeLink : IEquatable<RelativeLink>
    {
        public string Rel { get; set; }
        public Uri Url { get; set; }

        public bool Equals(RelativeLink other)
        {
            if (other == null) return false;

            if (ReferenceEquals(other, this)) return true;

            return string.Equals(Rel, other.Rel) && Url.Equals(other.Url);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RelativeLink);
        }

        public override int GetHashCode()
        {
            return (Rel + "::" + Url).GetHashCode();
        }

        public override string ToString()
        {
            return $"{Url} ({Rel})";
        }
    }
}
