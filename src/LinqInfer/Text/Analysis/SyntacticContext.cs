using System.Linq;
using System.Text;

namespace LinqInfer.Text.Analysis
{
    public sealed class SyntacticContext
    {
        public IToken[] ContextualWords { get; internal set; }

        public IToken TargetWord { get; internal set; }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append(TargetWord.Text);
            sb.Append(" =>");

            return ContextualWords
                .Aggregate(
                    sb, 
                    (s, t) => s.Append(" ").Append(t.Text)
                ).ToString();
        }
    }
}
