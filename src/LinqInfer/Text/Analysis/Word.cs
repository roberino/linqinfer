using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqInfer.Text.Analysis
{
    internal class Word : IEquatable<Word>
    {
        private readonly HashSet<Relationship> _rels;

        public Word(int id, string text)
        {
            Id = id;
            Text = text;
            _rels = new HashSet<Relationship>();
        }

        public int Frequency { get; set; }

        public string Text { get; private set; }

        public int Id { get; private set; }

        public IEnumerable<Relationship> Relationships { get { return _rels; } }

        public float RelationshipScore(Word word)
        {
            var matching = _rels.Where(r => word.Equals(r.Target)).ToList();
            return matching.Any() ? matching.Sum(r => r.Weight) : 0f;
        }

        public void AddRelationships(IEnumerable<Relationship> relationships)
        {
            foreach (var rel in relationships)
            {
                if (rel.Target.Id == Id) continue;

                var existing = _rels.FirstOrDefault(r => r.Equals(rel));

                if (existing == null)
                {
                    _rels.Add(rel);
                }
                else
                {
                    existing.Weight += 1f;
                }
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Word);
        }

        public bool Equals(Word other)
        {
            if (other == null) return false;

            if (ReferenceEquals(this, other)) return true;

            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}