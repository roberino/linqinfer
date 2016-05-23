using System;
using System.Linq.Expressions;
using System.Reflection;

namespace LinqInfer.Storage.SQLite.DataAccess
{
    internal class ExpressionParser
    {
        private readonly SqlTypeTranslator _translator;

        public ExpressionParser()
        {
            _translator = new SqlTypeTranslator();
        }

        /// <summary>
        /// Currently this only supports a single boolean expression.
        /// </summary>
        /// <remarks>
        /// To be replaced with a third party / fully implemented linq provider?
        /// </remarks>
        public string ParseWhereClause<T>(Expression<Func<T, bool>> expression)
        {
            var binOp = expression.Body as BinaryExpression;

            var member = binOp.Left as MemberExpression;

            if (member == null) throw new NotSupportedException("Expecting member expression on LHS");

            return string.Format("{0}{1}{2}", member.Member.Name, GetOp(binOp.NodeType), FormatValue(GetValue(binOp.Right)));
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
        private object GetValue(MemberExpression exp)
        {
            var x = GetValue(exp.Expression);

            if (exp.Member is PropertyInfo) return ((PropertyInfo)exp.Member).GetValue(x);
            if (exp.Member is FieldInfo) return ((FieldInfo)exp.Member).GetValue(x);

            throw new NotSupportedException(exp.Member.GetType().Name);
        }

        private object GetValue(Expression exp)
        {
            switch (exp.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return GetValue(((MemberExpression)exp));
                case ExpressionType.Constant:
                    return GetValue(((ConstantExpression)exp).Value);
                default:
                    throw new NotSupportedException("Expecting constant expression on RHS");
            }
        }

        private object GetValue(object value)
        {
            if (value is Expression)
            {
                return GetValue((Expression)value);
            }
            else
            {
                return _translator.ConvertToSqlValue(value);
            }
        }

        private string FormatValue(object val)
        {
            if (val == null || val == DBNull.Value) return "NULL";

            if (val is string || val is Uri || val is DateTime || val is DateTime?)
            {
                return Enquote(val);
            }

            return val.ToString();
        }

        private string Enquote(object val)
        {
            return "'" + val.ToString().Replace("'", "\\'") + "'";
        }
    }
}