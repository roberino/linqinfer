using System.Collections.Generic;

namespace LinqInfer.Utility.Expressions
{
    static class OperatorPrecedence
    {
        static readonly IDictionary<string, int> _precedence;

        static OperatorPrecedence()
        {
            _precedence = new Dictionary<string, int>
            {
                ["*"] = 100,
                ["/"] = 100,
                ["+"] = 50,
                ["-"] = 50,
                [">"] = 30,
                [">="] = 30,
                ["<"] = 30,
                ["<="] = 30,
                ["=="] = 20,
                ["!="] = 20,
                ["&&"] = 10,
                ["||"] = 10,
                ["=>"] = 0
            };
        }

        public static bool TakesPrecedence(string operator1, string operator2)
        {
            return _precedence[operator1] > _precedence[operator2];
        }
    }
}
