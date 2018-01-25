namespace LinqInfer.Text.Analysis
{
    public sealed class BiGram
    {
        public BiGram(string input = null)
        {
            Input = input;
        }

        public BiGram(string input, string output) : this(input)
        {
            Output = output;
        }

        public string Input { get; internal set; }
        public string Output { get; internal set; }
    }
}