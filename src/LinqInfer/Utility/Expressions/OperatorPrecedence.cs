using System.Collections.Generic;

namespace LinqInfer.Utility.Expressions
{
    internal static class OperatorPrecedence
    {
        private static readonly IDictionary<string, int> _precedence;

        static OperatorPrecedence()
        {
            _precedence = new Dictionary<string, int>();

            _precedence["*"] = 100;
            _precedence["/"] = 100;
            _precedence["+"] = 50;
            _precedence["-"] = 50;
        }

        public static bool TakesPrecedence(string operator1, string operator2)
        {
            return _precedence[operator1] > _precedence[operator2];
        }
    }
}
