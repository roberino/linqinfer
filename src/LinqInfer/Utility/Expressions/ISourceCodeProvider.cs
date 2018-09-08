namespace LinqInfer.Utility.Expressions
{
    public interface ISourceCodeProvider
    {
        bool Exists(string name);

        string GetSourceCode(string name);
    }
}