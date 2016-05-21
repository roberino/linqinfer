using System;
using System.Linq.Expressions;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    public class ExpressionParser
    {
        private readonly SqlTypeTranslator _translator;

        public ExpressionParser()
        {
            _translator = new SqlTypeTranslator();
        }

        public string ParseWhereClause<T>(Expression<Func<T, bool>> expression)
        {
            var binOp = expression.Body as BinaryExpression;

            var member = binOp.Left as MemberExpression;

            if (member == null) throw new NotSupportedException("Expecting member expression on LHS");

            return string.Format("{0}{1}{2}", member.Member.Name, GetOp(binOp.NodeType), GetValue(binOp.Right));
        }

        private string GetOp(ExpressionType op)
        {
            switch (op)
            {
                case ExpressionType.Equal:
                    return "=";
                case ExpressionType.NotEqual:
                    return "<>";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                default:
                    throw new NotSupportedException(op.ToString());
            }
        }

        private string GetValue(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return FormatValue(_translator.ConvertToSqlValue(""));
                case ExpressionType.Constant:
                    return FormatValue(_translator.ConvertToSqlValue(((ConstantExpression)exp).Value));
                default:
                    throw new NotSupportedException("Expecting constant expression on RHS");
            }
            var cons = exp as ConstantExpression;

            if (cons == null) throw new NotSupportedException("Expecting constant expression on RHS");

            return FormatValue(_translator.ConvertToSqlValue(cons.Value));
        }

        private string FormatValue(object val)
        {
            if (val == null || val == DBNull.Value) return "NULL";

            if(val is string)
            {
                return "'" + ((string)val).Replace("'", "\\'") + "'";
            }

            return val.ToString();
        }
    }
}
