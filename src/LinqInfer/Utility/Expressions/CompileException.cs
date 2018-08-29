using System;

namespace LinqInfer.Utility.Expressions
{
    public sealed class CompileException : Exception
    {
        public CompileException(string token, int position, CompileErrorReason reason) : base($"Compile error at char {position} - {reason}")
        {
            Token = token;
            Position = position;
            Reason = reason;
        }

        public string Token { get; }
        public int Position { get; }
        public CompileErrorReason Reason { get; }
    }

    public enum CompileErrorReason
    {
        UnknownToken,
        EndOfStream,
        TooManyArgs,
        NotEnoughArgs,
        InvalidArgs,
        UnknownFunction
    }
}