namespace LinqInfer.Utility.Expressions
{
    public sealed class SourceCode
    {
        public SourceCode(string name, string mimeType, string code, bool found = true)
        {
            Name = name;
            MimeType = mimeType;
            Code = code;
            Found = found;
        }

        public static SourceCode NotFound(string name)
        {
            return new SourceCode(name, null, null, false);
        }

        public static SourceCode Default(string code, string mimeType = KnownMimeTypes.Function)
        {
            return new SourceCode("main", mimeType, code);
        }

        public string Name { get; }
        public string MimeType { get; }
        public string Code { get; }
        public bool Found { get; }
    }
}