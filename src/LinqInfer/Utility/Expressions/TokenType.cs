using System;
using System.Linq.Expressions;

namespace LinqInfer.Utility.Expressions
{
    internal static class TokenTypeExtensions
    {
        public static string AsString(this ExpressionType expressionType)
        {
            switch (expressionType)
            {
                case ExpressionType.Add:
                    return "+";
                case ExpressionType.Subtract:
                    return "-";
                case ExpressionType.Multiply:
                    return "*";
                case ExpressionType.Divide:
                    return "/";
                case ExpressionType.AndAlso:
                    return "&&";
                case ExpressionType.OrElse:
                    return "||";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.Equal:
                    return "==";
                case ExpressionType.NotEqual:
                    return "!=";
                case ExpressionType.Negate:
                    return "!";
                case ExpressionType.Conditional:
                    return "?";
            }

            throw new NotSupportedException(expressionType.ToString());
        }

        public static bool IsBooleanOperator(this string token)
        {
            var et = token.AsExpressionType();

            return et == ExpressionType.And
                   || et == ExpressionType.AndAlso
                   || et == ExpressionType.Or
                   || et == ExpressionType.OrElse
                   || et == ExpressionType.Equal
                   || et == ExpressionType.NotEqual
                   || et == ExpressionType.Not
                   || et == ExpressionType.GreaterThan
                   || et == ExpressionType.GreaterThanOrEqual
                   || et == ExpressionType.LessThan
                   || et == ExpressionType.LessThanOrEqual;
        }

        public static ExpressionType AsExpressionType(this string token)
        {
            switch (token)
            {
                case "+":
                    return ExpressionType.Add;
                case "-":
                    return ExpressionType.Subtract;
                case "*":
                    return ExpressionType.Multiply;
                case "/":
                    return ExpressionType.Divide;
                case "&&":
                    return ExpressionType.AndAlso;
                case "||":
                    return ExpressionType.OrElse;
                case ">":
                    return ExpressionType.GreaterThan;
                case ">=":
                    return ExpressionType.GreaterThanOrEqual;
                case "<":
                    return ExpressionType.LessThan;
                case "<=":
                    return ExpressionType.LessThanOrEqual;
                case "==":
                    return ExpressionType.Equal;
                case "!=":
                    return ExpressionType.NotEqual;
                case "!":
                    return ExpressionType.Not;
                case "?":
                    return ExpressionType.Conditional;
            }

            throw new NotSupportedException(token);
        }

        public static int Capacity(this TokenType type)
        {
            switch (type)
            {
                case TokenType.Operator:
                    return 2;
                case TokenType.Name:
                case TokenType.Navigate:
                case TokenType.Negation:
                    return 1;
                case TokenType.Literal:
                    return 0;
                case TokenType.Condition:
                    return 3;
            }

            return int.MaxValue;
        }

        public static bool ShouldAccumulate(this TokenType tokenType)
        {
            return tokenType == TokenType.Literal
                   || tokenType == TokenType.Name
                   || tokenType == TokenType.Operator
                   || tokenType == TokenType.Space;
        }

        public static TokenType GetTokenType(this TokenType currentTokenContext, char c)
        {
            switch (c)
            {
                case '(': return TokenType.GroupOpen;
                case ')': return TokenType.GroupClose;
                case ' ': return TokenType.Space;
                case '+':
                case '-':
                case '/':
                case '*':
                case '=':
                case '!':
                case '|':
                case '&':
                case '<':
                case '>':
                    return TokenType.Operator;
                case '?':
                    return TokenType.Condition;
                case ',':
                    return TokenType.Separator;
                case ':':
                    return TokenType.Split;
                case '_':
                    return TokenType.Name;
                case '.':
                    if (currentTokenContext == TokenType.Literal) return TokenType.Literal;
                    if (currentTokenContext == TokenType.Name) return TokenType.Navigate;
                    return TokenType.Unknown;
                default:
                    if (char.IsDigit(c))
                    {
                        if (currentTokenContext == TokenType.Name) return TokenType.Name;
                        return TokenType.Literal;
                    }

                    if (char.IsLetter(c)) return TokenType.Name;
                    return TokenType.Unknown;

            }
        }
    }

    internal enum TokenType
    {
        Unknown,
        Root,
        Operator,
        Name,
        Navigate,
        Space,
        GroupClose,
        GroupOpen,
        Literal,
        Separator,
        Condition,
        Split,
        Negation
    }
}