using System.Text;

namespace LinqInfer.Data.Sampling.Parsers
{
    public class ParserSettings
    {
        private static readonly ParserSettings _default = new ParserSettings()
        {
            Encoding = Encoding.UTF8,
            ColumnDelimiters = new[] { ',' },
            FirstRowIsHeader = true
        };

        internal static ParserSettings Default
        {
            get
            {
                return _default;
            }
        }

        public bool FirstRowIsHeader { get; set; }

        public Encoding Encoding { get; set; }

        public char[] ColumnDelimiters { get; set; }
    }
}
