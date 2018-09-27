namespace LinqInfer.Utility.Expressions
{
    public interface ISourceCodeProvider
    {
        bool Exists(string name);

        SourceCode GetSourceCode(string name);
    }
}