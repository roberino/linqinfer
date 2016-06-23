using System.Collections.Generic;

namespace LinqInfer.Text.Analysis
{
    public class WordMap : List<Relationship>
    {
        public WordMap(int id)
        {
            Id = id;
        }

        public int Id { get; private set; }
    }
}
